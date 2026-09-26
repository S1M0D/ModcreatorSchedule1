using System.Diagnostics;
using System.IO;
using System.Text;
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
        private string? _deployedDllPath;

        /// <summary>
        /// Builds ModCreatorConnector and launches the game
        /// </summary>
        /// <param name="settings">Mod settings containing game path</param>
        /// <param name="useLocalDll">If true, uses ConnectorLocal config (local DLL), otherwise ConnectorNuGet (NuGet package)</param>
        /// <param name="previewEnabled">If true, enables NPC appearance preview mode</param>
        /// <returns>Result of the launch operation</returns>
        public GameLaunchResult LaunchGame(ModSettings settings, bool useLocalDll = false, bool previewEnabled = false)
        {
            var result = new GameLaunchResult();

            if (settings == null || string.IsNullOrWhiteSpace(settings.GameInstallPath))
            {
                result.Success = false;
                result.ErrorMessage = "Game install path is not configured. Please set it in Settings.";
                return result;
            }

            var resolvedGamePath = GameInstallPathResolver.TryResolve(settings.GameInstallPath, out var gameInstallPath)
                ? gameInstallPath
                : settings.GameInstallPath;

            var gameExePath = Path.Combine(resolvedGamePath, "Schedule I.exe");
            if (!File.Exists(gameExePath))
            {
                result.Success = false;
                result.ErrorMessage = $"Game executable not found at: {gameExePath}";
                return result;
            }

            try
            {
                var connectorProjectPath = FindConnectorFolder(settings.ConnectorFolderPath);
                if (connectorProjectPath == null)
                {
                    result.Success = false;
                    result.ErrorMessage = "Could not locate ModCreatorConnector.dll. Place the ModCreatorConnector folder beside the app or select it in Settings.";
                    return result;
                }

                var connectorCsproj = Path.Combine(connectorProjectPath, "ModCreatorConnector.csproj");
                var dllPath = Path.Combine(connectorProjectPath, "ModCreatorConnector.dll");
                // Published releases ship the compiled connector. Developers can still build from source
                // when no compiled connector is available.
                if (!File.Exists(dllPath) && File.Exists(connectorCsproj))
                {
                    var config = useLocalDll ? "ConnectorLocal" : "ConnectorNuGet";
                    result.BuildOutput = BuildConnectorMod(connectorCsproj, config, resolvedGamePath, settings.S1ApiDllPath, out var buildSuccess, out var buildError);
                    if (!buildSuccess)
                    {
                        result.Success = false;
                        result.ErrorMessage = $"Failed to build ModCreatorConnector: {buildError}";
                        return result;
                    }
                    dllPath = Path.Combine(connectorProjectPath, "bin", config, "netstandard2.1", "ModCreatorConnector.dll");
                }

                if (!File.Exists(dllPath))
                {
                    result.Success = false;
                    result.ErrorMessage = $"Built DLL not found at: {dllPath}";
                    return result;
                }

                // Copy DLL to Mods folder
                var modsPath = Path.Combine(resolvedGamePath, "Mods");
                if (!Directory.Exists(modsPath))
                {
                    Directory.CreateDirectory(modsPath);
                }

                var targetDllPath = Path.Combine(modsPath, "ModCreatorConnector.dll");
                
                // Compare content so an older preview DLL is replaced regardless of extracted timestamps.
                if (!File.Exists(targetDllPath) ||
                    !System.Security.Cryptography.SHA256.HashData(File.ReadAllBytes(dllPath)).AsSpan()
                        .SequenceEqual(System.Security.Cryptography.SHA256.HashData(File.ReadAllBytes(targetDllPath))))
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
                _deployedDllPath = targetDllPath; // Store for cleanup

                // Write preview config file
                WritePreviewConfig(modsPath, previewEnabled);

                // Launch the game
                var processStartInfo = new ProcessStartInfo
                {
                    FileName = gameExePath,
                    WorkingDirectory = resolvedGamePath,
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

        internal static string? FindConnectorFolder(string? configuredPath, string? appDirectory = null)
        {
            static string? Check(string? path)
            {
                if (string.IsNullOrWhiteSpace(path)) return null;
                var folder = path.Trim().Trim('"');
                if (File.Exists(folder)) folder = Path.GetDirectoryName(folder)!;
                var child = Path.Combine(folder, "ModCreatorConnector");
                if (Directory.Exists(child)) folder = child;
                return File.Exists(Path.Combine(folder, "ModCreatorConnector.dll")) ||
                       File.Exists(Path.Combine(folder, "ModCreatorConnector.csproj")) ? folder : null;
            }

            var explicitFolder = Check(configuredPath);
            if (explicitFolder != null) return explicitFolder;
            var directory = new DirectoryInfo(appDirectory ?? AppContext.BaseDirectory);
            while (directory != null)
            {
                var found = Check(directory.FullName);
                if (found != null) return found;
                directory = directory.Parent;
            }
            return null;
        }

        private string BuildConnectorMod(string csprojPath, string configuration, string gamePath, string? s1ApiDllPath, out bool success, out string error)
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
                    WorkingDirectory = Path.GetDirectoryName(csprojPath),
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };
                processStartInfo.ArgumentList.Add("build");
                processStartInfo.ArgumentList.Add(csprojPath);
                processStartInfo.ArgumentList.Add("-c");
                processStartInfo.ArgumentList.Add(configuration);
                processStartInfo.ArgumentList.Add("--verbosity");
                processStartInfo.ArgumentList.Add("minimal");
                processStartInfo.ArgumentList.Add($"-p:GamePath={gamePath}");
                if (configuration == "ConnectorLocal" && !string.IsNullOrWhiteSpace(s1ApiDllPath))
                    processStartInfo.ArgumentList.Add($"-p:S1ApiLocalDllPath={s1ApiDllPath}");

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

        private void WritePreviewConfig(string modsPath, bool previewEnabled)
        {
            try
            {
                var configPath = Path.Combine(modsPath, "ModCreatorConnector_Preview.json");
                var config = new
                {
                    PreviewEnabled = previewEnabled
                };
                var json = Newtonsoft.Json.JsonConvert.SerializeObject(config, Newtonsoft.Json.Formatting.Indented);
                File.WriteAllText(configPath, json);
            }
            catch (Exception ex)
            {
                // Log but don't fail launch if config write fails
                System.Diagnostics.Debug.WriteLine($"Failed to write preview config: {ex.Message}");
            }
        }

        private void MonitorGameProcess(Process process)
        {
            try
            {
                process.WaitForExit();
                
                // Cleanup: Remove connector mod DLL after game exits
                if (!string.IsNullOrEmpty(_deployedDllPath) && File.Exists(_deployedDllPath))
                {
                    try
                    {
                        // Wait a bit to ensure file handles are released
                        System.Threading.Thread.Sleep(500);
                        
                        File.Delete(_deployedDllPath);
                        System.Diagnostics.Debug.WriteLine($"GameLaunchService: Removed connector mod DLL: {_deployedDllPath}");
                    }
                    catch (Exception ex)
                    {
                        // DLL may be locked or already deleted - log but don't fail
                        System.Diagnostics.Debug.WriteLine($"GameLaunchService: Could not delete connector mod DLL: {ex.Message}");
                    }
                }
            }
            catch
            {
                // Ignore errors during monitoring
            }
            finally
            {
                _isMonitoring = false;
                _gameProcess = null;
                _deployedDllPath = null;
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

