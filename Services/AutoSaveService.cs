using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;
using Newtonsoft.Json;
using Schedule1ModdingTool.Models;

namespace Schedule1ModdingTool.Services
{
    /// <summary>
    /// Provides automatic periodic saving of projects to prevent data loss.
    /// Creates recovery snapshots in a temporary directory.
    /// </summary>
    public class AutoSaveService : IDisposable
    {
        private readonly DispatcherTimer _autoSaveTimer;
        private QuestProject? _currentProject;
        private string? _currentProjectPath;
        private bool _isEnabled;
        private int _autoSaveIntervalSeconds;
        private readonly string _autoSaveDirectory;
        private readonly object _lock = new object();
        private bool _disposed;

        public event EventHandler<AutoSaveEventArgs>? AutoSaveCompleted;
        public event EventHandler<AutoSaveErrorEventArgs>? AutoSaveError;

        /// <summary>
        /// Gets or sets whether auto-save is enabled.
        /// </summary>
        public bool IsEnabled
        {
            get => _isEnabled;
            set
            {
                if (_isEnabled != value)
                {
                    _isEnabled = value;
                    if (_isEnabled)
                        _autoSaveTimer.Start();
                    else
                        _autoSaveTimer.Stop();
                }
            }
        }

        /// <summary>
        /// Gets or sets the auto-save interval in seconds.
        /// </summary>
        public int AutoSaveIntervalSeconds
        {
            get => _autoSaveIntervalSeconds;
            set
            {
                if (value < 10)
                    throw new ArgumentException("Auto-save interval must be at least 10 seconds", nameof(value));

                _autoSaveIntervalSeconds = value;
                _autoSaveTimer.Interval = TimeSpan.FromSeconds(value);
            }
        }

        /// <summary>
        /// Gets the directory where auto-save files are stored.
        /// </summary>
        public string AutoSaveDirectory => _autoSaveDirectory;

        public AutoSaveService()
        {
            // Create auto-save directory in AppData
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            _autoSaveDirectory = Path.Combine(appDataPath, "Schedule1ModdingTool", "AutoSave");
            Directory.CreateDirectory(_autoSaveDirectory);

            _autoSaveIntervalSeconds = 60; // Default: 1 minute
            _isEnabled = true;

            _autoSaveTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(_autoSaveIntervalSeconds)
            };
            _autoSaveTimer.Tick += OnAutoSaveTimerTick;
        }

        /// <summary>
        /// Sets the current project to auto-save.
        /// </summary>
        public void SetCurrentProject(QuestProject? project, string? projectPath)
        {
            lock (_lock)
            {
                _currentProject = project;
                _currentProjectPath = projectPath;

                // Start timer if we have a project and auto-save is enabled
                if (_currentProject != null && _isEnabled)
                    _autoSaveTimer.Start();
                else
                    _autoSaveTimer.Stop();
            }
        }

        /// <summary>
        /// Manually triggers an auto-save.
        /// </summary>
        public async Task SaveNowAsync()
        {
            await PerformAutoSaveAsync();
        }

        private async void OnAutoSaveTimerTick(object? sender, EventArgs e)
        {
            await PerformAutoSaveAsync();
        }

        private async Task PerformAutoSaveAsync()
        {
            QuestProject? projectToSave;
            string? projectPath;

            lock (_lock)
            {
                projectToSave = _currentProject;
                projectPath = _currentProjectPath;
            }

            // Don't auto-save if no project or project has no changes
            if (projectToSave == null || !projectToSave.IsModified)
                return;

            try
            {
                await Task.Run(() =>
                {
                    // Generate auto-save filename
                    var fileName = GetAutoSaveFileName(projectPath);
                    var autoSavePath = Path.Combine(_autoSaveDirectory, fileName);

                    // Serialize project
                    var json = JsonConvert.SerializeObject(projectToSave, Formatting.Indented);
                    File.WriteAllText(autoSavePath, json);

                    // Create session marker file (used for crash detection)
                    var sessionMarkerPath = Path.Combine(_autoSaveDirectory, "session.marker");
                    File.WriteAllText(sessionMarkerPath, autoSavePath);

                    // Cleanup old auto-save files (keep last 5)
                    CleanupOldAutoSaves(fileName);
                });

                AutoSaveCompleted?.Invoke(this, new AutoSaveEventArgs
                {
                    ProjectName = projectToSave.ProjectName,
                    Timestamp = DateTime.Now
                });
            }
            catch (Exception ex)
            {
                AutoSaveError?.Invoke(this, new AutoSaveErrorEventArgs
                {
                    Exception = ex,
                    ProjectName = projectToSave?.ProjectName ?? "Unknown"
                });
            }
        }

        private string GetAutoSaveFileName(string? originalPath)
        {
            string baseName;
            if (!string.IsNullOrEmpty(originalPath))
            {
                baseName = Path.GetFileNameWithoutExtension(originalPath);
            }
            else
            {
                baseName = "UntitledProject";
            }

            // Include timestamp for uniqueness
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            return $"{baseName}_autosave_{timestamp}.s1proj";
        }

        private void CleanupOldAutoSaves(string currentFileName)
        {
            try
            {
                // Extract base name (without timestamp)
                var baseName = currentFileName.Substring(0, currentFileName.IndexOf("_autosave_"));
                var pattern = $"{baseName}_autosave_*.s1proj";

                var files = Directory.GetFiles(_autoSaveDirectory, pattern);

                // Sort by creation time, newest first
                Array.Sort(files, (a, b) => File.GetCreationTime(b).CompareTo(File.GetCreationTime(a)));

                // Delete all but the most recent 5
                for (int i = 5; i < files.Length; i++)
                {
                    try
                    {
                        File.Delete(files[i]);
                    }
                    catch
                    {
                        // Ignore errors deleting old files
                    }
                }
            }
            catch
            {
                // Ignore cleanup errors
            }
        }

        /// <summary>
        /// Clears the session marker to indicate clean shutdown.
        /// Call this when the application closes normally.
        /// </summary>
        public void ClearSessionMarker()
        {
            try
            {
                var sessionMarkerPath = Path.Combine(_autoSaveDirectory, "session.marker");
                if (File.Exists(sessionMarkerPath))
                {
                    File.Delete(sessionMarkerPath);
                }
            }
            catch
            {
                // Ignore errors
            }
        }

        /// <summary>
        /// Clears all auto-save files for the current project.
        /// </summary>
        public void ClearAutoSaves(string? projectPath)
        {
            if (string.IsNullOrEmpty(projectPath))
                return;

            try
            {
                var baseName = Path.GetFileNameWithoutExtension(projectPath);
                var pattern = $"{baseName}_autosave_*.s1proj";
                var files = Directory.GetFiles(_autoSaveDirectory, pattern);

                foreach (var file in files)
                {
                    try
                    {
                        File.Delete(file);
                    }
                    catch
                    {
                        // Ignore errors
                    }
                }
            }
            catch
            {
                // Ignore errors
            }
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _autoSaveTimer?.Stop();
            _disposed = true;
        }
    }

    public class AutoSaveEventArgs : EventArgs
    {
        public string ProjectName { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
    }

    public class AutoSaveErrorEventArgs : EventArgs
    {
        public Exception Exception { get; set; } = null!;
        public string ProjectName { get; set; } = string.Empty;
    }
}
