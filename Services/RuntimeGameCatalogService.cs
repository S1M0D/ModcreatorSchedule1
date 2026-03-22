using Schedule1ModdingTool.Models;

namespace Schedule1ModdingTool.Services
{
    /// <summary>
    /// Requests live item and shop data from the running connector mod.
    /// </summary>
    public class RuntimeGameCatalogService
    {
        private const int RequestTimeoutMs = 3000;
        private const int MaxResponseBytes = 8 * 1024 * 1024;

        public RuntimeGameCatalogResponse RequestRuntimeCatalog()
        {
            try
            {
                return ConnectorPipeClient.SendRequest<RuntimeGameCatalogResponse>(
                    new { request = "getRuntimeCatalog" },
                    RequestTimeoutMs,
                    MaxResponseBytes,
                    "Connector returned an empty response");
            }
            catch (ConnectorPipeException ex)
            {
                return new RuntimeGameCatalogResponse
                {
                    Success = false,
                    Error = ex.Message
                };
            }
            catch (Exception ex)
            {
                return new RuntimeGameCatalogResponse
                {
                    Success = false,
                    Error = $"Error requesting runtime catalog: {ex.Message}"
                };
            }
        }
    }
}
