using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Windows.Input;
using Schedule1ModdingTool.Models;
using Schedule1ModdingTool.Utils;

namespace Schedule1ModdingTool.ViewModels
{
    /// <summary>
    /// ViewModel for managing workspace state, filtering, sorting, and folder navigation.
    /// </summary>
    public class WorkspaceViewModel : ObservableObject
    {
        private string _searchQuery = string.Empty;
        private ModCategory? _selectedCategory;
        private QuestProject? _project;
        private ModFolder? _selectedFolder;
        private readonly ObservableCollection<ModFolder> _breadcrumb = new ObservableCollection<ModFolder>();
        private readonly HashSet<QuestBlueprint> _observedQuests = new HashSet<QuestBlueprint>();
        private readonly HashSet<NpcBlueprint> _observedNpcs = new HashSet<NpcBlueprint>();
        private readonly HashSet<ItemBlueprint> _observedItems = new HashSet<ItemBlueprint>();

        public WorkspaceViewModel()
        {
            ToggleViewModeCommand = new RelayCommand(() =>
            {
                ViewMode = ViewMode == WorkspaceViewMode.Tiles ? WorkspaceViewMode.List : WorkspaceViewMode.Tiles;
            });

            FilterCommand = new RelayCommand(() => { });
            SortCommand = new RelayCommand(() => { });

            InitializeCategories();
        }

        private string _workspaceTitle = "MOD ELEMENTS";
        private string _projectInfo = string.Empty;
        private WorkspaceViewMode _viewMode = WorkspaceViewMode.Tiles;

        public string WorkspaceTitle
        {
            get => _workspaceTitle;
            set => SetProperty(ref _workspaceTitle, value);
        }

        public string ProjectInfo
        {
            get => _projectInfo;
            set => SetProperty(ref _projectInfo, value);
        }

        public string SearchQuery
        {
            get => _searchQuery;
            set
            {
                if (SetProperty(ref _searchQuery, value))
                {
                    RaiseItemsChanged();
                }
            }
        }

        public WorkspaceViewMode ViewMode
        {
            get => _viewMode;
            set => SetProperty(ref _viewMode, value);
        }

        public ModCategory? SelectedCategory
        {
            get => _selectedCategory;
            set => SetProperty(ref _selectedCategory, value);
        }

        public ObservableCollection<string> FilterOptions { get; } = new ObservableCollection<string>
        {
            "All",
            "Enabled",
            "Disabled",
            "Coming Soon"
        };

        public ObservableCollection<string> SortOptions { get; } = new ObservableCollection<string>
        {
            "Name",
            "Count",
            "Category"
        };

        public ObservableCollection<ModCategoryInfo> Categories { get; } = new ObservableCollection<ModCategoryInfo>();

        public ObservableCollection<ModFolder> Breadcrumb => _breadcrumb;

        public QuestProject? Project
        {
            get => _project;
            private set => SetProperty(ref _project, value);
        }

        public ModFolder? SelectedFolder
        {
            get => _selectedFolder;
            private set
            {
                if (SetProperty(ref _selectedFolder, value))
                {
                    UpdateBreadcrumb();
                    RaiseItemsChanged();
                }
            }
        }

        public IEnumerable<ModFolder> CurrentFolders
        {
            get
            {
                if (Project == null || SelectedFolder == null)
                    return Enumerable.Empty<ModFolder>();

                return Project.Folders
                    .Where(f => f.ParentId == SelectedFolder.Id && f.Id != SelectedFolder.Id)
                    .Where(f => MatchesSearch(f.Name))
                    .OrderBy(f => f.Name, StringComparer.OrdinalIgnoreCase);
            }
        }

        public IEnumerable<QuestBlueprint> CurrentQuests
        {
            get
            {
                if (Project == null || SelectedFolder == null)
                    return Enumerable.Empty<QuestBlueprint>();

                return Project.Quests
                    .Where(q => string.Equals(q.FolderId, SelectedFolder.Id, StringComparison.Ordinal))
                    .Where(q => MatchesSearch(q.DisplayName))
                    .OrderBy(q => q.DisplayName, StringComparer.OrdinalIgnoreCase);
            }
        }

        public IEnumerable<NpcBlueprint> CurrentNpcs
        {
            get
            {
                if (Project == null || SelectedFolder == null)
                    return Enumerable.Empty<NpcBlueprint>();

                return Project.Npcs
                    .Where(n => string.Equals(n.FolderId, SelectedFolder.Id, StringComparison.Ordinal))
                    .Where(n => MatchesSearch(n.DisplayName))
                    .OrderBy(n => n.DisplayName, StringComparer.OrdinalIgnoreCase);
            }
        }

        public IEnumerable<ItemBlueprint> CurrentItems
        {
            get
            {
                if (Project == null || SelectedFolder == null)
                    return Enumerable.Empty<ItemBlueprint>();

                return Project.Items
                    .Where(i => string.Equals(i.FolderId, SelectedFolder.Id, StringComparison.Ordinal))
                    .Where(i => MatchesSearch(i.DisplayName))
                    .OrderBy(i => i.DisplayName, StringComparer.OrdinalIgnoreCase);
            }
        }

