using System.IO.Pipes;
using System.Text;
using Newtonsoft.Json;
using Schedule1ModdingTool.Models;

namespace Schedule1ModdingTool.Services
{
    /// <summary>
    /// Requests live item and shop data from the running connector mod.
    /// </summary>
    public class RuntimeGameCatalogService
    {
        private const string PipeName = "Schedule1ModCreator_Position";
        private const int RequestTimeoutMs = 3000;

        public RuntimeGameCatalogResponse RequestRuntimeCatalog()
        {
            NamedPipeClientStream? pipeClient = null;
            try
            {
                pipeClient = new NamedPipeClientStream(
                    ".",
                    PipeName,
                    PipeDirection.InOut,
                    PipeOptions.Asynchronous);

                if (!pipeClient.IsConnected)
                {
                    var connectTask = pipeClient.ConnectAsync(RequestTimeoutMs);
                    connectTask.Wait(RequestTimeoutMs);
                }

                if (!pipeClient.IsConnected)
                {
                    return new RuntimeGameCatalogResponse
                    {
                        Success = false,
                        Error = "Failed to connect to connector mod"
                    };
                }

                var request = new { request = "getRuntimeCatalog" };
                var requestJson = JsonConvert.SerializeObject(request);
                var requestBytes = Encoding.UTF8.GetBytes(requestJson);
                var lengthBytes = BitConverter.GetBytes(requestBytes.Length);

                pipeClient.Write(lengthBytes, 0, lengthBytes.Length);
                pipeClient.Write(requestBytes, 0, requestBytes.Length);
                pipeClient.Flush();

                var responseLengthBytes = new byte[4];
                var bytesRead = pipeClient.Read(responseLengthBytes, 0, 4);
                if (bytesRead != 4)
                {
                    return new RuntimeGameCatalogResponse
                    {
                        Success = false,
                        Error = "Failed to read response length"
                    };
                }

                var responseLength = BitConverter.ToInt32(responseLengthBytes, 0);
                if (responseLength <= 0 || responseLength > 8 * 1024 * 1024)
                {
                    return new RuntimeGameCatalogResponse
                    {
                        Success = false,
                        Error = "Invalid response length"
                    };
                }

                var responseBytes = new byte[responseLength];
                var totalRead = 0;
                while (totalRead < responseLength)
                {
                    var read = pipeClient.Read(responseBytes, totalRead, responseLength - totalRead);
                    if (read == 0)
                    {
                        return new RuntimeGameCatalogResponse
                        {
                            Success = false,
                            Error = "Connection closed while reading response"
                        };
                    }

                    totalRead += read;
                }

                var responseJson = Encoding.UTF8.GetString(responseBytes);
                return JsonConvert.DeserializeObject<RuntimeGameCatalogResponse>(responseJson)
                    ?? new RuntimeGameCatalogResponse
                    {
                        Success = false,
                        Error = "Connector returned an empty response"
                    };
            }
            catch (TimeoutException)
            {
                return new RuntimeGameCatalogResponse
                {
                    Success = false,
                    Error = "Runtime catalog request timed out"
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
            finally
            {
                try
                {
                    pipeClient?.Dispose();
                }
                catch
                {
                }
            }
        }
    }
}
