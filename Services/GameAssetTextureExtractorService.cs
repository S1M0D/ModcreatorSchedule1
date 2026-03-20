using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using AssetsTools.NET;
using AssetsTools.NET.Extra;
using AssetsTools.NET.Texture;

namespace Schedule1ModdingTool.Services
{
    /// <summary>
    /// Scans Schedule I data files for Texture2D assets and imports them into editor-ready PNGs.
    /// </summary>
    public sealed class GameAssetTextureExtractorService
    {
        private static readonly string[] ClothingKeywords =
        {
            "accessor", "apron", "bag", "band", "beanie", "belt", "blazer", "boot", "cap", "chain",
            "cloth", "coat", "combat", "dress", "eye", "face", "fabric", "freckle", "glasses",
            "glove", "goggle", "hair", "hat", "head", "hood", "hoodie", "jacket", "jean", "legging",
            "mask", "neck", "outfit", "pant", "scarf", "shirt", "shoe", "short", "skin", "skirt",
            "sock", "suit", "sweater", "tattoo", "tie", "top", "vest", "watch", "wear"
        };

        private string? _cachedDataDirectory;
        private IReadOnlyList<GameAssetTextureCatalogEntry> _cachedCatalog = Array.Empty<GameAssetTextureCatalogEntry>();

        public sealed class GameAssetTextureCatalogEntry
        {
            public string TextureName { get; init; } = string.Empty;
            public string AssetFileRelativePath { get; init; } = string.Empty;
            public long PathId { get; init; }
            public int Width { get; init; }
            public int Height { get; init; }
            public bool LooksLikeClothing { get; init; }

            public string DisplayLabel => $"{TextureName} ({Width}x{Height})";

            public string Summary => $"{AssetFileRelativePath} · Path ID {PathId}" +
                                     (LooksLikeClothing ? " · Clothing-like" : string.Empty);

            public string SearchKey =>
                $"{TextureName} {AssetFileRelativePath} {Width}x{Height}".ToLowerInvariant();
        }

        public sealed class ScanResult
        {
            public bool Success { get; init; }
            public string Message { get; init; } = string.Empty;
            public IReadOnlyList<GameAssetTextureCatalogEntry> Textures { get; init; } = Array.Empty<GameAssetTextureCatalogEntry>();
        }

        public sealed class ExtractResult
        {
            public bool Success { get; init; }
            public string Message { get; init; } = string.Empty;
            public byte[]? PngBytes { get; init; }
            public int Width { get; init; }
            public int Height { get; init; }
            public string TextureName { get; init; } = string.Empty;
        }

        public ScanResult ScanTextureCatalog(string? configuredGameInstallPath)
        {
            if (!TryResolveDataDirectory(configuredGameInstallPath, out var dataDirectory, out var error))
            {
                return new ScanResult
                {
                    Success = false,
                    Message = error
                };
            }

            if (string.Equals(_cachedDataDirectory, dataDirectory, StringComparison.OrdinalIgnoreCase) &&
                _cachedCatalog.Count > 0)
            {
                return new ScanResult
                {
                    Success = true,
                    Message = $"Loaded {_cachedCatalog.Count} game textures from cache.",
                    Textures = _cachedCatalog
                };
            }

            var tpkPath = ResolveTypeTreePackagePath();
            if (string.IsNullOrWhiteSpace(tpkPath))
            {
                return new ScanResult
                {
                    Success = false,
                    Message = "The bundled Unity type-tree package is missing. Rebuild the tool so Tools/AssetMetadata/unity_type_trees.tpk is copied beside the app."
                };
            }

            var textures = new List<GameAssetTextureCatalogEntry>();
            var failures = new List<string>();
            var manager = new AssetsManager();

            try
            {
                manager.LoadClassPackage(tpkPath);

                foreach (var assetFilePath in EnumerateAssetFiles(dataDirectory))
                {
                    try
                    {
                        textures.AddRange(ScanAssetFile(manager, dataDirectory, assetFilePath));
                    }
                    catch (Exception ex)
                    {
                        failures.Add($"{Path.GetFileName(assetFilePath)}: {ex.Message}");
                    }
                }
            }
            finally
            {
                manager.UnloadAllAssetsFiles(true);
                manager.UnloadAllBundleFiles();
            }

            var orderedTextures = textures
                .OrderByDescending(texture => texture.LooksLikeClothing)
                .ThenBy(texture => texture.TextureName, StringComparer.OrdinalIgnoreCase)
                .ThenBy(texture => texture.AssetFileRelativePath, StringComparer.OrdinalIgnoreCase)
                .ToArray();

            _cachedDataDirectory = dataDirectory;
            _cachedCatalog = orderedTextures;

            var message = failures.Count == 0
                ? $"Found {orderedTextures.Length} textures in Schedule I data files."
                : $"Found {orderedTextures.Length} textures. Skipped {failures.Count} file(s): {string.Join("; ", failures.Take(3))}";

            return new ScanResult
            {
                Success = orderedTextures.Length > 0,
                Message = orderedTextures.Length > 0 ? message : "No textures were found in the scanned game data files.",
                Textures = orderedTextures
            };
        }

