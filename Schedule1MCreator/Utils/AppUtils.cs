using System;
using System.IO;
using System.Windows;

namespace Schedule1ModdingTool.Utils
{
    /// <summary>
    /// Utility class for common application operations
    /// </summary>
    public static class AppUtils
    {
        /// <summary>
        /// Gets the application data directory for storing user files
        /// </summary>
        public static string GetAppDataDirectory()
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var appDir = Path.Combine(appData, "Schedule1ModdingTool");
            
            if (!Directory.Exists(appDir))
            {
                Directory.CreateDirectory(appDir);
            }
            
            return appDir;
        }

        /// <summary>
        /// Gets the directory for storing recent projects
        /// </summary>
        public static string GetRecentProjectsFile()
        {
            return Path.Combine(GetAppDataDirectory(), "recent_projects.json");
        }

        /// <summary>
        /// Gets the user's desktop directory for saving compiled DLLs
        /// </summary>
        public static string GetDesktopDirectory()
        {
            return Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
        }

        /// <summary>
        /// Shows an error message to the user
        /// </summary>
        public static void ShowError(string message, string title = "Error")
        {
            MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Error);
        }

        /// <summary>
        /// Shows an information message to the user
        /// </summary>
        public static void ShowInfo(string message, string title = "Information")
        {
            MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Information);
        }

        /// <summary>
        /// Shows a warning message to the user
        /// </summary>
        public static void ShowWarning(string message, string title = "Warning")
        {
            MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        /// <summary>
        /// Asks the user a yes/no question
        /// </summary>
        public static bool AskYesNo(string message, string title = "Question")
        {
            var result = MessageBox.Show(message, title, MessageBoxButton.YesNo, MessageBoxImage.Question);
            return result == MessageBoxResult.Yes;
        }

        /// <summary>
        /// Validates that a string is a valid C# identifier
        /// </summary>
        public static bool IsValidCSharpIdentifier(string identifier)
        {
            if (string.IsNullOrWhiteSpace(identifier))
                return false;

            // Check first character
            if (!char.IsLetter(identifier[0]) && identifier[0] != '_')
                return false;

            // Check remaining characters
            for (int i = 1; i < identifier.Length; i++)
            {
                if (!char.IsLetterOrDigit(identifier[i]) && identifier[i] != '_')
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Generates a safe filename from a given string
        /// </summary>
        public static string MakeSafeFilename(string filename)
        {
            if (string.IsNullOrWhiteSpace(filename))
                return "Untitled";

            var invalid = Path.GetInvalidFileNameChars();
            var result = filename;
            
            foreach (var c in invalid)
            {
                result = result.Replace(c, '_');
            }

            return result;
        }
    }
}