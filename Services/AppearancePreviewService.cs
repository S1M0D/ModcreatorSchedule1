using System;
using System.IO;
using System.IO.Pipes;
using System.Threading;
using System.Threading.Tasks;
using Schedule1ModdingTool.Models;
using Schedule1ModdingTool.Utils;
using Timer = System.Threading.Timer;

namespace Schedule1ModdingTool.Services
{
    /// <summary>
    /// Service that sends NPC appearance updates to the Connector mod via named pipes for real-time preview.
    /// </summary>
    public class AppearancePreviewService : IDisposable
    {
        private const string PipeName = "Schedule1ModCreator_Preview";
        private const int DebounceDelayMs = 300;

        private NamedPipeServerStream? _pipeServer;
        private CancellationTokenSource? _cancellationTokenSource;
        private Task? _connectionTask;
        private NpcAppearanceSettings? _pendingAppearance;
        private DateTime _lastUpdateTime;
        private readonly object _updateLock = new object();
        private Timer? _debounceTimer;
        private bool _isDisposed;

        /// <summary>
        /// Gets whether the service is currently connected to a client (Connector mod).
        /// </summary>
        public bool IsConnected => _pipeServer?.IsConnected ?? false;

        /// <summary>
        /// Starts the named pipe server and begins listening for connections.
        /// </summary>
        public void Start()
        {
            if (_isDisposed)
                throw new ObjectDisposedException(nameof(AppearancePreviewService));

            if (_connectionTask != null && !_connectionTask.IsCompleted)
                return; // Already started

            _cancellationTokenSource = new CancellationTokenSource();
            _connectionTask = Task.Run(() => ConnectionLoop(_cancellationTokenSource.Token));
        }

        /// <summary>
        /// Stops the service and closes any active connections.
        /// </summary>
        public void Stop()
        {
            _cancellationTokenSource?.Cancel();
            _connectionTask?.Wait(TimeSpan.FromSeconds(2));
            _connectionTask = null;

            try
            {
                _pipeServer?.Disconnect();
            }
            catch { }

            try
            {
                _pipeServer?.Dispose();
            }
            catch { }

            _pipeServer = null;
        }

        /// <summary>
        /// Sends an appearance update to the connected Connector mod.
        /// Updates are debounced to avoid overwhelming the connection.
        /// </summary>
        public void SendAppearanceUpdate(NpcAppearanceSettings appearance)
        {
            if (_isDisposed || appearance == null)
            {
                System.Diagnostics.Debug.WriteLine("AppearancePreviewService: SendAppearanceUpdate called but service is disposed or appearance is null");
                return;
            }

            System.Diagnostics.Debug.WriteLine($"AppearancePreviewService: SendAppearanceUpdate called (IsConnected: {IsConnected})");

            lock (_updateLock)
            {
                _pendingAppearance = appearance;
                _lastUpdateTime = DateTime.UtcNow;

                // Reset debounce timer
                _debounceTimer?.Dispose();
                _debounceTimer = new Timer(_ =>
                {
                    lock (_updateLock)
                    {
                        // Only send if no newer update has arrived
                        if (_pendingAppearance == appearance)
                        {
                            System.Diagnostics.Debug.WriteLine("AppearancePreviewService: Debounce timer fired, sending update");
                            SendUpdateInternal(_pendingAppearance);
                            _pendingAppearance = null;
                        }
                        _debounceTimer?.Dispose();
                        _debounceTimer = null;
                    }
                }, null, DebounceDelayMs, Timeout.Infinite);
            }
        }

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

                    // Wait for client connection (with cancellation support)
                    System.Diagnostics.Debug.WriteLine("AppearancePreviewService: Waiting for client connection...");
                    var connectTask = _pipeServer.WaitForConnectionAsync(cancellationToken);
                    connectTask.Wait(cancellationToken);

                    if (_pipeServer.IsConnected)
                    {
                        System.Diagnostics.Debug.WriteLine("AppearancePreviewService: Client connected! Ready to send updates.");
                        // Keep connection alive and send updates as they arrive
                        while (_pipeServer.IsConnected && !cancellationToken.IsCancellationRequested)
                        {
                            Thread.Sleep(100); // Small delay to avoid busy waiting
                        }
                        System.Diagnostics.Debug.WriteLine("AppearancePreviewService: Client disconnected");
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    // Log error and retry after delay
                    System.Diagnostics.Debug.WriteLine($"AppearancePreviewService connection error: {ex.Message}");
                    Thread.Sleep(1000);
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

        private void SendUpdateInternal(NpcAppearanceSettings appearance)
        {
            if (_pipeServer == null || !_pipeServer.IsConnected)
            {
                System.Diagnostics.Debug.WriteLine("AppearancePreviewService: Cannot send update - pipe not connected");
                return;
            }

            try
            {
                var json = AppearanceConverter.SerializeToJson(appearance);
                var bytes = System.Text.Encoding.UTF8.GetBytes(json);
                var lengthBytes = BitConverter.GetBytes(bytes.Length);

                System.Diagnostics.Debug.WriteLine($"AppearancePreviewService: Sending appearance update ({bytes.Length} bytes)");

                // Send length prefix, then data
                _pipeServer.Write(lengthBytes, 0, lengthBytes.Length);
                _pipeServer.Write(bytes, 0, bytes.Length);
                _pipeServer.Flush();
                
                System.Diagnostics.Debug.WriteLine("AppearancePreviewService: Appearance update sent successfully");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"AppearancePreviewService: Failed to send appearance update: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"AppearancePreviewService: Stack trace: {ex.StackTrace}");
                // Connection may be broken, will reconnect on next update
            }
        }

        public void Dispose()
        {
            if (_isDisposed)
                return;

            _isDisposed = true;
            Stop();
            _debounceTimer?.Dispose();
            _cancellationTokenSource?.Dispose();
        }
    }
}

