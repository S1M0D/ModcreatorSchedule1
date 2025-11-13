using System.IO.Pipes;
using Schedule1ModdingTool.Models;
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
        private NpcAppearanceSettings? _currentAppearance; // Store current appearance for initial request
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
        /// Sets the current appearance that will be sent when the client requests it on first connection.
        /// </summary>
        public void SetCurrentAppearance(NpcAppearanceSettings appearance)
        {
            System.Diagnostics.Debug.WriteLine($"[DEBUG] SetCurrentAppearance: Called (disposed={_isDisposed}, appearance={(appearance != null ? "not null" : "null")})");
            if (_isDisposed || appearance == null)
            {
                System.Diagnostics.Debug.WriteLine($"[DEBUG] SetCurrentAppearance: Early return - disposed={_isDisposed}, appearance null={appearance == null}");
                return;
            }

            lock (_updateLock)
            {
                _currentAppearance = appearance;
                System.Diagnostics.Debug.WriteLine($"[DEBUG] SetCurrentAppearance: Current appearance stored successfully (IsConnected={IsConnected})");
            }
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
                _currentAppearance = appearance; // Also update current appearance
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
                    // Create new pipe server (bidirectional for client requests)
                    _pipeServer = new NamedPipeServerStream(
                        PipeName,
                        PipeDirection.InOut,
                        1,
                        PipeTransmissionMode.Byte,
                        PipeOptions.Asynchronous);

                    // Wait for client connection (with cancellation support)
                    System.Diagnostics.Debug.WriteLine("[DEBUG] AppearancePreviewService: Waiting for client connection...");
                    var connectTask = _pipeServer.WaitForConnectionAsync(cancellationToken);
                    connectTask.Wait(cancellationToken);

                    if (_pipeServer.IsConnected)
                    {
                        System.Diagnostics.Debug.WriteLine("[DEBUG] AppearancePreviewService: Client connected! Ready to send updates and handle requests.");
                        System.Diagnostics.Debug.WriteLine($"[DEBUG] AppearancePreviewService: Pipe state - CanRead={_pipeServer.CanRead}, CanWrite={_pipeServer.CanWrite}, IsConnected={_pipeServer.IsConnected}");
                        
                        // Listen for client requests first (client will send REQUEST_APPEARANCE)
                        // This avoids race conditions with bidirectional pipe communication
                        System.Diagnostics.Debug.WriteLine("[DEBUG] AppearancePreviewService: Starting ListenForClientRequests task");
                        var listenTask = Task.Run(() => ListenForClientRequests(cancellationToken));
                        
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

        private void ListenForClientRequests(CancellationToken cancellationToken)
        {
            while (_pipeServer != null && _pipeServer.IsConnected && !cancellationToken.IsCancellationRequested)
            {
                try
                {
                    // Check if data is available (non-blocking)
                    if (!_pipeServer.IsConnected || !_pipeServer.CanRead)
                    {
                        Thread.Sleep(100);
                        continue;
                    }

                    // Try to read a request message
                    var lengthBytes = new byte[4];
                    int bytesRead;
                    
                    try
                    {
                        // Use Read with timeout - if no data available, continue
                        bytesRead = _pipeServer.Read(lengthBytes, 0, 4);
                    }
                    catch
                    {
                        // No data available or connection closed
                        Thread.Sleep(100);
                        continue;
                    }

                    if (bytesRead == 4)
                    {
                        var messageLength = BitConverter.ToInt32(lengthBytes, 0);
                        if (messageLength > 0 && messageLength <= 1024) // Max 1KB for request messages
                        {
                            var messageBytes = new byte[messageLength];
                            var totalRead = 0;
                            while (totalRead < messageLength && _pipeServer.IsConnected)
                            {
                                var read = _pipeServer.Read(messageBytes, totalRead, messageLength - totalRead);
                                if (read == 0) break;
                                totalRead += read;
                            }

                            if (totalRead == messageLength)
                            {
                                var request = System.Text.Encoding.UTF8.GetString(messageBytes);
                                System.Diagnostics.Debug.WriteLine($"[DEBUG] AppearancePreviewService: Received request: '{request}'");
                                if (request == "REQUEST_APPEARANCE")
                                {
                                    System.Diagnostics.Debug.WriteLine("[DEBUG] AppearancePreviewService: Client requested appearance, sending current appearance");
                                    lock (_updateLock)
                                    {
                                        System.Diagnostics.Debug.WriteLine($"[DEBUG] AppearancePreviewService: REQUEST_APPEARANCE handler - _currentAppearance is {(_currentAppearance != null ? "not null" : "null")}");
                                        if (_currentAppearance != null)
                                        {
                                            SendUpdateInternal(_currentAppearance);
                                        }
                                        else
                                        {
                                            System.Diagnostics.Debug.WriteLine("[DEBUG] AppearancePreviewService: WARNING - REQUEST_APPEARANCE received but _currentAppearance is null!");
                                        }
                                    }
                                }
                            }
                        }
                    }
                    else if (bytesRead == 0)
                    {
                        // No data available, wait a bit
                        Thread.Sleep(100);
                    }
                }
                catch (Exception ex)
                {
                    // Connection may be closed or error occurred
                    System.Diagnostics.Debug.WriteLine($"AppearancePreviewService: Error listening for requests: {ex.Message}");
                    break;
                }
            }
        }

        private void SendUpdateInternal(NpcAppearanceSettings appearance)
        {
            System.Diagnostics.Debug.WriteLine($"[DEBUG] SendUpdateInternal: Called (appearance={(appearance != null ? "not null" : "null")}, pipeServer={(_pipeServer != null ? "not null" : "null")}, IsConnected={_pipeServer?.IsConnected ?? false})");
            if (appearance == null)
            {
                System.Diagnostics.Debug.WriteLine("[DEBUG] SendUpdateInternal: Cannot send update - appearance is null");
                return;
            }

            if (_pipeServer == null || !_pipeServer.IsConnected)
            {
                System.Diagnostics.Debug.WriteLine("[DEBUG] SendUpdateInternal: Cannot send update - pipe not connected");
                return;
            }

            try
            {
                var json = AppearanceConverter.SerializeToJson(appearance);
                var bytes = System.Text.Encoding.UTF8.GetBytes(json);
                var lengthBytes = BitConverter.GetBytes(bytes.Length);

                System.Diagnostics.Debug.WriteLine($"[DEBUG] SendUpdateInternal: Sending appearance update ({bytes.Length} bytes, JSON length={json.Length})");

                // Send length prefix, then data
                lock (_updateLock)
                {
                    _pipeServer.Write(lengthBytes, 0, lengthBytes.Length);
                    _pipeServer.Write(bytes, 0, bytes.Length);
                    _pipeServer.Flush();
                }
                
                System.Diagnostics.Debug.WriteLine("[DEBUG] SendUpdateInternal: Appearance update sent successfully");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[DEBUG] SendUpdateInternal: Failed to send appearance update: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[DEBUG] SendUpdateInternal: Stack trace: {ex.StackTrace}");
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

