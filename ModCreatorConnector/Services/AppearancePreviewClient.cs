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
            _connectionTask = Task.Run(() => ConnectionLoop(_cancellationTokenSource.Token));
        }

        /// <summary>
        /// Stops the client and closes any active connections.
        /// </summary>
        public void Stop()
        {
            _cancellationTokenSource?.Cancel();
            _connectionTask?.Wait(TimeSpan.FromSeconds(2));
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
            MelonLogger.Msg("AppearancePreviewClient: Connection loop started");
            
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    // Check current scene
                    var currentScene = SceneManager.GetActiveScene();
                    var sceneName = currentScene != null ? currentScene.name : "Unknown";
                    
                    MelonLogger.Msg($"AppearancePreviewClient: Current scene: {sceneName}");
                    
                    // Wait for Main scene before connecting
                    if (sceneName != "Menu")
                    {
                        MelonLogger.Msg($"AppearancePreviewClient: Waiting for Menu scene (currently: {sceneName})...");
                        Thread.Sleep(1000);
                        continue;
                    }

                    MelonLogger.Msg("AppearancePreviewClient: Menu scene detected, initializing avatar manager...");

                    // Initialize avatar manager if not already done
                    _avatarManager.Initialize();

                    if (!_avatarManager.IsAvailable)
                    {
                        MelonLogger.Warning("AppearancePreviewClient: Preview Avatar not available, retrying...");
                        Thread.Sleep(ReconnectDelayMs);
                        continue;
                    }

                    MelonLogger.Msg("AppearancePreviewClient: Avatar found, attempting to connect to mod creator...");

                    // Create pipe client
                    _pipeClient = new NamedPipeClientStream(
                        ".",
                        PipeName,
                        PipeDirection.In,
                        PipeOptions.Asynchronous);

                    // Try to connect
                    MelonLogger.Msg("AppearancePreviewClient: Connecting to named pipe...");
                    _pipeClient.Connect(5000); // 5 second timeout

                    if (_pipeClient.IsConnected)
                    {
                        MelonLogger.Msg("AppearancePreviewClient: Connected to mod creator! Listening for updates...");
                        
                        // Listen for messages
                        while (_pipeClient.IsConnected && !cancellationToken.IsCancellationRequested)
                        {
                            try
                            {
                                var message = ReadMessage();
                                if (!string.IsNullOrEmpty(message))
                                {
                                    MelonLogger.Msg($"AppearancePreviewClient: Received message ({message.Length} chars), queuing for main thread");
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
                    MelonLogger.Msg("AppearancePreviewClient: Connection loop cancelled");
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
                    MelonLogger.Msg($"AppearancePreviewClient: Waiting {ReconnectDelayMs}ms before reconnecting...");
                    Thread.Sleep(ReconnectDelayMs);
                }
            }
            
            MelonLogger.Msg("AppearancePreviewClient: Connection loop ended");
        }

        private string? ReadMessage()
        {
            if (_pipeClient == null || !_pipeClient.IsConnected)
            {
                MelonLogger.Warning("AppearancePreviewClient: ReadMessage called but pipe is not connected");
                return null;
            }

            try
            {
                // Check if data is available (non-blocking check)
                if (!_pipeClient.IsConnected)
                {
                    return null;
                }

                // Read length prefix (4 bytes)
                var lengthBytes = new byte[4];
                var bytesRead = _pipeClient.Read(lengthBytes, 0, 4);
                if (bytesRead == 0)
                {
                    MelonLogger.Warning("AppearancePreviewClient: Pipe closed while reading length");
                    return null;
                }
                
                if (bytesRead != 4)
                {
                    MelonLogger.Warning($"AppearancePreviewClient: Incomplete length read: {bytesRead}/4 bytes");
                    return null;
                }

                var messageLength = BitConverter.ToInt32(lengthBytes, 0);
                if (messageLength <= 0 || messageLength > 1024 * 1024) // Max 1MB
                {
                    MelonLogger.Error($"AppearancePreviewClient: Invalid message length: {messageLength}");
                    return null;
                }

                MelonLogger.Msg($"AppearancePreviewClient: Reading message of length {messageLength} bytes");

                // Read message data
                var messageBytes = new byte[messageLength];
                var totalRead = 0;
                while (totalRead < messageLength)
                {
                    if (!_pipeClient.IsConnected)
                    {
                        MelonLogger.Warning("AppearancePreviewClient: Pipe disconnected while reading message");
                        return null;
                    }
                    
                    var read = _pipeClient.Read(messageBytes, totalRead, messageLength - totalRead);
                    if (read == 0)
                    {
                        MelonLogger.Warning("AppearancePreviewClient: Pipe closed while reading message data");
                        return null; // Connection closed
                    }
                    totalRead += read;
                }

                var message = System.Text.Encoding.UTF8.GetString(messageBytes);
                MelonLogger.Msg($"AppearancePreviewClient: Successfully read message");
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
                
                // Log layer counts for debugging
                MelonLogger.Msg($"AppearancePreviewClient: Applying settings - FaceLayers: {avatarSettings.FaceLayerCount}, BodyLayers: {avatarSettings.BodyLayerCount}, Accessories: {avatarSettings.AccessoryCount}");
                
                avatar.LoadAvatarSettings(avatarSettings);
                MelonLogger.Msg("AppearancePreviewClient: Applied appearance update to preview Avatar");
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"AppearancePreviewClient: Failed to process appearance update: {ex.Message}");
                MelonLogger.Error($"AppearancePreviewClient: Stack trace: {ex.StackTrace}");
            }
        }

        public void Dispose()
        {
            if (_isDisposed)
                return;

            _isDisposed = true;
            Stop();
            _cancellationTokenSource?.Dispose();
        }
    }
}