        public ExtractResult ExtractTexturePng(string? configuredGameInstallPath, GameAssetTextureCatalogEntry entry)
        {
            ArgumentNullException.ThrowIfNull(entry);

            if (!TryResolveDataDirectory(configuredGameInstallPath, out var dataDirectory, out var error))
            {
                return new ExtractResult
                {
                    Success = false,
                    Message = error
                };
            }

            var tpkPath = ResolveTypeTreePackagePath();
            if (string.IsNullOrWhiteSpace(tpkPath))
            {
                return new ExtractResult
                {
                    Success = false,
                    Message = "The bundled Unity type-tree package is missing."
                };
            }

            var assetFilePath = Path.Combine(dataDirectory, entry.AssetFileRelativePath.Replace('/', Path.DirectorySeparatorChar));
            if (!File.Exists(assetFilePath))
            {
                return new ExtractResult
                {
                    Success = false,
                    Message = $"Could not find game asset file:\n{assetFilePath}"
                };
            }

            var manager = new AssetsManager();
            try
            {
                manager.LoadClassPackage(tpkPath);

                var inst = manager.LoadAssetsFile(assetFilePath, false);
                try
                {
                    manager.LoadClassDatabaseFromPackage(inst.file.Metadata.UnityVersion);

                    var assetInfo = inst.file
                        .GetAssetsOfType(AssetClassID.Texture2D)
                        .FirstOrDefault(info => info.PathId == entry.PathId);

                    if (assetInfo == null)
                    {
                        return new ExtractResult
                        {
                            Success = false,
                            Message = $"Could not find texture '{entry.TextureName}' in {entry.AssetFileRelativePath}."
                        };
                    }

                    var field = manager.GetBaseField(inst, assetInfo, AssetReadFlags.None);
                    var textureFile = TextureFile.ReadTextureFile(field);
                    var pictureData = textureFile.FillPictureData(inst);
                    var rawBytes = textureFile.DecodeTextureRaw(pictureData, useBgra: true);
                    var pngBytes = ConvertRawTextureToPng(rawBytes, textureFile.m_Width, textureFile.m_Height);

                    if (pngBytes == null)
                    {
                        return new ExtractResult
                        {
                            Success = false,
                            Message = $"Failed to decode '{entry.TextureName}' into an editor-ready PNG."
                        };
                    }

                    return new ExtractResult
                    {
                        Success = true,
                        Message = $"Extracted '{entry.TextureName}' from {entry.AssetFileRelativePath}.",
                        PngBytes = pngBytes,
                        Width = textureFile.m_Width,
                        Height = textureFile.m_Height,
                        TextureName = entry.TextureName
                    };
                }
                finally
                {
                    manager.UnloadAssetsFile(inst);
                }
            }
            catch (Exception ex)
            {
                return new ExtractResult
                {
                    Success = false,
                    Message = $"Failed to extract '{entry.TextureName}': {ex.Message}"
                };
            }
            finally
            {
                manager.UnloadAllAssetsFiles(true);
                manager.UnloadAllBundleFiles();
            }
        }

