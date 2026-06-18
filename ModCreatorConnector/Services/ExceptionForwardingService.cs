using System;
using System.Collections.Generic;
using System.IO.Pipes;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MelonLoader;
using Newtonsoft.Json;

namespace ModCreatorConnector.Services
{
    /// <summary>
    /// Service that captures exceptions from generated mods and forwards them to the mod creator via named pipe.
    /// </summary>
    public class ExceptionForwardingService : IDisposable
    {
        private const string PipeName = "Schedule1ModCreator_Exceptions";
        private const int MaxQueueSize = 100;
        private const int ThrottleDelayMs = 100; // Throttle FirstChanceException spam

        private readonly HashSet<string> _generatedModAssemblies = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> _excludedAssemblies = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly System.Collections.Concurrent.ConcurrentQueue<ExceptionData> _exceptionQueue = new();
        private readonly SemaphoreSlim _queueSemaphore = new SemaphoreSlim(1, 1);
        private readonly object _lastExceptionLock = new object();
        private DateTime _lastExceptionTime = DateTime.MinValue;
        private string? _lastExceptionType = null;

        private NamedPipeServerStream? _pipeServer;
        private CancellationTokenSource? _cancellationTokenSource;
        private Task? _connectionTask;
        private Task? _forwardingTask;
        private bool _isDisposed;

        /// <summary>
        /// Represents exception data to be forwarded.
        /// </summary>
        private class ExceptionData
        {
            [JsonProperty("exceptionType")]
            public string ExceptionType { get; set; } = "";

            [JsonProperty("message")]
            public string Message { get; set; } = "";

            [JsonProperty("stackTrace")]
            public string? StackTrace { get; set; }

            [JsonProperty("sourceAssembly")]
            public string? SourceAssembly { get; set; }

            [JsonProperty("modName")]
            public string? ModName { get; set; }

            [JsonProperty("timestamp")]
            public DateTime Timestamp { get; set; }

            [JsonProperty("isUnhandled")]
            public bool IsUnhandled { get; set; }

            [JsonProperty("innerException")]
            public ExceptionData? InnerException { get; set; }
        }

        public ExceptionForwardingService()
        {
            // Initialize excluded assemblies (system, Unity, MelonLoader, etc.)
            _excludedAssemblies.Add("ModCreatorConnector");
            _excludedAssemblies.Add("MelonLoader");
            _excludedAssemblies.Add("0Harmony");
            _excludedAssemblies.Add("Il2CppInterop.Runtime");
            _excludedAssemblies.Add("S1API");
            _excludedAssemblies.Add("UnityEngine");
            _excludedAssemblies.Add("Unity");
            _excludedAssemblies.Add("System");
            _excludedAssemblies.Add("mscorlib");
            _excludedAssemblies.Add("netstandard");
            _excludedAssemblies.Add("Assembly-CSharp");
            _excludedAssemblies.Add("Assembly-CSharp-firstpass");
        }

        /// <summary>
        /// Starts the exception forwarding service.
        /// </summary>
        public void Start()
        {
            if (_isDisposed)
                throw new ObjectDisposedException(nameof(ExceptionForwardingService));

            if (_connectionTask != null && !_connectionTask.IsCompleted)
                return; // Already started

            RefreshGeneratedModAssemblies();

            // Subscribe to AppDomain exception events
            AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
            AppDomain.CurrentDomain.FirstChanceException += OnFirstChanceException;

            _cancellationTokenSource = new CancellationTokenSource();
            _connectionTask = Task.Run(() => ConnectionLoop(_cancellationTokenSource.Token));
            _forwardingTask = Task.Run(() => ForwardingLoop(_cancellationTokenSource.Token));

            MelonLogger.Msg("ExceptionForwardingService: Started exception forwarding");
        }

        /// <summary>
        /// Refreshes the list of generated mod assemblies by examining registered MelonMods.
        /// </summary>
        private void RefreshGeneratedModAssemblies()
        {
            _generatedModAssemblies.Clear();

            try
            {
                var registeredMelons = MelonMod.RegisteredMelons;
                if (registeredMelons == null)
                    return;

                foreach (var melon in registeredMelons)
                {
                    if (melon == null)
                        continue;

                    // Skip ModCreatorConnector itself
                    if (melon.Info?.Name == "ModCreatorConnector")
                        continue;

                    // Get assembly name from the MelonMod
                    var assembly = melon.GetType().Assembly;
                    var assemblyName = assembly.GetName().Name;
                    
                    if (!string.IsNullOrEmpty(assemblyName) && !_excludedAssemblies.Contains(assemblyName))
                    {
                        _generatedModAssemblies.Add(assemblyName);
                        MelonLogger.Msg($"ExceptionForwardingService: Tracking mod assembly: {assemblyName} (Mod: {melon.Info?.Name ?? "Unknown"})");
                    }
                }
            }
            catch (Exception ex)
            {
                MelonLogger.Warning($"ExceptionForwardingService: Error refreshing mod assemblies: {ex.Message}");
            }
        }

