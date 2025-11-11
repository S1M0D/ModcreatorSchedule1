using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using Schedule1ModdingTool.Models;
using Schedule1ModdingTool.Services;
using Schedule1ModdingTool.Utils;
using Schedule1ModdingTool.ViewModels;

namespace Schedule1ModdingTool.Views
{
    /// <summary>
    /// Interaction logic for StartupDialog.xaml
    /// </summary>
    public partial class StartupDialog : Window
    {
        public enum StartupAction
        {
            None,
            CreateNew,
            OpenExisting,
            Exit
        }

        public StartupAction SelectedAction { get; private set; } = StartupAction.None;
        public QuestProject? SelectedProject { get; private set; }

        public StartupDialog()
        {
            InitializeComponent();
        }

        private void CreateNewProject_Click(object sender, MouseButtonEventArgs e)
        {
            var wizardVm = new NewProjectWizardViewModel();
            var wizardWindow = new NewProjectWizardWindow
            {
                DataContext = wizardVm,
                Owner = this
            };

            bool wizardCompleted = false;

            wizardVm.ProjectCreated += (vm) =>
            {
                try
                {
                    // Create the project folder if needed
                    var fullPath = vm.FullProjectPath;
                    if (!Directory.Exists(fullPath))
                    {
                        Directory.CreateDirectory(fullPath);
                    }

                    // Create new project with defaults from wizard
                    var newProject = new QuestProject
                    {
                        ProjectName = vm.ModName,
                        ProjectDescription = $"Mod project for {vm.ModName}"
                    };

                    // Set default values for all quests from wizard
                    var settings = Models.ModSettings.Load();
                    settings.DefaultModNamespace = vm.ModNamespace;
                    settings.DefaultModAuthor = vm.ModAuthor;
                    settings.DefaultModVersion = vm.ModVersion;
                    settings.Save();

                    // Set project file path
                    var projectFilePath = Path.Combine(fullPath, $"{AppUtils.MakeSafeFilename(vm.ModName)}.qproj");
                    newProject.FilePath = projectFilePath;

                    // Save the project file
                    newProject.SaveToFile(projectFilePath);

                    // Load it back to ensure proper initialization
                    SelectedProject = QuestProject.LoadFromFile(projectFilePath) ?? newProject;

                    wizardCompleted = true;
                    wizardWindow.DialogResult = true;
                    wizardWindow.Close();

                    SelectedAction = StartupAction.CreateNew;
                    DialogResult = true;
                    Close();
                }
                catch (Exception ex)
                {
                    AppUtils.ShowError($"Failed to create project: {ex.Message}");
                }
            };

            wizardVm.WizardCancelled += () =>
            {
                wizardWindow.DialogResult = false;
                wizardWindow.Close();
                // Keep StartupDialog open if cancelled
            };

            wizardWindow.ShowDialog();
        }

        private void OpenProject_Click(object sender, MouseButtonEventArgs e)
        {
            try
            {
                var dialog = new Microsoft.Win32.OpenFileDialog
                {
                    Filter = "Quest Project files (*.qproj)|*.qproj|All files (*.*)|*.*",
                    Title = "Open Quest Project",
                    CheckFileExists = true
                };

                if (dialog.ShowDialog(this) == true)
                {
                    var project = QuestProject.LoadFromFile(dialog.FileName);
                    
                    if (project != null)
                    {
                        SelectedProject = project;
                        SelectedAction = StartupAction.OpenExisting;
                        // Set DialogResult before closing to ensure App.xaml.cs sees it
                        DialogResult = true;
                        // Don't call Close() explicitly - setting DialogResult will close it
                    }
                    else
                    {
                        AppUtils.ShowError("Failed to load the project file. The file may be corrupted or invalid.");
                    }
                }
                // If dialog.ShowDialog returns false (user cancelled), keep StartupDialog open
            }
            catch (Exception ex)
            {
                AppUtils.ShowError($"Error opening project: {ex.Message}");
            }
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            SelectedAction = StartupAction.Exit;
            DialogResult = false;
            Close();
        }

        private void GitHub_Click(object sender, RoutedEventArgs e)
        {
            OpenUrl("https://github.com/ESTONlA/ModcreatorSchedule1");
        }

        private void Discord_Click(object sender, RoutedEventArgs e)
        {
            OpenUrl("https://discord.gg/MfQMVj4VYy");
        }

        private void Issues_Click(object sender, RoutedEventArgs e)
        {
            OpenUrl("https://github.com/ESTONlA/ModcreatorSchedule1/issues");
        }

        private void Donate_Click(object sender, RoutedEventArgs e)
        {
            OpenUrl("https://ko-fi.com");
        }

        private void Preferences_Click(object sender, RoutedEventArgs e)
        {
            var settingsVm = new ViewModels.SettingsViewModel();
            var settingsWindow = new SettingsWindow
            {
                Owner = this,
                DataContext = settingsVm
            };

            settingsVm.CloseRequested += () => settingsWindow.Close();
            settingsWindow.ShowDialog();
        }

        private void OpenUrl(string url)
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true
                });
            }
            catch
            {
                // Silently fail if URL cannot be opened
            }
        }

        private void ActionButton_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (sender is System.Windows.Controls.Border border)
            {
                border.BorderBrush = (SolidColorBrush)FindResource("AccentOrangeBrush");
                border.Background = (SolidColorBrush)FindResource("DarkHoverBrush");
            }
        }

        private void ActionButton_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (sender is System.Windows.Controls.Border border)
            {
                border.BorderBrush = (SolidColorBrush)FindResource("DarkBorderBrush");
                border.Background = (SolidColorBrush)FindResource("DarkBackgroundBrush");
            }
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            // Only drag if clicking on non-interactive elements
            if (e.ChangedButton == MouseButton.Left && e.ButtonState == MouseButtonState.Pressed)
            {
                // Check if the original source or its parent is a button or interactive control
                var originalSource = e.OriginalSource as DependencyObject;
                if (originalSource != null)
                {
                    // Walk up the visual tree to check if we clicked on a button or action border
                    var current = originalSource;
                    while (current != null)
                    {
                        if (current is Button || 
                            (current is System.Windows.Controls.Border border && border.Cursor == System.Windows.Input.Cursors.Hand))
                        {
                            return; // Don't drag if clicking on a button or action button
                        }
                        current = System.Windows.Media.VisualTreeHelper.GetParent(current);
                    }
                }
                
                // Verify mouse button is still pressed before calling DragMove
                if (Mouse.LeftButton == MouseButtonState.Pressed)
                {
                    e.Handled = true;
                    DragMove();
                }
            }
        }
    }
}