        public IEnumerable<object> CurrentTiles =>
            CurrentFolders.Cast<object>()
                .Concat(CurrentNpcs)
                .Concat(CurrentItems)
                .Concat(CurrentQuests);

        public ICommand ToggleViewModeCommand { get; }
        public ICommand FilterCommand { get; }
        public ICommand SortCommand { get; }

        public void BindProject(QuestProject project)
        {
            if (Project == project)
                return;

            if (Project != null)
            {
                Project.Quests.CollectionChanged -= OnProjectQuestsChanged;
                Project.Npcs.CollectionChanged -= OnProjectNpcsChanged;
                Project.Items.CollectionChanged -= OnProjectItemsChanged;
                Project.Folders.CollectionChanged -= OnProjectFoldersChanged;
                foreach (var quest in _observedQuests.ToArray())
                {
                    quest.PropertyChanged -= BlueprintOnPropertyChanged;
                }
                foreach (var npc in _observedNpcs.ToArray())
                {
                    npc.PropertyChanged -= BlueprintOnPropertyChanged;
                }
                foreach (var item in _observedItems.ToArray())
                {
                    item.PropertyChanged -= BlueprintOnPropertyChanged;
                }
                _observedQuests.Clear();
                _observedNpcs.Clear();
                _observedItems.Clear();
            }

            Project = project;
            Project.Quests.CollectionChanged += OnProjectQuestsChanged;
            Project.Npcs.CollectionChanged += OnProjectNpcsChanged;
            Project.Items.CollectionChanged += OnProjectItemsChanged;
            Project.Folders.CollectionChanged += OnProjectFoldersChanged;

            foreach (var quest in Project.Quests)
            {
                ObserveBlueprint(quest);
            }

            foreach (var npc in Project.Npcs)
            {
                ObserveBlueprint(npc);
            }

            foreach (var item in Project.Items)
            {
                ObserveBlueprint(item);
            }

            SelectedFolder = Project.GetFolderById(SelectedFolder?.Id ?? QuestProject.RootFolderId) ??
                             Project.GetFolderById(QuestProject.RootFolderId);
            UpdateBreadcrumb();
            RaiseItemsChanged();
        }

        public void NavigateToFolder(ModFolder folder)
        {
            if (folder == null)
                return;
            SelectedFolder = folder;
        }

        public void NavigateToFolder(string? folderId)
        {
            if (Project == null || string.IsNullOrWhiteSpace(folderId))
                return;
            var folder = Project.GetFolderById(folderId);
            if (folder != null)
            {
                SelectedFolder = folder;
            }
        }

        public ModFolder CreateFolder(string? name = null)
        {
            if (Project == null)
                throw new InvalidOperationException("Project not initialized.");

            var parentId = SelectedFolder?.Id ?? QuestProject.RootFolderId;
            var folderName = string.IsNullOrWhiteSpace(name)
                ? $"Folder {Project.Folders.Count(f => f.ParentId == parentId) + 1}"
                : name;
            var folder = Project.CreateFolder(folderName, parentId);
            SelectedFolder = folder;
            return folder;
        }

        public void GoToParentFolder()
        {
            if (Project == null || SelectedFolder == null)
                return;

            if (string.IsNullOrWhiteSpace(SelectedFolder.ParentId))
                return;

            NavigateToFolder(SelectedFolder.ParentId);
        }

        public void UpdateQuestCount(int count)
        {
            var questCategory = Categories.FirstOrDefault(c => c.Category == ModCategory.Quests);
            if (questCategory != null)
            {
                questCategory.Count = count;
            }
        }

        public void UpdateNpcCount(int count)
        {
            var npcCategory = Categories.FirstOrDefault(c => c.Category == ModCategory.NPCs);
            if (npcCategory != null)
            {
                npcCategory.Count = count;
            }
        }

        public void UpdateItemCount(int count)
        {
            var itemCategory = Categories.FirstOrDefault(c => c.Category == ModCategory.Items);
            if (itemCategory != null)
            {
                itemCategory.Count = count;
            }
        }

        private void InitializeCategories()
        {
            Categories.Add(new ModCategoryInfo
            {
                Category = ModCategory.Quests,
                DisplayName = "Quests",
                IconKey = "QuestIcon",
                Description = "Create and manage quests for your mod",
                IsEnabled = true,
                Count = 0
            });

            Categories.Add(new ModCategoryInfo
            {
                Category = ModCategory.NPCs,
                DisplayName = "NPCs",
                IconKey = "NPCsIcon",
                Description = "Create custom non-player characters",
                IsEnabled = true,
                Count = 0
            });

            Categories.Add(new ModCategoryInfo
            {
                Category = ModCategory.PhoneApps,
                DisplayName = "Phone Apps",
                IconKey = "PhoneAppsIcon",
                Description = "Create custom phone applications",
                IsEnabled = false,
                Count = 0,
                ComingSoonText = "Coming Soon"
            });

            Categories.Add(new ModCategoryInfo
            {
                Category = ModCategory.Items,
                DisplayName = "Items",
                IconKey = "ItemsIcon",
                Description = "Create custom items",
                IsEnabled = true,
                Count = 0,
                ComingSoonText = string.Empty
            });
        }

