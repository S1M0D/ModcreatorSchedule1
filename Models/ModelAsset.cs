using Newtonsoft.Json;

namespace Schedule1ModdingTool.Models
{
    /// <summary>A Unity AssetBundle in the project model library.</summary>
    public sealed class ModelAsset : ResourceAsset
    {
        private string _prefabName = string.Empty;

        [JsonProperty("prefabName")]
        public string PrefabName
        {
            get => _prefabName;
            set => SetProperty(ref _prefabName, value ?? string.Empty);
        }
    }
}
