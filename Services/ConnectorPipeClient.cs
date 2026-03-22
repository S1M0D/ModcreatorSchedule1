using System.IO.Pipes;
using System.Text;
using Newtonsoft.Json;

namespace Schedule1ModdingTool.Services
{
    internal sealed class ConnectorPipeException : Exception
    {
        public ConnectorPipeException(string message, Exception? innerException = null)
            : base(message, innerException)
        {
        }
    }

    internal static class ConnectorPipeClient
    {
        private const string PipeName = "Schedule1ModCreator_Position";

        public static TResponse SendRequest<TResponse>(
            object request,
            int timeoutMs,
            int maxResponseBytes,
            string emptyResponseMessage)
        {
            using var pipeClient = CreateConnectedClient(timeoutMs);
            WriteMessage(pipeClient, request);
            var responseJson = ReadMessage(pipeClient, maxResponseBytes);

            return JsonConvert.DeserializeObject<TResponse>(responseJson)
                ?? throw new ConnectorPipeException(emptyResponseMessage);
        }

        private static NamedPipeClientStream CreateConnectedClient(int timeoutMs)
        {
            var pipeClient = new NamedPipeClientStream(".", PipeName, PipeDirection.InOut, PipeOptions.None);

            try
            {
                pipeClient.Connect(timeoutMs);
                return pipeClient;
            }
            catch (TimeoutException timeoutException)
            {
                pipeClient.Dispose();
                throw new ConnectorPipeException("Failed to connect to connector mod", timeoutException);
            }
            catch
            {
                pipeClient.Dispose();
                throw;
            }
        }

        private static void WriteMessage(NamedPipeClientStream pipeClient, object request)
        {
            var requestJson = JsonConvert.SerializeObject(request);
            var requestBytes = Encoding.UTF8.GetBytes(requestJson);
            var requestLengthBytes = BitConverter.GetBytes(requestBytes.Length);

            pipeClient.Write(requestLengthBytes, 0, requestLengthBytes.Length);
            pipeClient.Write(requestBytes, 0, requestBytes.Length);
            pipeClient.Flush();
        }

        private static string ReadMessage(NamedPipeClientStream pipeClient, int maxResponseBytes)
        {
            var responseLengthBytes = ReadExact(pipeClient, sizeof(int), "Failed to read response length");
            var responseLength = BitConverter.ToInt32(responseLengthBytes, 0);
            if (responseLength <= 0 || responseLength > maxResponseBytes)
            {
                throw new ConnectorPipeException("Invalid response length");
            }

            var responseBytes = ReadExact(pipeClient, responseLength, "Connection closed while reading response");
            return Encoding.UTF8.GetString(responseBytes);
        }

        private static byte[] ReadExact(NamedPipeClientStream pipeClient, int byteCount, string errorMessage)
        {
            var buffer = new byte[byteCount];
            var totalRead = 0;

            while (totalRead < byteCount)
            {
                var bytesRead = pipeClient.Read(buffer, totalRead, byteCount - totalRead);
                if (bytesRead == 0)
                {
                    throw new ConnectorPipeException(errorMessage);
                }

                totalRead += bytesRead;
            }

            return buffer;
        }
    }
}
