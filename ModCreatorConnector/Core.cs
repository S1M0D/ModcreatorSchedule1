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

        public override void OnLateInitializeMelon()
        {
            Instance = this;

            // Check if preview is enabled via config file
            var previewEnabled = PreviewConfig.IsPreviewEnabled();

            if (previewEnabled)
            {
                // Initialize appearance preview system
                _avatarManager = new PreviewAvatarManager();
                _previewClient = new AppearancePreviewClient(_avatarManager);
                _previewClient.Start();

                MelonLogger.Msg("ModCreatorConnector: Appearance preview system initialized");
            }
            else
            {
                MelonLogger.Msg("ModCreatorConnector: Preview disabled, skipping appearance preview initialization");
            }
        }

        public override void OnSceneWasLoaded(int buildIndex, string sceneName)
        {
            if (sceneName == "Menu")
            {
                // Reset avatar manager when Menu scene loads
                _avatarManager?.Reset();
                MelonLogger.Msg("ModCreatorConnector: Menu scene loaded, ready for appearance preview");
            }
        }

        public override void OnUpdate()
        {
            // Process queued appearance updates on the main thread (only if preview is enabled)
            _previewClient?.ProcessQueuedUpdates();
        }

        public override void OnApplicationQuit()
        {
            _previewClient?.Dispose();
            Instance = null;
        }
    }
}