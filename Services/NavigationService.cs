using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using Schedule1ModdingTool.Models;
using Schedule1ModdingTool.ViewModels;

namespace Schedule1ModdingTool.Services
{
    /// <summary>
    /// Manages navigation state and workspace information.
    /// </summary>
    public class NavigationService : INotifyPropertyChanged
    {
        private readonly ObservableCollection<NavigationItem> _navigationItems;
        private readonly WorkspaceViewModel _workspaceViewModel;
        private NavigationItem? _selectedNavigationItem;

        /// <summary>
        /// Event raised when a property changes.
        /// </summary>
        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>
        /// Gets the collection of navigation items.
        /// </summary>
        public ObservableCollection<NavigationItem> NavigationItems => _navigationItems;

        /// <summary>
        /// Gets or sets the selected navigation item.
        /// </summary>
        public NavigationItem? SelectedNavigationItem
        {
            get => _selectedNavigationItem;
            set
            {
                if (ReferenceEquals(_selectedNavigationItem, value))
                    return;

                if (_selectedNavigationItem != null)
                {
                    _selectedNavigationItem.IsSelected = false;
                }
                _selectedNavigationItem = value;
                if (_selectedNavigationItem != null)
                {
                    _selectedNavigationItem.IsSelected = true;
                }
                OnPropertyChanged(nameof(SelectedNavigationItem));
            }
        }

        /// <summary>
        /// Raises the PropertyChanged event.
        /// </summary>
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public NavigationService(ObservableCollection<NavigationItem> navigationItems, WorkspaceViewModel workspaceViewModel)
        {
            _navigationItems = navigationItems;
            _workspaceViewModel = workspaceViewModel;
        }

        /// <summary>
        /// Initializes the navigation items with default values.
        /// </summary>
        public void InitializeNavigation()
        {
            _navigationItems.Add(new NavigationItem
            {
                Id = "ModElements",
                DisplayName = "Mod Elements",
                IconKey = "CubeIcon",
                IsEnabled = true,
                IsSelected = true,
                Tooltip = "Create and manage mod elements (Quests, NPCs, etc.)"
            });

            _navigationItems.Add(new NavigationItem
            {
                Id = "Resources",
                DisplayName = "Resources",
                IconKey = "FolderIcon",
                IsEnabled = true,
                IsSelected = false,
                Tooltip = "Manage custom resources (icons, images, etc.)"
            });

            SelectedNavigationItem = _navigationItems.First();
        }

        /// <summary>
        /// Selects a navigation item and updates the workspace title.
        /// </summary>
        /// <param name="item">The navigation item to select.</param>
        public void SelectNavigation(NavigationItem? item)
        {
            if (item != null && item.IsEnabled)
            {
                SelectedNavigationItem = item;
                // Reset category selection when switching navigation
                _workspaceViewModel.SelectedCategory = null;
                // Update workspace title based on selected navigation
                _workspaceViewModel.WorkspaceTitle = item.Id switch
                {
                    "ModElements" => "MOD ELEMENTS",
                    "Resources" => "RESOURCES",
                    _ => "WORKSPACE"
                };
            }
        }

        /// <summary>
        /// Selects a category and updates the workspace title.
        /// </summary>
        /// <param name="category">The category to select.</param>
        public void SelectCategory(ModCategory category)
        {
            _workspaceViewModel.SelectedCategory = category;
            // Update workspace title based on selected category
            _workspaceViewModel.WorkspaceTitle = category switch
            {
                ModCategory.Quests => "QUESTS",
                ModCategory.NPCs => "NPCS",
                ModCategory.PhoneApps => "PHONE APPS",
                ModCategory.Items => "ITEMS",
                _ => "MOD ELEMENTS"
            };
        }

        /// <summary>
        /// Updates the workspace project info display.
        /// </summary>
        /// <param name="project">The current project.</param>
        public void UpdateWorkspaceProjectInfo(QuestProject project)
        {
            var projectName = string.IsNullOrEmpty(project.ProjectName) ? "Untitled Project" : project.ProjectName;
            var totalElements = project.Quests.Count;
            _workspaceViewModel.ProjectInfo = $"{projectName}: {totalElements} mod elements";
        }

        /// <summary>
        /// Updates quest and NPC counts in the workspace.
        /// </summary>
        /// <param name="questCount">Number of quests.</param>
        /// <param name="npcCount">Number of NPCs.</param>
        public void UpdateElementCounts(int questCount, int npcCount)
        {
            _workspaceViewModel.UpdateQuestCount(questCount);
            _workspaceViewModel.UpdateNpcCount(npcCount);
        }
    }
}
