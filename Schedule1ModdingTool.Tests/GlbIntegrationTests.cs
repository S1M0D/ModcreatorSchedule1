using System.IO;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Schedule1ModdingTool.Models;
using Schedule1ModdingTool.Services;
using Schedule1ModdingTool.Services.CodeGeneration.Item;
using Xunit;

namespace Schedule1ModdingTool.Tests;

public class GlbIntegrationTests
{
    [Fact]
    public void AcceptsSelfContainedIndexedTriangle()
    {
        using var stream = Triangle();
        GlbValidationService.Validate(stream);
    }

    [Theory]
    [InlineData("non-indexed", "indexed")]
    [InlineData("missing-material", "material")]
    [InlineData("empty-scene", "mesh instances")]
    [InlineData("cycle", "hierarchy")]
    [InlineData("multi-scene", "single scene")]
    [InlineData("external-buffer", "embedded")]
    [InlineData("animation", "static")]
    [InlineData("skin", "static")]
    [InlineData("draco", "extension")]
    [InlineData("sparse", "Sparse")]
    [InlineData("line", "triangle")]
    [InlineData("morph", "morph")]
    [InlineData("external-texture", "embedded")]
    [InlineData("bad-view", "outside")]
    [InlineData("bad-accessor", "outside")]
    [InlineData("quantized", "attribute")]
    public void RejectsUnsupportedOrMalformedGeometry(string mutation, string expectedMessage)
    {
        using var stream = Triangle(root =>
        {
            var primitive = (JObject)root["meshes"]![0]!["primitives"]![0]!;
            switch (mutation)
            {
                case "non-indexed": primitive.Remove("indices"); break;
                case "missing-material": primitive.Remove("material"); break;
                case "empty-scene": ((JObject)root["nodes"]![0]!).Remove("mesh"); break;
                case "cycle": root["nodes"]![0]!["children"] = new JArray(0); break;
                case "multi-scene": ((JArray)root["scenes"]!).Add(new JObject()); break;
                case "external-buffer": root["buffers"]![0]!["uri"] = "outside.bin"; break;
                case "animation": root["animations"] = new JArray(new JObject()); break;
                case "skin": root["skins"] = new JArray(new JObject()); break;
                case "draco": root["extensionsRequired"] = new JArray("KHR_draco_mesh_compression"); break;
                case "sparse": root["accessors"]![0]!["sparse"] = new JObject(); break;
                case "line": primitive["mode"] = 1; break;
                case "morph": primitive["targets"] = new JArray(); break;
                case "external-texture": root["images"] = new JArray(new JObject { ["uri"] = "texture.png" }); break;
                case "bad-view": root["bufferViews"]![0]!["byteLength"] = 999; break;
                case "bad-accessor": root["accessors"]![0]!["count"] = 999; break;
                case "quantized": root["accessors"]![0]!["componentType"] = 5123; break;
            }
        });
        var error = Assert.Throws<InvalidDataException>(() => GlbValidationService.Validate(stream));
        Assert.Contains(expectedMessage, error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void RejectsIndexOutsideVertexBuffer()
    {
        using var stream = Triangle();
        var bytes = stream.ToArray();
        bytes[^8] = 100;
        using var invalid = new MemoryStream(bytes);
        Assert.Throws<InvalidDataException>(() => GlbValidationService.Validate(invalid));
    }

    [Fact]
    public void RejectsTruncatedContainer()
    {
        using var valid = Triangle();
        using var truncated = new MemoryStream(valid.ToArray()[..^4]);
        Assert.Throws<InvalidDataException>(() => GlbValidationService.Validate(truncated));
    }

    [Fact]
    public void LegacyBundleAndNewTransformRoundTrip()
    {
        var legacy = JsonConvert.DeserializeObject<ItemBlueprint>("{\"modelBundleResourcePath\":\"Models/old.bundle\",\"modelPrefabName\":\"Chair\"}")!;
        Assert.False(legacy.IsGlbModel);
        Assert.Equal(1f, legacy.ModelScale);
        Assert.Equal("Chair", legacy.ModelPrefabName);
        var item = CreateItem(ItemKindOption.Buildable);
        item.ModelScale = 0.25f;
        item.ModelRotationY = 90;
        item.ModelOffsetZ = -0.2f;
        var copy = JsonConvert.DeserializeObject<ItemBlueprint>(JsonConvert.SerializeObject(item))!;
        Assert.True(copy.IsGlbModel);
        Assert.Equal(item.ModelScale, copy.ModelScale);
        Assert.Equal(item.ModelRotationY, copy.ModelRotationY);
        Assert.Equal(item.ModelOffsetZ, copy.ModelOffsetZ);
    }

    [Theory]
    [InlineData(ItemKindOption.Buildable)]
    [InlineData(ItemKindOption.CustomDrug)]
    public void GeneratesMAPIForSupportedConsumers(ItemKindOption kind)
    {
        var item = CreateItem(kind);
        item.ModelScale = 0.25f;
        var generator = new ItemCodeGenerator();
        Assert.True(generator.Validate(item).IsValid, string.Join("; ", generator.Validate(item).Errors));
        var code = generator.GenerateCode(item);
        Assert.Contains("S1MAPI.Gltf.GltfImporter", code);
        Assert.Contains("LoadGlb(bytes.ToArray())", code);
        Assert.Contains("DontDestroyOnLoad(_modelPrefab)", code);
        Assert.Contains("Vector3.one * 0.25f", code);
        Assert.DoesNotContain("AssetLoader.EasyLoad", code);
    }

    [Fact]
    public void BundleGenerationKeepsExistingLoader()
    {
        var item = CreateItem(ItemKindOption.Buildable);
        item.ModelBundleResourcePath = "Models/chair.bundle";
        item.ModelPrefabName = "Chair";
        var code = new ItemCodeGenerator().GenerateCode(item);
        Assert.Contains("AssetLoader.EasyLoad<GameObject>", code);
        Assert.DoesNotContain("S1MAPI", code);
    }

    [Theory]
    [InlineData(ItemKindOption.Generic)]
    [InlineData(ItemKindOption.Clothing)]
    [InlineData(ItemKindOption.WeedDrug)]
    public void RejectsGLBForUnsupportedConsumers(ItemKindOption kind)
    {
        var validation = new ItemCodeGenerator().Validate(CreateItem(kind));
        Assert.False(validation.IsValid);
        Assert.Contains(validation.Errors, message => message.Contains("Furniture only"));
    }

    [Theory]
    [InlineData(float.NaN)]
    [InlineData(0f)]
    [InlineData(-1f)]
    [InlineData(float.PositiveInfinity)]
    public void RejectsInvalidScale(float scale)
    {
        var item = CreateItem(ItemKindOption.Buildable);
        item.ModelScale = scale;
        Assert.False(new ItemCodeGenerator().Validate(item).IsValid);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ExportChecksGLBAndEmitsRuntimeSpecificDependency(bool corrupt)
    {
        var directory = Path.Combine(Path.GetTempPath(), "ModCreatorGlbTests", Guid.NewGuid().ToString("N"));
        try
        {
            Directory.CreateDirectory(Path.Combine(directory, "Models"));
            var project = new QuestProject { ProjectName = "GlbFixture", FilePath = Path.Combine(directory, "fixture.s1proj") };
            project.Items.Add(CreateItem(ItemKindOption.Buildable));
            project.Models.Add(new ModelAsset { RelativePath = "Models/triangle.glb" });
            using var model = Triangle();
            File.WriteAllBytes(Path.Combine(directory, "Models", "triangle.glb"), corrupt ? [1, 2, 3] : model.ToArray());
            var result = new ModProjectGeneratorService().GenerateModProject(project, Path.Combine(directory, "Export"));
            Assert.Equal(!corrupt, result.Success);
            if (corrupt) Assert.Contains(result.Errors, message => message.Contains("GLB"));
            else
            {
                var csproj = File.ReadAllText(Path.Combine(directory, "Export", "GlbFixture.csproj"));
                Assert.Contains("S1MAPI_Il2cpp", csproj);
                Assert.Contains("S1MAPI_Mono", csproj);
                Assert.Contains("ValidateGlbDependency", csproj);
                Assert.Contains("EmbeddedResource Include=\"Models\\triangle.glb\"", csproj);
                Assert.True(File.Exists(Path.Combine(directory, "Export", "Models", "triangle.glb")));
            }
        }
        finally { if (Directory.Exists(directory)) Directory.Delete(directory, true); }
    }

    public static ItemBlueprint CreateItem(ItemKindOption kind) => new()
    {
        ItemType = kind, ItemId = "glbtest:triangle", ProductKindId = "glbtest:shapes",
        ClassName = "TriangleItem", Namespace = "GlbFixture.Items", RepresentationTemplateItemId = "meth",
        ModelBundleResourcePath = "Models/triangle.glb", ShopIntegrationMode = ShopIntegrationModeOption.None
    };

    public static MemoryStream Triangle(Action<JObject>? mutate = null)
    {
        var root = JObject.Parse("""
        {"asset":{"version":"2.0"},"scene":0,"scenes":[{"nodes":[0]}],"nodes":[{"mesh":0}],
         "buffers":[{"byteLength":44}],"bufferViews":[{"buffer":0,"byteOffset":0,"byteLength":36},{"buffer":0,"byteOffset":36,"byteLength":6}],
         "accessors":[{"bufferView":0,"componentType":5126,"count":3,"type":"VEC3","min":[0,0,0],"max":[1,1,0]},
                      {"bufferView":1,"componentType":5123,"count":3,"type":"SCALAR"}],
         "materials":[{"pbrMetallicRoughness":{"baseColorFactor":[1,0.5,0,1],"metallicFactor":0}}],
         "meshes":[{"primitives":[{"attributes":{"POSITION":0},"indices":1,"material":0}]}]}
        """);
        mutate?.Invoke(root);
        var json = Encoding.UTF8.GetBytes(root.ToString(Formatting.None));
        var jsonLength = (json.Length + 3) & ~3;
        var stream = new MemoryStream();
        using (var writer = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true))
        {
            writer.Write(0x46546C67u); writer.Write(2u); writer.Write(12 + 8 + jsonLength + 8 + 44);
            writer.Write(jsonLength); writer.Write(0x4E4F534Au); writer.Write(json);
            for (var i = json.Length; i < jsonLength; i++) writer.Write((byte)32);
            writer.Write(44); writer.Write(0x004E4942u);
            foreach (var value in new float[] { 0, 0, 0, 1, 0, 0, 0, 1, 0 }) writer.Write(value);
            writer.Write((ushort)0); writer.Write((ushort)1); writer.Write((ushort)2); writer.Write((ushort)0);
        }
        stream.Position = 0;
        return stream;
    }
}
