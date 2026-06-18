using System;
using System.IO;
using System.IO.Pipes;
using System.Threading;
using System.Threading.Tasks;
using MelonLoader;
using UnityEngine.SceneManagement;

namespace ModCreatorConnector.Services
{
    /// <summary>
    /// Client that connects to the mod creator's named pipe server to receive appearance updates.
    /// </summary>
    public class AppearancePreviewClient : IDisposable
    {
        private const string PipeName = "Schedule1ModCreator_Preview";
        private const int ReconnectDelayMs = 2000;

        private NamedPipeClientStream? _pipeClient;
        private CancellationTokenSource? _cancellationTokenSource;
        private Task? _connectionTask;
        private readonly PreviewAvatarManager _avatarManager;
        private readonly System.Collections.Concurrent.ConcurrentQueue<string> _updateQueue = new();
        private bool _isDisposed;

        public AppearancePreviewClient(PreviewAvatarManager avatarManager)
        {
            _avatarManager = avatarManager ?? throw new ArgumentNullException(nameof(avatarManager));
        }

        /// <summary>
        /// Starts the client and begins listening for appearance updates.
        /// </summary>
        public void Start()
        {
            if (_isDisposed)
                throw new ObjectDisposedException(nameof(AppearancePreviewClient));

            if (_connectionTask != null && !_connectionTask.IsCompleted)
                return; // Already started

            _cancellationTokenSource = new CancellationTokenSource();
            _connectionTask = Task.Run(() =>
            {
                try
                {
                    ConnectionLoop(_cancellationTokenSource.Token);
                }
                catch (OperationCanceledException)
                {
                    // Expected when cancelled, ignore
                }
                catch (Exception ex)
                {
                    MelonLogger.Error($"AppearancePreviewClient: ConnectionLoop exception: {ex.Message}");
                }
            });
        }

        /// <summary>
        /// Stops the client and closes any active connections.
        /// </summary>
        public void Stop()
        {
            _cancellationTokenSource?.Cancel();
            
            try
            {
                _connectionTask?.Wait(TimeSpan.FromSeconds(5));
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

            try
            {
                _pipeClient?.Dispose();
            }
            catch { }

            _pipeClient = null;
        }

        private void ConnectionLoop(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    // Check current scene
                    var currentScene = SceneManager.GetActiveScene();
                    var sceneName = currentScene.IsValid() ? currentScene.name : "Unknown";
                    
                    // Wait for Menu scene before connecting
                    if (sceneName != "Menu")
                    {
                        Thread.Sleep(1000);
                        continue;
                    }

                    // Initialize avatar manager if not already done
                    _avatarManager.Initialize();

                    if (!_avatarManager.IsAvailable)
                    {
                        MelonLogger.Warning("AppearancePreviewClient: Preview Avatar not available, retrying...");
                        Thread.Sleep(ReconnectDelayMs);
                        continue;
                    }

                    // Create pipe client (bidirectional for sending requests)
                    _pipeClient = new NamedPipeClientStream(
                        ".",
                        PipeName,
                        PipeDirection.InOut,
                        PipeOptions.Asynchronous);

                    // Try to connect
                    _pipeClient.Connect(5000); // 5 second timeout

                    if (_pipeClient.IsConnected)
                    {
                        MelonLogger.Msg("AppearancePreviewClient: Connected to mod creator, requesting current appearance...");
                        
                        // Start reading messages first (server may send appearance immediately, or will respond to REQUEST_APPEARANCE)
                        // We'll send REQUEST_APPEARANCE in a background task to avoid blocking the read loop
                        Task.Run(() =>
                        {
                            try
                            {
                                Thread.Sleep(200); // Small delay to let connection stabilize
                                SendRequestAppearance();
                            }
                            catch (Exception ex)
                            {
                                MelonLogger.Warning($"AppearancePreviewClient: Failed to send REQUEST_APPEARANCE: {ex.Message}");
                            }
                        });
                        while (_pipeClient.IsConnected && !cancellationToken.IsCancellationRequested)
                        {
                            try
                            {
                                var message = ReadMessage();
                                if (!string.IsNullOrEmpty(message))
                                {
                                    _updateQueue.Enqueue(message);
                                }
                                else if (_pipeClient.IsConnected)
                                {
                                    // No message but still connected, small delay to avoid busy waiting
                                    Thread.Sleep(100);
                                }
                            }
                            catch (Exception ex)
                            {
                                MelonLogger.Error($"AppearancePreviewClient: Error reading message: {ex.Message}");
                                MelonLogger.Error($"AppearancePreviewClient: Stack trace: {ex.StackTrace}");
                                break; // Break to reconnect
                            }
                        }
                        
                        MelonLogger.Warning("AppearancePreviewClient: Connection lost, will reconnect...");
                    }
                    else
                    {
                        MelonLogger.Warning("AppearancePreviewClient: Failed to connect (pipe not connected)");
                    }
                }
                catch (TimeoutException ex)
                {
                    MelonLogger.Warning($"AppearancePreviewClient: Connection timeout - mod creator may not be running: {ex.Message}");
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    MelonLogger.Error($"AppearancePreviewClient: Connection error: {ex.Message}");
                    MelonLogger.Error($"AppearancePreviewClient: Stack trace: {ex.StackTrace}");
                }
                finally
                {
                    try
                    {
                        _pipeClient?.Dispose();
                    }
                    catch { }
                    _pipeClient = null;
                }

                // Wait before reconnecting
                if (!cancellationToken.IsCancellationRequested)
                {
                    Thread.Sleep(ReconnectDelayMs);
                }
            }
        }

