using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using Schedule1ModdingTool.Models;
using Schedule1ModdingTool.Services;
using Schedule1ModdingTool.Utils;

namespace Schedule1ModdingTool.ViewModels
{
    /// <summary>
    /// Main view model for the application
    /// </summary>
    public class MainViewModel : ObservableObject
    {
        private QuestProject _currentProject;
        private QuestBlueprint? _selectedQuest;
        private string _generatedCode = "";
        private bool _isCodeVisible = true;

        public QuestProject CurrentProject
        {
            get => _currentProject;
            set => SetProperty(ref _currentProject, value);
        }

        public QuestBlueprint? SelectedQuest
        {
            get => _selectedQuest;
            set
            {
                SetProperty(ref _selectedQuest, value);
                if (value != null)
                {
                    RegenerateCode();
                }
            }
        }

        public string GeneratedCode
        {
            get => _generatedCode;
            set => SetProperty(ref _generatedCode, value);
        }

        public bool IsCodeVisible
        {
            get => _isCodeVisible;
            set => SetProperty(ref _isCodeVisible, value);
        }

        public ObservableCollection<QuestBlueprint> AvailableBlueprints { get; } = new ObservableCollection<QuestBlueprint>();

        // Commands with private backing fields
        private ICommand? _newProjectCommand;
        private ICommand? _openProjectCommand;
        private ICommand? _saveProjectCommand;
        private ICommand? _saveProjectAsCommand;
        private ICommand? _exitCommand;
        private ICommand? _addQuestCommand;
        private ICommand? _removeQuestCommand;
        private ICommand? _editQuestCommand;
        private ICommand? _regenerateCodeCommand;
        private ICommand? _compileCommand;
        private ICommand? _toggleCodeViewCommand;

        public ICommand NewProjectCommand => _newProjectCommand!;
        public ICommand OpenProjectCommand => _openProjectCommand!;
        public ICommand SaveProjectCommand => _saveProjectCommand!;
        public ICommand SaveProjectAsCommand => _saveProjectAsCommand!;
        public ICommand ExitCommand => _exitCommand!;
        public ICommand AddQuestCommand => _addQuestCommand!;
        public ICommand RemoveQuestCommand => _removeQuestCommand!;
        public ICommand EditQuestCommand => _editQuestCommand!;
        public ICommand RegenerateCodeCommand => _regenerateCodeCommand!;
        public ICommand CompileCommand => _compileCommand!;
        public ICommand ToggleCodeViewCommand => _toggleCodeViewCommand!;

        private readonly CodeGenerationService _codeGenService;
        private readonly ProjectService _projectService;

        public MainViewModel()
        {
            _currentProject = new QuestProject();
            _codeGenService = new CodeGenerationService();
            _projectService = new ProjectService();

            InitializeCommands();
            InitializeBlueprints();
        }

        private void InitializeCommands()
        {
            _newProjectCommand = new RelayCommand(NewProject);
            _openProjectCommand = new RelayCommand(OpenProject);
            _saveProjectCommand = new RelayCommand(SaveProject, () => CurrentProject.IsModified);
            _saveProjectAsCommand = new RelayCommand(SaveProjectAs);
            _exitCommand = new RelayCommand(Exit);
            _addQuestCommand = new RelayCommand<QuestBlueprint>(AddQuest);
            _removeQuestCommand = new RelayCommand(RemoveQuest, () => SelectedQuest != null);
            _editQuestCommand = new RelayCommand(EditQuest, () => SelectedQuest != null);
            _regenerateCodeCommand = new RelayCommand(RegenerateCode, () => SelectedQuest != null);
            _compileCommand = new RelayCommand(Compile, () => SelectedQuest != null);
            _toggleCodeViewCommand = new RelayCommand(() => IsCodeVisible = !IsCodeVisible);
        }

        private void InitializeBlueprints()
        {
            AvailableBlueprints.Add(new QuestBlueprint(QuestBlueprintType.Standard)
            {
                ClassName = "Standard Quest",
                QuestTitle = "Standard Quest Template",
                QuestDescription = "A standard quest template",
                BlueprintType = QuestBlueprintType.Standard
            });
            AvailableBlueprints.Add(new QuestBlueprint(QuestBlueprintType.Advanced)
            {
                ClassName = "Advanced Quest",
                QuestTitle = "Advanced Quest Template",
                QuestDescription = "An advanced quest template with more features",
                BlueprintType = QuestBlueprintType.Advanced
            });
        }

        private void NewProject()
        {
            if (ConfirmUnsavedChanges())
            {
                CurrentProject = new QuestProject();
                SelectedQuest = null;
                GeneratedCode = "";
            }
        }

        private void OpenProject()
        {
            if (!ConfirmUnsavedChanges()) return;

            var project = _projectService.OpenProject();
            if (project != null)
            {
                CurrentProject = project;
                SelectedQuest = CurrentProject.Quests.FirstOrDefault();
            }
        }

        private void SaveProject()
        {
            _projectService.SaveProject(CurrentProject);
        }

        private void SaveProjectAs()
        {
            _projectService.SaveProjectAs(CurrentProject);
        }

        private void Exit()
        {
            if (ConfirmUnsavedChanges())
            {
                Application.Current.Shutdown();
            }
        }

        private void AddQuest(QuestBlueprint? template)
        {
            if (template == null) return;

            var quest = new QuestBlueprint(template.BlueprintType)
            {
                ClassName = $"Quest{CurrentProject.Quests.Count + 1}",
                QuestTitle = $"New Quest {CurrentProject.Quests.Count + 1}",
                QuestDescription = "A new quest for Schedule 1",
                BlueprintType = template.BlueprintType
            };

            CurrentProject.AddQuest(quest);
            SelectedQuest = quest;
        }

        private void RemoveQuest()
        {
            if (SelectedQuest == null) return;

            var result = MessageBox.Show($"Are you sure you want to remove '{SelectedQuest.DisplayName}'?", 
                "Remove Quest", MessageBoxButton.YesNo, MessageBoxImage.Question);
            
            if (result == MessageBoxResult.Yes)
            {
                CurrentProject.RemoveQuest(SelectedQuest);
                SelectedQuest = CurrentProject.Quests.FirstOrDefault();
            }
        }

        private void EditQuest()
        {
            // This will be handled by the properties panel
        }

        private void RegenerateCode()
        {
            if (SelectedQuest == null) return;
            GeneratedCode = _codeGenService.GenerateQuestCode(SelectedQuest);
        }

        private void Compile()
        {
            if (SelectedQuest == null) return;
            
            try
            {
                var success = _codeGenService.CompileToDll(SelectedQuest, GeneratedCode);
                if (success)
                {
                    MessageBox.Show($"Successfully compiled '{SelectedQuest.ClassName}.dll'", "Compilation Successful", 
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Compilation failed: {ex.Message}", "Compilation Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private bool ConfirmUnsavedChanges()
        {
            if (!CurrentProject.IsModified) return true;

            var result = MessageBox.Show(
                "You have unsaved changes. Do you want to save them before continuing?",
                "Unsaved Changes",
                MessageBoxButton.YesNoCancel,
                MessageBoxImage.Question);

            switch (result)
            {
                case MessageBoxResult.Yes:
                    SaveProject();
                    return true;
                case MessageBoxResult.No:
                    return true;
                case MessageBoxResult.Cancel:
                default:
                    return false;
            }
        }
    }

}