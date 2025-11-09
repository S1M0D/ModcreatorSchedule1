using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Schedule1ModdingTool.Models;

namespace Schedule1ModdingTool.Services
{
    /// <summary>
    /// Service for building ModCreatorConnector and launching the game with it
    /// </summary>
    public class GameLaunchService
    {
        private Process? _gameProcess;
        private bool _isMonitoring;

        /// <summary>
        /// Builds ModCreatorConnector and launches the game
        /// </summary>
        /// <param name="settings">Mod settings containing game path</param>
        /// <param name="useLocalDll">If true, uses ConnectorLocal config (local DLL), otherwise ConnectorNuGet (NuGet package)</param>
        /// <returns>Result of the launch operation</returns>
        public GameLaunchResult LaunchGame(ModSettings settings, bool useLocalDll = false)
        {
            var result = new GameLaunchResult();

            if (settings == null || string.IsNullOrWhiteSpace(settings.GameInstallPath))
            {
                result.Success = false;
                result.ErrorMessage = "Game install path is not configured. Please set it in Settings.";
                return result;
            }

            var gameExePath = Path.Combine(settings.GameInstallPath, "Schedule I.exe");
            if (!File.Exists(gameExePath))
            {
                result.Success = false;
                result.ErrorMessage = $"Game executable not found at: {gameExePath}";
                return result;
            }

            try
            {
                // Get ModCreatorConnector project path
                var solutionPath = GetSolutionPath();
                if (string.IsNullOrEmpty(solutionPath))
                {
                    result.Success = false;
                    result.ErrorMessage = "Could not locate solution directory. ModCreatorConnector project not found.";
                    return result;
                }

                var connectorProjectPath = Path.Combine(solutionPath, "ModCreatorConnector");
                var connectorCsproj = Path.Combine(connectorProjectPath, "ModCreatorConnector.csproj");

                if (!File.Exists(connectorCsproj))
                {
                    result.Success = false;
                    result.ErrorMessage = $"ModCreatorConnector project not found at: {connectorCsproj}";
                    return result;
                }

                // Build ModCreatorConnector
                var config = useLocalDll ? "ConnectorLocal" : "ConnectorNuGet";
                result.BuildOutput = BuildConnectorMod(connectorCsproj, config, out var buildSuccess, out var buildError);

                if (!buildSuccess)
                {
                    result.Success = false;
                    result.ErrorMessage = $"Failed to build ModCreatorConnector: {buildError}";
                    return result;
                }

                // Find the built DLL
                var binPath = Path.Combine(connectorProjectPath, "bin", config, "netstandard2.1");
                var dllPath = Path.Combine(binPath, "ModCreatorConnector.dll");

                if (!File.Exists(dllPath))
                {
                    result.Success = false;
                    result.ErrorMessage = $"Built DLL not found at: {dllPath}";
                    return result;
                }

                // Copy DLL to Mods folder
                var modsPath = Path.Combine(settings.GameInstallPath, "Mods");
                if (!Directory.Exists(modsPath))
                {
                    Directory.CreateDirectory(modsPath);
                }

                var targetDllPath = Path.Combine(modsPath, "ModCreatorConnector.dll");
                
                // Only copy if DLL is newer or doesn't exist
                if (!File.Exists(targetDllPath) || File.GetLastWriteTime(dllPath) > File.GetLastWriteTime(targetDllPath))
                {
                    File.Copy(dllPath, targetDllPath, overwrite: true);
                    result.DllCopied = true;
                }
                else
                {
                    result.DllCopied = false;
                    result.Warnings.Add("DLL already up to date, skipping copy");
                }

                result.DeployedDllPath = targetDllPath;

                // Launch the game
                var processStartInfo = new ProcessStartInfo
                {
                    FileName = gameExePath,
                    WorkingDirectory = settings.GameInstallPath,
                    UseShellExecute = true
                };

                _gameProcess = Process.Start(processStartInfo);
                if (_gameProcess == null)
                {
                    result.Success = false;
                    result.ErrorMessage = "Failed to start game process";
                    return result;
                }

                result.GameProcessId = _gameProcess.Id;
                result.Success = true;

                // Start monitoring game process (optional cleanup)
                if (!_isMonitoring)
                {
                    _isMonitoring = true;
                    Task.Run(() => MonitorGameProcess(_gameProcess));
                }
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorMessage = $"Exception during launch: {ex.Message}";
            }

            return result;
        }

