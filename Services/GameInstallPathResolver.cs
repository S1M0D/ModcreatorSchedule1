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

        private static readonly string[] RequiredMonoAssemblies =
        {
            "Unity.TextMeshPro.dll", "UnityEngine.AssetBundleModule.dll", "UnityEngine.CoreModule.dll",
            "UnityEngine.dll", "UnityEngine.JSONSerializeModule.dll", "UnityEngine.TextRenderingModule.dll",
            "UnityEngine.UI.dll", "UnityEngine.UIElementsModule.dll", "UnityEngine.UIModule.dll"
        };

        public static bool TryResolveManagedAssembliesPath(string? configuredPath, string? gameInstallPath, out string managedPath)
        {
            var candidates = new List<string>();
            if (!string.IsNullOrWhiteSpace(configuredPath))
                candidates.Add(configuredPath.Trim().Trim('"'));
            if (!string.IsNullOrWhiteSpace(gameInstallPath))
            {
                var gamePath = gameInstallPath.Trim().Trim('"');
                candidates.Add(gamePath);
                var parent = Directory.GetParent(gamePath);
                if (parent != null)
                {
                    candidates.Add(Path.Combine(parent.FullName, "Schedule I"));
                    candidates.Add(Path.Combine(parent.FullName, "Schedule I_alternate"));
                    candidates.Add(Path.Combine(parent.FullName, "Schedule I_public"));
                }
            }
            candidates.AddRange(GetKnownInstallCandidates());

            foreach (var candidate in candidates)
            {
                var path = candidate.EndsWith("Managed", StringComparison.OrdinalIgnoreCase)
                    ? candidate
                    : Path.Combine(candidate, "Schedule I_Data", "Managed");
                if (IsValidManagedAssembliesPath(path))
                {
                    managedPath = Path.GetFullPath(path);
                    return true;
                }
            }
            managedPath = string.Empty;
            return false;
        }

        public static bool IsValidManagedAssembliesPath(string? path) =>
            !string.IsNullOrWhiteSpace(path) && Directory.Exists(path) &&
            RequiredMonoAssemblies.All(name => File.Exists(Path.Combine(path, name)));

        public static bool TryResolveIl2CppAssembliesPath(string? configuredPath, string? gameInstallPath, out string assembliesPath)
        {
            var candidates = new List<string>();
            if (!string.IsNullOrWhiteSpace(configuredPath))
                candidates.Add(configuredPath.Trim().Trim('"'));
            if (!string.IsNullOrWhiteSpace(gameInstallPath))
                candidates.Add(Path.Combine(gameInstallPath, "MelonLoader", "Il2CppAssemblies"));
            foreach (var install in GetKnownInstallCandidates())
                candidates.Add(Path.Combine(install, "MelonLoader", "Il2CppAssemblies"));
            foreach (var path in candidates)
            {
                if (Directory.Exists(path) && File.Exists(Path.Combine(path, "Assembly-CSharp.dll")) &&
                    File.Exists(Path.Combine(path, "UnityEngine.CoreModule.dll")) &&
                    File.Exists(Path.Combine(path, "Il2Cppmscorlib.dll")))
                {
                    assembliesPath = Path.GetFullPath(path);
                    return true;
                }
            }
            assembliesPath = string.Empty;
            return false;
        }

        public static string ResolveOrDefault(string? configuredPath)
        {
            return TryResolve(configuredPath, out var resolvedPath)
                ? resolvedPath
                : TryResolveKnownInstall(out resolvedPath)
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

            var parentDirectory = Directory.GetParent(cleanedPath);
            if (parentDirectory != null)
            {
                var siblingAlternate = Path.Combine(parentDirectory.FullName, "Schedule I_alternate");
                if (seen.Add(siblingAlternate))
                {
                    yield return siblingAlternate;
                }
            }
        }

        private static bool TryResolveKnownInstall(out string resolvedPath)
        {
            foreach (var candidate in GetKnownInstallCandidates())
            {
                if (IsValidInstallDirectory(candidate))
                {
                    resolvedPath = candidate;
                    return true;
                }
            }

            resolvedPath = string.Empty;
            return false;
        }

        private static IEnumerable<string> GetKnownInstallCandidates()
        {
            yield return DefaultSteamInstallPath;
            yield return "D:\\SteamLibrary\\steamapps\\common\\Schedule I_alternate";
            yield return "D:\\SteamLibrary\\steamapps\\common\\Schedule I";
            yield return "D:\\SteamLibrary\\steamapps\\common\\Schedule I_public";
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
