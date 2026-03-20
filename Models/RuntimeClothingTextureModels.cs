using Newtonsoft.Json;

namespace Schedule1ModdingTool.Models
{
    /// <summary>
    /// Response from the connector when importing a clothing texture from the running game.
    /// </summary>
    public class RuntimeClothingTextureResponse
    {
        [JsonProperty("success")]
        public bool Success { get; set; }

        [JsonProperty("error")]
        public string? Error { get; set; }

        [JsonProperty("textureBytesBase64")]
        public string? TextureBytesBase64 { get; set; }

        [JsonProperty("pixelFormat")]
        public string PixelFormat { get; set; } = "rgba32";

        [JsonProperty("sourceAssetPath")]
        public string SourceAssetPath { get; set; } = string.Empty;

        [JsonProperty("resolvedTextureName")]
        public string ResolvedTextureName { get; set; } = string.Empty;

        [JsonProperty("resolvedShaderProperty")]
        public string ResolvedShaderProperty { get; set; } = string.Empty;

        [JsonProperty("width")]
        public int Width { get; set; }

        [JsonProperty("height")]
        public int Height { get; set; }
    }
}
