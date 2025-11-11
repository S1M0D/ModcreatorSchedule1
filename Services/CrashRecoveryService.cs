using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Schedule1ModdingTool.Models;

namespace Schedule1ModdingTool.Services
{
    /// <summary>
    /// Detects crashes and provides recovery functionality for unsaved projects.
    /// </summary>
    public class CrashRecoveryService
    {
        private readonly string _autoSaveDirectory;

        public CrashRecoveryService(string autoSaveDirectory)
        {
            _autoSaveDirectory = autoSaveDirectory ?? throw new ArgumentNullException(nameof(autoSaveDirectory));
        }

        /// <summary>
        /// Checks if there was a crash with unsaved changes.
        /// </summary>
        public bool HasRecoverableSession()
        {
            var sessionMarkerPath = Path.Combine(_autoSaveDirectory, "session.marker");
            return File.Exists(sessionMarkerPath);
        }

        /// <summary>
        /// Gets the list of recoverable projects from the last session.
        /// </summary>
        public List<RecoverableProject> GetRecoverableProjects()
        {
            var recoverableProjects = new List<RecoverableProject>();

            try
            {
                var sessionMarkerPath = Path.Combine(_autoSaveDirectory, "session.marker");
                if (!File.Exists(sessionMarkerPath))
                    return recoverableProjects;

                // Read the last auto-saved file from session marker
                var lastAutoSavePath = File.ReadAllText(sessionMarkerPath).Trim();

                if (File.Exists(lastAutoSavePath))
                {
                    var fileInfo = new FileInfo(lastAutoSavePath);
                    var fileName = Path.GetFileNameWithoutExtension(fileInfo.Name);

                    // Extract project name from filename (format: ProjectName_autosave_timestamp)
                    var projectName = fileName.Substring(0, fileName.IndexOf("_autosave_"));

                    recoverableProjects.Add(new RecoverableProject
                    {
                        ProjectName = projectName,
                        AutoSaveFilePath = lastAutoSavePath,
                        Timestamp = fileInfo.LastWriteTime,
                        FileSizeBytes = fileInfo.Length
                    });
                }

                // Also check for any other recent auto-save files (within last hour)
                var autoSaveFiles = Directory.GetFiles(_autoSaveDirectory, "*_autosave_*.s1proj");
                var recentFiles = autoSaveFiles
                    .Where(f => f != lastAutoSavePath) // Don't duplicate the main one
                    .Select(f => new FileInfo(f))
                    .Where(fi => (DateTime.Now - fi.LastWriteTime).TotalHours < 1)
                    .OrderByDescending(fi => fi.LastWriteTime)
                    .Take(5); // Limit to 5 most recent

                foreach (var fileInfo in recentFiles)
                {
                    var fileName = Path.GetFileNameWithoutExtension(fileInfo.Name);
                    var projectName = fileName.Substring(0, fileName.IndexOf("_autosave_"));

                    // Don't add duplicates
                    if (!recoverableProjects.Any(rp => rp.ProjectName == projectName))
                    {
                        recoverableProjects.Add(new RecoverableProject
                        {
                            ProjectName = projectName,
                            AutoSaveFilePath = fileInfo.FullName,
                            Timestamp = fileInfo.LastWriteTime,
                            FileSizeBytes = fileInfo.Length
                        });
                    }
                }
            }
            catch
            {
                // If we can't read recoverable projects, return empty list
            }

            return recoverableProjects;
        }

        /// <summary>
        /// Recovers a project from an auto-save file.
        /// </summary>
        public QuestProject? RecoverProject(string autoSaveFilePath)
        {
            try
            {
                if (!File.Exists(autoSaveFilePath))
                    return null;

                var json = File.ReadAllText(autoSaveFilePath);
                var project = JsonConvert.DeserializeObject<QuestProject>(json);

                if (project != null)
                {
                    // Mark as modified since this is a recovery
                    project.IsModified = true;
                }

                return project;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Discards a recoverable project by deleting its auto-save file.
        /// </summary>
        public void DiscardRecovery(string autoSaveFilePath)
        {
            try
            {
                if (File.Exists(autoSaveFilePath))
                {
                    File.Delete(autoSaveFilePath);
                }
            }
            catch
            {
                // Ignore errors
            }
        }

        /// <summary>
        /// Clears the session marker after recovery has been handled.
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
        /// Cleans up old auto-save files (older than 7 days).
        /// </summary>
        public void CleanupOldAutoSaves()
        {
            try
            {
                var autoSaveFiles = Directory.GetFiles(_autoSaveDirectory, "*_autosave_*.s1proj");
                var cutoffDate = DateTime.Now.AddDays(-7);

                foreach (var file in autoSaveFiles)
                {
                    try
                    {
                        var fileInfo = new FileInfo(file);
                        if (fileInfo.LastWriteTime < cutoffDate)
                        {
                            File.Delete(file);
                        }
                    }
                    catch
                    {
                        // Ignore errors deleting individual files
                    }
                }
            }
            catch
            {
                // Ignore cleanup errors
            }
        }
    }

    /// <summary>
    /// Represents a project that can be recovered from an auto-save.
    /// </summary>
    public class RecoverableProject
    {
        public string ProjectName { get; set; } = string.Empty;
        public string AutoSaveFilePath { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public long FileSizeBytes { get; set; }

        public string DisplayName => $"{ProjectName} ({Timestamp:g})";
        public string SizeDisplay => FormatFileSize(FileSizeBytes);

        private string FormatFileSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB" };
            double len = bytes;
            int order = 0;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }
            return $"{len:0.##} {sizes[order]}";
        }
    }
}
