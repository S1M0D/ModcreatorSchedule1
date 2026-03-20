using System.IO;

namespace Schedule1ModdingTool.Services
{
    /// <summary>
    /// Resolves user-provided Schedule I paths to an actual game installation directory.
    /// Users may select the game folder itself, the parent Steam "common" folder, or the exe path.
    /// </summary>
    public static class GameInstallPathResolver
    {
        public const string DefaultSteamInstallPath = "C:\\Program Files (x86)\\Steam\\steamapps\\common\\Schedule I";

        public static string ResolveOrDefault(string? configuredPath)
        {
            return TryResolve(configuredPath, out var resolvedPath)
                ? resolvedPath
                : DefaultSteamInstallPath;
        }

        public static bool TryResolve(string? configuredPath, out string resolvedPath)
        {
            resolvedPath = string.Empty;

            var cleanedPath = NormalizeInput(configuredPath);
            if (string.IsNullOrWhiteSpace(cleanedPath))
                return false;

            foreach (var candidate in GetCandidates(cleanedPath))
            {
                if (IsValidInstallDirectory(candidate))
                {
                    resolvedPath = candidate;
                    return true;
                }
            }

            return false;
        }

        private static IEnumerable<string> GetCandidates(string cleanedPath)
        {
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            if (File.Exists(cleanedPath) &&
                string.Equals(Path.GetFileName(cleanedPath), "Schedule I.exe", StringComparison.OrdinalIgnoreCase))
            {
                var exeDirectory = Path.GetDirectoryName(cleanedPath);
                if (!string.IsNullOrWhiteSpace(exeDirectory) && seen.Add(exeDirectory))
                {
                    yield return exeDirectory;
                }
            }

            if (seen.Add(cleanedPath))
            {
                yield return cleanedPath;
            }

            var scheduleIChild = Path.Combine(cleanedPath, "Schedule I");
            if (seen.Add(scheduleIChild))
            {
                yield return scheduleIChild;
            }

            var alternateChild = Path.Combine(cleanedPath, "Schedule I_alternate");
            if (seen.Add(alternateChild))
            {
                yield return alternateChild;
            }
        }

        private static bool IsValidInstallDirectory(string candidatePath)
        {
            if (string.IsNullOrWhiteSpace(candidatePath) || !Directory.Exists(candidatePath))
                return false;

            return File.Exists(Path.Combine(candidatePath, "Schedule I.exe")) ||
                   Directory.Exists(Path.Combine(candidatePath, "Schedule I_Data", "Managed")) ||
                   Directory.Exists(Path.Combine(candidatePath, "MelonLoader"));
        }

        private static string NormalizeInput(string? configuredPath)
        {
            return configuredPath?.Trim().Trim('"') ?? string.Empty;
        }
    }
}
