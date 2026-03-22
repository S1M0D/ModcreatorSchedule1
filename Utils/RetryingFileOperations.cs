using System.Diagnostics;
using System.IO;

namespace Schedule1ModdingTool.Utils
{
    /// <summary>
    /// Shared retry helpers for transiently locked project files.
    /// </summary>
    public static class RetryingFileOperations
    {
        private const FileShare SharedReadAccess = FileShare.ReadWrite | FileShare.Delete;

        public static string GenerateUniqueFileName(string directory, string fileName)
        {
            var name = Path.GetFileNameWithoutExtension(fileName);
            var extension = Path.GetExtension(fileName);
            var candidate = fileName;
            var counter = 1;

            while (File.Exists(Path.Combine(directory, candidate)))
            {
                candidate = $"{name}_{counter++}{extension}";
            }

            return candidate;
        }

        public static bool TryCopyFile(string source, string destination, out string error, int maxRetries = 5)
        {
            error = string.Empty;

            for (var attempt = 1; attempt <= maxRetries; attempt++)
            {
                try
                {
                    Debug.WriteLine($"[RetryingFileOperations] Copy attempt {attempt}/{maxRetries}: '{source}' -> '{destination}'");

                    var destinationDirectory = Path.GetDirectoryName(destination);
                    if (!string.IsNullOrEmpty(destinationDirectory))
                    {
                        Directory.CreateDirectory(destinationDirectory);
                    }

                    using var sourceStream = new FileStream(source, FileMode.Open, FileAccess.Read, SharedReadAccess);
                    using var destinationStream = new FileStream(destination, FileMode.Create, FileAccess.Write, FileShare.None);
                    sourceStream.CopyTo(destinationStream);
                    return true;
                }
                catch (IOException ioException)
                {
                    error = ioException.Message;
                    Debug.WriteLine($"[RetryingFileOperations] Copy IO error ({attempt}/{maxRetries}): {ioException.Message}");
                }
                catch (UnauthorizedAccessException unauthorizedException)
                {
                    error = unauthorizedException.Message;
                    Debug.WriteLine($"[RetryingFileOperations] Copy access error ({attempt}/{maxRetries}): {unauthorizedException.Message}");
                }
                catch (Exception exception)
                {
                    error = exception.Message;
                    Debug.WriteLine($"[RetryingFileOperations] Copy failed: {exception.Message}");
                    return false;
                }

                if (attempt < maxRetries)
                {
                    Thread.Sleep(GetRetryDelayMs(attempt));
                }
            }

            return false;
        }

        public static bool TryDeleteFile(string path, out string error, int maxRetries = 5)
        {
            error = string.Empty;

            for (var attempt = 1; attempt <= maxRetries; attempt++)
            {
                try
                {
                    if (File.Exists(path))
                    {
                        Debug.WriteLine($"[RetryingFileOperations] Delete attempt {attempt}/{maxRetries}: '{path}'");
                        File.Delete(path);
                    }

                    return true;
                }
                catch (IOException ioException)
                {
                    error = ioException.Message;
                    Debug.WriteLine($"[RetryingFileOperations] Delete IO error ({attempt}/{maxRetries}): {ioException.Message}");
                }
                catch (UnauthorizedAccessException unauthorizedException)
                {
                    error = unauthorizedException.Message;
                    Debug.WriteLine($"[RetryingFileOperations] Delete access error ({attempt}/{maxRetries}): {unauthorizedException.Message}");
                }
                catch (Exception exception)
                {
                    error = exception.Message;
                    Debug.WriteLine($"[RetryingFileOperations] Delete failed: {exception.Message}");
                    return false;
                }

                if (attempt < maxRetries)
                {
                    Thread.Sleep(GetRetryDelayMs(attempt));
                }
            }

            return false;
        }

        private static int GetRetryDelayMs(int attempt)
        {
            return 100 * (1 << (attempt - 1));
        }
    }
}
