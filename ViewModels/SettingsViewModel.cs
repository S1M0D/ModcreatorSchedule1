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

        public ICommand SaveCommand => _saveCommand!;
        public ICommand CancelCommand => _cancelCommand!;
        public ICommand BrowseGamePathCommand => _browseGamePathCommand!;

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
        }

        private void SaveSettings()
        {
            Settings.Save();
            AppUtils.ShowInfo("Settings saved successfully.");
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

        public event System.Action? CloseRequested;
    }
}

