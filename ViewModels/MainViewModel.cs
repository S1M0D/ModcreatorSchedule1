using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using Microsoft.Win32;
using Schedule1ModdingTool.Models;
using Schedule1ModdingTool.Services;
using Schedule1ModdingTool.Services.CodeGeneration.Orchestration;
using System.ComponentModel;
using Schedule1ModdingTool.Utils;
using Schedule1ModdingTool.Views;

namespace Schedule1ModdingTool.ViewModels
{
    /// <summary>
    /// Main view model for the application
    /// </summary>
    public class MainViewModel : ObservableObject
    {
        private QuestProject _currentProject = null!;
        private QuestBlueprint? _selectedQuest;
        private NpcBlueprint? _selectedNpc;
        private ResourceAsset? _selectedResource;
        private string _generatedCode = "";
        private bool _isCodeVisible = false;
        private readonly AppearancePreviewService _appearancePreviewService = new AppearancePreviewService();

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
                        WorkspaceViewModel.BindProject(_currentProject);
                        WorkspaceViewModel.UpdateQuestCount(_currentProject.Quests.Count);
                        WorkspaceViewModel.UpdateNpcCount(_currentProject.Npcs.Count);
                        UpdateWorkspaceProjectInfo();
                        UpdateProcessState();
                    }
                    else
                    {
                        ProcessState = "Waiting for project...";
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
                        SelectedNpc = null;
                        RegenerateCode();
                    }
                    else if (SelectedNpc == null)
                    {
                        GeneratedCode = string.Empty;
                    }

