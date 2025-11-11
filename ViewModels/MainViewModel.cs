using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using Schedule1ModdingTool.Models;
using Schedule1ModdingTool.Services;
using Schedule1ModdingTool.Services.CodeGeneration.Orchestration;
using Schedule1ModdingTool.Utils;
using Schedule1ModdingTool.Views;

namespace Schedule1ModdingTool.ViewModels
{
    /// <summary>
    /// Main view model for the application
    /// </summary>
    public class MainViewModel : ObservableObject
    {
        // Core state
        private QuestProject _currentProject = null!;
        private QuestBlueprint? _selectedQuest;
        private NpcBlueprint? _selectedNpc;
        private ResourceAsset? _selectedResource;
        private NpcScheduleAction? _selectedScheduleAction;
        private string _generatedCode = "";
        private bool _isCodeVisible = false;
        private string _processState = "Waiting for project...";

        // Services
        private readonly AppearancePreviewService _appearancePreviewService = new AppearancePreviewService();
        private readonly CodeGenerationOrchestrator _codeGenService;
        private readonly ProjectService _projectService;
        private readonly ModProjectGeneratorService _modProjectGenerator;
        private readonly ModBuildService _modBuildService;
        private readonly GameLaunchService _gameLaunchService;
        private readonly TabManagementService _tabManagementService;
        private readonly ResourceManagementService _resourceManagementService;
        private readonly NavigationService _navigationService;
        private readonly ElementManagementService _elementManagementService;
        private readonly UndoRedoService _undoRedoService;
        private ModSettings _modSettings;
        private bool _isRestoringFromUndoRedo = false;
        private DispatcherTimer? _debounceSnapshotTimer;
        private bool _wasModifiedBeforeChange = false;

        // ViewModels
        private WorkspaceViewModel _workspaceViewModel;

        // Collections
        public ObservableCollection<QuestBlueprint> AvailableBlueprints { get; } = new ObservableCollection<QuestBlueprint>();
        public ObservableCollection<NpcBlueprint> AvailableNpcBlueprints { get; } = new ObservableCollection<NpcBlueprint>();

        // Delegated collections (managed by services)
        public ObservableCollection<NavigationItem> NavigationItems => _navigationService.NavigationItems;
        public ObservableCollection<OpenElementTab> OpenTabs => _tabManagementService.OpenTabs;

        #region Properties

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
                        _navigationService.UpdateElementCounts(_currentProject.Quests.Count, _currentProject.Npcs.Count);
                        _navigationService.UpdateWorkspaceProjectInfo(_currentProject);
                        UpdateProcessState();
                        
                        // Subscribe to property changes on all NPCs and Quests for undo/redo tracking
                        SubscribeToElementPropertyChanges();
                        
                        // Track initial modified state
                        _wasModifiedBeforeChange = _currentProject.IsModified;
                        
