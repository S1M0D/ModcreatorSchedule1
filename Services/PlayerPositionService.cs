using Newtonsoft.Json;

namespace Schedule1ModdingTool.Services
{
    /// <summary>
    /// Service that requests player position from the Connector mod via named pipe.
    /// </summary>
    public class PlayerPositionService : IDisposable
    {
        private const int RequestTimeoutMs = 3000;
        private const int MaxResponseBytes = 1024 * 1024;

        /// <summary>
        /// Represents a position response from the connector mod.
        /// </summary>
        public class PositionResponse
        {
            [JsonProperty("success")]
            public bool Success { get; set; }

            [JsonProperty("position")]
            public PositionData? Position { get; set; }

            [JsonProperty("error")]
            public string? Error { get; set; }
        }

        /// <summary>
        /// Represents position data (x, y, z).
        /// </summary>
        public class PositionData
        {
            [JsonProperty("x")]
            public float X { get; set; }

            [JsonProperty("y")]
            public float Y { get; set; }

            [JsonProperty("z")]
            public float Z { get; set; }
        }

        /// <summary>
        /// Requests the current player position from the connector mod.
        /// </summary>
        /// <returns>Position response, or null if request failed or timed out.</returns>
        public PositionResponse? RequestPlayerPosition()
        {
            try
            {
                return ConnectorPipeClient.SendRequest<PositionResponse>(
                    new { request = "getPosition" },
                    RequestTimeoutMs,
                    MaxResponseBytes,
                    "Connector returned an empty response");
            }
            catch (ConnectorPipeException ex)
            {
                return new PositionResponse
                {
                    Success = false,
                    Error = ex.Message
                };
            }
            catch (Exception ex)
            {
                return new PositionResponse
                {
                    Success = false,
                    Error = $"Error requesting position: {ex.Message}"
                };
            }
        }

        public void Dispose()
        {
            // Nothing to dispose for this service
        }
    }
}

