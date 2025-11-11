using System.Windows;
using Schedule1ModdingTool.Models;
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
                catch (System.Exception ex)
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