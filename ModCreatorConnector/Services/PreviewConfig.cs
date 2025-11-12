using System;
using System.IO;
using MelonLoader;
using MelonLoader.Utils;
using Newtonsoft.Json;

namespace ModCreatorConnector.Services
{
    /// <summary>
    /// Manages reading the preview configuration file written by the mod creator tool.
    /// </summary>
    public static class PreviewConfig
    {
        private const string ConfigFileName = "ModCreatorConnector_Preview.json";

        /// <summary>
        /// Reads the preview enabled flag from the config file.
        /// Returns false if the file doesn't exist or cannot be read.
        /// </summary>
        public static bool IsPreviewEnabled()
        {
            try
            {
                var modsPath = MelonEnvironment.ModsDirectory;
                var configPath = Path.Combine(modsPath, ConfigFileName);

                if (!File.Exists(configPath))
                {
                    MelonLogger.Msg($"PreviewConfig: Config file not found at {configPath}, preview disabled");
                    return false;
                }

                var json = File.ReadAllText(configPath);
                var config = JsonConvert.DeserializeObject<PreviewConfigData>(json);

                if (config == null)
                {
                    MelonLogger.Warning("PreviewConfig: Failed to deserialize config file, preview disabled");
                    return false;
                }

                MelonLogger.Msg($"PreviewConfig: Preview enabled = {config.PreviewEnabled}");
                return config.PreviewEnabled;
            }
            catch (Exception ex)
            {
                MelonLogger.Warning($"PreviewConfig: Error reading config file: {ex.Message}, preview disabled");
                return false;
            }
        }

        private class PreviewConfigData
        {
            [JsonProperty("PreviewEnabled")]
            public bool PreviewEnabled { get; set; }
        }
    }
}