                        // Save initial snapshot for undo (unless we're restoring from undo/redo)
                        if (!_isRestoringFromUndoRedo)
                        {
                            _undoRedoService.SaveSnapshot(_currentProject);
                        }
                    }
                    else
                    {
                        ProcessState = "Waiting for project...";
                        _undoRedoService.Clear();
                        UnsubscribeFromElementPropertyChanges();
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
                Debug.WriteLine($"[MainViewModel] SelectedNpc setter called. Old: '{_selectedNpc?.NpcId ?? "null"}', New: '{value?.NpcId ?? "null"}', Same instance: {ReferenceEquals(_selectedNpc, value)}");

                // Don't unsubscribe/resubscribe if it's the same instance
                if (ReferenceEquals(_selectedNpc, value))
                {
                    Debug.WriteLine("[MainViewModel] Same NPC instance, skipping unsubscribe/resubscribe");
                    return;
                }

                // Unsubscribe from previous NPC's appearance changes
                if (_selectedNpc?.Appearance != null)
                {
                    Debug.WriteLine("[MainViewModel] Unsubscribing from previous NPC appearance");
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
                            Debug.WriteLine($"[MainViewModel] Subscribing to NPC '{value.NpcId}' appearance changes");
                            value.Appearance.PropertyChanged += OnAppearancePropertyChanged;
                            Debug.WriteLine("[MainViewModel] Sending initial appearance to preview service");
                            _appearancePreviewService.SendAppearanceUpdate(value.Appearance);
                        }
                        else
                        {
                            Debug.WriteLine("[MainViewModel] Warning: NPC Appearance is null!");
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

        public NpcScheduleAction? SelectedScheduleAction
        {
            get => _selectedScheduleAction;
            set => SetProperty(ref _selectedScheduleAction, value);
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

        public string ProcessState
        {
            get => _processState;
            set => SetProperty(ref _processState, value);
        }

        public string SelectedElementName => SelectedQuest?.DisplayName ?? SelectedNpc?.DisplayName ?? "None";

        public NavigationItem? SelectedNavigationItem
        {
            get => _navigationService.SelectedNavigationItem;
            set
            {
                _navigationService.SelectNavigation(value);
                OnPropertyChanged(nameof(SelectedNavigationItem));
            }
        }

        public OpenElementTab? SelectedTab
        {
            get => _tabManagementService.SelectedTab;
            set
            {
                var previousTab = _tabManagementService.SelectedTab;
                _tabManagementService.SelectedTab = value;

                if (_tabManagementService.SelectedTab != null)
                {
                    // Don't set SelectedQuest/SelectedNpc for workspace tabs
                    if (_tabManagementService.SelectedTab.IsWorkspace)
                    {
                        // Keep current selection, don't change it
                    }
                    else if (_tabManagementService.SelectedTab.Quest != null)
                    {
                        SelectedQuest = _tabManagementService.SelectedTab.Quest;
                        SelectedNpc = null;
                    }
                    else if (_tabManagementService.SelectedTab.Npc != null)
                    {
                        SelectedNpc = _tabManagementService.SelectedTab.Npc;
                        SelectedQuest = null;
                    }
                }
                else
                {
                    SelectedQuest = null;
                    SelectedNpc = null;
                }

                OnPropertyChanged(nameof(SelectedTab));
            }
        }

        public WorkspaceViewModel WorkspaceViewModel
        {
            get => _workspaceViewModel;
            set => SetProperty(ref _workspaceViewModel, value);
        }

        #endregion

        #region Commands

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
        private ICommand? _undoCommand;
        private ICommand? _redoCommand;

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
        public ICommand UndoCommand => _undoCommand!;
        public ICommand RedoCommand => _redoCommand!;

        #endregion

        public MainViewModel()
        {
            // Initialize core services
            _codeGenService = new CodeGenerationOrchestrator();
            _projectService = new ProjectService();
            _modProjectGenerator = new ModProjectGeneratorService();
            _modBuildService = new ModBuildService();
            _gameLaunchService = new GameLaunchService();
            _undoRedoService = new Services.UndoRedoService();
            _modSettings = ModSettings.Load();
            
            // Configure undo history size from settings
            _undoRedoService.MaxHistorySize = _modSettings.UndoHistorySize;

            // Set code visibility based on user experience level
            _isCodeVisible = _modSettings.ExperienceLevel != ExperienceLevel.NoCodingExperience;

            // Initialize WorkspaceViewModel BEFORE setting CurrentProject
            _workspaceViewModel = new WorkspaceViewModel
            {
                SelectedCategory = null
            };

            // Initialize modular services
            _tabManagementService = new TabManagementService(new ObservableCollection<OpenElementTab>());
            _resourceManagementService = new ResourceManagementService();
            _navigationService = new NavigationService(new ObservableCollection<NavigationItem>(), _workspaceViewModel);
            _elementManagementService = new ElementManagementService(_workspaceViewModel);
            
            // Subscribe to NavigationService property changes to notify MainViewModel bindings
            _navigationService.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(NavigationService.SelectedNavigationItem))
                {
                    OnPropertyChanged(nameof(SelectedNavigationItem));
                }
            };
            
            // Subscribe to TabManagementService property changes to notify MainViewModel bindings
            _tabManagementService.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(TabManagementService.SelectedTab))
                {
                    // Update SelectedQuest/SelectedNpc based on the selected tab
                    if (_tabManagementService.SelectedTab != null)
                    {
                        // Don't set SelectedQuest/SelectedNpc for workspace tabs
                        if (_tabManagementService.SelectedTab.IsWorkspace)
                        {
                            // Keep current selection, don't change it
                        }
                        else if (_tabManagementService.SelectedTab.Quest != null)
                        {
                            SelectedQuest = _tabManagementService.SelectedTab.Quest;
                            SelectedNpc = null;
                        }
                        else if (_tabManagementService.SelectedTab.Npc != null)
                        {
                            SelectedNpc = _tabManagementService.SelectedTab.Npc;
                            SelectedQuest = null;
                        }
                    }
                    else
                    {
                        SelectedQuest = null;
                        SelectedNpc = null;
                    }
                    
                    OnPropertyChanged(nameof(SelectedTab));
                }
            };

