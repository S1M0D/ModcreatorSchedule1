using System.IO;
using System.Text;
using Newtonsoft.Json.Linq;

namespace Schedule1ModdingTool.Services;

/// <summary>Checks the static, self-contained GLB subset supported by the generated MAPI loader.</summary>
public static class GlbValidationService
{
    public const int MaximumFileBytes = 64 * 1024 * 1024;

    public static void ValidateFile(string path)
    {
        using var stream = File.OpenRead(path);
        Validate(stream);
    }

    public static void Validate(Stream stream)
    {
        if (stream.Length < 28 || stream.Length > MaximumFileBytes)
            throw new InvalidDataException("GLB files must contain a model and be no larger than 64 MB.");
        using var reader = new BinaryReader(stream, Encoding.UTF8, leaveOpen: true);
        if (reader.ReadUInt32() != 0x46546C67 || reader.ReadUInt32() != 2 || reader.ReadUInt32() != stream.Length)
            throw new InvalidDataException("Expected a complete glTF 2.0 binary (.glb) file.");
        JObject? document = null;
        long binaryLength = 0;
        long binaryOffset = 0;
        while (stream.Position < stream.Length)
        {
            if (stream.Length - stream.Position < 8)
                throw new InvalidDataException("Incomplete GLB chunk header.");
            var length = reader.ReadUInt32();
            var type = reader.ReadUInt32();
            if (length % 4 != 0 || length > stream.Length - stream.Position)
                throw new InvalidDataException("Invalid GLB chunk length.");
            if (document == null && type != 0x4E4F534A)
                throw new InvalidDataException("The first GLB chunk must contain JSON.");
            if (type == 0x4E4F534A)
            {
                if (document != null) throw new InvalidDataException("Duplicate GLB JSON chunk.");
                document = JObject.Parse(Encoding.UTF8.GetString(reader.ReadBytes((int)length)));
            }
            else
            {
                if (type == 0x004E4942)
                {
                    if (binaryLength != 0) throw new InvalidDataException("Duplicate GLB binary chunk.");
                    binaryLength = length;
                    binaryOffset = stream.Position;
                }
                stream.Position += length;
            }
        }
        if (document == null || binaryLength == 0 || (string?)document["asset"]?["version"] != "2.0")
            throw new InvalidDataException("The GLB must contain glTF 2.0 JSON and embedded geometry.");
        ValidateDocument(document, binaryLength, binaryOffset, reader);
    }

