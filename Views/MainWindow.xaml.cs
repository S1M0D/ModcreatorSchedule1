using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Schedule1ModdingTool.ViewModels;

namespace Schedule1ModdingTool.Views
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            DataContext = new MainViewModel();
            
            // Set up key bindings
            SetupKeyBindings();
            
            // Set up code editor syntax highlighting
            if (CodeEditor != null)
            {
                CodeEditor.SyntaxHighlighting = ICSharpCode.AvalonEdit.Highlighting.HighlightingManager.Instance.GetDefinition("C#");
            }

            // Show wizard on startup if no project is loaded
            Loaded += MainWindow_Loaded;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            var vm = DataContext as MainViewModel;
            if (vm != null && string.IsNullOrWhiteSpace(vm.CurrentProject.ProjectName))
            {
                // Show startup dialog instead of directly showing wizard
                var startupDialog = new StartupDialog
                {
                    Owner = this
                };

                var result = startupDialog.ShowDialog();
                
                if (startupDialog.SelectedAction == StartupDialog.StartupAction.CreateNew)
                {
                    vm.NewProjectCommand.Execute(null);
                }
                else if (startupDialog.SelectedAction == StartupDialog.StartupAction.OpenExisting)
                {
                    vm.OpenProjectCommand.Execute(null);
                }
                else if (startupDialog.SelectedAction == StartupDialog.StartupAction.Exit)
                {
                    Application.Current.Shutdown();
                }
                else if (result == false)
                {
                    // User closed dialog without selecting an option
                    Application.Current.Shutdown();
                }
            }
        }

        private void SetupKeyBindings()
        {
            // Add keyboard shortcuts
            var vm = DataContext as MainViewModel;
            if (vm == null) return;

            // Ctrl+N - New Project
            InputBindings.Add(new KeyBinding(vm.NewProjectCommand, Key.N, ModifierKeys.Control));
            
            // Ctrl+O - Open Project
            InputBindings.Add(new KeyBinding(vm.OpenProjectCommand, Key.O, ModifierKeys.Control));
            
            // Ctrl+S - Save Project
            InputBindings.Add(new KeyBinding(vm.SaveProjectCommand, Key.S, ModifierKeys.Control));
            
            // Ctrl+Shift+S - Save Project As
            InputBindings.Add(new KeyBinding(vm.SaveProjectAsCommand, Key.S, ModifierKeys.Control | ModifierKeys.Shift));
            
            // F5 - Regenerate Code
            InputBindings.Add(new KeyBinding(vm.RegenerateCodeCommand, Key.F5, ModifierKeys.None));
            
            // F6 - Build Mod
            InputBindings.Add(new KeyBinding(vm.BuildModCommand, Key.F6, ModifierKeys.None));
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            var vm = DataContext as MainViewModel;
            if (vm?.CurrentProject.IsModified == true)
            {
                var result = MessageBox.Show(
                    "You have unsaved changes. Do you want to save them before closing?",
                    "Unsaved Changes",
                    MessageBoxButton.YesNoCancel,
                    MessageBoxImage.Question);

                switch (result)
                {
                    case MessageBoxResult.Yes:
                        vm.SaveProjectCommand.Execute(null);
                        break;
                    case MessageBoxResult.Cancel:
                        e.Cancel = true;
                        return;
                }
            }

            base.OnClosing(e);
        }

        private void AddQuestButton_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is MainViewModel vm && vm.AvailableBlueprints.Count > 0)
            {
                vm.AddQuestCommand.Execute(vm.AvailableBlueprints[0]);
            }
        }

        private void CloseTabButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is OpenElementTab tab && DataContext is MainViewModel vm)
            {
                vm.CloseTab(tab);
            }
        }
    }
}