        private static IEnumerable<GameAssetTextureCatalogEntry> ScanAssetFile(AssetsManager manager, string dataDirectory, string assetFilePath)
        {
            var inst = manager.LoadAssetsFile(assetFilePath, false);
            try
            {
                manager.LoadClassDatabaseFromPackage(inst.file.Metadata.UnityVersion);
                var assetFileRelativePath = Path.GetRelativePath(dataDirectory, assetFilePath).Replace('\\', '/');

                foreach (var assetInfo in inst.file.GetAssetsOfType(AssetClassID.Texture2D))
                {
                    AssetTypeValueField field;
                    try
                    {
                        field = manager.GetBaseField(inst, assetInfo, AssetReadFlags.None);
                    }
                    catch
                    {
                        continue;
                    }

                    var textureName = field["m_Name"].AsString;
                    if (string.IsNullOrWhiteSpace(textureName))
                        continue;

                    TextureFile textureFile;
                    try
                    {
                        textureFile = TextureFile.ReadTextureFile(field);
                    }
                    catch
                    {
                        continue;
                    }

                    if (textureFile.m_Width < 1 || textureFile.m_Height < 1)
                        continue;

                    yield return new GameAssetTextureCatalogEntry
                    {
                        TextureName = textureName,
                        AssetFileRelativePath = assetFileRelativePath,
                        PathId = assetInfo.PathId,
                        Width = textureFile.m_Width,
                        Height = textureFile.m_Height,
                        LooksLikeClothing = LooksLikeClothingTexture(textureName)
                    };
                }
            }
            finally
            {
                manager.UnloadAssetsFile(inst);
            }
        }

        private static IEnumerable<string> EnumerateAssetFiles(string dataDirectory)
        {
            var files = Directory.GetFiles(dataDirectory, "*.assets", SearchOption.TopDirectoryOnly);
            return files
                .OrderBy(path => Path.GetFileName(path).Equals("resources.assets", StringComparison.OrdinalIgnoreCase) ? 0 : 1)
                .ThenBy(path => Path.GetFileName(path), StringComparer.OrdinalIgnoreCase);
        }

        private static bool TryResolveDataDirectory(string? configuredGameInstallPath, out string dataDirectory, out string error)
        {
            dataDirectory = string.Empty;
            error = string.Empty;

            var requestedPath = GameInstallPathResolver.ResolveOrDefault(configuredGameInstallPath);
            if (!GameInstallPathResolver.TryResolve(requestedPath, out var gameInstallPath))
            {
                error = "Game install path is not configured or does not point to a valid Schedule I install.";
                return false;
            }

            dataDirectory = Path.Combine(gameInstallPath, "Schedule I_Data");
            if (!Directory.Exists(dataDirectory))
            {
                error = $"Could not find Schedule I_Data under:\n{gameInstallPath}";
                return false;
            }

            return true;
        }

        private static string? ResolveTypeTreePackagePath()
        {
            var candidates = new[]
            {
                Path.Combine(AppContext.BaseDirectory, "Tools", "AssetMetadata", "unity_type_trees.tpk"),
                Path.Combine(AppContext.BaseDirectory, "unity_type_trees.tpk"),
                Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "Tools", "AssetMetadata", "unity_type_trees.tpk"))
            };

            return candidates.FirstOrDefault(File.Exists);
        }

        private static bool LooksLikeClothingTexture(string textureName)
        {
            if (string.IsNullOrWhiteSpace(textureName))
                return false;

            var normalized = textureName.ToLowerInvariant();
            return ClothingKeywords.Any(normalized.Contains);
        }

        private static byte[]? ConvertRawTextureToPng(byte[] rawTextureBytes, int width, int height)
        {
            if (rawTextureBytes == null || rawTextureBytes.Length == 0 || width < 1 || height < 1)
                return null;

            var stride = width * 4;
            if (rawTextureBytes.Length < stride * height)
                return null;

            var bitmap = BitmapSource.Create(
                width,
                height,
                96,
                96,
                PixelFormats.Bgra32,
                null,
                rawTextureBytes,
                stride);

            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(bitmap));
            using var outputStream = new MemoryStream();
            encoder.Save(outputStream);
            return outputStream.ToArray();
        }
    }
}