        private string GetSolutionPath()
        {
            // Try multiple approaches to find the solution directory
            
            // Approach 1: Walk up from current executable directory
            var currentDir = AppDomain.CurrentDomain.BaseDirectory;
            var directory = new DirectoryInfo(currentDir);

            while (directory != null)
            {
                var solutionFile = Path.Combine(directory.FullName, "Schedule1ModdingTool.sln");
                if (File.Exists(solutionFile))
                {
                    return directory.FullName;
                }
                directory = directory.Parent;
            }

            // Approach 2: Try relative path from common locations
            var relativePaths = new[]
            {
                Path.Combine("..", "..", ".."), // From bin/Debug/net8.0-windows
                Path.Combine("..", ".."),       // From bin/Debug
                ".."                            // From bin
            };

            foreach (var relativePath in relativePaths)
            {
                var testPath = Path.GetFullPath(Path.Combine(currentDir, relativePath));
                var solutionFile = Path.Combine(testPath, "Schedule1ModdingTool.sln");
                if (File.Exists(solutionFile))
                {
                    return testPath;
                }
            }

            return string.Empty;
        }

        private string BuildConnectorMod(string csprojPath, string configuration, out bool success, out string error)
        {
            success = false;
            error = string.Empty;
            var outputBuilder = new StringBuilder();
            var errorBuilder = new StringBuilder(); // Local variable for lambda capture

            try
            {
                var processStartInfo = new ProcessStartInfo
                {
                    FileName = "dotnet",
                    Arguments = $"build \"{csprojPath}\" -c {configuration} --verbosity minimal",
                    WorkingDirectory = Path.GetDirectoryName(csprojPath),
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                using (var process = Process.Start(processStartInfo))
                {
                    if (process == null)
                    {
                        error = "Failed to start dotnet build process";
                        return string.Empty;
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
                            outputBuilder.AppendLine(e.Data);
                            errorBuilder.AppendLine(e.Data);
                        }
                    };

                    process.BeginOutputReadLine();
                    process.BeginErrorReadLine();

                    process.WaitForExit();

                    success = process.ExitCode == 0;
                    error = errorBuilder.ToString();
                    if (!success && string.IsNullOrEmpty(error))
                    {
                        error = $"Build failed with exit code {process.ExitCode}";
                    }
                }
            }
            catch (Exception ex)
            {
                error = $"Exception during build: {ex.Message}";
            }

            return outputBuilder.ToString();
        }

        private void MonitorGameProcess(Process process)
        {
            try
            {
                process.WaitForExit();
                // Game has exited - cleanup could happen here if needed
                // For example, you could remove the DLL from Mods folder
            }
            catch
            {
                // Ignore errors during monitoring
            }
            finally
            {
                _isMonitoring = false;
                _gameProcess = null;
            }
        }

        /// <summary>
        /// Checks if the game is currently running
        /// </summary>
        public bool IsGameRunning()
        {
            if (_gameProcess != null && !_gameProcess.HasExited)
            {
                return true;
            }

            // Also check if game process exists by name
            var processes = Process.GetProcessesByName("Schedule I");
            return processes.Length > 0;
        }
    }

    /// <summary>
    /// Result of game launch operation
    /// </summary>
    public class GameLaunchResult
    {
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public string? BuildOutput { get; set; }
        public bool DllCopied { get; set; }
        public string? DeployedDllPath { get; set; }
        public int? GameProcessId { get; set; }
        public System.Collections.Generic.List<string> Warnings { get; } = new System.Collections.Generic.List<string>();
    }
}

