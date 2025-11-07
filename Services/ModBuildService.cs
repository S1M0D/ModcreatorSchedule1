using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
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
                var processStartInfo = new ProcessStartInfo
                {
                    FileName = "dotnet",
                    Arguments = $"build \"{csprojFile}\" -c CrossCompat --verbosity normal",
                    WorkingDirectory = projectPath,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                var outputBuilder = new StringBuilder();
                var errorBuilder = new StringBuilder();

                using (var process = Process.Start(processStartInfo))
                {
                    if (process == null)
                    {
                        result.Success = false;
                        result.ErrorMessage = "Failed to start dotnet build process";
                        return result;
                    }

                    process.OutputDataReceived += (sender, e) =>
                    {
                        if (!string.IsNullOrEmpty(e.Data))
                        {
                            outputBuilder.AppendLine(e.Data);
                        }
                    };

                    process.ErrorDataReceived += (sender, e) =>
                    {
                        if (!string.IsNullOrEmpty(e.Data))
                        {
                            errorBuilder.AppendLine(e.Data);
                        }
                    };

                    process.BeginOutputReadLine();
                    process.BeginErrorReadLine();

                    process.WaitForExit();

                    result.ExitCode = process.ExitCode;
                    result.Output = outputBuilder.ToString();
                    result.ErrorOutput = errorBuilder.ToString();
                    result.Success = process.ExitCode == 0;

                    if (result.Success)
                    {
                        // Find the output DLL (CrossCompat builds to bin/CrossCompat/netstandard2.1)
                        var binPath = Path.Combine(projectPath, "bin", "CrossCompat", "netstandard2.1");
                        if (!Directory.Exists(binPath))
                        {
                            // Fallback to Release if CrossCompat doesn't exist
                            binPath = Path.Combine(projectPath, "bin", "Release", "netstandard2.1");
                        }

                        if (Directory.Exists(binPath))
                        {
                            // Get the mod name from the .csproj file
                            var modName = Path.GetFileNameWithoutExtension(csprojFile);
                            
                            // Known dependency DLLs to exclude
                            var dependencyDlls = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                            {
                                "0Harmony",
                                "MelonLoader",
                                "Newtonsoft.Json",
                                "Unity.TextMeshPro",
                                "UnityEngine.AssetBundleModule",
                                "UnityEngine.CoreModule",
                                "UnityEngine.JSONSerializeModule",
                                "UnityEngine.TextRenderingModule",
                                "UnityEngine.UI",
                                "UnityEngine.UIElementsModule",
                                "UnityEngine.UIModule",
                                "S1API",
                                "S1API.Forked"
                            };

                            // Find the mod DLL (should match the mod name, not dependencies)
                            var dllFiles = Directory.GetFiles(binPath, "*.dll");
                            var modDll = dllFiles.FirstOrDefault(dll =>
                            {
                                var dllName = Path.GetFileNameWithoutExtension(dll);
                                return dllName.Equals(modName, StringComparison.OrdinalIgnoreCase) &&
                                       !dependencyDlls.Contains(dllName);
                            });

                            // If exact match not found, try to find DLL that's not a known dependency
                            if (modDll == null)
                            {
                                modDll = dllFiles.FirstOrDefault(dll =>
                                {
                                    var dllName = Path.GetFileNameWithoutExtension(dll);
                                    return !dependencyDlls.Contains(dllName);
                                });
                            }

                            // Fallback to first DLL if still not found
                            result.OutputDllPath = modDll ?? dllFiles.FirstOrDefault();
                        }

                        // Optionally copy to game Mods folder if path is configured
                        if (result.Success && !string.IsNullOrEmpty(result.OutputDllPath) && settings != null && !string.IsNullOrEmpty(settings.GameInstallPath))
                        {
                            TryCopyToModsFolder(result.OutputDllPath, settings.GameInstallPath, result);
                        }
                    }
                    else
                    {
                        result.ErrorMessage = $"Build failed with exit code {process.ExitCode}";
                        // Combine output and error output for full log
                        var fullOutput = new StringBuilder();
                        if (outputBuilder.Length > 0)
                        {
                            fullOutput.AppendLine("=== Build Output ===");
                            fullOutput.AppendLine(outputBuilder.ToString());
                        }
                        if (errorBuilder.Length > 0)
                        {
                            fullOutput.AppendLine("=== Build Errors ===");
                            fullOutput.AppendLine(errorBuilder.ToString());
                        }
                        result.Output = fullOutput.ToString();
                    }
                }
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
        public bool DeployedToModsFolder { get; set; }
        public string? DeployedDllPath { get; set; }
        public List<string> Warnings { get; } = new List<string>();
    }
}

