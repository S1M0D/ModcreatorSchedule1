using System.Diagnostics;
using System.IO;
using System.Text;
using Schedule1ModdingTool.Models;

namespace Schedule1ModdingTool.Services
{
    /// <summary>
    /// Service for building mod projects using dotnet CLI
    /// </summary>
    public class ModBuildService
    {
        /// <summary>
        /// Builds a mod project using dotnet CLI
        /// </summary>
        public ModBuildResult BuildModProject(string projectPath, ModSettings? settings = null)
        {
            var result = new ModBuildResult();

            if (string.IsNullOrWhiteSpace(projectPath))
            {
                result.Success = false;
                result.ErrorMessage = "Project path cannot be empty";
                return result;
            }

            if (!Directory.Exists(projectPath))
            {
                result.Success = false;
                result.ErrorMessage = $"Project directory does not exist: {projectPath}";
                return result;
            }

            var csprojFile = Directory.GetFiles(projectPath, "*.csproj").FirstOrDefault();
            if (csprojFile == null)
            {
                result.Success = false;
                result.ErrorMessage = "No .csproj file found in project directory";
                return result;
            }

            try
            {
                var resolvedGamePath = settings != null && GameInstallPathResolver.TryResolve(settings.GameInstallPath, out var gameInstallPath)
                    ? gameInstallPath
                    : null;
                var buildTarget = settings?.BuildTarget ?? ModBuildTarget.Mono;
                var configurations = buildTarget switch
                {
                    ModBuildTarget.Il2Cpp => new[] { "Il2cpp" },
                    ModBuildTarget.Both => new[] { "CrossCompat", "Il2cpp" },
                    _ => new[] { "CrossCompat" }
                };
                var allOutput = new StringBuilder();
                var allErrors = new StringBuilder();

                foreach (var configuration in configurations)
                {
                    var il2Cpp = configuration == "Il2cpp";
                    var hasAssemblies = il2Cpp
                        ? GameInstallPathResolver.TryResolveIl2CppAssembliesPath(settings?.Il2CppAssembliesPath, resolvedGamePath, out var assembliesPath)
                        : GameInstallPathResolver.TryResolveManagedAssembliesPath(settings?.ManagedAssembliesPath, resolvedGamePath, out assembliesPath);
                    if (il2Cpp && !hasAssemblies)
                    {
                        result.ErrorMessage = "IL2CPP assemblies were not found. In Settings, select MelonLoader\\Il2CppAssemblies from an IL2CPP game install.";
                        return result;
                    }
                    if (il2Cpp && (resolvedGamePath == null || !File.Exists(Path.Combine(resolvedGamePath, "MelonLoader", "net6", "Il2CppInterop.Runtime.dll"))))
                    {
                        result.ErrorMessage = "IL2CPP builds also need MelonLoader\\net6 in the selected game folder.";
                        return result;
                    }

                    var startInfo = new ProcessStartInfo
                    {
                        FileName = "dotnet", WorkingDirectory = projectPath, UseShellExecute = false,
                        RedirectStandardOutput = true, RedirectStandardError = true, CreateNoWindow = true
                    };
                    foreach (var argument in new[] { "build", csprojFile, "-c", configuration, "--verbosity", "normal" })
                        startInfo.ArgumentList.Add(argument);
                    if (resolvedGamePath != null)
                        startInfo.ArgumentList.Add($"/p:GamePath={resolvedGamePath}");
                    if (hasAssemblies)
                        startInfo.ArgumentList.Add($"/p:ManagedPath={assembliesPath}");

                    var output = new StringBuilder();
                    var errors = new StringBuilder();
                    using var process = Process.Start(startInfo);
                    if (process == null)
                    {
                        result.ErrorMessage = "Failed to start dotnet build process";
                        return result;
                    }
                    process.OutputDataReceived += (_, e) => { if (e.Data != null) output.AppendLine(e.Data); };
                    process.ErrorDataReceived += (_, e) => { if (e.Data != null) errors.AppendLine(e.Data); };
                    process.BeginOutputReadLine();
                    process.BeginErrorReadLine();
                    process.WaitForExit();
                    allOutput.AppendLine($"=== {configuration} Build Output ===").Append(output);
                    allErrors.Append(errors);
                    result.ExitCode = process.ExitCode;
                    result.Output = allOutput.ToString();
                    result.ErrorOutput = allErrors.ToString();
                    if (process.ExitCode != 0)
                    {
                        result.ErrorMessage = $"{configuration} build failed with exit code {process.ExitCode}" +
                            (!hasAssemblies ? ". If Unity types are missing, set 'Mono game assemblies' in Settings." : "");
                        result.Output += errors;
                        return result;
                    }

                    var framework = il2Cpp ? "net6.0" : "netstandard2.1";
                    var dll = Path.Combine(projectPath, "bin", configuration, framework, Path.GetFileNameWithoutExtension(csprojFile) + ".dll");
                    if (!File.Exists(dll))
                    {
                        result.ErrorMessage = $"Build reported success but mod DLL was not found at: {dll}";
                        return result;
                    }
                    result.OutputDllPaths[configuration] = dll;
                    allOutput.AppendLine($"Output DLL: {dll}");
                }

                result.Success = true;
                result.Output = allOutput.ToString();
                var deployIl2Cpp = buildTarget == ModBuildTarget.Il2Cpp ||
                    (buildTarget == ModBuildTarget.Both && resolvedGamePath != null && File.Exists(Path.Combine(resolvedGamePath, "GameAssembly.dll")));
                result.OutputDllPath = result.OutputDllPaths[deployIl2Cpp ? "Il2cpp" : "CrossCompat"];
                if (settings != null && resolvedGamePath != null)
                    TryCopyToModsFolder(result.OutputDllPath, resolvedGamePath, result);
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorMessage = $"Exception during build: {ex.Message}";
            }

            return result;
        }

