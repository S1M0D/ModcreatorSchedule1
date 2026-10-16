using Newtonsoft.Json;

namespace Schedule1ModdingTool.Models;

/// <summary>The editable composition in the project's 3D model workspace.</summary>
public sealed class ModelSceneDocument
{
    [JsonProperty("objects")]
    public List<ModelSceneObject> Objects { get; set; } = new();
}

public sealed class ModelSceneObject
{
    [JsonProperty("id")]
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    [JsonProperty("name")]
    public string Name { get; set; } = "Object";

    [JsonProperty("kind")]
    public string Kind { get; set; } = "cube";

    [JsonProperty("modelId")]
    public string? ModelId { get; set; }

    [JsonProperty("color")]
    public string Color { get; set; } = "#d0d0d0";

    [JsonProperty("visible")]
    public bool Visible { get; set; } = true;

    [JsonProperty("position")]
    public float[] Position { get; set; } = new float[3];

    [JsonProperty("rotation")]
    public float[] Rotation { get; set; } = new float[3];

    [JsonProperty("scale")]
    public float[] Scale { get; set; } = new[] { 1f, 1f, 1f };
}
