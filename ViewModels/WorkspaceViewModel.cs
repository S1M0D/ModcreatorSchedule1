using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using Schedule1ModdingTool.Models;
using Schedule1ModdingTool.Utils;

namespace Schedule1ModdingTool.ViewModels
{
    /// <summary>
    /// ViewModel for managing workspace state, filtering, sorting, and navigation
    /// </summary>
    public class WorkspaceViewModel : ObservableObject
    {
        private string _searchQuery = "";
        private string _selectedFilter = "All";
        private string _selectedSort = "Name";
        private ModCategory? _selectedCategory;
        private WorkspaceViewMode _viewMode = WorkspaceViewMode.Tiles;

        private string _workspaceTitle = "MOD ELEMENTS";
        private string _projectInfo = "";

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
            set => SetProperty(ref _searchQuery, value);
        }

        public string SelectedFilter
        {
            get => _selectedFilter;
            set => SetProperty(ref _selectedFilter, value);
        }

        public string SelectedSort
        {
            get => _selectedSort;
            set => SetProperty(ref _selectedSort, value);
        }

        public ModCategory? SelectedCategory
        {
            get => _selectedCategory;
            set => SetProperty(ref _selectedCategory, value);
        }

        public WorkspaceViewMode ViewMode
        {
            get => _viewMode;
            set => SetProperty(ref _viewMode, value);
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

        public ICommand ToggleViewModeCommand { get; }
        public ICommand FilterCommand { get; }
        public ICommand SortCommand { get; }

        public WorkspaceViewModel()
        {
            ToggleViewModeCommand = new RelayCommand(() =>
            {
                ViewMode = ViewMode == WorkspaceViewMode.Tiles ? WorkspaceViewMode.List : WorkspaceViewMode.Tiles;
            });

            FilterCommand = new RelayCommand(() =>
            {
                // TODO: Show filter dropdown
            });

            SortCommand = new RelayCommand(() =>
            {
                // TODO: Show sort dropdown
            });

            InitializeCategories();
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
                IsEnabled = false,
                Count = 0,
                ComingSoonText = "Coming Soon"
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
                IsEnabled = false,
                Count = 0,
                ComingSoonText = "Coming Soon"
            });
        }

        public void UpdateQuestCount(int count)
        {
            var questCategory = Categories.FirstOrDefault(c => c.Category == ModCategory.Quests);
            if (questCategory != null)
            {
                questCategory.Count = count;
            }
        }
    }

    /// <summary>
    /// Workspace view display mode
    /// </summary>
    public enum WorkspaceViewMode
    {
        Tiles,
        List
    }
}

