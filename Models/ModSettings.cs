using System.IO;
using Newtonsoft.Json;

namespace Schedule1ModdingTool.Models
{
    /// <summary>
    /// Stores user configuration for mod generation and build settings
    /// </summary>
    public class ModSettings : ObservableObject
    {
        private static readonly string SettingsPath = Path.Combine(
            System.Environment.GetFolderPath(System.Environment.SpecialFolder.ApplicationData),
            "Schedule1ModdingTool",
            "settings.json");

        private string _gameInstallPath = "";
        private string _defaultModNamespace = "Schedule1Mods";
        private string _defaultModAuthor = "Quest Creator";
        private string _defaultModVersion = "1.0.0";

        [JsonProperty("gameInstallPath")]
        public string GameInstallPath
        {
            get => _gameInstallPath;
            set => SetProperty(ref _gameInstallPath, value);
        }

        [JsonProperty("defaultModNamespace")]
        public string DefaultModNamespace
        {
            get => _defaultModNamespace;
            set => SetProperty(ref _defaultModNamespace, value);
        }

        [JsonProperty("defaultModAuthor")]
        public string DefaultModAuthor
        {
            get => _defaultModAuthor;
            set => SetProperty(ref _defaultModAuthor, value);
        }

        [JsonProperty("defaultModVersion")]
        public string DefaultModVersion
        {
            get => _defaultModVersion;
            set => SetProperty(ref _defaultModVersion, value);
        }

        public static ModSettings Load()
        {
            try
            {
                if (File.Exists(SettingsPath))
                {
                    var json = File.ReadAllText(SettingsPath);
                    return JsonConvert.DeserializeObject<ModSettings>(json) ?? new ModSettings();
                }
            }
            catch
            {
                // If loading fails, return defaults
            }

            return new ModSettings();
        }

        public void Save()
        {
            try
            {
                var directory = Path.GetDirectoryName(SettingsPath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                var json = JsonConvert.SerializeObject(this, Formatting.Indented);
                File.WriteAllText(SettingsPath, json);
            }
            catch
            {
                // Silently fail - settings are optional
            }
        }
    }
}

