using System.IO;
using Microsoft.Win32;
using Schedule1ModdingTool.Models;

namespace Schedule1ModdingTool.Services
{
    /// <summary>
    /// Service for handling project file operations
    /// </summary>
    public class ProjectService
    {
        private const string FileFilter = "Quest Project files (*.qproj)|*.qproj|All files (*.*)|*.*";

        public QuestProject? OpenProject()
        {
            var dialog = new OpenFileDialog
            {
                Filter = FileFilter,
                Title = "Open Quest Project",
                CheckFileExists = true
            };

            if (dialog.ShowDialog() == true)
            {
                return QuestProject.LoadFromFile(dialog.FileName);
            }

            return null;
        }

        public bool SaveProject(QuestProject project)
        {
            if (string.IsNullOrEmpty(project.FilePath))
            {
                return SaveProjectAs(project);
            }

            project.SaveToFile(project.FilePath);
            return true;
        }

        public bool SaveProjectAs(QuestProject project)
        {
            var dialog = new SaveFileDialog
            {
                Filter = FileFilter,
                Title = "Save Quest Project As",
                DefaultExt = "qproj",
                FileName = string.IsNullOrEmpty(project.ProjectName) ? "Untitled" : project.ProjectName
            };

            if (dialog.ShowDialog() == true)
            {
                project.SaveToFile(dialog.FileName);
                return true;
            }

            return false;
        }

        public void ExportCode(string code, string suggestedFileName)
        {
            var dialog = new SaveFileDialog
            {
                Filter = "C# files (*.cs)|*.cs|All files (*.*)|*.*",
                Title = "Export Generated Code",
                DefaultExt = "cs",
                FileName = suggestedFileName
            };

            if (dialog.ShowDialog() == true)
            {
                File.WriteAllText(dialog.FileName, code);
            }
        }
    }
}