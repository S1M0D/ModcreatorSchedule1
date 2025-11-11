using System.Windows.Input;
using Schedule1ModdingTool.Models;
using Schedule1ModdingTool.Utils;

namespace Schedule1ModdingTool.ViewModels
{
    /// <summary>
    /// ViewModel for application settings
    /// </summary>
    public class SettingsViewModel : ObservableObject
    {
        private ModSettings _settings;

        public ModSettings Settings
        {
            get => _settings;
            set => SetProperty(ref _settings, value);
        }

        private ICommand? _saveCommand;
        private ICommand? _cancelCommand;
        private ICommand? _browseGamePathCommand;
        private ICommand? _browseWorkspacePathCommand;
        private ICommand? _browseS1ApiDllCommand;

        public ICommand SaveCommand => _saveCommand!;
        public ICommand CancelCommand => _cancelCommand!;
        public ICommand BrowseGamePathCommand => _browseGamePathCommand!;
        public ICommand BrowseWorkspacePathCommand => _browseWorkspacePathCommand!;
        public ICommand BrowseS1ApiDllCommand => _browseS1ApiDllCommand!;

        public SettingsViewModel()
        {
            _settings = ModSettings.Load();
            InitializeCommands();
        }

        private void InitializeCommands()
        {
            _saveCommand = new RelayCommand(SaveSettings);
            _cancelCommand = new RelayCommand(Cancel);
            _browseGamePathCommand = new RelayCommand(BrowseGamePath);
            _browseWorkspacePathCommand = new RelayCommand(BrowseWorkspacePath);
            _browseS1ApiDllCommand = new RelayCommand(BrowseS1ApiDllPath);
        }

        private void SaveSettings()
        {
            Settings.Save();
            CloseRequested?.Invoke();
        }

        private void Cancel()
        {
            // Reload settings to discard changes
            Settings = ModSettings.Load();
            CloseRequested?.Invoke();
        }

        private void BrowseGamePath()
        {
            using var dialog = new System.Windows.Forms.FolderBrowserDialog
            {
                Description = "Select Schedule I game installation folder",
                ShowNewFolderButton = false
            };

            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                Settings.GameInstallPath = dialog.SelectedPath;
            }
        }

        private void BrowseWorkspacePath()
        {
            using var dialog = new System.Windows.Forms.FolderBrowserDialog
            {
                Description = "Select default workspace folder for mod projects",
                ShowNewFolderButton = true
            };

            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                Settings.WorkspacePath = dialog.SelectedPath;
            }
        }

        private void BrowseS1ApiDllPath()
        {
            using var dialog = new System.Windows.Forms.OpenFileDialog
            {
                Title = "Select S1API.dll",
                Filter = "S1API.dll|S1API.dll|DLL files (*.dll)|*.dll|All files (*.*)|*.*",
                CheckFileExists = true
            };

            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                Settings.S1ApiDllPath = dialog.FileName;
            }
        }

        public event System.Action? CloseRequested;
    }
}