        private void SendRequestAppearance()
        {
            if (_pipeClient == null || !_pipeClient.IsConnected || !_pipeClient.CanWrite)
            {
                MelonLogger.Warning("AppearancePreviewClient: Cannot send request - pipe not connected or not writable");
                return;
            }

            try
            {
                var request = "REQUEST_APPEARANCE";
                var bytes = System.Text.Encoding.UTF8.GetBytes(request);
                var lengthBytes = BitConverter.GetBytes(bytes.Length);

                // Send length prefix, then data
                _pipeClient.Write(lengthBytes, 0, lengthBytes.Length);
                _pipeClient.Write(bytes, 0, bytes.Length);
                _pipeClient.Flush();
                
                MelonLogger.Msg("AppearancePreviewClient: Appearance request sent");
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"AppearancePreviewClient: Failed to send appearance request: {ex.Message}");
            }
        }

        private string? ReadMessage()
        {
            if (_pipeClient == null || !_pipeClient.IsConnected)
            {
                return null;
            }

            try
            {
                // Check if data is available (non-blocking check)
                if (!_pipeClient.IsConnected)
                {
                    return null;
                }

                // Read length prefix (4 bytes) - this will block until data is available or pipe closes
                var lengthBytes = new byte[4];
                int bytesRead;
                try
                {
                    bytesRead = _pipeClient.Read(lengthBytes, 0, 4);
                }
                catch (System.IO.IOException)
                {
                    return null;
                }
                if (bytesRead == 0)
                {
                    return null;
                }
                
                if (bytesRead != 4)
                {
                    return null;
                }

                var messageLength = BitConverter.ToInt32(lengthBytes, 0);
                if (messageLength <= 0 || messageLength > 1024 * 1024) // Max 1MB
                {
                    MelonLogger.Error($"AppearancePreviewClient: Invalid message length: {messageLength}");
                    return null;
                }

                // Read message data
                var messageBytes = new byte[messageLength];
                var totalRead = 0;
                while (totalRead < messageLength)
                {
                    if (!_pipeClient.IsConnected)
                    {
                        return null;
                    }
                    
                    int read;
                    try
                    {
                        read = _pipeClient.Read(messageBytes, totalRead, messageLength - totalRead);
                    }
                    catch (System.IO.IOException)
                    {
                        return null;
                    }
                    if (read == 0)
                    {
                        return null; // Connection closed
                    }
                    totalRead += read;
                }

                var message = System.Text.Encoding.UTF8.GetString(messageBytes);
                return message;
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"AppearancePreviewClient: Exception in ReadMessage: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Processes queued appearance updates on the main thread. Call this from OnUpdate.
        /// </summary>
        public void ProcessQueuedUpdates()
        {
            while (_updateQueue.TryDequeue(out var json))
            {
                ProcessAppearanceUpdate(json);
            }
        }

        private void ProcessAppearanceUpdate(string json)
        {
            try
            {
                if (!_avatarManager.IsAvailable)
                {
                    MelonLogger.Warning("AppearancePreviewClient: Avatar not available, skipping update");
                    return;
                }

                var avatar = _avatarManager.PreviewAvatar;
                if (avatar == null)
                {
                    MelonLogger.Warning("AppearancePreviewClient: Avatar is null");
                    return;
                }

                // Create fresh avatar settings (null baseSettings) to wipe any default appearance
                var avatarSettings = AppearanceConverter.ConvertJsonToAvatarSettings(json, null);
                avatar.LoadAvatarSettings(avatarSettings);
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"AppearancePreviewClient: Failed to process appearance update: {ex.Message}");
            }
        }

        public void Dispose()
        {
            if (_isDisposed)
                return;

            _isDisposed = true;
            Stop();
            
            // Ensure task completes before disposing cancellation token
            try
            {
                _connectionTask?.Wait(TimeSpan.FromSeconds(1));
            }
            catch { }
            
            _cancellationTokenSource?.Dispose();
            _connectionTask = null;
        }
    }
}
