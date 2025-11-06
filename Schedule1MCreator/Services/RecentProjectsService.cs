using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace Schedule1ModdingTool.Services
{
    /// <summary>
    /// Service for managing recent projects list
    /// </summary>
    public class RecentProjectsService
    {
        private const int MaxRecentProjects = 10;
        private readonly string _recentProjectsFile;

        public RecentProjectsService()
        {
            _recentProjectsFile = Utils.AppUtils.GetRecentProjectsFile();
        }

        /// <summary>
        /// Gets the list of recent project file paths
        /// </summary>
        public List<string> GetRecentProjects()
        {
            if (!File.Exists(_recentProjectsFile))
                return new List<string>();

            try
            {
                var json = File.ReadAllText(_recentProjectsFile);
                return JsonConvert.DeserializeObject<List<string>>(json) ?? new List<string>();
            }
            catch
            {
                return new List<string>();
            }
        }

        /// <summary>
        /// Adds a project to the recent projects list
        /// </summary>
        public void AddRecentProject(string projectPath)
        {
            var recent = GetRecentProjects();
            
            // Remove if already exists
            recent.Remove(projectPath);
            
            // Add to beginning
            recent.Insert(0, projectPath);
            
            // Limit to max count
            if (recent.Count > MaxRecentProjects)
            {
                recent.RemoveRange(MaxRecentProjects, recent.Count - MaxRecentProjects);
            }
            
            // Remove non-existent files
            recent.RemoveAll(path => !File.Exists(path));
            
            SaveRecentProjects(recent);
        }

        /// <summary>
        /// Removes a project from the recent projects list
        /// </summary>
        public void RemoveRecentProject(string projectPath)
        {
            var recent = GetRecentProjects();
            recent.Remove(projectPath);
            SaveRecentProjects(recent);
        }

        private void SaveRecentProjects(List<string> recentProjects)
        {
            try
            {
                var json = JsonConvert.SerializeObject(recentProjects, Formatting.Indented);
                var directory = Path.GetDirectoryName(_recentProjectsFile);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }
                File.WriteAllText(_recentProjectsFile, json);
            }
            catch
            {
                // Ignore save errors
            }
        }
    }
}