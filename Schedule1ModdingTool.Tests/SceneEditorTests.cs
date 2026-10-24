using System.IO;
using System.Text;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using Schedule1ModdingTool.Models;
using Schedule1ModdingTool.Services;
using Xunit;

namespace Schedule1ModdingTool.Tests;

public sealed class SceneEditorTests
{
    [Fact]
    public void OlderSceneObjectsGetDefaultMaterialValues()
    {
        var shape = JsonConvert.DeserializeObject<ModelSceneObject>("{\"kind\":\"cube\",\"color\":\"#ffffff\"}");
        Assert.NotNull(shape);
        Assert.Equal(0f, shape.Metalness);
        Assert.Equal(0.72f, shape.Roughness);
    }

    [Fact]
    public void ExportedMultiObjectScenePassesGlbValidation()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Fixtures", "scene-editor-materials.glb");
        GlbValidationService.ValidateFile(path);
        using var stream = File.OpenRead(path);
        using var reader = new BinaryReader(stream);
        stream.Position = 12;
        var jsonLength = reader.ReadUInt32();
        Assert.Equal(0x4E4F534Au, reader.ReadUInt32());
        var gltf = JObject.Parse(Encoding.UTF8.GetString(reader.ReadBytes((int)jsonLength)));
        Assert.Equal(3, ((JArray)gltf["meshes"]!).Count); // The fourth, hidden mesh is excluded.
        var materials = (JArray)gltf["materials"]!;
        Assert.Contains(materials, material => (float?)material["pbrMetallicRoughness"]?["metallicFactor"] == 0.6f);
    }

    [Fact]
    public void ProjectRoundTripKeepsEditableSceneObjects()
    {
        var file = Path.Combine(Path.GetTempPath(), "scene-editor-" + Guid.NewGuid().ToString("N") + ".s1proj");
        try
        {
            var project = new QuestProject();
            project.ModelScene = new ModelSceneDocument
            {
                Objects = new List<ModelSceneObject>
                {
                    new()
                    {
                        Name = "Orange Sphere",
                        Kind = "sphere",
                        Color = "#ef794e",
                        Metalness = 0.6f,
                        Roughness = 0.25f,
                        Visible = false,
                        Position = new[] { 1.5f, 0.25f, -0.5f },
                        Rotation = new[] { 0f, 60f, 0f },
                        Scale = new[] { 1f, 1f, 1f }
                    }
                }
            };
            project.SaveToFile(file);

            var loaded = QuestProject.LoadFromFile(file);
            var sphere = Assert.Single(Assert.IsType<QuestProject>(loaded).ModelScene.Objects);
            Assert.Equal("sphere", sphere.Kind);
            Assert.Equal("#ef794e", sphere.Color);
            Assert.Equal(0.6f, sphere.Metalness);
            Assert.Equal(0.25f, sphere.Roughness);
            Assert.False(sphere.Visible);
            Assert.Equal(1.5f, sphere.Position[0]);
            Assert.Equal(60f, sphere.Rotation[1]);
        }
        finally { if (File.Exists(file)) File.Delete(file); }
    }
}
