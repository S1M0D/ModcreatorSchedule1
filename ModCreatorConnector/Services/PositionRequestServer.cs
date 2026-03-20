using System;
using System.Collections.Generic;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MelonLoader;
using Newtonsoft.Json;
using S1API.Entities;
using S1API.Items;
using S1API.Shops;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ModCreatorConnector.Services
{
    /// <summary>
    /// Server that handles position requests from the mod creator via named pipe.
    /// </summary>
    public class PositionRequestServer : IDisposable
    {
        private const string PipeName = "Schedule1ModCreator_Position";

        private class PositionRequest
        {
            [JsonProperty("request")]
            public string? Request { get; set; }

            [JsonProperty("assetPath")]
            public string? AssetPath { get; set; }

            [JsonProperty("applicationType")]
            public string? ApplicationType { get; set; }
        }

        private NamedPipeServerStream? _pipeServer;
        private CancellationTokenSource? _cancellationTokenSource;
        private Task? _connectionTask;
        private bool _isDisposed;

        /// <summary>
        /// Starts the position request server.
        /// </summary>
        public void Start()
        {
            if (_isDisposed)
                throw new ObjectDisposedException(nameof(PositionRequestServer));

            if (_connectionTask != null && !_connectionTask.IsCompleted)
                return; // Already started

            _cancellationTokenSource = new CancellationTokenSource();
            _connectionTask = Task.Run(() =>
            {
                try
                {
                    ConnectionLoop(_cancellationTokenSource.Token);
                }
                catch (OperationCanceledException)
                {
                    // Expected when cancelled, ignore
                }
                catch (Exception ex)
                {
                    MelonLogger.Error($"PositionRequestServer: ConnectionLoop exception: {ex.Message}");
                }
            });
        }

        /// <summary>
        /// Stops the position request server.
        /// </summary>
        public void Stop()
        {
            _cancellationTokenSource?.Cancel();
            
            try
            {
                _connectionTask?.Wait(TimeSpan.FromSeconds(5));
            }
            catch (AggregateException)
            {
                // Task may have been cancelled, ignore
            }
            catch (Exception)
            {
                // Ignore other exceptions during shutdown
            }
            
            _connectionTask = null;

            try
            {
                _pipeServer?.Dispose();
            }
            catch { }

            _pipeServer = null;
        }

        private void ConnectionLoop(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    // Create new pipe server
                    _pipeServer = new NamedPipeServerStream(
                        PipeName,
                        PipeDirection.InOut,
                        1,
                        PipeTransmissionMode.Byte,
                        PipeOptions.Asynchronous);

                    // Wait for client connection
                    var connectTask = _pipeServer.WaitForConnectionAsync(cancellationToken);
                    connectTask.GetAwaiter().GetResult();

                    if (_pipeServer.IsConnected)
                    {
                        // Handle request
                        HandleRequest();
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    MelonLogger.Warning($"PositionRequestServer: Connection error: {ex.Message}");
                    Thread.Sleep(1000);
                }
                finally
                {
                    try
                    {
                        _pipeServer?.Dispose();
                    }
                    catch { }
                    _pipeServer = null;
                }
            }
        }

        private void HandleRequest()
        {
            if (_pipeServer == null || !_pipeServer.IsConnected)
                return;

            try
            {
                // Read request length
                var lengthBytes = new byte[4];
                var bytesRead = _pipeServer.Read(lengthBytes, 0, 4);
                if (bytesRead != 4)
                {
                    SendErrorResponse("Failed to read request length");
                    return;
                }

                var requestLength = BitConverter.ToInt32(lengthBytes, 0);
                if (requestLength <= 0 || requestLength > 1024 * 1024) // Max 1MB
                {
                    SendErrorResponse("Invalid request length");
                    return;
                }

                // Read request data
                var requestBytes = new byte[requestLength];
                var totalRead = 0;
                while (totalRead < requestLength)
                {
                    if (!_pipeServer.IsConnected)
                    {
                        return;
                    }
                    var read = _pipeServer.Read(requestBytes, totalRead, requestLength - totalRead);
                    if (read == 0)
                    {
                        return;
                    }
                    totalRead += read;
                }

                var requestJson = Encoding.UTF8.GetString(requestBytes);
                var request = JsonConvert.DeserializeObject<PositionRequest>(requestJson);

                if (request?.Request == "getPosition")
                {
                    HandlePositionRequest();
                }
                else if (request?.Request == "getRuntimeCatalog")
                {
                    HandleRuntimeCatalogRequest();
                }
                else if (request?.Request == "getClothingTexture")
                {
                    HandleClothingTextureRequest(request);
                }
                else
                {
                    SendErrorResponse("Unknown request type");
                }
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"PositionRequestServer: Error handling request: {ex.Message}");
                SendErrorResponse($"Error handling request: {ex.Message}");
            }
        }

        private void HandlePositionRequest()
        {
            if (_pipeServer == null || !_pipeServer.IsConnected)
                return;

            try
            {
                // Check if we're in Main scene
                var currentScene = SceneManager.GetActiveScene();
                if (currentScene == null || currentScene.name != "Main")
                {
                    SendErrorResponse("Not in Main scene. Player position is only available in the Main scene.");
                    return;
                }

                // Get player position
                var localPlayer = Player.Local;
                if (localPlayer == null)
                {
                    SendErrorResponse("Local player not available");
                    return;
                }

                var position = localPlayer.Position;

                // Send success response
                var response = new
                {
                    success = true,
                    position = new
                    {
                        x = position.x,
                        y = position.y,
                        z = position.z
                    }
                };

                SendResponse(response);
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"PositionRequestServer: Error getting player position: {ex.Message}");
                SendErrorResponse($"Error getting player position: {ex.Message}");
            }
        }

        private void HandleRuntimeCatalogRequest()
        {
            if (_pipeServer == null || !_pipeServer.IsConnected)
                return;

            try
            {
                var itemDefinitions = ItemManager.GetAllItemDefinitions() ?? new List<ItemDefinition>();
                var itemLookup = itemDefinitions
                    .Where(definition => !string.IsNullOrWhiteSpace(definition.ID))
                    .GroupBy(definition => definition.ID, StringComparer.OrdinalIgnoreCase)
                    .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);

                var items = itemDefinitions
                    .Where(definition => !string.IsNullOrWhiteSpace(definition.ID))
                    .Select(definition => new
                    {
                        itemId = definition.ID,
                        name = definition.Name ?? string.Empty,
                        category = definition.Category.ToString(),
                        itemType = GetItemType(definition),
                        basePurchasePrice = definition is StorableItemDefinition storable ? storable.BasePurchasePrice : 0f,
                        resellMultiplier = definition is StorableItemDefinition priced ? priced.ResellMultiplier : 0f,
                        legalStatus = definition.LegalStatus.ToString(),
                        availableInDemo = definition.AvailableInDemo
                    })
                    .OrderBy(item => item.name)
                    .ThenBy(item => item.itemId)
                    .ToArray();

                string? warning = null;
                var shops = Array.Empty<object>();
                var currentScene = SceneManager.GetActiveScene();
                if (currentScene.IsValid() && string.Equals(currentScene.name, "Main", StringComparison.OrdinalIgnoreCase))
                {
                    shops = (ShopManager.GetAllShops() ?? Array.Empty<Shop>())
                        .Where(shop => !string.IsNullOrWhiteSpace(shop.Name))
                        .Select(shop =>
                        {
                            var itemIds = shop.GetItemIds()
                                .Where(itemId => !string.IsNullOrWhiteSpace(itemId))
                                .Distinct(StringComparer.OrdinalIgnoreCase)
                                .OrderBy(itemId => itemId, StringComparer.OrdinalIgnoreCase)
                                .ToArray();

                            var categories = itemIds
                                .Select(itemId => itemLookup.TryGetValue(itemId, out var definition) ? definition.Category.ToString() : null)
                                .Where(category => !string.IsNullOrWhiteSpace(category))
                                .Distinct(StringComparer.OrdinalIgnoreCase)
                                .OrderBy(category => category, StringComparer.OrdinalIgnoreCase)
                                .ToArray();

                            return new
                            {
                                name = shop.Name,
                                listingCount = itemIds.Length,
                                categories = categories,
                                itemIds = itemIds
                            };
                        })
                        .OrderBy(shop => shop.name, StringComparer.OrdinalIgnoreCase)
                        .Cast<object>()
                        .ToArray();
                }
                else
                {
                    warning = "Shops are only available in the Main scene. Enter the game world to load live shops.";
                }

                SendResponse(new
                {
                    success = true,
                    items,
                    shops,
                    error = warning
                });
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"PositionRequestServer: Error getting runtime catalog: {ex.Message}");
                SendErrorResponse($"Error getting runtime catalog: {ex.Message}");
            }
        }

        private void HandleClothingTextureRequest(PositionRequest request)
        {
            if (_pipeServer == null || !_pipeServer.IsConnected)
                return;

            try
            {
                var sourceAssetPath = request.AssetPath?.Trim();
                if (string.IsNullOrWhiteSpace(sourceAssetPath))
                {
                    SendErrorResponse("A source clothing asset path is required.");
                    return;
                }

                Texture? sourceTexture;
                var resolvedTextureName = string.Empty;
                var resolvedShaderProperty = "_MainTex";

                if (string.Equals(request.ApplicationType, "Accessory", StringComparison.OrdinalIgnoreCase))
                {
                    var accessoryPrefab = Resources.Load<GameObject>(sourceAssetPath);
                    if (accessoryPrefab == null)
                    {
                        SendErrorResponse($"Could not load accessory prefab '{sourceAssetPath}'.");
                        return;
                    }

                    if (!TryFindAccessoryTexture(accessoryPrefab, out sourceTexture, out resolvedTextureName, out resolvedShaderProperty) || sourceTexture == null)
                    {
                        SendErrorResponse($"Could not find a readable texture on accessory '{sourceAssetPath}'.");
                        return;
                    }
                }
                else
                {
                    sourceTexture = Resources.Load<Texture2D>(sourceAssetPath) ?? Resources.Load(sourceAssetPath) as Texture2D;
                    if (sourceTexture == null)
                    {
                        SendErrorResponse($"Could not load texture '{sourceAssetPath}'.");
                        return;
                    }

                    resolvedTextureName = sourceTexture.name ?? string.Empty;
                }

                if (!TryExtractTextureBytes(sourceTexture, out var textureBytes, out var width, out var height, out var error))
                {
                    SendErrorResponse(error ?? $"Failed to export '{sourceAssetPath}' as PNG.");
                    return;
                }

                SendResponse(new
                {
                    success = true,
                    sourceAssetPath,
                    resolvedTextureName,
                    resolvedShaderProperty,
                    width,
                    height,
                    pixelFormat = "bgra32",
                    textureBytesBase64 = Convert.ToBase64String(textureBytes)
                });
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"PositionRequestServer: Error importing clothing texture: {ex.Message}");
                SendErrorResponse($"Error importing clothing texture: {ex.Message}");
            }
        }

        private static string GetItemType(ItemDefinition definition)
        {
            var typeName = definition.GetType().Name;
            if (typeName.Contains("Additive", StringComparison.OrdinalIgnoreCase))
            {
                return "Additive";
            }

            if (typeName.Contains("Buildable", StringComparison.OrdinalIgnoreCase))
            {
                return "Buildable";
            }

            if (typeName.Contains("Clothing", StringComparison.OrdinalIgnoreCase))
            {
                return "Clothing";
            }

            return "Generic";
        }

        private static bool TryFindAccessoryTexture(GameObject accessoryPrefab, out Texture? texture, out string textureName, out string shaderProperty)
        {
            texture = null;
            textureName = string.Empty;
            shaderProperty = "_MainTex";

            var preferredProperties = new[] { "_MainTex", "_BaseMap", "_BaseColorMap", "_BaseTex", "_MaskTex" };
            var renderers = accessoryPrefab.GetComponentsInChildren<Renderer>(true) ?? Array.Empty<Renderer>();
            foreach (var renderer in renderers)
            {
                var materials = renderer.sharedMaterials ?? Array.Empty<Material>();
                foreach (var material in materials.Where(material => material != null))
                {
                    foreach (var propertyName in preferredProperties)
                    {
                        if (!material.HasProperty(propertyName))
                            continue;

                        var candidate = material.GetTexture(propertyName);
                        if (candidate == null)
                            continue;

                        texture = candidate;
                        textureName = candidate.name ?? string.Empty;
                        shaderProperty = propertyName;
                        return true;
                    }

                    if (material.mainTexture != null)
                    {
                        texture = material.mainTexture;
                        textureName = material.mainTexture.name ?? string.Empty;
                        shaderProperty = "_MainTex";
                        return true;
                    }
                }
            }

            return false;
        }

        private static bool TryExtractTextureBytes(Texture sourceTexture, out byte[] textureBytes, out int width, out int height, out string? error)
        {
            textureBytes = Array.Empty<byte>();
            width = 0;
            height = 0;
            error = null;

            Texture2D? readableTexture = null;
            try
            {
                readableTexture = CreateReadableTexture(sourceTexture);
                if (readableTexture == null)
                {
                    error = "Could not create a readable texture copy.";
                    return false;
                }

                width = readableTexture.width;
                height = readableTexture.height;
                textureBytes = ConvertToBgraBytes(readableTexture);
                if (textureBytes.Length == 0)
                {
                    error = "Texture byte extraction returned no data.";
                    return false;
                }

                return true;
            }
            finally
            {
                if (readableTexture != null)
                {
                    UnityEngine.Object.Destroy(readableTexture);
                }
            }
        }

        private static byte[] ConvertToBgraBytes(Texture2D readableTexture)
        {
            var pixels = readableTexture.GetPixels32();
            var bytes = new byte[pixels.Length * 4];
            for (var i = 0; i < pixels.Length; i++)
            {
                var pixel = pixels[i];
                var offset = i * 4;
                bytes[offset] = pixel.b;
                bytes[offset + 1] = pixel.g;
                bytes[offset + 2] = pixel.r;
                bytes[offset + 3] = pixel.a;
            }

            return bytes;
        }

        private static Texture2D? CreateReadableTexture(Texture sourceTexture)
        {
            var width = Math.Max(1, sourceTexture.width);
            var height = Math.Max(1, sourceTexture.height);
            var previousRenderTarget = RenderTexture.active;
            var tempRenderTarget = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.ARGB32);

            try
            {
                Graphics.Blit(sourceTexture, tempRenderTarget);
                RenderTexture.active = tempRenderTarget;

                var readableTexture = new Texture2D(width, height, TextureFormat.RGBA32, false);
                readableTexture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                readableTexture.Apply();
                return readableTexture;
            }
            finally
            {
                RenderTexture.active = previousRenderTarget;
                RenderTexture.ReleaseTemporary(tempRenderTarget);
            }
        }

        private void SendResponse(object response)
        {
            if (_pipeServer == null || !_pipeServer.IsConnected)
                return;

            try
            {
                var responseJson = JsonConvert.SerializeObject(response);
                var responseBytes = Encoding.UTF8.GetBytes(responseJson);
                var lengthBytes = BitConverter.GetBytes(responseBytes.Length);

                _pipeServer.Write(lengthBytes, 0, lengthBytes.Length);
                _pipeServer.Write(responseBytes, 0, responseBytes.Length);
                _pipeServer.Flush();
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"PositionRequestServer: Error sending response: {ex.Message}");
            }
        }

        private void SendErrorResponse(string error)
        {
            var response = new
            {
                success = false,
                error = error
            };
            SendResponse(response);
        }

        public void Dispose()
        {
            if (_isDisposed)
                return;

            _isDisposed = true;
            Stop();
            
            // Ensure task completes before disposing cancellation token
            try
            {
                _connectionTask?.Wait(TimeSpan.FromSeconds(1));
            }
            catch { }
            
            _cancellationTokenSource?.Dispose();
            _connectionTask = null;
        }
    }
}

