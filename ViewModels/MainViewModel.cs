using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
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
        private QuestProject _currentProject = null!;
        private QuestBlueprint? _selectedQuest;
        private string _generatedCode = "";
        private bool _isCodeVisible = false;

        public QuestProject CurrentProject
        {
            get => _currentProject;
            set
            {
                if (ReferenceEquals(_currentProject, value))
                    return;

                if (_currentProject != null)
                {
                    _currentProject.PropertyChanged -= CurrentProjectOnPropertyChanged;
                }

                if (SetProperty(ref _currentProject, value))
                {
                    if (_currentProject != null)
                    {
                        _currentProject.PropertyChanged += CurrentProjectOnPropertyChanged;
                        if (_workspaceViewModel != null)
                        {
                            WorkspaceViewModel.UpdateQuestCount(_currentProject.Quests.Count);
                            UpdateWorkspaceProjectInfo();
                        }
                    }
                    CommandManager.InvalidateRequerySuggested();
                }
            }
        }

        public QuestBlueprint? SelectedQuest
        {
            get => _selectedQuest;
            set
            {
                if (SetProperty(ref _selectedQuest, value))
                {
                    if (value != null)
                    {
                        RegenerateCode();
                    }
                    else
                    {
                        GeneratedCode = string.Empty;
                    }

                    CommandManager.InvalidateRequerySuggested();
                }
            }
        }

        public string GeneratedCode
        {
            get => _generatedCode;
            set
            {
                if (SetProperty(ref _generatedCode, value))
                {
                    CommandManager.InvalidateRequerySuggested();
                }
            }
        }

        public bool IsCodeVisible
        {
            get => _isCodeVisible;
            set => SetProperty(ref _isCodeVisible, value);
        }

        public ObservableCollection<QuestBlueprint> AvailableBlueprints { get; } = new ObservableCollection<QuestBlueprint>();

        // Navigation properties
        private NavigationItem? _selectedNavigationItem;
        private WorkspaceViewModel _workspaceViewModel;
        private OpenElementTab? _selectedTab;

        public ObservableCollection<NavigationItem> NavigationItems { get; } = new ObservableCollection<NavigationItem>();
        public ObservableCollection<OpenElementTab> OpenTabs { get; } = new ObservableCollection<OpenElementTab>();
        
        public NavigationItem? SelectedNavigationItem
        {
            get => _selectedNavigationItem;
            set
            {
                if (_selectedNavigationItem != null)
                {
                    _selectedNavigationItem.IsSelected = false;
                }
                if (SetProperty(ref _selectedNavigationItem, value))
                {
                    if (_selectedNavigationItem != null)
                    {
                        _selectedNavigationItem.IsSelected = true;
                    }
                }
            }
        }

        public OpenElementTab? SelectedTab
        {
            get => _selectedTab;
            set
            {
                if (_selectedTab != null)
                {
                    _selectedTab.IsSelected = false;
                }
                if (SetProperty(ref _selectedTab, value))
                {
                    if (_selectedTab != null)
                    {
                        _selectedTab.IsSelected = true;
                        SelectedQuest = _selectedTab.Quest;
                    }
                    else
                    {
                        SelectedQuest = null;
                    }
                }
            }
        }

        public WorkspaceViewModel WorkspaceViewModel
        {
            get => _workspaceViewModel;
            set => SetProperty(ref _workspaceViewModel, value);
        }

        private string _processState = "Ready";

        public string ProcessState
        {
            get => _processState;
            set => SetProperty(ref _processState, value);
        }

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
        private ICommand? _copyCodeCommand;
        private ICommand? _exportCodeCommand;
        private ICommand? _exportModProjectCommand;
        private ICommand? _buildModCommand;
        private ICommand? _openSettingsCommand;
        private ICommand? _selectNavigationCommand;
        private ICommand? _selectCategoryCommand;

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
        public ICommand CopyCodeCommand => _copyCodeCommand!;
        public ICommand ExportCodeCommand => _exportCodeCommand!;
        public ICommand ExportModProjectCommand => _exportModProjectCommand!;
        public ICommand BuildModCommand => _buildModCommand!;
        public ICommand OpenSettingsCommand => _openSettingsCommand!;
        public ICommand SelectNavigationCommand => _selectNavigationCommand!;
        public ICommand SelectCategoryCommand => _selectCategoryCommand!;

        private readonly CodeGenerationService _codeGenService;
        private readonly ProjectService _projectService;
        private readonly ModProjectGeneratorService _modProjectGenerator;
        private readonly ModBuildService _modBuildService;
        private ModSettings _modSettings;

        public MainViewModel()
        {
            _codeGenService = new CodeGenerationService();
            _projectService = new ProjectService();
            _modProjectGenerator = new ModProjectGeneratorService();
            _modBuildService = new ModBuildService();
            _modSettings = ModSettings.Load();

            // Initialize WorkspaceViewModel BEFORE setting CurrentProject
            _workspaceViewModel = new WorkspaceViewModel();

            // Don't create default project - wait for wizard
            CurrentProject = new QuestProject();
            CurrentProject.ProjectName = ""; // Empty name indicates no project loaded

            InitializeCommands();
            InitializeBlueprints();
            InitializeNavigation();
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
            _copyCodeCommand = new RelayCommand(CopyGeneratedCode, () => !string.IsNullOrWhiteSpace(GeneratedCode));
            _exportCodeCommand = new RelayCommand(ExportGeneratedCode, () => SelectedQuest != null && !string.IsNullOrWhiteSpace(GeneratedCode));
            _exportModProjectCommand = new RelayCommand(ExportModProject, () => CurrentProject.Quests.Count > 0);
            _buildModCommand = new RelayCommand(BuildMod, () => CurrentProject.Quests.Count > 0);
            _openSettingsCommand = new RelayCommand(OpenSettings);
            _selectNavigationCommand = new RelayCommand<NavigationItem>(SelectNavigation);
            _selectCategoryCommand = new RelayCommand<ModCategory>(SelectCategory);
        }

        private void InitializeNavigation()
        {
            NavigationItems.Add(new NavigationItem
            {
                Id = "ModElements",
                DisplayName = "Mod Elements",
                IconKey = "CubeIcon",
                IsEnabled = true,
                IsSelected = true,
                Tooltip = "Create and manage mod elements (Quests, NPCs, etc.)"
            });

            NavigationItems.Add(new NavigationItem
            {
                Id = "Resources",
                DisplayName = "Resources",
                IconKey = "FolderIcon",
                IsEnabled = true,
                IsSelected = false,
                Tooltip = "Manage custom resources (icons, images, etc.)"
            });

            SelectedNavigationItem = NavigationItems.First();
        }

        private void SelectNavigation(NavigationItem? item)
        {
            if (item != null && item.IsEnabled)
            {
                SelectedNavigationItem = item;
                // Reset category selection when switching navigation
                WorkspaceViewModel.SelectedCategory = null;
                // Update workspace title based on selected navigation
                WorkspaceViewModel.WorkspaceTitle = item.Id switch
                {
                    "ModElements" => "MOD ELEMENTS",
                    "Resources" => "RESOURCES",
                    _ => "WORKSPACE"
                };
            }
        }

        private void SelectCategory(ModCategory category)
        {
            WorkspaceViewModel.SelectedCategory = category;
            // Update workspace title based on selected category
            WorkspaceViewModel.WorkspaceTitle = category switch
            {
                ModCategory.Quests => "QUESTS",
                ModCategory.NPCs => "NPCS",
                ModCategory.PhoneApps => "PHONE APPS",
                ModCategory.Items => "ITEMS",
                _ => "MOD ELEMENTS"
            };
        }

        private void UpdateWorkspaceProjectInfo()
        {
            var projectName = string.IsNullOrEmpty(CurrentProject.ProjectName) ? "Untitled Project" : CurrentProject.ProjectName;
            var totalElements = CurrentProject.Quests.Count;
            WorkspaceViewModel.ProjectInfo = $"{projectName}: {totalElements} mod elements";
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
            // Skip confirmation if no project is loaded (empty project on startup)
            if (!string.IsNullOrWhiteSpace(CurrentProject.ProjectName) && !ConfirmUnsavedChanges())
                return;

            var wizardVm = new NewProjectWizardViewModel();
            var wizardWindow = new Views.NewProjectWizardWindow
            {
                DataContext = wizardVm,
                Owner = System.Windows.Application.Current.MainWindow
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
                    var settings = ModSettings.Load();
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
                    CurrentProject = QuestProject.LoadFromFile(projectFilePath) ?? newProject;
                    SelectedQuest = null;
                    GeneratedCode = "";

                    wizardCompleted = true;
                    wizardWindow.DialogResult = true;
                    wizardWindow.Close();

                    AppUtils.ShowInfo($"Project created successfully at:\n{fullPath}");
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
                
                // If cancelled on startup (empty project), close the app
                if (string.IsNullOrWhiteSpace(CurrentProject.ProjectName))
                {
                    Application.Current.Shutdown();
                }
            };

            wizardWindow.ShowDialog();
        }

        private void OpenProject()
        {
            // Skip confirmation if no project is loaded (empty project on startup)
            if (!string.IsNullOrWhiteSpace(CurrentProject.ProjectName) && !ConfirmUnsavedChanges())
                return;

            var project = _projectService.OpenProject();
            if (project != null)
            {
                CurrentProject = project;
                SelectedQuest = CurrentProject.Quests.FirstOrDefault();
            }
            else if (string.IsNullOrWhiteSpace(CurrentProject.ProjectName))
            {
                // If user cancelled opening a project and no project is loaded, close the app
                Application.Current.Shutdown();
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

            var settings = ModSettings.Load();
            var quest = new QuestBlueprint(template.BlueprintType)
            {
                ClassName = $"Quest{CurrentProject.Quests.Count + 1}",
                QuestTitle = $"New Quest {CurrentProject.Quests.Count + 1}",
                QuestDescription = "A new quest for Schedule 1",
                BlueprintType = template.BlueprintType,
                Namespace = $"{settings.DefaultModNamespace}.Quests",
                ModName = CurrentProject.ProjectName,
                ModAuthor = settings.DefaultModAuthor,
                ModVersion = settings.DefaultModVersion
            };

            CurrentProject.AddQuest(quest);
            SelectedQuest = quest;
            WorkspaceViewModel.UpdateQuestCount(CurrentProject.Quests.Count);
            UpdateWorkspaceProjectInfo();

            // Open the quest in a tab automatically
            OpenQuestInTab(quest);
        }

        public void OpenQuestInTab(QuestBlueprint quest)
        {
            // Check if quest is already open
            var existingTab = OpenTabs.FirstOrDefault(t => t.Quest == quest);
            if (existingTab != null)
            {
                SelectedTab = existingTab;
                return;
            }

            // Create new tab
            var tab = new OpenElementTab { Quest = quest };
            OpenTabs.Add(tab);
            SelectedTab = tab;
        }

        public void CloseTab(OpenElementTab tab)
        {
            if (tab == SelectedTab)
            {
                var index = OpenTabs.IndexOf(tab);
                if (index > 0)
                {
                    SelectedTab = OpenTabs[index - 1];
                }
                else if (OpenTabs.Count > 1)
                {
                    SelectedTab = OpenTabs[1];
                }
                else
                {
                    SelectedTab = null;
                }
            }
            OpenTabs.Remove(tab);
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
                WorkspaceViewModel.UpdateQuestCount(CurrentProject.Quests.Count);
                UpdateWorkspaceProjectInfo();
            }
        }

        private void EditQuest()
        {
            // This will be handled by the properties panel
        }

        private void RegenerateCode()
        {
            if (SelectedQuest == null) return;

            try
            {
                GeneratedCode = _codeGenService.GenerateQuestCode(SelectedQuest);
            }
            catch (Exception ex)
            {
                GeneratedCode = $"// Failed to generate quest code: {ex.Message}";
            }
        }

        private void Compile()
        {
            // Redirect to BuildMod - the old Compile button now builds the full mod project
            BuildMod();
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

        private void CopyGeneratedCode()
        {
            if (string.IsNullOrWhiteSpace(GeneratedCode))
                return;

            try
            {
                Clipboard.SetText(GeneratedCode);
                AppUtils.ShowInfo("Generated quest code copied to the clipboard.");
            }
            catch (Exception ex)
            {
                AppUtils.ShowError($"Unable to copy code: {ex.Message}");
            }
        }

        private void ExportGeneratedCode()
        {
            if (SelectedQuest == null || string.IsNullOrWhiteSpace(GeneratedCode))
                return;

            try
            {
                var suggestedName = AppUtils.MakeSafeFilename($"{SelectedQuest.ClassName}.cs");
                _projectService.ExportCode(GeneratedCode, suggestedName);
            }
            catch (Exception ex)
            {
                AppUtils.ShowError($"Export failed: {ex.Message}");
            }
        }

        private void ExportModProject()
        {
            if (CurrentProject.Quests.Count == 0)
            {
                AppUtils.ShowWarning("No quests in project. Add at least one quest before exporting.");
                return;
            }

            if (string.IsNullOrWhiteSpace(CurrentProject.FilePath) || !File.Exists(CurrentProject.FilePath))
            {
                AppUtils.ShowWarning("Project must be saved before exporting. Please save the project first.");
                return;
            }

            try
            {
                ProcessState = "Exporting...";
                // Use the project directory directly (where .qproj file is located)
                var projectDir = Path.GetDirectoryName(CurrentProject.FilePath);
                if (string.IsNullOrWhiteSpace(projectDir) || !Directory.Exists(projectDir))
                {
                    AppUtils.ShowError("Project directory not found. Please save the project first.");
                    ProcessState = "Ready";
                    return;
                }

                _modSettings = ModSettings.Load(); // Reload settings
                var result = _modProjectGenerator.GenerateModProject(CurrentProject, projectDir, _modSettings);

                ProcessState = "Ready";
                if (result.Success)
                {
                    AppUtils.ShowInfo($"Mod project exported successfully to:\n{result.OutputPath}\n\nGenerated {result.GeneratedFiles.Count} files.");
                }
                else
                {
                    AppUtils.ShowError($"Failed to export mod project:\n{result.ErrorMessage}");
                }
            }
            catch (Exception ex)
            {
                ProcessState = "Error";
                AppUtils.ShowError($"Export failed: {ex.Message}");
            }
        }

        private void BuildMod()
        {
            if (CurrentProject.Quests.Count == 0)
            {
                AppUtils.ShowWarning("No quests in project. Add at least one quest before building.");
                return;
            }

            if (string.IsNullOrWhiteSpace(CurrentProject.FilePath) || !File.Exists(CurrentProject.FilePath))
            {
                AppUtils.ShowWarning("Project must be saved before building. Please save the project first.");
                return;
            }

            try
            {
                ProcessState = "Building...";
                // Use the project directory directly (where .qproj file is located)
                var projectDir = Path.GetDirectoryName(CurrentProject.FilePath);
                if (string.IsNullOrWhiteSpace(projectDir) || !Directory.Exists(projectDir))
                {
                    AppUtils.ShowError("Project directory not found. Please save the project first.");
                    ProcessState = "Ready";
                    return;
                }

                // Check if mod project already exists (look for .csproj in project directory)
                var csprojFiles = Directory.GetFiles(projectDir, "*.csproj", SearchOption.TopDirectoryOnly);
                if (csprojFiles.Length > 0)
                {
                    // Mod project already exists, build it directly
                    var existingBuildResult = _modBuildService.BuildModProject(projectDir, _modSettings);
                    ProcessState = existingBuildResult.Success ? "Ready" : "Build failed";
                    ShowBuildResult(existingBuildResult);
                    return;
                }

                _modSettings = ModSettings.Load(); // Reload settings
                
                // Generate the project in the same directory as .qproj
                var genResult = _modProjectGenerator.GenerateModProject(CurrentProject, projectDir, _modSettings);
                
                if (!genResult.Success)
                {
                    ProcessState = "Generation failed";
                    AppUtils.ShowError($"Failed to generate mod project:\n{genResult.ErrorMessage}");
                    return;
                }

                if (string.IsNullOrEmpty(genResult.OutputPath))
                {
                    ProcessState = "Ready";
                    AppUtils.ShowError("Generated project path is empty.");
                    return;
                }

                // Then build it
                var buildResult = _modBuildService.BuildModProject(genResult.OutputPath, _modSettings);
                ProcessState = buildResult.Success ? "Ready" : "Build failed";
                ShowBuildResult(buildResult);
            }
            catch (Exception ex)
            {
                ProcessState = "Error";
                AppUtils.ShowError($"Build failed: {ex.Message}");
            }
        }

        private void ShowBuildResult(ModBuildResult buildResult)
        {
            if (buildResult.Success)
            {
                var message = $"Mod built successfully!\n\nOutput: {buildResult.OutputDllPath}";
                if (buildResult.DeployedToModsFolder)
                {
                    message += $"\n\nDeployed to: {buildResult.DeployedDllPath}";
                }
                if (buildResult.Warnings.Count > 0)
                {
                    message += $"\n\nWarnings:\n{string.Join("\n", buildResult.Warnings)}";
                }
                AppUtils.ShowInfo(message);
            }
            else
            {
                // Show build log window with full output
                var logVm = new BuildLogViewModel
                {
                    Title = "Build Failed - Build Log",
                    LogContent = BuildLogContent(buildResult)
                };

                var logWindow = new Views.BuildLogWindow(logVm)
                {
                    Owner = System.Windows.Application.Current.MainWindow
                };

                logWindow.ShowDialog();
            }
        }

        private string BuildLogContent(ModBuildResult buildResult)
        {
            var sb = new System.Text.StringBuilder();
            
            sb.AppendLine($"Build Status: FAILED");
            sb.AppendLine($"Exit Code: {buildResult.ExitCode}");
            sb.AppendLine();
            
            if (!string.IsNullOrEmpty(buildResult.ErrorMessage))
            {
                sb.AppendLine("=== Error Message ===");
                sb.AppendLine(buildResult.ErrorMessage);
                sb.AppendLine();
            }

            if (!string.IsNullOrEmpty(buildResult.Output))
            {
                sb.AppendLine("=== Build Output ===");
                sb.AppendLine(buildResult.Output);
                sb.AppendLine();
            }

            if (!string.IsNullOrEmpty(buildResult.ErrorOutput))
            {
                sb.AppendLine("=== Build Errors ===");
                sb.AppendLine(buildResult.ErrorOutput);
                sb.AppendLine();
            }

            if (buildResult.Warnings.Count > 0)
            {
                sb.AppendLine("=== Warnings ===");
                foreach (var warning in buildResult.Warnings)
                {
                    sb.AppendLine(warning);
                }
            }

            return sb.ToString();
        }

        private void OpenSettings()
        {
            var settingsVm = new SettingsViewModel();
            var settingsWindow = new Views.SettingsWindow
            {
                DataContext = settingsVm
            };

            settingsVm.CloseRequested += () => settingsWindow.Close();
            settingsWindow.ShowDialog();

            // Reload settings after dialog closes
            _modSettings = ModSettings.Load();
        }

        private void CurrentProjectOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(QuestProject.IsModified))
            {
                CommandManager.InvalidateRequerySuggested();
            }
            else if (e.PropertyName == nameof(QuestProject.Quests))
            {
                WorkspaceViewModel.UpdateQuestCount(CurrentProject.Quests.Count);
                UpdateWorkspaceProjectInfo();
            }
            else if (e.PropertyName == nameof(QuestProject.ProjectName))
            {
                UpdateWorkspaceProjectInfo();
            }
        }
    }

}