        private void OnProjectQuestsChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems != null)
            {
                foreach (var item in e.NewItems.OfType<QuestBlueprint>())
                {
                    ObserveBlueprint(item);
                }
            }

            if (e.OldItems != null)
            {
                foreach (var item in e.OldItems.OfType<QuestBlueprint>())
                {
                    if (_observedQuests.Remove(item))
                    {
                        item.PropertyChanged -= BlueprintOnPropertyChanged;
                    }
                }
            }

            RaiseItemsChanged();
        }

        private void OnProjectNpcsChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems != null)
            {
                foreach (var item in e.NewItems.OfType<NpcBlueprint>())
                {
                    ObserveBlueprint(item);
                }
            }

            if (e.OldItems != null)
            {
                foreach (var item in e.OldItems.OfType<NpcBlueprint>())
                {
                    if (_observedNpcs.Remove(item))
                    {
                        item.PropertyChanged -= BlueprintOnPropertyChanged;
                    }
                }
            }

            RaiseItemsChanged();
        }

        private void OnProjectItemsChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems != null)
            {
                foreach (var item in e.NewItems.OfType<ItemBlueprint>())
                {
                    ObserveBlueprint(item);
                }
            }

            if (e.OldItems != null)
            {
                foreach (var item in e.OldItems.OfType<ItemBlueprint>())
                {
                    if (_observedItems.Remove(item))
                    {
                        item.PropertyChanged -= BlueprintOnPropertyChanged;
                    }
                }
            }

            RaiseItemsChanged();
        }

        private void OnProjectFoldersChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (Project == null)
                return;

            if (SelectedFolder == null)
            {
                SelectedFolder = Project.GetFolderById(QuestProject.RootFolderId);
            }
            else
            {
                var refreshed = Project.GetFolderById(SelectedFolder.Id);
                SelectedFolder = refreshed ?? Project.GetFolderById(QuestProject.RootFolderId);
            }

            RaiseItemsChanged();
        }

        private void BlueprintOnPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(QuestBlueprint.FolderId) ||
                e.PropertyName == nameof(NpcBlueprint.FolderId) ||
                e.PropertyName == nameof(ItemBlueprint.FolderId))
            {
                RaiseItemsChanged();
            }
        }

        private void ObserveBlueprint(QuestBlueprint quest)
        {
            if (_observedQuests.Add(quest))
            {
                quest.PropertyChanged += BlueprintOnPropertyChanged;
            }
        }

        private void ObserveBlueprint(NpcBlueprint npc)
        {
            if (_observedNpcs.Add(npc))
            {
                npc.PropertyChanged += BlueprintOnPropertyChanged;
            }
        }

        private void ObserveBlueprint(ItemBlueprint item)
        {
            if (_observedItems.Add(item))
            {
                item.PropertyChanged += BlueprintOnPropertyChanged;
            }
        }

        private void UpdateBreadcrumb()
        {
            _breadcrumb.Clear();
            if (Project == null)
                return;

            var stack = new Stack<ModFolder>();
            var cursor = SelectedFolder ?? Project.GetFolderById(QuestProject.RootFolderId);
            while (cursor != null)
            {
                stack.Push(cursor);
                if (string.IsNullOrWhiteSpace(cursor.ParentId))
                    break;
                cursor = Project.GetFolderById(cursor.ParentId);
            }

            while (stack.Count > 0)
            {
                _breadcrumb.Add(stack.Pop());
            }

            if (_breadcrumb.Count == 0 && Project != null)
            {
                var root = Project.GetFolderById(QuestProject.RootFolderId);
                if (root != null)
                    _breadcrumb.Add(root);
            }

            OnPropertyChanged(nameof(Breadcrumb));
        }

        private void RaiseItemsChanged()
        {
            OnPropertyChanged(nameof(CurrentFolders));
            OnPropertyChanged(nameof(CurrentQuests));
            OnPropertyChanged(nameof(CurrentNpcs));
            OnPropertyChanged(nameof(CurrentItems));
            OnPropertyChanged(nameof(CurrentTiles));
        }

        private bool MatchesSearch(string? value)
        {
            if (string.IsNullOrWhiteSpace(SearchQuery))
                return true;
            return !string.IsNullOrWhiteSpace(value) &&
                   value.IndexOf(SearchQuery, StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }

    public enum WorkspaceViewMode
    {
        Tiles,
        List
    }
}