            // Don't create default project - wait for wizard
            CurrentProject = new QuestProject();
            CurrentProject.ProjectName = ""; // Empty name indicates no project loaded

            // Start appearance preview service
            _appearancePreviewService.Start();

            InitializeCommands();
            InitializeBlueprints();
            _navigationService.InitializeNavigation();
        }

        #region Initialization

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
            _selectNavigationCommand = new RelayCommand<NavigationItem>(item => _navigationService.SelectNavigation(item));
            _selectCategoryCommand = new RelayCommand<ModCategory>(category => _navigationService.SelectCategory(category));
            _addFolderCommand = new RelayCommand(AddFolder);
            _addResourceCommand = new RelayCommand(() =>
            {
                Debug.WriteLine("[AddResourceCommand] Command executed");
                AddResource();
            });
            _removeResourceCommand = new RelayCommand<ResourceAsset>(resource => RemoveResource(resource));
            _duplicateQuestCommand = new RelayCommand<QuestBlueprint>(DuplicateQuest);
            _duplicateNpcCommand = new RelayCommand<NpcBlueprint>(DuplicateNpc);
            _duplicateFolderCommand = new RelayCommand<ModFolder>(DuplicateFolder);
            _deleteFolderCommand = new RelayCommand<ModFolder>(DeleteFolder);
            _undoCommand = new RelayCommand(Undo, () => _undoRedoService.CanUndo);
            _redoCommand = new RelayCommand(Redo, () => _undoRedoService.CanRedo);
            
            // Subscribe to undo/redo state changes
            _undoRedoService.StateChanged += (s, e) => CommandManager.InvalidateRequerySuggested();
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

        #endregion

        #region Project Operations

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
                    var fullPath = vm.FullProjectPath;
                    if (!Directory.Exists(fullPath))
                    {
                        Directory.CreateDirectory(fullPath);
                    }

                    var newProject = new QuestProject
                    {
                        ProjectName = vm.ModName,
                        ProjectDescription = $"Mod project for {vm.ModName}"
                    };

                    var settings = ModSettings.Load();
                    settings.DefaultModNamespace = vm.ModNamespace;
                    settings.DefaultModAuthor = vm.ModAuthor;
                    settings.DefaultModVersion = vm.ModVersion;
                    settings.Save();

                    var projectFilePath = Path.Combine(fullPath, $"{AppUtils.MakeSafeFilename(vm.ModName)}.qproj");
                    newProject.FilePath = projectFilePath;

                    ProcessState = "Saving project...";
                    newProject.SaveToFile(projectFilePath);

                    ProcessState = "Loading project...";
                    CurrentProject = QuestProject.LoadFromFile(projectFilePath) ?? newProject;
                    NormalizeProjectResources();
                    SelectedQuest = null;
                    GeneratedCode = "";

                    // Clear undo/redo history when creating a new project
                    _undoRedoService.Clear();
                    _undoRedoService.SaveSnapshot(CurrentProject);

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
                
                // Clear undo/redo history when loading a new project
                _undoRedoService.Clear();
                _undoRedoService.SaveSnapshot(CurrentProject);
                
                UpdateProcessState();
            }
            else if (string.IsNullOrWhiteSpace(CurrentProject.ProjectName))
            {
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

        private void Exit()
        {
            if (ConfirmUnsavedChanges())
            {
                Application.Current.Shutdown();
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

        #endregion

        #region Element Operations (Delegated to ElementManagementService)

        private void AddQuest(QuestBlueprint? template)
        {
            // Save snapshot before making changes
            _undoRedoService.SaveSnapshot(CurrentProject);

            var quest = _elementManagementService.AddQuest(CurrentProject, template);
            if (quest != null)
            {
                // Subscribe to property changes for undo/redo tracking
                quest.PropertyChanged -= OnElementPropertyChanged;
                quest.PropertyChanged += OnElementPropertyChanged;
                
                SelectedQuest = quest;
                _navigationService.UpdateWorkspaceProjectInfo(CurrentProject);
                _tabManagementService.OpenQuestInTab(quest);
            }
        }

        private void RemoveQuest()
        {
            if (SelectedQuest == null) return;

            // Save snapshot before making changes
            _undoRedoService.SaveSnapshot(CurrentProject);

            if (_elementManagementService.RemoveQuest(CurrentProject, SelectedQuest))
            {
                SelectedQuest = CurrentProject.Quests.FirstOrDefault();
                _navigationService.UpdateWorkspaceProjectInfo(CurrentProject);
            }
        }

        private void DuplicateQuest(QuestBlueprint? quest)
        {
            if (quest == null) return;

            // Save snapshot before making changes
            _undoRedoService.SaveSnapshot(CurrentProject);

            var duplicate = _elementManagementService.DuplicateQuest(CurrentProject, quest);
            if (duplicate != null)
            {
                // Subscribe to property changes for undo/redo tracking
                duplicate.PropertyChanged -= OnElementPropertyChanged;
                duplicate.PropertyChanged += OnElementPropertyChanged;
                
                SelectedQuest = duplicate;
                _navigationService.UpdateWorkspaceProjectInfo(CurrentProject);
                _tabManagementService.OpenQuestInTab(duplicate);
            }
        }

        private void AddNpc(NpcBlueprint? template)
        {
            // Save snapshot before making changes
            _undoRedoService.SaveSnapshot(CurrentProject);

            var npc = _elementManagementService.AddNpc(CurrentProject, template);
            
            // Subscribe to property changes for undo/redo tracking
            npc.PropertyChanged -= OnElementPropertyChanged;
            npc.PropertyChanged += OnElementPropertyChanged;
            
            // Also subscribe to appearance changes
            if (npc.Appearance != null)
            {
                npc.Appearance.PropertyChanged -= OnElementPropertyChanged;
                npc.Appearance.PropertyChanged += OnElementPropertyChanged;
            }
            
            SelectedNpc = npc;
            _navigationService.UpdateWorkspaceProjectInfo(CurrentProject);
        }

        private void RemoveNpc()
        {
            if (SelectedNpc == null) return;

            // Save snapshot before making changes
            _undoRedoService.SaveSnapshot(CurrentProject);

            if (_elementManagementService.RemoveNpc(CurrentProject, SelectedNpc))
            {
                SelectedNpc = CurrentProject.Npcs.FirstOrDefault();
                _navigationService.UpdateWorkspaceProjectInfo(CurrentProject);
            }
        }

        private void DuplicateNpc(NpcBlueprint? npc)
        {
            if (npc == null) return;

            // Save snapshot before making changes
            _undoRedoService.SaveSnapshot(CurrentProject);

            var duplicate = _elementManagementService.DuplicateNpc(CurrentProject, npc);
            if (duplicate != null)
            {
                // Subscribe to property changes for undo/redo tracking
                duplicate.PropertyChanged -= OnElementPropertyChanged;
                duplicate.PropertyChanged += OnElementPropertyChanged;
                
                // Also subscribe to appearance changes
                if (duplicate.Appearance != null)
                {
                    duplicate.Appearance.PropertyChanged -= OnElementPropertyChanged;
                    duplicate.Appearance.PropertyChanged += OnElementPropertyChanged;
                }
                
                SelectedNpc = duplicate;
                _navigationService.UpdateWorkspaceProjectInfo(CurrentProject);
            }
        }

        private void AddFolder()
        {
            try
            {
                _elementManagementService.CreateFolder();
            }
            catch (Exception ex)
            {
                AppUtils.ShowError($"Unable to create folder: {ex.Message}");
            }
        }

        private void DuplicateFolder(ModFolder? folder)
        {
            _elementManagementService.DuplicateFolder(CurrentProject, folder);
        }

        private void DeleteFolder(ModFolder? folder)
        {
            _elementManagementService.DeleteFolder(CurrentProject, folder);
        }

        private void EditQuest()
        {
            // Handled by properties panel
        }

        private void EditNpc()
        {
            if (SelectedNpc != null)
            {
                _tabManagementService.OpenNpcInTab(SelectedNpc);
            }
        }

        #endregion

        #region Tab Operations (Delegated to TabManagementService)

        public void OpenWorkspaceTab()
        {
            _tabManagementService.OpenWorkspaceTab();
        }

        public void OpenQuestInTab(QuestBlueprint quest)
        {
            _tabManagementService.OpenQuestInTab(quest);
        }

        public void OpenNpcInTab(NpcBlueprint npc)
        {
            _tabManagementService.OpenNpcInTab(npc);
        }

        public void CloseTab(OpenElementTab tab)
        {
            if (_tabManagementService.CloseTab(tab))
            {
                if (_tabManagementService.SelectedTab == null)
                {
                    SelectedQuest = null;
                    SelectedNpc = null;
                }
            }
        }

        #endregion

        #region Resource Operations (Delegated to ResourceManagementService)

        private void AddResource()
        {
            Debug.WriteLine("[AddResource] Method called");
            try
            {
                if (CurrentProject == null)
                {
                    Debug.WriteLine("[AddResource] CurrentProject is null, showing error and returning");
                    AppUtils.ShowError("No project is currently loaded.", "Cannot Add Resource");
                    return;
                }

                if (!EnsureProjectDirectory(out var projectDir))
                {
                    Debug.WriteLine("[AddResource] EnsureProjectDirectory returned false, returning");
                    UpdateProcessState();
                    return;
                }

                ProcessState = "Adding resources...";
                var result = _resourceManagementService.AddResources(CurrentProject, projectDir);

                if (result.Success && result.AddedAssets.Count > 0)
                {
                    SelectedResource = result.LastAddedAsset;
                    Debug.WriteLine($"[AddResource] SelectedResource set to: {result.LastAddedAsset?.DisplayName}");
                }

                if (result.Failures.Count > 0)
                {
                    var message = $"Some files could not be added:\n{string.Join("\n", result.Failures)}";
                    AppUtils.ShowWarning(message, "Resource Import Issues");
                }

                UpdateProcessState();
                Debug.WriteLine("[AddResource] Method completed successfully");
            }
            catch (Exception ex)
            {
                ProcessState = "Resource upload failed";
                Debug.WriteLine($"[AddResource] Unhandled exception: {ex.Message}\n{ex.StackTrace}");
                AppUtils.ShowError($"An error occurred while adding resources: {ex.Message}\n\n{ex.StackTrace}", "Resource Upload Error");
            }
        }

        private void RemoveResource(ResourceAsset? resource)
        {
            if (resource == null)
                resource = SelectedResource;
            if (resource == null || CurrentProject == null)
                return;

            if (!TryGetProjectDirectory(out var projectDir))
                return;

            _resourceManagementService.RemoveResource(CurrentProject, resource, projectDir);

            if (SelectedResource == resource)
            {
                SelectedResource = null;
            }
        }

        private void NormalizeProjectResources()
        {
            if (!TryGetProjectDirectory(out var projectDir))
            {
                Debug.WriteLine("[NormalizeProjectResources] Unable to resolve project directory");
                return;
            }

            _resourceManagementService.NormalizeProjectResources(CurrentProject, projectDir);
        }

        private List<string> ValidateProjectResources()
        {
            if (!TryGetProjectDirectory(out var projectDir))
            {
                return new List<string> { "Unable to determine project directory" };
            }

            return _resourceManagementService.ValidateProjectResources(CurrentProject, projectDir);
        }

        #endregion

        #region Code Generation & Build

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
            BuildMod();
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
                if (SelectedQuest != null || SelectedNpc != null)
                {
                    RegenerateCode();
                }

                ProcessState = "Exporting mod project...";
                var projectDir = Path.GetDirectoryName(CurrentProject.FilePath);
                if (string.IsNullOrWhiteSpace(projectDir) || !Directory.Exists(projectDir))
                {
                    AppUtils.ShowError("Project directory not found. Please save the project first.");
                    UpdateProcessState();
                    return false;
                }

                _modSettings = ModSettings.Load();
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

                var useLocalDll = true;
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
            var oldUndoSize = _modSettings.UndoHistorySize;
            _modSettings = ModSettings.Load();
            
            // Update undo history size if it changed
            if (_modSettings.UndoHistorySize != oldUndoSize)
            {
                _undoRedoService.MaxHistorySize = _modSettings.UndoHistorySize;
            }
            
            CommandManager.InvalidateRequerySuggested();
        }

        #endregion

        #region Helper Methods

        private bool HasAnyElements() =>
            CurrentProject.Quests.Count > 0 ||
            CurrentProject.Npcs.Count > 0 ||
            CurrentProject.Resources.Count > 0;

        private bool TryGetProjectDirectory(out string projectDir)
        {
            projectDir = string.Empty;
            if (CurrentProject == null || string.IsNullOrWhiteSpace(CurrentProject.FilePath))
            {
                return false;
            }

            var dir = Path.GetDirectoryName(CurrentProject.FilePath);
            if (string.IsNullOrWhiteSpace(dir))
            {
                return false;
            }

            projectDir = dir;
            return true;
        }

        private bool EnsureProjectDirectory(out string projectDir)
        {
            projectDir = string.Empty;

            if (TryGetProjectDirectory(out projectDir))
            {
                return true;
            }

            if (CurrentProject == null)
            {
                return false;
            }

            var shouldSave = AppUtils.AskYesNo(
                "You need to save the project before adding resources. Would you like to save it now?",
                "Save Project Required");

            if (!shouldSave)
            {
                return false;
            }

            if (!_projectService.SaveProject(CurrentProject))
            {
                AppUtils.ShowWarning("Project was not saved. Upload cancelled.", "Resource Upload Cancelled");
                return false;
            }

            if (TryGetProjectDirectory(out projectDir))
            {
                return true;
            }

            AppUtils.ShowError("Unable to determine project location after saving. Please try again.", "Resource Upload Failed");
            return false;
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

        #endregion

        #region Undo/Redo Operations

        private void Undo()
        {
            if (CurrentProject == null || !_undoRedoService.CanUndo)
                return;

            // Save IDs of currently selected elements before restoring
            var selectedQuestId = SelectedQuest?.QuestId;
            var selectedNpcId = SelectedNpc?.NpcId;
            
            var restoredProject = _undoRedoService.Undo(CurrentProject);
            if (restoredProject != null)
            {
                _isRestoringFromUndoRedo = true;
                try
                {
                    // Cancel any pending snapshot saves
                    CancelDebouncedSnapshot();
                    
                    // Temporarily unsubscribe to avoid saving snapshot during restore
                    CurrentProject.PropertyChanged -= CurrentProjectOnPropertyChanged;
                    
                    CurrentProject = restoredProject;
                    
                    // Resubscribe
                    CurrentProject.PropertyChanged += CurrentProjectOnPropertyChanged;
                    
                    // Resubscribe to element property changes
                    SubscribeToElementPropertyChanges();
                    
                    // Update tracked modified state
                    _wasModifiedBeforeChange = CurrentProject.IsModified;
                    
                    // Restore selected quest/NPC references to point to new objects
                    RestoreSelectedElements(selectedQuestId, selectedNpcId);
                    
                    // Update open tabs to reference new objects
                    UpdateOpenTabsReferences();
                    
                    // Update UI
                    WorkspaceViewModel.BindProject(CurrentProject);
                    _navigationService.UpdateElementCounts(CurrentProject.Quests.Count, CurrentProject.Npcs.Count);
                    _navigationService.UpdateWorkspaceProjectInfo(CurrentProject);
                    UpdateProcessState();
                    
                    // Regenerate code if needed
                    if (SelectedQuest != null || SelectedNpc != null)
                    {
                        RegenerateCode();
                    }
                }
                finally
                {
                    _isRestoringFromUndoRedo = false;
                }
            }
        }

        private void Redo()
        {
            if (CurrentProject == null || !_undoRedoService.CanRedo)
                return;

            // Save IDs of currently selected elements before restoring
            var selectedQuestId = SelectedQuest?.QuestId;
            var selectedNpcId = SelectedNpc?.NpcId;
            
            var restoredProject = _undoRedoService.Redo(CurrentProject);
            if (restoredProject != null)
            {
                _isRestoringFromUndoRedo = true;
                try
                {
                    // Cancel any pending snapshot saves
                    CancelDebouncedSnapshot();
                    
                    // Temporarily unsubscribe to avoid saving snapshot during restore
                    CurrentProject.PropertyChanged -= CurrentProjectOnPropertyChanged;
                    
                    CurrentProject = restoredProject;
                    
                    // Resubscribe
                    CurrentProject.PropertyChanged += CurrentProjectOnPropertyChanged;
                    
                    // Resubscribe to element property changes
                    SubscribeToElementPropertyChanges();
                    
                    // Update tracked modified state
                    _wasModifiedBeforeChange = CurrentProject.IsModified;
                    
                    // Restore selected quest/NPC references to point to new objects
                    RestoreSelectedElements(selectedQuestId, selectedNpcId);
                    
                    // Update open tabs to reference new objects
                    UpdateOpenTabsReferences();
                    
                    // Update UI
                    WorkspaceViewModel.BindProject(CurrentProject);
                    _navigationService.UpdateElementCounts(CurrentProject.Quests.Count, CurrentProject.Npcs.Count);
                    _navigationService.UpdateWorkspaceProjectInfo(CurrentProject);
                    UpdateProcessState();
                    
                    // Regenerate code if needed
                    if (SelectedQuest != null || SelectedNpc != null)
                    {
                        RegenerateCode();
                    }
                }
                finally
                {
                    _isRestoringFromUndoRedo = false;
                }
            }
        }

        /// <summary>
        /// Restores SelectedQuest and SelectedNpc references to point to objects in the restored project
        /// </summary>
        private void RestoreSelectedElements(string? selectedQuestId, string? selectedNpcId)
        {
            // Restore selected quest if it existed before
            if (!string.IsNullOrEmpty(selectedQuestId))
            {
                var restoredQuest = CurrentProject.Quests.FirstOrDefault(q => q.QuestId == selectedQuestId);
                if (restoredQuest != null)
                {
                    // Clear current selection first to ensure property change notification
                    var currentQuest = _selectedQuest;
                    _selectedQuest = null;
                    OnPropertyChanged(nameof(SelectedQuest));
                    
                    // Set to restored quest using the property setter to trigger all side effects
                    SelectedQuest = restoredQuest;
                }
                else
                {
                    // Quest was deleted, clear selection
                    SelectedQuest = null;
                }
            }
            else if (!string.IsNullOrEmpty(selectedNpcId))
            {
                // Only clear quest if we're restoring an NPC
                SelectedQuest = null;
            }

            // Restore selected NPC if it existed before
            if (!string.IsNullOrEmpty(selectedNpcId))
            {
                var restoredNpc = CurrentProject.Npcs.FirstOrDefault(n => n.NpcId == selectedNpcId);
                if (restoredNpc != null)
                {
                    // Clear current selection first to ensure property change notification
                    var currentNpc = _selectedNpc;
                    if (currentNpc?.Appearance != null)
                    {
                        currentNpc.Appearance.PropertyChanged -= OnAppearancePropertyChanged;
                    }
                    _selectedNpc = null;
                    OnPropertyChanged(nameof(SelectedNpc));
                    
                    // Set to restored NPC using the property setter to trigger all side effects
                    SelectedNpc = restoredNpc;
                }
                else
                {
                    // NPC was deleted, clear selection
                    SelectedNpc = null;
                }
            }
            else if (!string.IsNullOrEmpty(selectedQuestId))
            {
                // Only clear NPC if we're restoring a quest
                SelectedNpc = null;
            }
        }

        /// <summary>
        /// Updates OpenTabs to reference the new objects from the restored project
        /// </summary>
        private void UpdateOpenTabsReferences()
        {
            var tabsToClose = new List<OpenElementTab>();
            
            foreach (var tab in OpenTabs)
            {
                if (tab.Quest != null)
                {
                    // Find the quest in the restored project by ID
                    var restoredQuest = CurrentProject.Quests.FirstOrDefault(q => q.QuestId == tab.Quest.QuestId);
                    if (restoredQuest != null)
                    {
                        tab.Quest = restoredQuest;
                    }
                    else
                    {
                        // Quest was deleted, mark tab for closing
                        tabsToClose.Add(tab);
                    }
                }
                else if (tab.Npc != null)
                {
                    // Find the NPC in the restored project by ID
                    var restoredNpc = CurrentProject.Npcs.FirstOrDefault(n => n.NpcId == tab.Npc.NpcId);
                    if (restoredNpc != null)
                    {
                        tab.Npc = restoredNpc;
                    }
                    else
                    {
                        // NPC was deleted, mark tab for closing
                        tabsToClose.Add(tab);
                    }
                }
            }
            
            // Close tabs that reference deleted elements
            foreach (var tab in tabsToClose)
            {
                CloseTab(tab);
            }
        }

        /// <summary>
        /// Subscribes to property changes on all NPCs and Quests to track changes for undo/redo
        /// </summary>
        private void SubscribeToElementPropertyChanges()
        {
            if (CurrentProject == null)
                return;

            // Subscribe to all quest property changes
            foreach (var quest in CurrentProject.Quests)
            {
                quest.PropertyChanged -= OnElementPropertyChanged;
                quest.PropertyChanged += OnElementPropertyChanged;
            }

            // Subscribe to all NPC property changes
            foreach (var npc in CurrentProject.Npcs)
            {
                npc.PropertyChanged -= OnElementPropertyChanged;
                npc.PropertyChanged += OnElementPropertyChanged;
                
                // Also subscribe to appearance changes
                if (npc.Appearance != null)
                {
                    npc.Appearance.PropertyChanged -= OnElementPropertyChanged;
                    npc.Appearance.PropertyChanged += OnElementPropertyChanged;
                }
            }
        }

        /// <summary>
        /// Unsubscribes from property changes on all NPCs and Quests
        /// </summary>
        private void UnsubscribeFromElementPropertyChanges()
        {
            if (CurrentProject == null)
                return;

            foreach (var quest in CurrentProject.Quests)
            {
                quest.PropertyChanged -= OnElementPropertyChanged;
            }

            foreach (var npc in CurrentProject.Npcs)
            {
                npc.PropertyChanged -= OnElementPropertyChanged;
                if (npc.Appearance != null)
                {
                    npc.Appearance.PropertyChanged -= OnElementPropertyChanged;
                }
            }
        }

        /// <summary>
        /// Handles property changes on NPCs and Quests to trigger debounced snapshot saves
        /// </summary>
        private void OnElementPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            // Don't save snapshots during undo/redo operations
            if (_isRestoringFromUndoRedo)
                return;

            // Schedule a debounced snapshot save
            ScheduleDebouncedSnapshot();
        }

        /// <summary>
        /// Schedules a debounced snapshot save to avoid excessive undo states
        /// </summary>
        private void ScheduleDebouncedSnapshot()
        {
            if (CurrentProject == null || _isRestoringFromUndoRedo)
                return;

            // Cancel existing timer if any
            CancelDebouncedSnapshot();

            // Create new timer with 500ms delay
            _debounceSnapshotTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(500)
            };
            _debounceSnapshotTimer.Tick += (s, e) =>
            {
                if (CurrentProject != null && !_isRestoringFromUndoRedo)
                {
                    _undoRedoService.SaveSnapshot(CurrentProject);
                }
                CancelDebouncedSnapshot();
            };
            _debounceSnapshotTimer.Start();
        }

        /// <summary>
        /// Cancels any pending debounced snapshot save
        /// </summary>
        private void CancelDebouncedSnapshot()
        {
            if (_debounceSnapshotTimer != null)
            {
                _debounceSnapshotTimer.Stop();
                _debounceSnapshotTimer = null;
            }
        }

        #endregion

        #region Event Handlers

        private void OnAppearancePropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            Debug.WriteLine($"[MainViewModel] OnAppearancePropertyChanged - Property: {e.PropertyName}");

            if (sender is NpcAppearanceSettings appearance && SelectedNpc?.Appearance == appearance)
            {
                Debug.WriteLine($"[MainViewModel] Sending appearance update to preview service");
                _appearancePreviewService.SendAppearanceUpdate(appearance);
            }
            else
            {
                Debug.WriteLine($"[MainViewModel] Skipping update - sender check failed");
            }
        }

        private void CurrentProjectOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(QuestProject.IsModified))
            {
                // Save snapshot when project becomes modified (if it wasn't modified before)
                if (CurrentProject.IsModified && !_wasModifiedBeforeChange && !_isRestoringFromUndoRedo)
                {
                    ScheduleDebouncedSnapshot();
                }
                _wasModifiedBeforeChange = CurrentProject.IsModified;
                UpdateProcessState();
                CommandManager.InvalidateRequerySuggested();
            }
            else if (e.PropertyName == nameof(QuestProject.Quests))
            {
                // Resubscribe to quest property changes when collection changes
                SubscribeToElementPropertyChanges();
                _navigationService.UpdateElementCounts(CurrentProject.Quests.Count, CurrentProject.Npcs.Count);
                _navigationService.UpdateWorkspaceProjectInfo(CurrentProject);
            }
            else if (e.PropertyName == nameof(QuestProject.Npcs))
            {
                // Resubscribe to NPC property changes when collection changes
                SubscribeToElementPropertyChanges();
                _navigationService.UpdateElementCounts(CurrentProject.Quests.Count, CurrentProject.Npcs.Count);
                _navigationService.UpdateWorkspaceProjectInfo(CurrentProject);
            }
            else if (e.PropertyName == nameof(QuestProject.Resources))
            {
                OnPropertyChanged(nameof(CurrentProject.Resources));
            }
            else if (e.PropertyName == nameof(QuestProject.ProjectName))
            {
                _navigationService.UpdateWorkspaceProjectInfo(CurrentProject);
                UpdateProcessState();
            }
        }

        #endregion
    }
}
