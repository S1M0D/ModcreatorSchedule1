using System.IO;
using System.Windows.Input;
using Schedule1ModdingTool.Models;
using Schedule1ModdingTool.Services;
using Schedule1ModdingTool.Utils;

namespace Schedule1ModdingTool.ViewModels
{
    /// <summary>
    /// ViewModel for the New Project Wizard
    /// </summary>
    public class NewProjectWizardViewModel : ObservableObject
    {
        private string _modName = "";
        private string _projectPath = "";
        private string _modAuthor = "";
        private string _modVersion = "1.0.0";
        private string _modNamespace = "";

        public string ModName
        {
            get => _modName;
            set
            {
                if (SetProperty(ref _modName, value))
                {
                    // Auto-update namespace based on mod name
                    if (string.IsNullOrWhiteSpace(_modNamespace) || _modNamespace == MakeSafeNamespace(_previousModName))
                    {
                        _modNamespace = MakeSafeNamespace(value);
                        OnPropertyChanged(nameof(ModNamespace));
                    }
                    _previousModName = value;
                    CommandManager.InvalidateRequerySuggested();
                    OnPropertyChanged(nameof(FullProjectPath));
                }
            }
        }

        private string _previousModName = "";

        public string ProjectPath
        {
            get => _projectPath;
            set
            {
                SetProperty(ref _projectPath, value);
                CommandManager.InvalidateRequerySuggested();
                OnPropertyChanged(nameof(FullProjectPath));
            }
        }

        public string ModAuthor
        {
            get => _modAuthor;
            set => SetProperty(ref _modAuthor, value);
        }

        public string ModVersion
        {
            get => _modVersion;
            set => SetProperty(ref _modVersion, value);
        }

        public string ModNamespace
        {
            get => _modNamespace;
            set => SetProperty(ref _modNamespace, value);
        }

        public string FullProjectPath
        {
            get
            {
                if (string.IsNullOrWhiteSpace(ProjectPath) || string.IsNullOrWhiteSpace(ModName))
                    return "";

                // Always create folder named after mod
                return Path.Combine(ProjectPath, MakeSafeFilename(ModName));
            }
        }

        private ICommand? _browsePathCommand;
        private ICommand? _createCommand;
        private ICommand? _cancelCommand;

        public ICommand BrowsePathCommand => _browsePathCommand!;
        public ICommand CreateCommand => _createCommand!;
        public ICommand CancelCommand => _cancelCommand!;

        public NewProjectWizardViewModel()
        {
            // Load defaults from settings
            var settings = ModSettings.Load();
            _modAuthor = settings.DefaultModAuthor;
            _modVersion = settings.DefaultModVersion;
            _modNamespace = settings.DefaultModNamespace;

            InitializeCommands();
        }

        private void InitializeCommands()
        {
            _browsePathCommand = new RelayCommand(BrowsePath);
            _createCommand = new RelayCommand(CreateProject, CanCreateProject);
            _cancelCommand = new RelayCommand(Cancel);
        }

        private void BrowsePath()
        {
            using var dialog = new System.Windows.Forms.FolderBrowserDialog
            {
                Description = "Select folder for mod project",
                ShowNewFolderButton = true
            };

            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                ProjectPath = dialog.SelectedPath;
            }
        }

        private bool CanCreateProject()
        {
            return !string.IsNullOrWhiteSpace(ModName) &&
                   !string.IsNullOrWhiteSpace(ProjectPath) &&
                   !string.IsNullOrWhiteSpace(ModNamespace) &&
                   Directory.Exists(ProjectPath);
        }

        private void CreateProject()
        {
            if (!CanCreateProject())
                return;

            var fullPath = FullProjectPath;
            if (Directory.Exists(fullPath) && Directory.GetFiles(fullPath, "*", SearchOption.AllDirectories).Length > 0)
            {
                if (!AppUtils.AskYesNo($"The folder '{fullPath}' already exists and is not empty. Do you want to continue?", "Folder Exists"))
                {
                    return;
                }
            }

            ProjectCreated?.Invoke(this);
        }

        private void Cancel()
        {
            WizardCancelled?.Invoke();
        }

        private static string MakeSafeNamespace(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return "Schedule1Mods";

            var safe = AppUtils.MakeSafeFilename(name);
            // Remove invalid namespace characters
            safe = safe.Replace(" ", "").Replace("-", "");
            if (char.IsDigit(safe[0]))
                safe = "_" + safe;

            return string.IsNullOrWhiteSpace(safe) ? "Schedule1Mods" : safe;
        }

        private static string MakeSafeFilename(string name)
        {
            return AppUtils.MakeSafeFilename(name);
        }

        public event System.Action<NewProjectWizardViewModel>? ProjectCreated;
        public event System.Action? WizardCancelled;
    }
}

