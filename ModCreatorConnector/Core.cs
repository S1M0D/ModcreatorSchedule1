using System;
using MelonLoader;
using ModCreatorConnector.Services;
using ModCreatorConnector.Utils;
using S1API;
using UnityEngine.SceneManagement;

[assembly: MelonInfo(typeof(ModCreatorConnector.Core), Constants.MOD_NAME, Constants.MOD_VERSION, Constants.MOD_AUTHOR)]
[assembly: MelonGame(Constants.Game.GAME_STUDIO, Constants.Game.GAME_NAME)]

namespace ModCreatorConnector
{
    public class Core : MelonMod
    {
        public static Core? Instance { get; private set; }

        private PreviewAvatarManager? _avatarManager;
        private AppearancePreviewClient? _previewClient;
        private PositionRequestServer? _positionServer;

        public override void OnLateInitializeMelon()
        {
            Instance = this;

            // Always start position request server (independent of preview mode)
            _positionServer = new PositionRequestServer();
            _positionServer.Start();

            // Check if preview is enabled via config file
            var previewEnabled = PreviewConfig.IsPreviewEnabled();

            if (previewEnabled)
            {
                // Initialize appearance preview system components (but don't start client yet)
                _avatarManager = new PreviewAvatarManager();
                _previewClient = new AppearancePreviewClient(_avatarManager);

                MelonLogger.Msg("ModCreatorConnector: Appearance preview system components initialized (will start when Menu scene loads)");
            }
            else
            {
                MelonLogger.Msg("ModCreatorConnector: Preview disabled, skipping appearance preview initialization");
            }
        }

        public override void OnSceneWasInitialized(int buildIndex, string sceneName)
        {
            if (sceneName == "Menu")
            {
                // Reset avatar manager when Menu scene loads
                _avatarManager?.Reset();

                // Start preview client when Menu scene is initialized
                // Stop first if already running to ensure clean restart
                if (_previewClient != null)
                {
                    try
                    {
                        _previewClient.Stop(); // Stop if already running
                        _previewClient.Start();
                        MelonLogger.Msg("ModCreatorConnector: Appearance preview client started (Menu scene initialized)");
                    }
                    catch (Exception ex)
                    {
                        MelonLogger.Error($"ModCreatorConnector: Failed to start preview client: {ex.Message}");
                    }
                }
            }
        }

        public override void OnUpdate()
        {
            // Process queued appearance updates on the main thread (only if preview is enabled)
            if (_previewClient != null)
            {
                _previewClient.ProcessQueuedUpdates();
            }
        }

        public override void OnApplicationQuit()
        {
            // Dispose in reverse order of initialization
            _previewClient?.Dispose();
            _positionServer?.Dispose();
            Instance = null;
        }
    }
}