        /// <summary>
        /// Checks if an exception originates from a generated mod.
        /// </summary>
        private bool IsExceptionFromGeneratedMod(Exception exception)
        {
            if (exception == null)
                return false;

            try
            {
                // Check the exception's source assembly
                var sourceAssembly = exception.Source;
                if (!string.IsNullOrEmpty(sourceAssembly) && _generatedModAssemblies.Contains(sourceAssembly))
                {
                    return true;
                }

                // Check stack trace for generated mod assemblies
                var stackTrace = exception.StackTrace;
                if (!string.IsNullOrEmpty(stackTrace))
                {
                    foreach (var assemblyName in _generatedModAssemblies)
                    {
                        if (stackTrace.Contains(assemblyName, StringComparison.OrdinalIgnoreCase))
                        {
                            return true;
                        }
                    }
                }

                // Check the declaring type's assembly
                var targetSite = exception.TargetSite;
                if (targetSite != null)
                {
                    var declaringType = targetSite.DeclaringType;
                    if (declaringType != null)
                    {
                        var assembly = declaringType.Assembly;
                        var assemblyName = assembly.GetName().Name;
                        if (!string.IsNullOrEmpty(assemblyName) && _generatedModAssemblies.Contains(assemblyName))
                        {
                            return true;
                        }
                    }
                }

                // Check inner exception recursively
                if (exception.InnerException != null)
                {
                    return IsExceptionFromGeneratedMod(exception.InnerException);
                }
            }
            catch
            {
                // If we can't determine, err on the side of forwarding
                return true;
            }

            return false;
        }

        /// <summary>
        /// Gets the mod name associated with an exception.
        /// </summary>
        private string? GetModNameForException(Exception exception)
        {
            if (exception == null)
                return null;

            try
            {
                var sourceAssembly = exception.Source;
                if (!string.IsNullOrEmpty(sourceAssembly))
                {
                    var melon = MelonMod.RegisteredMelons?.FirstOrDefault(m => 
                        m != null && m.GetType().Assembly.GetName().Name?.Equals(sourceAssembly, StringComparison.OrdinalIgnoreCase) == true);
                    return melon?.Info?.Name;
                }

                var targetSite = exception.TargetSite;
                if (targetSite != null)
                {
                    var declaringType = targetSite.DeclaringType;
                    if (declaringType != null)
                    {
                        var assembly = declaringType.Assembly;
                        var assemblyName = assembly.GetName().Name;
                        if (!string.IsNullOrEmpty(assemblyName))
                        {
                            var melon = MelonMod.RegisteredMelons?.FirstOrDefault(m => 
                                m != null && m.GetType().Assembly.GetName().Name?.Equals(assemblyName, StringComparison.OrdinalIgnoreCase) == true);
                            return melon?.Info?.Name;
                        }
                    }
                }
            }
            catch
            {
                // Ignore errors in mod name detection
            }

            return null;
        }

        /// <summary>
        /// Handles unhandled exceptions.
        /// </summary>
        private void OnUnhandledException(object? sender, UnhandledExceptionEventArgs e)
        {
            if (e.ExceptionObject is Exception exception)
            {
                HandleException(exception, isUnhandled: true);
            }
        }

        /// <summary>
        /// Handles first-chance exceptions (throttled to avoid spam).
        /// </summary>
        private void OnFirstChanceException(object? sender, System.Runtime.ExceptionServices.FirstChanceExceptionEventArgs e)
        {
            if (e.Exception == null)
                return;

            // Throttle FirstChanceException to avoid spam
            lock (_lastExceptionLock)
            {
                var now = DateTime.UtcNow;
                var timeSinceLastException = (now - _lastExceptionTime).TotalMilliseconds;
                var exceptionType = e.Exception.GetType().FullName ?? "";

                // Skip if same exception type within throttle delay
                if (timeSinceLastException < ThrottleDelayMs && exceptionType == _lastExceptionType)
                {
                    return;
                }

                _lastExceptionTime = now;
                _lastExceptionType = exceptionType;
            }

            HandleException(e.Exception, isUnhandled: false);
        }

        /// <summary>
        /// Processes an exception and queues it for forwarding if it's from a generated mod.
        /// </summary>
        private void HandleException(Exception exception, bool isUnhandled)
        {
            if (exception == null)
                return;

            // Check if exception is from a generated mod
            if (!IsExceptionFromGeneratedMod(exception))
                return;

            try
            {
                var exceptionData = new ExceptionData
                {
                    ExceptionType = exception.GetType().FullName ?? exception.GetType().Name,
                    Message = exception.Message ?? "",
                    StackTrace = exception.StackTrace,
                    SourceAssembly = exception.Source,
                    ModName = GetModNameForException(exception),
                    Timestamp = DateTime.UtcNow,
                    IsUnhandled = isUnhandled
                };

                // Handle inner exception
                if (exception.InnerException != null)
                {
                    exceptionData.InnerException = new ExceptionData
                    {
                        ExceptionType = exception.InnerException.GetType().FullName ?? exception.InnerException.GetType().Name,
                        Message = exception.InnerException.Message ?? "",
                        StackTrace = exception.InnerException.StackTrace,
                        SourceAssembly = exception.InnerException.Source,
                        Timestamp = DateTime.UtcNow,
                        IsUnhandled = isUnhandled
                    };
                }

                // Queue exception (drop if queue is full to avoid memory issues)
                if (_exceptionQueue.Count < MaxQueueSize)
                {
                    _exceptionQueue.Enqueue(exceptionData);
                }
                else
                {
                    MelonLogger.Warning("ExceptionForwardingService: Exception queue full, dropping exception");
                }
            }
            catch (Exception ex)
            {
                // Don't forward exceptions from the forwarding mechanism itself
                MelonLogger.Error($"ExceptionForwardingService: Error handling exception: {ex.Message}");
            }
        }

