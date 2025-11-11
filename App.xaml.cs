using System;
using System.IO;
using System.Windows;
using Schedule1ModdingTool.Models;
using Schedule1ModdingTool.Services;
using Schedule1ModdingTool.ViewModels;
using Schedule1ModdingTool.Views;

namespace Schedule1ModdingTool
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private void Application_Startup(object sender, StartupEventArgs e)
        {
            // Check if first start wizard needs to be shown
            var settings = ModSettings.Load();
            if (!settings.IsFirstStartComplete)
            {
                var firstStartWizard = new FirstStartWizardWindow();
                var wizardResult = firstStartWizard.ShowDialog();
                if (wizardResult != true)
                {
                    // User cancelled first start wizard - exit application
                    Shutdown();
                    return;
                }
            }

            // Check for crash recovery
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var autoSaveDirectory = Path.Combine(appDataPath, "Schedule1ModdingTool", "AutoSave");
            var crashRecoveryService = new CrashRecoveryService(autoSaveDirectory);

            QuestProject? recoveredProject = null;

            if (crashRecoveryService.HasRecoverableSession())
            {
                var recoveryWindow = new CrashRecoveryWindow(crashRecoveryService);
                var recoveryResult = recoveryWindow.ShowDialog();

                if (recoveryResult == true && recoveryWindow.RecoveredProject != null)
                {
                    // User recovered a project - open it directly
                    recoveredProject = recoveryWindow.RecoveredProject;
                }
                else if (!recoveryWindow.UserSkippedRecovery)
                {
                    // User closed recovery or handled all projects - cleanup old auto-saves
                    crashRecoveryService.CleanupOldAutoSaves();
                }
            }

            // If we recovered a project, open it directly
            if (recoveredProject != null)
            {
                try
                {
                    var mainWindow = new MainWindow(recoveredProject);
                    MainWindow = mainWindow;
                    mainWindow.Show();
                    return;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to open recovered project: {ex.Message}",
                        "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    // Continue to startup dialog on error
                }
            }

            // Show the startup dialog
            var startupDialog = new StartupDialog();
            var result = startupDialog.ShowDialog();

            // If user selected a project, show the main window with that project
            if (result == true && startupDialog.SelectedProject != null)
            {
                try
                {
                    var mainWindow = new MainWindow(startupDialog.SelectedProject);
                    MainWindow = mainWindow; // Set as the application's main window
                    mainWindow.Show();
                    // Ensure the window stays open - don't shutdown
                    return;
                }
                catch (Exception ex)
                {
                    // If MainWindow creation fails, show error and shutdown
                    MessageBox.Show($"Failed to open project: {ex.Message}\n\nStack trace: {ex.StackTrace}",
                        "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    Shutdown();
                    return;
                }
            }

            // User chose to exit or closed the dialog without selecting a project
            Shutdown();
        }
    }
}