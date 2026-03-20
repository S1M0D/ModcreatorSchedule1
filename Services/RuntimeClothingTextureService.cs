using System.IO.Pipes;
using System.Text;
using Newtonsoft.Json;
using Schedule1ModdingTool.Models;

namespace Schedule1ModdingTool.Services
{
    /// <summary>
    /// Imports clothing textures from the running connector mod over the shared named pipe.
    /// </summary>
    public class RuntimeClothingTextureService
    {
        private const string PipeName = "Schedule1ModCreator_Position";
        private const int RequestTimeoutMs = 5000;
        private const int MaxResponseBytes = 32 * 1024 * 1024;

        public RuntimeClothingTextureResponse ImportTexture(string sourceAssetPath, string applicationType)
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
                    return new RuntimeClothingTextureResponse
                    {
                        Success = false,
                        Error = "Failed to connect to connector mod."
                    };
                }

                var request = new
                {
                    request = "getClothingTexture",
                    assetPath = sourceAssetPath,
                    applicationType = applicationType
                };

                var requestJson = JsonConvert.SerializeObject(request);
                var requestBytes = Encoding.UTF8.GetBytes(requestJson);
                var requestLengthBytes = BitConverter.GetBytes(requestBytes.Length);

                pipeClient.Write(requestLengthBytes, 0, requestLengthBytes.Length);
                pipeClient.Write(requestBytes, 0, requestBytes.Length);
                pipeClient.Flush();

                var responseLengthBytes = new byte[4];
                var bytesRead = pipeClient.Read(responseLengthBytes, 0, 4);
                if (bytesRead != 4)
                {
                    return new RuntimeClothingTextureResponse
                    {
                        Success = false,
                        Error = "Failed to read response length."
                    };
                }

                var responseLength = BitConverter.ToInt32(responseLengthBytes, 0);
                if (responseLength <= 0 || responseLength > MaxResponseBytes)
                {
                    return new RuntimeClothingTextureResponse
                    {
                        Success = false,
                        Error = "Invalid response length."
                    };
                }

                var responseBytes = new byte[responseLength];
                var totalRead = 0;
                while (totalRead < responseLength)
                {
                    var read = pipeClient.Read(responseBytes, totalRead, responseLength - totalRead);
                    if (read == 0)
                    {
                        return new RuntimeClothingTextureResponse
                        {
                            Success = false,
                            Error = "Connection closed while reading response."
                        };
                    }

                    totalRead += read;
                }

                var responseJson = Encoding.UTF8.GetString(responseBytes);
                return JsonConvert.DeserializeObject<RuntimeClothingTextureResponse>(responseJson)
                    ?? new RuntimeClothingTextureResponse
                    {
                        Success = false,
                        Error = "Connector returned an empty response."
                    };
            }
            catch (TimeoutException)
            {
                return new RuntimeClothingTextureResponse
                {
                    Success = false,
                    Error = "Texture import request timed out."
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
