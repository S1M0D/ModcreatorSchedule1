using Newtonsoft.Json;

namespace Schedule1ModdingTool.Models
{
    /// <summary>A GLB model or Unity AssetBundle in the project model library.</summary>
    public sealed class ModelAsset : ResourceAsset
    {
        [JsonIgnore]
        public bool IsGlb => RelativePath.EndsWith(".glb", StringComparison.OrdinalIgnoreCase);

        [JsonIgnore]
        public string FormatLabel => IsGlb ? "GLB" : "Unity AssetBundle";

        private string _prefabName = string.Empty;

        [JsonProperty("prefabName")]
        public string PrefabName
        {
            get => _prefabName;
            set => SetProperty(ref _prefabName, value ?? string.Empty);
        }
    }
}