                    OnPropertyChanged(nameof(SelectedElementName));
                    CommandManager.InvalidateRequerySuggested();
                }
            }
        }

        public NpcBlueprint? SelectedNpc
        {
            get => _selectedNpc;
            set
            {
                var stackTrace = new System.Diagnostics.StackTrace(1, true);
                System.Diagnostics.Debug.WriteLine($"[MainViewModel] SelectedNpc setter called. Old: '{_selectedNpc?.NpcId ?? "null"}', New: '{value?.NpcId ?? "null"}', Same instance: {ReferenceEquals(_selectedNpc, value)}");
                System.Diagnostics.Debug.WriteLine($"[MainViewModel] Call stack:\n{stackTrace}");

                // Don't unsubscribe/resubscribe if it's the same instance
                if (ReferenceEquals(_selectedNpc, value))
                {
                    System.Diagnostics.Debug.WriteLine("[MainViewModel] Same NPC instance, skipping unsubscribe/resubscribe");
                    return;
                }

                // Unsubscribe from previous NPC's appearance changes
                if (_selectedNpc?.Appearance != null)
                {
                    System.Diagnostics.Debug.WriteLine("[MainViewModel] Unsubscribing from previous NPC appearance");
                    _selectedNpc.Appearance.PropertyChanged -= OnAppearancePropertyChanged;
                }

                if (SetProperty(ref _selectedNpc, value))
                {
                    if (value != null)
                    {
                        _selectedQuest = null;
                        OnPropertyChanged(nameof(SelectedQuest));
                        RegenerateCode();

                        // Subscribe to appearance changes for preview
                        if (value.Appearance != null)
                        {
                            System.Diagnostics.Debug.WriteLine($"[MainViewModel] Subscribing to NPC '{value.NpcId}' appearance changes");
                            value.Appearance.PropertyChanged += OnAppearancePropertyChanged;
                            // Send initial appearance
                            System.Diagnostics.Debug.WriteLine("[MainViewModel] Sending initial appearance to preview service");
                            _appearancePreviewService.SendAppearanceUpdate(value.Appearance);
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine("[MainViewModel] Warning: NPC Appearance is null!");
                        }
                    }
                    else if (SelectedQuest == null)
                    {
                        GeneratedCode = string.Empty;
                    }

                    OnPropertyChanged(nameof(SelectedElementName));
                    CommandManager.InvalidateRequerySuggested();
                }
            }
        }

        public ResourceAsset? SelectedResource
        {
            get => _selectedResource;
            set
            {
                if (SetProperty(ref _selectedResource, value))
                {
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

        public string SelectedElementName => SelectedQuest?.DisplayName ?? SelectedNpc?.DisplayName ?? "None";

        public ObservableCollection<QuestBlueprint> AvailableBlueprints { get; } = new ObservableCollection<QuestBlueprint>();
        public ObservableCollection<NpcBlueprint> AvailableNpcBlueprints { get; } = new ObservableCollection<NpcBlueprint>();

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
                        if (_selectedTab.Quest != null)
                        {
                            SelectedQuest = _selectedTab.Quest;
                            SelectedNpc = null;
                        }
                        else if (_selectedTab.Npc != null)
                        {
                            SelectedNpc = _selectedTab.Npc;
                            SelectedQuest = null;
                        }
                    }
                    else
                    {
                        SelectedQuest = null;
                        SelectedNpc = null;
                    }
                }
            }
        }

        public WorkspaceViewModel WorkspaceViewModel
        {
            get => _workspaceViewModel;
            set => SetProperty(ref _workspaceViewModel, value);
        }

        private string _processState = "Waiting for project...";

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
        private ICommand? _playGameCommand;
        private ICommand? _openSettingsCommand;
        private ICommand? _selectNavigationCommand;
        private ICommand? _selectCategoryCommand;
        private ICommand? _addNpcCommand;
        private ICommand? _removeNpcCommand;
        private ICommand? _editNpcCommand;
        private ICommand? _addFolderCommand;
        private ICommand? _addResourceCommand;
        private ICommand? _removeResourceCommand;
        private ICommand? _duplicateQuestCommand;
        private ICommand? _duplicateNpcCommand;
        private ICommand? _duplicateFolderCommand;
        private ICommand? _deleteFolderCommand;

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
        public ICommand PlayGameCommand => _playGameCommand!;
        public ICommand OpenSettingsCommand => _openSettingsCommand!;
        public ICommand SelectNavigationCommand => _selectNavigationCommand!;
        public ICommand SelectCategoryCommand => _selectCategoryCommand!;
        public ICommand AddNpcCommand => _addNpcCommand!;
        public ICommand RemoveNpcCommand => _removeNpcCommand!;
        public ICommand EditNpcCommand => _editNpcCommand!;
        public ICommand AddFolderCommand => _addFolderCommand!;
        public ICommand AddResourceCommand => _addResourceCommand!;
        public ICommand RemoveResourceCommand => _removeResourceCommand!;
        public ICommand DuplicateQuestCommand => _duplicateQuestCommand!;
        public ICommand DuplicateNpcCommand => _duplicateNpcCommand!;
        public ICommand DuplicateFolderCommand => _duplicateFolderCommand!;
        public ICommand DeleteFolderCommand => _deleteFolderCommand!;

        private readonly CodeGenerationOrchestrator _codeGenService;
        private readonly ProjectService _projectService;
        private readonly ModProjectGeneratorService _modProjectGenerator;
        private readonly ModBuildService _modBuildService;
        private readonly GameLaunchService _gameLaunchService;
        private ModSettings _modSettings;

        public MainViewModel()
        {
            _codeGenService = new CodeGenerationOrchestrator();
            _projectService = new ProjectService();
            _modProjectGenerator = new ModProjectGeneratorService();
            _modBuildService = new ModBuildService();
            _gameLaunchService = new GameLaunchService();
            _modSettings = ModSettings.Load();

            // Set code visibility based on user experience level
            _isCodeVisible = _modSettings.ExperienceLevel != ExperienceLevel.NoCodingExperience;

            // Initialize WorkspaceViewModel BEFORE setting CurrentProject
            _workspaceViewModel = new WorkspaceViewModel
            {
                SelectedCategory = null
            };

            // Don't create default project - wait for wizard
            CurrentProject = new QuestProject();
            CurrentProject.ProjectName = ""; // Empty name indicates no project loaded

            // Start appearance preview service
            _appearancePreviewService.Start();

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
            _addNpcCommand = new RelayCommand<NpcBlueprint>(AddNpc);
            _removeNpcCommand = new RelayCommand(RemoveNpc, () => SelectedNpc != null);
            _editNpcCommand = new RelayCommand(EditNpc, () => SelectedNpc != null);
            _regenerateCodeCommand = new RelayCommand(RegenerateCode, () => SelectedQuest != null || SelectedNpc != null);
            _compileCommand = new RelayCommand(Compile, () => SelectedQuest != null);
            _toggleCodeViewCommand = new RelayCommand(() => IsCodeVisible = !IsCodeVisible);
            _copyCodeCommand = new RelayCommand(CopyGeneratedCode, () => !string.IsNullOrWhiteSpace(GeneratedCode));
            _exportCodeCommand = new RelayCommand(ExportGeneratedCode, () => (SelectedQuest != null || SelectedNpc != null) && !string.IsNullOrWhiteSpace(GeneratedCode));
            _exportModProjectCommand = new RelayCommand(ExportModProject, HasAnyElements);
            _buildModCommand = new RelayCommand(BuildMod, HasAnyElements);
            _playGameCommand = new RelayCommand(PlayGame, () => !string.IsNullOrWhiteSpace(_modSettings.GameInstallPath));
            _openSettingsCommand = new RelayCommand(OpenSettings);
            _selectNavigationCommand = new RelayCommand<NavigationItem>(SelectNavigation);
            _selectCategoryCommand = new RelayCommand<ModCategory>(SelectCategory);
            _addFolderCommand = new RelayCommand(AddFolder);
            _addResourceCommand = new RelayCommand(() =>
            {
                System.Diagnostics.Debug.WriteLine("[AddResourceCommand] Command executed");
                AddResource();
            });
            _removeResourceCommand = new RelayCommand<ResourceAsset>(resource => RemoveResource(resource));
            _duplicateQuestCommand = new RelayCommand<QuestBlueprint>(DuplicateQuest);
            _duplicateNpcCommand = new RelayCommand<NpcBlueprint>(DuplicateNpc);
            _duplicateFolderCommand = new RelayCommand<ModFolder>(DuplicateFolder);
            _deleteFolderCommand = new RelayCommand<ModFolder>(DeleteFolder);
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

        private void UpdateProcessState()
        {
            if (CurrentProject == null || string.IsNullOrWhiteSpace(CurrentProject.ProjectName))
            {
                ProcessState = "Waiting for project...";
            }
            else if (CurrentProject.IsModified)
            {
                ProcessState = "Ready (unsaved changes)";
            }
            else
            {
                ProcessState = "Ready";
            }
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

            AvailableNpcBlueprints.Add(new NpcBlueprint
            {
                ClassName = "SampleNpc",
                FirstName = "Alex",
                LastName = "Sample",
                NpcId = "sample_npc"
            });
        }

        private void NewProject()
        {
            // Skip confirmation if no project is loaded (empty project on startup)
            if (!string.IsNullOrWhiteSpace(CurrentProject.ProjectName) && !ConfirmUnsavedChanges())
                return;

            ProcessState = "Opening project wizard...";
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
                    ProcessState = "Creating project...";
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
                    ProcessState = "Saving project...";
                    newProject.SaveToFile(projectFilePath);

                    // Load it back to ensure proper initialization
                    ProcessState = "Loading project...";
                    CurrentProject = QuestProject.LoadFromFile(projectFilePath) ?? newProject;
                    NormalizeProjectResources();
                    SelectedQuest = null;
                    GeneratedCode = "";

                    wizardCompleted = true;
                    wizardWindow.DialogResult = true;
                    wizardWindow.Close();

                    UpdateProcessState();
                    AppUtils.ShowInfo($"Project created successfully at:\n{fullPath}");
                }
                catch (Exception ex)
                {
                    ProcessState = "Project creation failed";
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
                else
                {
                    UpdateProcessState();
                }
            };

            wizardWindow.ShowDialog();
            if (!wizardCompleted)
            {
                UpdateProcessState();
            }
        }

        private void OpenProject()
        {
            // Skip confirmation if no project is loaded (empty project on startup)
            if (!string.IsNullOrWhiteSpace(CurrentProject.ProjectName) && !ConfirmUnsavedChanges())
                return;

            ProcessState = "Opening project...";
            var project = _projectService.OpenProject();
            if (project != null)
            {
                ProcessState = "Loading project...";
                CurrentProject = project;
                NormalizeProjectResources();
                SelectedQuest = CurrentProject.Quests.FirstOrDefault();
                UpdateProcessState();
            }
            else if (string.IsNullOrWhiteSpace(CurrentProject.ProjectName))
            {
                // If user cancelled opening a project and no project is loaded, close the app
                Application.Current.Shutdown();
            }
            else
            {
                UpdateProcessState();
            }
        }

        private void SaveProject()
        {
            try
            {
                // Auto-regenerate code before saving
                if (SelectedQuest != null || SelectedNpc != null)
                {
                    RegenerateCode();
                }

                ProcessState = "Saving project...";
                var success = _projectService.SaveProject(CurrentProject);
                UpdateProcessState();
                if (!success)
                {
                    ProcessState = "Save failed";
                }
            }
            catch (Exception)
            {
                ProcessState = "Save failed";
                throw;
            }
        }

        private void SaveProjectAs()
        {
            try
            {
                // Auto-regenerate code before saving
                if (SelectedQuest != null || SelectedNpc != null)
                {
                    RegenerateCode();
                }

                ProcessState = "Saving project...";
                var success = _projectService.SaveProjectAs(CurrentProject);
                UpdateProcessState();
                if (!success)
                {
                    ProcessState = "Save failed";
                }
            }
            catch (Exception)
            {
                ProcessState = "Save failed";
                throw;
            }
        }

        private bool HasAnyElements() =>
            CurrentProject.Quests.Count > 0 ||
            CurrentProject.Npcs.Count > 0 ||
            CurrentProject.Resources.Count > 0;

        private bool TryGetProjectDirectory(out string projectDir)
        {
            System.Diagnostics.Debug.WriteLine("[TryGetProjectDirectory] Method called");
            projectDir = string.Empty;
            if (CurrentProject == null)
            {
                System.Diagnostics.Debug.WriteLine("[TryGetProjectDirectory] CurrentProject is null");
                return false;
            }
            
            if (string.IsNullOrWhiteSpace(CurrentProject.FilePath))
            {
                System.Diagnostics.Debug.WriteLine("[TryGetProjectDirectory] CurrentProject.FilePath is null or empty");
                return false;
            }

            System.Diagnostics.Debug.WriteLine($"[TryGetProjectDirectory] CurrentProject.FilePath: {CurrentProject.FilePath}");
            var dir = Path.GetDirectoryName(CurrentProject.FilePath);
            if (string.IsNullOrWhiteSpace(dir))
            {
                System.Diagnostics.Debug.WriteLine("[TryGetProjectDirectory] Path.GetDirectoryName returned null or empty");
                return false;
            }

            projectDir = dir;
            System.Diagnostics.Debug.WriteLine($"[TryGetProjectDirectory] Success, projectDir: {projectDir}");
            return true;
        }

        private bool EnsureProjectDirectory(out string projectDir)
        {
            System.Diagnostics.Debug.WriteLine("[EnsureProjectDirectory] Method called");
            projectDir = string.Empty;

            System.Diagnostics.Debug.WriteLine("[EnsureProjectDirectory] Calling TryGetProjectDirectory...");
            if (TryGetProjectDirectory(out projectDir))
            {
                System.Diagnostics.Debug.WriteLine($"[EnsureProjectDirectory] TryGetProjectDirectory succeeded: {projectDir}");
                return true;
            }

            System.Diagnostics.Debug.WriteLine("[EnsureProjectDirectory] TryGetProjectDirectory failed");
            if (CurrentProject == null)
            {
                System.Diagnostics.Debug.WriteLine("[EnsureProjectDirectory] CurrentProject is null, returning false");
                return false;
            }

            System.Diagnostics.Debug.WriteLine("[EnsureProjectDirectory] Showing save dialog...");
            var shouldSave = AppUtils.AskYesNo(
                "You need to save the project before adding resources. Would you like to save it now?",
                "Save Project Required");

            System.Diagnostics.Debug.WriteLine($"[EnsureProjectDirectory] User chose to save: {shouldSave}");
            if (!shouldSave)
            {
                System.Diagnostics.Debug.WriteLine("[EnsureProjectDirectory] User declined to save, returning false");
                return false;
            }

            System.Diagnostics.Debug.WriteLine("[EnsureProjectDirectory] Saving project...");
            if (!_projectService.SaveProject(CurrentProject))
            {
                System.Diagnostics.Debug.WriteLine("[EnsureProjectDirectory] SaveProject failed");
                AppUtils.ShowWarning("Project was not saved. Upload cancelled.", "Resource Upload Cancelled");
                return false;
            }

            System.Diagnostics.Debug.WriteLine("[EnsureProjectDirectory] SaveProject succeeded, calling TryGetProjectDirectory again...");
            if (TryGetProjectDirectory(out projectDir))
            {
                System.Diagnostics.Debug.WriteLine($"[EnsureProjectDirectory] TryGetProjectDirectory succeeded after save: {projectDir}");
                return true;
            }

            System.Diagnostics.Debug.WriteLine("[EnsureProjectDirectory] TryGetProjectDirectory failed after save");
            AppUtils.ShowError("Unable to determine project location after saving. Please try again.", "Resource Upload Failed");
            return false;
        }

        private static string GenerateUniqueResourceName(string directory, string fileName)
        {
            var name = Path.GetFileNameWithoutExtension(fileName);
            var ext = Path.GetExtension(fileName);
            var candidate = fileName;
            var counter = 1;

            while (File.Exists(Path.Combine(directory, candidate)))
            {
                candidate = $"{name}_{counter++}{ext}";
            }

            return candidate;
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
                ModVersion = settings.DefaultModVersion,
                FolderId = WorkspaceViewModel.SelectedFolder?.Id ?? QuestProject.RootFolderId
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
            var tab = new OpenElementTab { Quest = quest, Npc = null };
            OpenTabs.Add(tab);
            SelectedTab = tab;
        }

        public void OpenNpcInTab(NpcBlueprint npc)
        {
            var existingTab = OpenTabs.FirstOrDefault(t => t.Npc == npc);
            if (existingTab != null)
            {
                SelectedTab = existingTab;
                return;
            }

            var tab = new OpenElementTab { Npc = npc };
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
            if (SelectedTab == null)
            {
                SelectedQuest = null;
                SelectedNpc = null;
            }
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

        private void AddNpc(NpcBlueprint? template)
        {
            var settings = ModSettings.Load();
            var npc = template?.DeepCopy() ?? new NpcBlueprint();
            npc.ClassName = $"Npc{CurrentProject.Npcs.Count + 1}";
            npc.NpcId = $"npc_{CurrentProject.Npcs.Count + 1}";
            npc.FirstName = string.IsNullOrWhiteSpace(npc.FirstName) ? "New" : npc.FirstName;
            npc.LastName = string.IsNullOrWhiteSpace(npc.LastName) ? "NPC" : npc.LastName;
            npc.Namespace = $"{settings.DefaultModNamespace}.NPCs";
            npc.ModName = string.IsNullOrWhiteSpace(CurrentProject.ProjectName) ? npc.ModName : CurrentProject.ProjectName;
            npc.ModAuthor = settings.DefaultModAuthor;
            npc.ModVersion = settings.DefaultModVersion;
            npc.FolderId = WorkspaceViewModel.SelectedFolder?.Id ?? QuestProject.RootFolderId;

            CurrentProject.AddNpc(npc);
            SelectedNpc = npc;
            WorkspaceViewModel.UpdateNpcCount(CurrentProject.Npcs.Count);
            UpdateWorkspaceProjectInfo();
        }

        private void RemoveNpc()
        {
            if (SelectedNpc == null) return;

            var result = MessageBox.Show($"Are you sure you want to remove '{SelectedNpc.DisplayName}'?",
                "Remove NPC", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                CurrentProject.RemoveNpc(SelectedNpc);
                SelectedNpc = CurrentProject.Npcs.FirstOrDefault();
                WorkspaceViewModel.UpdateNpcCount(CurrentProject.Npcs.Count);
                UpdateWorkspaceProjectInfo();
            }
        }

        private void AddFolder()
        {
            try
            {
                WorkspaceViewModel.CreateFolder();
            }
            catch (Exception ex)
            {
                AppUtils.ShowError($"Unable to create folder: {ex.Message}");
            }
        }

        private void DuplicateQuest(QuestBlueprint? quest)
        {
            if (quest == null) return;

            var settings = ModSettings.Load();
            var duplicate = quest.DeepCopy();
            duplicate.ClassName = $"{duplicate.ClassName}Copy";
            duplicate.QuestTitle = $"{duplicate.QuestTitle} (Copy)";
            duplicate.QuestId = $"{duplicate.QuestId}_copy";
            duplicate.FolderId = quest.FolderId; // Keep in same folder

            CurrentProject.AddQuest(duplicate);
            SelectedQuest = duplicate;
            WorkspaceViewModel.UpdateQuestCount(CurrentProject.Quests.Count);
            UpdateWorkspaceProjectInfo();

            // Open the duplicated quest in a tab
            OpenQuestInTab(duplicate);
        }

        private void DuplicateNpc(NpcBlueprint? npc)
        {
            if (npc == null) return;

            var settings = ModSettings.Load();
            var duplicate = npc.DeepCopy();
            duplicate.ClassName = $"{duplicate.ClassName}Copy";
            duplicate.FirstName = duplicate.FirstName;
            duplicate.LastName = $"{duplicate.LastName} (Copy)";
            duplicate.NpcId = $"{duplicate.NpcId}_copy";
            duplicate.FolderId = npc.FolderId; // Keep in same folder

            CurrentProject.AddNpc(duplicate);
            SelectedNpc = duplicate;
            WorkspaceViewModel.UpdateNpcCount(CurrentProject.Npcs.Count);
            UpdateWorkspaceProjectInfo();
        }

        private void DuplicateFolder(ModFolder? folder)
        {
            if (folder == null || CurrentProject == null) return;

            try
            {
                var duplicate = new ModFolder
                {
                    Name = $"{folder.Name} (Copy)",
                    ParentId = folder.ParentId
                };

                CurrentProject.Folders.Add(duplicate);
                WorkspaceViewModel.NavigateToFolder(duplicate);
            }
            catch (Exception ex)
            {
                AppUtils.ShowError($"Unable to duplicate folder: {ex.Message}");
            }
        }

        private void DeleteFolder(ModFolder? folder)
        {
            if (folder == null || CurrentProject == null) return;

            // Prevent deleting root folder
            if (string.IsNullOrWhiteSpace(folder.ParentId))
            {
                AppUtils.ShowWarning("Cannot delete the root folder.");
                return;
            }

            // Check if folder has children
            var hasChildren = CurrentProject.Folders.Any(f => f.ParentId == folder.Id) ||
                             CurrentProject.Quests.Any(q => q.FolderId == folder.Id) ||
                             CurrentProject.Npcs.Any(n => n.FolderId == folder.Id);

            if (hasChildren)
            {
                var result = MessageBox.Show(
                    $"Folder '{folder.Name}' contains items. Do you want to delete it and move all items to its parent folder?",
                    "Delete Folder",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result != MessageBoxResult.Yes)
                    return;

                // Move all children to parent folder
                var parentId = folder.ParentId ?? QuestProject.RootFolderId;
                foreach (var childFolder in CurrentProject.Folders.Where(f => f.ParentId == folder.Id).ToList())
                {
                    childFolder.ParentId = parentId;
                }
                foreach (var quest in CurrentProject.Quests.Where(q => q.FolderId == folder.Id))
                {
                    quest.FolderId = parentId;
                }
                foreach (var npc in CurrentProject.Npcs.Where(n => n.FolderId == folder.Id))
                {
                    npc.FolderId = parentId;
                }
            }
            else
            {
                var result = MessageBox.Show(
                    $"Are you sure you want to delete folder '{folder.Name}'?",
                    "Delete Folder",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result != MessageBoxResult.Yes)
                    return;
            }

            // Navigate to parent folder before deleting
            var parentFolder = CurrentProject.GetFolderById(folder.ParentId ?? QuestProject.RootFolderId);
            if (parentFolder != null)
            {
                WorkspaceViewModel.NavigateToFolder(parentFolder);
            }

            CurrentProject.Folders.Remove(folder);
        }

        private void AddResource()
        {
            System.Diagnostics.Debug.WriteLine("[AddResource] Method called");
            try
            {
                System.Diagnostics.Debug.WriteLine($"[AddResource] CurrentProject is null: {CurrentProject == null}");
                if (CurrentProject == null)
                {
                    System.Diagnostics.Debug.WriteLine("[AddResource] CurrentProject is null, showing error and returning");
                    AppUtils.ShowError("No project is currently loaded.", "Cannot Add Resource");
                    return;
                }

                System.Diagnostics.Debug.WriteLine($"[AddResource] CurrentProject.FilePath: {CurrentProject.FilePath ?? "null"}");
                System.Diagnostics.Debug.WriteLine("[AddResource] Calling EnsureProjectDirectory...");
                if (!EnsureProjectDirectory(out var projectDir))
                {
                    System.Diagnostics.Debug.WriteLine("[AddResource] EnsureProjectDirectory returned false, returning");
                    // EnsureProjectDirectory already shows user-facing messages, so we just return
                    UpdateProcessState();
                    return;
                }

                System.Diagnostics.Debug.WriteLine($"[AddResource] Project directory: {projectDir}");

                System.Diagnostics.Debug.WriteLine("[AddResource] Creating OpenFileDialog...");
                var dialog = new OpenFileDialog
                {
                    Filter = "PNG Images (*.png)|*.png",
                    Title = "Select PNG Resource",
                    Multiselect = true,
                    CheckFileExists = true
                };

                System.Diagnostics.Debug.WriteLine("[AddResource] Calling dialog.ShowDialog()...");
                bool? result;
                try
                {
                    result = dialog.ShowDialog();
                    System.Diagnostics.Debug.WriteLine($"[AddResource] dialog.ShowDialog() returned: {result}");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[AddResource] Exception in ShowDialog: {ex.Message}\n{ex.StackTrace}");
                    AppUtils.ShowError($"Failed to open file dialog: {ex.Message}", "Dialog Error");
                    return;
                }

                System.Diagnostics.Debug.WriteLine($"[AddResource] FileNames.Length: {dialog.FileNames.Length}");
                if (result != true || dialog.FileNames.Length == 0)
                {
                    System.Diagnostics.Debug.WriteLine("[AddResource] No files selected or dialog cancelled, returning");
                    UpdateProcessState();
                    return;
                }

                System.Diagnostics.Debug.WriteLine($"[AddResource] Processing {dialog.FileNames.Length} file(s)");
                ProcessState = $"Adding {dialog.FileNames.Length} resource(s)...";
                var resourcesDir = Path.Combine(projectDir, "Resources");
                System.Diagnostics.Debug.WriteLine($"[AddResource] Resources directory: {resourcesDir}");
                Directory.CreateDirectory(resourcesDir);

                var addedAssets = new List<ResourceAsset>();
                var failures = new List<string>();

                foreach (var file in dialog.FileNames)
                {
                    try
                    {
                        if (!File.Exists(file))
                        {
                            failures.Add(Path.GetFileName(file));
                            continue;
                        }

                        var baseName = AppUtils.MakeSafeFilename(Path.GetFileNameWithoutExtension(file));
                        var uniqueFileName = GenerateUniqueResourceName(resourcesDir, $"{baseName}.png");
                        var destination = Path.Combine(resourcesDir, uniqueFileName);
                        if (!TryCopyResourceFile(file, destination, out var copyError))
                        {
                            failures.Add($"{Path.GetFileName(file)} ({copyError})");
                            continue;
                        }

                        var asset = new ResourceAsset
                        {
                            DisplayName = baseName,
                            RelativePath = Path.Combine("Resources", uniqueFileName).Replace('\\', '/')
                        };

                        CurrentProject.AddResource(asset);
                        addedAssets.Add(asset);
                    }
                    catch (Exception ex)
                    {
                        failures.Add($"{Path.GetFileName(file)} ({ex.Message})");
                    }
                }

                System.Diagnostics.Debug.WriteLine($"[AddResource] Successfully added {addedAssets.Count} asset(s), {failures.Count} failure(s)");
                if (addedAssets.Count > 0)
                {
                    SelectedResource = addedAssets.Last();
                    System.Diagnostics.Debug.WriteLine($"[AddResource] SelectedResource set to: {addedAssets.Last().DisplayName}");
                }

                if (failures.Count > 0)
                {
                    var message = $"Some files could not be added:\n{string.Join("\n", failures)}";
                    AppUtils.ShowWarning(message, "Resource Import Issues");
                }
                UpdateProcessState();
                System.Diagnostics.Debug.WriteLine("[AddResource] Method completed successfully");
            }
            catch (Exception ex)
            {
                ProcessState = "Resource upload failed";
                System.Diagnostics.Debug.WriteLine($"[AddResource] Unhandled exception: {ex.Message}\n{ex.StackTrace}");
                AppUtils.ShowError($"An error occurred while adding resources: {ex.Message}\n\n{ex.StackTrace}", "Resource Upload Error");
            }
        }

        private void NormalizeProjectResources()
        {
            try
            {
                if (CurrentProject == null || CurrentProject.Resources.Count == 0)
                {
                    Debug.WriteLine("[NormalizeProjectResources] No project/resources to normalize");
                    return;
                }

                if (!TryGetProjectDirectory(out var projectDir))
                {
                    Debug.WriteLine("[NormalizeProjectResources] Unable to resolve project directory");
                    return;
                }

                var resourcesDir = Path.Combine(projectDir, "Resources");
                Directory.CreateDirectory(resourcesDir);
                var resourcesDirFull = NormalizeDirectoryPath(resourcesDir);

                Debug.WriteLine($"[NormalizeProjectResources] Normalizing {CurrentProject.Resources.Count} resource(s) into '{resourcesDirFull}'");
                foreach (var asset in CurrentProject.Resources.ToList())
                {
                    EnsureResourceAssetLocal(asset, projectDir, resourcesDir, resourcesDirFull);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[NormalizeProjectResources] {ex.Message}\n{ex.StackTrace}");
            }
        }

        private void EnsureResourceAssetLocal(ResourceAsset asset, string projectDir, string resourcesDir, string resourcesDirFull)
        {
            if (asset == null)
            {
                Debug.WriteLine("[EnsureResourceAssetLocal] Asset was null");
                return;
            }

            var relativePath = asset.RelativePath;
            if (string.IsNullOrWhiteSpace(relativePath))
            {
                Debug.WriteLine("[EnsureResourceAssetLocal] Asset relative path was null/empty");
                return;
            }

            string absolutePath;
            try
            {
                absolutePath = ResourcePathHelper.GetAbsolutePath(relativePath, projectDir);
            }
            catch
            {
                absolutePath = relativePath;
            }

            if (!File.Exists(absolutePath) && Path.IsPathRooted(relativePath) && File.Exists(relativePath))
            {
                absolutePath = relativePath;
            }

            if (!File.Exists(absolutePath))
            {
                Debug.WriteLine($"[EnsureResourceAssetLocal] File missing for '{relativePath}' (expected '{absolutePath}')");
                return;
            }

            Debug.WriteLine($"[EnsureResourceAssetLocal] Found '{absolutePath}' for '{relativePath}'");
            var absoluteFull = NormalizeDirectoryPath(absolutePath);
            if (!absoluteFull.StartsWith(resourcesDirFull, StringComparison.OrdinalIgnoreCase))
            {
                var extension = Path.GetExtension(absolutePath);
                if (string.IsNullOrWhiteSpace(extension))
                {
                    extension = ".png";
                }

                var safeName = AppUtils.MakeSafeFilename(Path.GetFileNameWithoutExtension(absolutePath));
                var destinationName = GenerateUniqueResourceName(resourcesDir, $"{safeName}{extension}");
                var destinationPath = Path.Combine(resourcesDir, destinationName);
                Debug.WriteLine($"[EnsureResourceAssetLocal] Copying '{absoluteFull}' into project resources '{destinationPath}'");

                if (!TryCopyResourceFile(absolutePath, destinationPath, out var error))
                {
                    Debug.WriteLine($"[EnsureResourceAssetLocal] Failed to copy {absolutePath}: {error}");
                    return;
                }

                absoluteFull = NormalizeDirectoryPath(destinationPath);
                absolutePath = destinationPath;
            }

            var normalizedRelative = ResourcePathHelper.GetProjectRelativePath(absolutePath, projectDir);
            if (!string.Equals(asset.RelativePath, normalizedRelative, StringComparison.OrdinalIgnoreCase))
            {
                asset.RelativePath = normalizedRelative;
            }
        }

        private static string NormalizeDirectoryPath(string path)
        {
            return Path.GetFullPath(path)
                       .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        }

        private static bool TryCopyResourceFile(string source, string destination, out string error)
        {
            error = string.Empty;

            // Retry copy to mitigate transient file locks from antivirus/indexers or image decoders
            const int maxRetries = 5;
            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                try
                {
                    Debug.WriteLine($"[TryCopyResourceFile] Attempt {attempt}/{maxRetries}: '{source}' -> '{destination}'");
                    var destDir = Path.GetDirectoryName(destination);
                    if (!string.IsNullOrEmpty(destDir))
                    {
                        Directory.CreateDirectory(destDir);
                    }

                    if (File.Exists(destination))
                    {
                        try
                        {
                            File.Delete(destination);
                        }
                        catch (IOException ioEx)
                        {
                            Debug.WriteLine($"[TryCopyResourceFile] Destination delete failed: {ioEx.Message}");
                            // Destination locked; wait and retry delete on next attempt
                        }
                    }

                    // Use FileStream with FileShare.ReadWrite to allow reading even if file is open elsewhere
                    // This allows copying files that might be open in Explorer preview or image viewers
                    using (var sourceStream = new FileStream(source, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
                    using (var destStream = new FileStream(destination, FileMode.Create, FileAccess.Write, FileShare.None))
                    {
                        sourceStream.CopyTo(destStream);
                    }
                    Debug.WriteLine($"[TryCopyResourceFile] Copy succeeded: '{destination}'");
                    return true;
                }
                catch (IOException ioEx)
                {
                    error = ioEx.Message;
                    Debug.WriteLine($"[TryCopyResourceFile] IO exception ({attempt}/{maxRetries}): {ioEx.Message}");
                    if (attempt == maxRetries)
                        break;

                    // Exponential backoff: 100ms, 200ms, 400ms, 800ms, 1600ms
                    System.Threading.Thread.Sleep(100 * (int)Math.Pow(2, attempt - 1));
                    continue;
                }
                catch (UnauthorizedAccessException unauthEx)
                {
                    error = unauthEx.Message;
                    Debug.WriteLine($"[TryCopyResourceFile] Unauthorized ({attempt}/{maxRetries}): {unauthEx.Message}");
                    if (attempt == maxRetries)
                        break;
                    System.Threading.Thread.Sleep(100 * (int)Math.Pow(2, attempt - 1));
                    continue;
                }
                catch (Exception ex)
                {
                    error = ex.Message;
                    Debug.WriteLine($"[TryCopyResourceFile] Fatal error: {ex.Message}");
                    return false;
                }
            }

            Debug.WriteLine($"[TryCopyResourceFile] Failed after {maxRetries} attempts: '{source}' -> '{destination}' ({error})");
            return false;
        }

        private static bool TryDeleteFileWithRetry(string absolutePath, out string error)
        {
            error = string.Empty;
            const int maxRetries = 5;
            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                try
                {
                    if (File.Exists(absolutePath))
                    {
                        File.Delete(absolutePath);
                    }
                    return true;
                }
                catch (IOException ioEx)
                {
                    error = ioEx.Message;
                    if (attempt == maxRetries)
                        break;
                    System.Threading.Thread.Sleep(100 * (int)Math.Pow(2, attempt - 1));
                }
                catch (UnauthorizedAccessException unauthEx)
                {
                    error = unauthEx.Message;
                    if (attempt == maxRetries)
                        break;
                    System.Threading.Thread.Sleep(100 * (int)Math.Pow(2, attempt - 1));
                }
                catch (Exception ex)
                {
                    error = ex.Message;
                    return false;
                }
            }
            return false;
        }

        private void RemoveResource(ResourceAsset? resource)
        {
            if (resource == null)
                resource = SelectedResource;
            if (resource == null || CurrentProject == null)
                return;

            if (!TryGetProjectDirectory(out var projectDir))
                return;

            var absolutePath = Path.Combine(projectDir,
                (resource.RelativePath ?? string.Empty).Replace('/', Path.DirectorySeparatorChar));

            try
            {
                if (File.Exists(absolutePath))
                {
                    if (!TryDeleteFileWithRetry(absolutePath, out var delError))
                    {
                        AppUtils.ShowWarning($"Failed to delete file '{absolutePath}': {delError}");
                    }
                }
            }
            catch (Exception ex)
            {
                AppUtils.ShowWarning($"Failed to delete file '{absolutePath}': {ex.Message}");
            }

            CurrentProject.RemoveResource(resource);
            if (SelectedResource == resource)
            {
                SelectedResource = null;
            }
        }

        /// <summary>
        /// Validates all resources in the current project and returns a list of missing resources.
        /// </summary>
        /// <returns>List of missing resource display names and paths, or empty list if all valid</returns>
        private List<string> ValidateProjectResources()
        {
            var missingResources = new List<string>();

            if (CurrentProject == null || CurrentProject.Resources.Count == 0)
                return missingResources;

            if (!TryGetProjectDirectory(out var projectDir))
            {
                missingResources.Add("Unable to determine project directory");
                return missingResources;
            }

            foreach (var asset in CurrentProject.Resources)
            {
                if (string.IsNullOrWhiteSpace(asset.RelativePath))
                {
                    missingResources.Add($"{asset.DisplayName}: No path specified");
                    continue;
                }

                if (!ResourcePathHelper.ResourceExists(asset.RelativePath, projectDir))
                {
                    var expectedPath = Path.Combine(projectDir, asset.RelativePath.Replace('/', Path.DirectorySeparatorChar));
                    missingResources.Add($"{asset.DisplayName} ({asset.RelativePath})\n  Expected at: {expectedPath}");
                }
            }

            return missingResources;
        }

        private void EditQuest()
        {
            // This will be handled by the properties panel
        }

        private void EditNpc()
        {
            if (SelectedNpc != null)
            {
                OpenNpcInTab(SelectedNpc);
            }
        }

        private void RegenerateCode()
        {
            try
            {
                if (SelectedQuest != null)
                {
                    GeneratedCode = _codeGenService.GenerateQuestCode(SelectedQuest);
                }
                else if (SelectedNpc != null)
                {
                    GeneratedCode = _codeGenService.GenerateNpcCode(SelectedNpc);
                }
                else
                {
                    GeneratedCode = string.Empty;
                }
            }
            catch (Exception ex)
            {
                GeneratedCode = $"// Failed to generate code: {ex.Message}";
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
                AppUtils.ShowInfo("Generated code copied to the clipboard.");
            }
            catch (Exception ex)
            {
                AppUtils.ShowError($"Unable to copy code: {ex.Message}");
            }
        }

        private void ExportGeneratedCode()
        {
            if ((SelectedQuest == null && SelectedNpc == null) || string.IsNullOrWhiteSpace(GeneratedCode))
                return;

            try
            {
                var fileName = SelectedQuest != null
                    ? $"{SelectedQuest.ClassName}.cs"
                    : $"{SelectedNpc!.ClassName}.cs";
                var suggestedName = AppUtils.MakeSafeFilename(fileName);
                _projectService.ExportCode(GeneratedCode, suggestedName);
            }
            catch (Exception ex)
            {
                AppUtils.ShowError($"Export failed: {ex.Message}");
            }
        }

        private void ExportModProject()
        {
            TryExportModProject(showSuccessDialog: true, out _);
        }

        private bool TryExportModProject(bool showSuccessDialog, out string? generatedProjectPath)
        {
            generatedProjectPath = null;

            if (!HasAnyElements())
            {
                AppUtils.ShowWarning("No mod elements in project. Add at least one quest or NPC before exporting.");
                return false;
            }

            if (string.IsNullOrWhiteSpace(CurrentProject.FilePath) || !File.Exists(CurrentProject.FilePath))
            {
                AppUtils.ShowWarning("Project must be saved before exporting. Please save the project first.");
                return false;
            }

            // Validate resources before export
            var missingResources = ValidateProjectResources();
            if (missingResources.Count > 0)
            {
                var missingMessage = $"Warning: {missingResources.Count} resource file(s) are missing:\n\n" +
                                     string.Join("\n\n", missingResources) +
                                     "\n\nThese resources will be excluded from the exported mod. Do you want to continue?";

                var confirmation = MessageBox.Show(missingMessage, "Missing Resources", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (confirmation != MessageBoxResult.Yes)
                {
                    return false;
                }
            }

            try
            {
                // Auto-regenerate code before export
                if (SelectedQuest != null || SelectedNpc != null)
                {
                    RegenerateCode();
                }

                ProcessState = "Exporting mod project...";
                // Use the project directory directly (where .qproj file is located)
                var projectDir = Path.GetDirectoryName(CurrentProject.FilePath);
                if (string.IsNullOrWhiteSpace(projectDir) || !Directory.Exists(projectDir))
                {
                    AppUtils.ShowError("Project directory not found. Please save the project first.");
                    UpdateProcessState();
                    return false;
                }

                _modSettings = ModSettings.Load(); // Reload settings
                ProcessState = "Generating mod files...";
                var result = _modProjectGenerator.GenerateModProject(CurrentProject, projectDir, _modSettings);

                UpdateProcessState();
                if (result.Success)
                {
                    generatedProjectPath = result.OutputPath;
                    var message = $"Mod project exported successfully to:\n{result.OutputPath}\n\nGenerated {result.GeneratedFiles.Count} files.";

                    var hasIssues = result.Errors.Count > 0 || result.Warnings.Count > 0;
                    if (hasIssues)
                    {
                        if (result.Errors.Count > 0)
                        {
                            message += $"\n\nErrors ({result.Errors.Count}):\n{string.Join("\n", result.Errors)}";
                        }
                        if (result.Warnings.Count > 0)
                        {
                            message += $"\n\nWarnings ({result.Warnings.Count}):\n{string.Join("\n", result.Warnings)}";
                        }
                        AppUtils.ShowWarning(message);
                    }
                    else if (showSuccessDialog)
                    {
                        AppUtils.ShowInfo(message);
                    }

                    return true;
                }
                else
                {
                    var errorMessage = result.ErrorMessage ?? "Unknown error";
                    if (result.Errors.Count > 0)
                    {
                        errorMessage += $"\n\nAdditional errors:\n{string.Join("\n", result.Errors)}";
                    }
                    if (result.Warnings.Count > 0)
                    {
                        errorMessage += $"\n\nWarnings:\n{string.Join("\n", result.Warnings)}";
                    }
                    AppUtils.ShowError($"Failed to export mod project:\n{errorMessage}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                ProcessState = "Export failed";
                AppUtils.ShowError($"Export failed: {ex.Message}");
                return false;
            }
        }

        private void BuildMod()
        {
            try
            {
                if (!TryExportModProject(showSuccessDialog: false, out var exportPath))
                {
                    return;
                }

                if (string.IsNullOrWhiteSpace(exportPath) || !Directory.Exists(exportPath))
                {
                    AppUtils.ShowError("Generated project path is empty or missing. Please export the project again.");
                    return;
                }

                ProcessState = "Building mod...";
                var buildResult = _modBuildService.BuildModProject(exportPath, _modSettings);
                UpdateProcessState();
                if (!buildResult.Success)
                {
                    ProcessState = "Build failed";
                }
                ShowBuildResult(buildResult);
            }
            catch (Exception ex)
            {
                ProcessState = "Build failed";
                AppUtils.ShowError($"Build failed: {ex.Message}");
            }
        }

        private void PlayGame()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(_modSettings.GameInstallPath))
                {
                    AppUtils.ShowError("Game install path is not configured. Please set it in Settings.");
                    return;
                }

                ProcessState = "Building connector mod and launching game...";
                
                // Use ConnectorLocal for development (local DLL), ConnectorNuGet for release
                var useLocalDll = true; // Set to false to use NuGet package instead
                var launchResult = _gameLaunchService.LaunchGame(_modSettings, useLocalDll);
                
                UpdateProcessState();
                
                if (launchResult.Success)
                {
                    var message = "Game launched successfully!";
                    if (launchResult.DllCopied)
                    {
                        message += $"\n\nModCreatorConnector DLL deployed to:\n{launchResult.DeployedDllPath}";
                    }
                    if (launchResult.Warnings.Count > 0)
                    {
                        message += $"\n\nWarnings:\n{string.Join("\n", launchResult.Warnings)}";
                    }
                    AppUtils.ShowInfo(message);
                }
                else
                {
                    ProcessState = "Launch failed";
                    var errorMessage = $"Failed to launch game: {launchResult.ErrorMessage}";
                    if (!string.IsNullOrEmpty(launchResult.BuildOutput))
                    {
                        errorMessage += $"\n\nBuild Output:\n{launchResult.BuildOutput}";
                    }
                    AppUtils.ShowError(errorMessage);
                }
            }
            catch (Exception ex)
            {
                ProcessState = "Launch failed";
                AppUtils.ShowError($"Failed to launch game: {ex.Message}");
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
            CommandManager.InvalidateRequerySuggested(); // Refresh PlayGameCommand enabled state
        }

        private void OnAppearancePropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine($"[MainViewModel] OnAppearancePropertyChanged - Property: {e.PropertyName}");

            if (sender is NpcAppearanceSettings appearance && SelectedNpc?.Appearance == appearance)
            {
                System.Diagnostics.Debug.WriteLine($"[MainViewModel] Sending appearance update to preview service");
                _appearancePreviewService.SendAppearanceUpdate(appearance);
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"[MainViewModel] Skipping update - sender check failed");
            }
        }

        private void CurrentProjectOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(QuestProject.IsModified))
            {
                UpdateProcessState();
                CommandManager.InvalidateRequerySuggested();
            }
            else if (e.PropertyName == nameof(QuestProject.Quests))
            {
                WorkspaceViewModel.UpdateQuestCount(CurrentProject.Quests.Count);
                UpdateWorkspaceProjectInfo();
            }
            else if (e.PropertyName == nameof(QuestProject.Npcs))
            {
                WorkspaceViewModel.UpdateNpcCount(CurrentProject.Npcs.Count);
                UpdateWorkspaceProjectInfo();
            }
            else if (e.PropertyName == nameof(QuestProject.Resources))
            {
                OnPropertyChanged(nameof(CurrentProject.Resources));
            }
            else if (e.PropertyName == nameof(QuestProject.ProjectName))
            {
                UpdateWorkspaceProjectInfo();
                UpdateProcessState();
            }
        }
    }

}