    private static void ValidateDocument(JObject root, long binaryLength, long binaryOffset, BinaryReader reader)
    {
        var buffers = root["buffers"] as JArray;
        if (buffers?.Count != 1 || buffers[0]["uri"] != null ||
            (long?)buffers[0]["byteLength"] is not > 0 || (long)buffers[0]["byteLength"]! > binaryLength)
            throw new InvalidDataException("Export one GLB with all geometry and textures embedded; external buffers are unsupported.");
        foreach (var extension in (root["extensionsUsed"] as JArray ?? new JArray()).Concat(root["extensionsRequired"] as JArray ?? new JArray()))
            if ((string?)extension != "KHR_materials_emissive_strength")
                throw new InvalidDataException($"GLB extension '{extension}' is not supported by this import profile. Export without compression or optional material extensions.");
        if ((root["skins"] as JArray)?.Count > 0 || (root["animations"] as JArray)?.Count > 0)
            throw new InvalidDataException("This version supports static models. Export without skins or animation.");

        var views = root["bufferViews"] as JArray ?? new JArray();
        foreach (var view in views)
        {
            var offset = (long?)view["byteOffset"] ?? 0;
            var length = (long?)view["byteLength"] ?? 0;
            if ((int?)view["buffer"] != 0 || offset < 0 || length <= 0 || offset + length > (long)buffers[0]["byteLength"]!)
                throw new InvalidDataException("A GLB buffer view points outside its embedded data.");
        }
        var accessors = root["accessors"] as JArray ?? new JArray();
        foreach (var accessor in accessors)
        {
            if (accessor["sparse"] != null)
                throw new InvalidDataException("Sparse accessors are unsupported. Export fully populated mesh data.");
            var viewIndex = (int?)accessor["bufferView"] ?? -1;
            var count = (long?)accessor["count"] ?? 0;
            if (viewIndex < 0 || viewIndex >= views.Count || count <= 0 || count > 3_000_000)
                throw new InvalidDataException("Invalid or excessively large GLB accessor.");
            var componentSize = (int?)accessor["componentType"] switch { 5120 or 5121 => 1, 5122 or 5123 => 2, 5125 or 5126 => 4, _ => 0 };
            var components = (string?)accessor["type"] switch { "SCALAR" => 1, "VEC2" => 2, "VEC3" => 3, "VEC4" => 4, _ => 0 };
            var elementSize = componentSize * components;
            var offset = (long?)accessor["byteOffset"] ?? 0;
            var stride = (long?)views[viewIndex]["byteStride"] ?? elementSize;
            if (elementSize == 0 || offset < 0 || stride < elementSize || offset + (count - 1) * stride + elementSize > (long)views[viewIndex]["byteLength"]!)
                throw new InvalidDataException("A GLB accessor points outside its buffer view.");
        }
        foreach (var image in root["images"] as JArray ?? new JArray())
        {
            var view = (int?)image["bufferView"] ?? -1;
            if (image["uri"] != null || view < 0 || view >= views.Count || (string?)image["mimeType"] is not ("image/png" or "image/jpeg"))
                throw new InvalidDataException("Use PNG or JPEG textures embedded inside the GLB.");
        }
        var meshes = root["meshes"] as JArray;
        if (meshes == null || meshes.Count == 0) throw new InvalidDataException("The GLB contains no meshes.");
        ValidateHierarchy(root, meshes.Count);
        foreach (var mesh in meshes)
        {
            var primitives = mesh["primitives"] as JArray;
            if (primitives == null || primitives.Count == 0) throw new InvalidDataException("A GLB mesh contains no primitives.");
            string? attributeLayout = null;
            foreach (var primitive in primitives)
            {
                if (((int?)primitive["mode"] ?? 4) != 4 || primitive["targets"] != null)
                    throw new InvalidDataException("Use triangle meshes without morph targets.");
                var indices = (int?)primitive["indices"] ?? -1;
                if (indices < 0 || indices >= accessors.Count || (string?)accessors[indices]["type"] != "SCALAR" ||
                    (int?)accessors[indices]["componentType"] is not (5121 or 5123 or 5125) || (int)accessors[indices]["count"]! % 3 != 0)
                    throw new InvalidDataException("This MAPI import profile requires indexed triangles. Re-export with mesh indices enabled.");
                var attributes = primitive["attributes"] as JObject;
                if (attributes?["POSITION"] == null) throw new InvalidDataException("A mesh is missing vertex positions.");
                var materialIndex = (int?)primitive["material"] ?? -1;
                if (materialIndex < 0 || materialIndex >= ((root["materials"] as JArray)?.Count ?? 0))
                    throw new InvalidDataException("Assign an explicit material to every mesh primitive before exporting the GLB.");
                var layout = string.Join(",", attributes.Properties().Select(attribute => attribute.Name).OrderBy(name => name, StringComparer.Ordinal));
                if (attributeLayout != null && layout != attributeLayout)
                    throw new InvalidDataException("Mesh primitives must use the same attributes. Export normals and UVs consistently across material slots.");
                attributeLayout = layout;
                foreach (var attribute in attributes.Properties())
                {
                    var index = (int)attribute.Value;
                    var expected = attribute.Name switch { "POSITION" or "NORMAL" => "VEC3", "TEXCOORD_0" => "VEC2", "TANGENT" => "VEC4", _ => null };
                    if (expected == null || index < 0 || index >= accessors.Count ||
                        (int?)accessors[index]["componentType"] != 5126 || (string?)accessors[index]["type"] != expected)
                        throw new InvalidDataException($"Unsupported vertex attribute '{attribute.Name}'. Use float positions, normals, tangents and one UV set.");
                }
                var vertexCount = (int)accessors[(int)attributes["POSITION"]!]["count"]!;
                if (attributes.Properties().Any(attribute => (int)accessors[(int)attribute.Value]["count"]! != vertexCount))
                    throw new InvalidDataException("All attributes of a primitive must have the same vertex count.");
                var indexAccessor = accessors[indices];
                var indexView = views[(int)indexAccessor["bufferView"]!];
                var componentType = (int)indexAccessor["componentType"]!;
                var size = componentType == 5121 ? 1 : componentType == 5123 ? 2 : 4;
                var stride = (int?)indexView["byteStride"] ?? size;
                var start = binaryOffset + ((long?)indexView["byteOffset"] ?? 0) + ((long?)indexAccessor["byteOffset"] ?? 0);
                for (var i = 0; i < (int)indexAccessor["count"]!; i++)
                {
                    reader.BaseStream.Position = start + (long)i * stride;
                    uint vertex = componentType switch { 5121 => reader.ReadByte(), 5123 => reader.ReadUInt16(), _ => reader.ReadUInt32() };
                    if (vertex >= vertexCount) throw new InvalidDataException("A triangle references a vertex outside its mesh.");
                }
            }
        }
    }

    private static void ValidateHierarchy(JObject root, int meshCount)
    {
        var nodes = root["nodes"] as JArray;
        var scenes = root["scenes"] as JArray;
        if (nodes == null || nodes.Count == 0 || nodes.Count > 10000 || scenes?.Count != 1 ||
            ((int?)root["scene"] ?? 0) != 0)
            throw new InvalidDataException("Export a single scene with between 1 and 10000 model nodes.");
        var parents = new int[nodes.Count];
        var hasMesh = false;
        foreach (var node in nodes)
        {
            if (node["mesh"] != null)
            {
                var index = (int)node["mesh"]!;
                if (index < 0 || index >= meshCount) throw new InvalidDataException("A scene node references an invalid mesh.");
                hasMesh = true;
            }
            foreach (var child in node["children"] as JArray ?? new JArray())
            {
                var index = (int)child;
                if (index < 0 || index >= nodes.Count || ++parents[index] > 1)
                    throw new InvalidDataException("Model nodes must form a hierarchy with one parent per child.");
            }
        }
        if (!hasMesh) throw new InvalidDataException("The scene contains no mesh instances.");
        var roots = Enumerable.Range(0, nodes.Count).Where(index => parents[index] == 0).ToHashSet();
        var sceneRoots = scenes![0]["nodes"] as JArray ?? new JArray();
        if (!roots.SetEquals(sceneRoots.Select(node => (int)node)) || sceneRoots.Count != roots.Count)
            throw new InvalidDataException("Export all model nodes into one scene so the preview and game use the same hierarchy.");
        var pending = new Queue<int>(roots);
        var visited = 0;
        while (pending.TryDequeue(out var index))
        {
            visited++;
            foreach (var child in nodes[index]["children"] as JArray ?? new JArray()) pending.Enqueue((int)child);
        }
        if (visited != nodes.Count) throw new InvalidDataException("The model hierarchy contains a cycle.");
    }
}