        /// <summary>
        /// Connection loop that waits for the mod creator to connect.
        /// </summary>
        private void ConnectionLoop(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    // Create new pipe server
                    _pipeServer = new NamedPipeServerStream(
                        PipeName,
                        PipeDirection.Out,
                        1,
                        PipeTransmissionMode.Byte,
                        PipeOptions.Asynchronous);

                    // Wait for client connection
                    var connectTask = _pipeServer.WaitForConnectionAsync(cancellationToken);
                    connectTask.GetAwaiter().GetResult();

                    if (_pipeServer.IsConnected)
                    {
                        MelonLogger.Msg("ExceptionForwardingService: Mod creator connected");
                        
                        // Keep connection alive and handle disconnection
                        while (_pipeServer.IsConnected && !cancellationToken.IsCancellationRequested)
                        {
                            Thread.Sleep(1000);
                        }

                        MelonLogger.Msg("ExceptionForwardingService: Mod creator disconnected");
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    MelonLogger.Warning($"ExceptionForwardingService: Connection error: {ex.Message}");
                    Thread.Sleep(2000); // Wait before retrying
                }
                finally
                {
                    try
                    {
                        _pipeServer?.Dispose();
                    }
                    catch { }
                    _pipeServer = null;
                }
            }
        }

        /// <summary>
        /// Forwarding loop that sends queued exceptions to the mod creator.
        /// </summary>
        private void ForwardingLoop(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    // Wait for connection
                    while (_pipeServer == null || !_pipeServer.IsConnected)
                    {
                        if (cancellationToken.IsCancellationRequested)
                            return;
                        Thread.Sleep(500);
                    }

                    // Process queued exceptions
                    while (_pipeServer.IsConnected && !cancellationToken.IsCancellationRequested)
                    {
                        if (_exceptionQueue.TryDequeue(out var exceptionData))
                        {
                            _queueSemaphore.Wait(cancellationToken);
                            try
                            {
                                SendException(exceptionData);
                            }
                            finally
                            {
                                _queueSemaphore.Release();
                            }
                        }
                        else
                        {
                            Thread.Sleep(100); // No exceptions, wait a bit
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    MelonLogger.Warning($"ExceptionForwardingService: Forwarding error: {ex.Message}");
                    Thread.Sleep(1000);
                }
            }
        }

        /// <summary>
        /// Sends an exception to the mod creator via named pipe.
        /// </summary>
        private void SendException(ExceptionData exceptionData)
        {
            if (_pipeServer == null || !_pipeServer.IsConnected)
                return;

            try
            {
                var json = JsonConvert.SerializeObject(exceptionData);
                var bytes = Encoding.UTF8.GetBytes(json);
                var lengthBytes = BitConverter.GetBytes(bytes.Length);

                _pipeServer.Write(lengthBytes, 0, lengthBytes.Length);
                _pipeServer.Write(bytes, 0, bytes.Length);
                _pipeServer.Flush();
            }
            catch (Exception ex)
            {
                MelonLogger.Warning($"ExceptionForwardingService: Error sending exception: {ex.Message}");
            }
        }

        /// <summary>
        /// Stops the exception forwarding service.
        /// </summary>
        public void Stop()
        {
            // Unsubscribe from exception events
            AppDomain.CurrentDomain.UnhandledException -= OnUnhandledException;
            AppDomain.CurrentDomain.FirstChanceException -= OnFirstChanceException;

            _cancellationTokenSource?.Cancel();

            try
            {
                _connectionTask?.Wait(TimeSpan.FromSeconds(5));
                _forwardingTask?.Wait(TimeSpan.FromSeconds(5));
            }
            catch (AggregateException)
            {
                // Task may have been cancelled, ignore
            }
            catch (Exception)
            {
                // Ignore other exceptions during shutdown
            }

            _connectionTask = null;
            _forwardingTask = null;

            try
            {
                _pipeServer?.Dispose();
            }
            catch { }
            _pipeServer = null;
        }

        public void Dispose()
        {
            if (_isDisposed)
                return;

            _isDisposed = true;
            Stop();

            try
            {
                _connectionTask?.Wait(TimeSpan.FromSeconds(1));
                _forwardingTask?.Wait(TimeSpan.FromSeconds(1));
            }
            catch { }

            _cancellationTokenSource?.Dispose();
            _queueSemaphore?.Dispose();
            _connectionTask = null;
            _forwardingTask = null;
        }
    }
}

