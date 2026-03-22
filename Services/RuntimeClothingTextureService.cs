using Schedule1ModdingTool.Models;

namespace Schedule1ModdingTool.Services
{
    /// <summary>
    /// Imports clothing textures from the running connector mod over the shared named pipe.
    /// </summary>
    public class RuntimeClothingTextureService
    {
        private const int RequestTimeoutMs = 5000;
        private const int MaxResponseBytes = 32 * 1024 * 1024;

        public RuntimeClothingTextureResponse ImportTexture(string sourceAssetPath, string applicationType)
        {
            try
            {
                return ConnectorPipeClient.SendRequest<RuntimeClothingTextureResponse>(
                    new
                    {
                        request = "getClothingTexture",
                        assetPath = sourceAssetPath,
                        applicationType = applicationType
                    },
                    RequestTimeoutMs,
                    MaxResponseBytes,
                    "Connector returned an empty response.");
            }
            catch (ConnectorPipeException ex)
            {
                return new RuntimeClothingTextureResponse
                {
                    Success = false,
                    Error = ex.Message
                };
            }
            catch (Exception ex)
            {
                return new RuntimeClothingTextureResponse
                {
                    Success = false,
                    Error = $"Error importing clothing texture: {ex.Message}"
                };
            }
        }
    }
}