        private void TryCopyToModsFolder(string dllPath, string gameInstallPath, ModBuildResult result)
        {
            try
            {
                var modsPath = Path.Combine(gameInstallPath, "Mods");
                if (!Directory.Exists(modsPath))
                {
                    result.Warnings.Add($"Mods folder not found at {modsPath}, skipping auto-deploy");
                    return;
                }

                var fileName = Path.GetFileName(dllPath);
                var targetPath = Path.Combine(modsPath, fileName);

                File.Copy(dllPath, targetPath, overwrite: true);
                result.DeployedToModsFolder = true;
                result.DeployedDllPath = targetPath;
            }
            catch (Exception ex)
            {
                result.Warnings.Add($"Failed to copy DLL to Mods folder: {ex.Message}");
            }
        }

        /// <summary>
        /// Validates that a project structure is valid before building
        /// </summary>
        public bool ValidateProjectStructure(string projectPath)
        {
            if (string.IsNullOrWhiteSpace(projectPath) || !Directory.Exists(projectPath))
            {
                return false;
            }

            var csprojFile = Directory.GetFiles(projectPath, "*.csproj").FirstOrDefault();
            if (csprojFile == null)
            {
                return false;
            }

            var coreFile = Path.Combine(projectPath, "Core.cs");
            if (!File.Exists(coreFile))
            {
                return false;
            }

            return true;
        }
    }

    /// <summary>
    /// Result of mod build operation
    /// </summary>
    public class ModBuildResult
    {
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public string? Output { get; set; }
        public string? ErrorOutput { get; set; }
        public int ExitCode { get; set; }
        public string? OutputDllPath { get; set; }
        public Dictionary<string, string> OutputDllPaths { get; } = new(StringComparer.OrdinalIgnoreCase);
        public bool DeployedToModsFolder { get; set; }
        public string? DeployedDllPath { get; set; }
        public List<string> Warnings { get; } = new List<string>();
    }
}

