using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using MelonLoader;
using S1API.Avatar;
using S1API.UI;
using Avatar = S1API.Avatar.Avatar;

namespace ModCreatorConnector.Services
{
    /// <summary>
    /// Manages finding and managing the preview Avatar in the Main scene.
    /// </summary>
    public class PreviewAvatarManager
    {
        private Avatar? _previewAvatar;
        private bool _isInitialized;

        /// <summary>
        /// Gets the preview Avatar instance, or null if not available.
        /// </summary>
        public Avatar? PreviewAvatar => _previewAvatar;

        /// <summary>
        /// Gets whether the preview Avatar is available and ready.
        /// </summary>
        public bool IsAvailable => _previewAvatar != null && _previewAvatar.IsActive;

        /// <summary>
        /// Initializes the preview Avatar manager. Should be called after Main scene loads.
        /// </summary>
        public void Initialize()
        {
            if (_isInitialized)
                return;

            if (SceneManager.GetActiveScene().name != "Menu")
            {
                MelonLogger.Warning("PreviewAvatarManager: Not in Menu scene, cannot initialize");
                return;
            }

            TryFindPreviewAvatar();
            _isInitialized = true;
        }

        /// <summary>
        /// Attempts to find or create a preview Avatar.
        /// Strategy: Try MainMenuRig.Avatar first, then fallback to first Avatar in scene.
        /// </summary>
        private void TryFindPreviewAvatar()
        {
            // Strategy 1: Try to find MainMenuRig.Avatar
            var mainMenuRigs = MainMenuRig.FindInScene();
            if (mainMenuRigs != null && mainMenuRigs.Length > 0)
            {
                var mainMenuRig = mainMenuRigs.FirstOrDefault();
                if (mainMenuRig != null && mainMenuRig.Avatar != null)
                {
                    _previewAvatar = mainMenuRig.Avatar;
                    MelonLogger.Msg("PreviewAvatarManager: Found MainMenuRig.Avatar for preview");
                    return;
                }
            }

            // Strategy 2: Fallback to first Avatar in scene
            var avatars = Avatar.FindInScene();
            if (avatars != null && avatars.Length > 0)
            {
                _previewAvatar = avatars.FirstOrDefault();
                if (_previewAvatar != null)
                {
                    MelonLogger.Msg($"PreviewAvatarManager: Using first Avatar in scene: {_previewAvatar.GameObject?.name ?? "Unknown"}");
                    return;
                }
            }

            MelonLogger.Warning("PreviewAvatarManager: No Avatar found in Main scene for preview");
        }

        /// <summary>
        /// Resets the preview Avatar reference. Call this when scene changes.
        /// </summary>
        public void Reset()
        {
            _previewAvatar = null;
            _isInitialized = false;
        }
    }
}

