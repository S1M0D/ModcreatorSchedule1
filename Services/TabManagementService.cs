using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using Schedule1ModdingTool.Models;
using Schedule1ModdingTool.ViewModels;

namespace Schedule1ModdingTool.Services
{
    /// <summary>
    /// Manages tab operations including opening, closing, and workspace tab logic.
    /// </summary>
    public class TabManagementService : INotifyPropertyChanged
    {
        private readonly ObservableCollection<OpenElementTab> _openTabs;
        private OpenElementTab? _selectedTab;

        /// <summary>
        /// Event raised when a property changes.
        /// </summary>
        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>
        /// Gets the collection of open tabs.
        /// </summary>
        public ObservableCollection<OpenElementTab> OpenTabs => _openTabs;

        /// <summary>
        /// Gets or sets the currently selected tab.
        /// </summary>
        public OpenElementTab? SelectedTab
        {
            get => _selectedTab;
            set
            {
                if (ReferenceEquals(_selectedTab, value))
                    return;

                if (_selectedTab != null)
                {
                    _selectedTab.IsSelected = false;
                }
                _selectedTab = value;
                if (_selectedTab != null)
                {
                    _selectedTab.IsSelected = true;
                }
                OnPropertyChanged(nameof(SelectedTab));
            }
        }

        /// <summary>
        /// Raises the PropertyChanged event.
        /// </summary>
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public TabManagementService(ObservableCollection<OpenElementTab> openTabs)
        {
            _openTabs = openTabs;
        }

        /// <summary>
        /// Opens or activates the workspace tab.
        /// </summary>
        public void OpenWorkspaceTab()
        {
            // Check if workspace tab already exists
            var existingTab = _openTabs.FirstOrDefault(t => t.IsWorkspace);
            if (existingTab != null)
            {
                SelectedTab = existingTab;
                return;
            }

            // Create workspace tab and add it as the first tab
            var tab = new OpenElementTab { IsWorkspace = true };
            _openTabs.Insert(0, tab);
            SelectedTab = tab;
        }

        /// <summary>
        /// Ensures the workspace tab exists before adding editor tabs.
        /// </summary>
        public void EnsureWorkspaceTabExists()
        {
            // Check if any editor tabs exist (non-workspace tabs)
            var hasEditorTabs = _openTabs.Any(t => !t.IsWorkspace);

            // If no editor tabs exist yet, we're about to add the first one
            // So we need to add the workspace tab first
            if (!hasEditorTabs)
            {
                // Check if workspace tab already exists
                var workspaceTab = _openTabs.FirstOrDefault(t => t.IsWorkspace);
                if (workspaceTab == null)
                {
                    // Add workspace tab as the first tab
                    var tab = new OpenElementTab { IsWorkspace = true };
                    _openTabs.Insert(0, tab);
                }
            }
        }

        /// <summary>
        /// Opens a quest in a tab, or activates it if already open.
        /// </summary>
        /// <param name="quest">The quest to open.</param>
        public void OpenQuestInTab(QuestBlueprint quest)
        {
            // Check if quest is already open
            var existingTab = _openTabs.FirstOrDefault(t => t.Quest == quest);
            if (existingTab != null)
            {
                SelectedTab = existingTab;
                return;
            }

            // Ensure workspace tab exists before adding editor tab
            EnsureWorkspaceTabExists();

            // Create new tab
            var tab = new OpenElementTab { Quest = quest, Npc = null };
            _openTabs.Add(tab);
            SelectedTab = tab;
        }

        /// <summary>
        /// Opens an NPC in a tab, or activates it if already open.
        /// </summary>
        /// <param name="npc">The NPC to open.</param>
        public void OpenNpcInTab(NpcBlueprint npc)
        {
            var existingTab = _openTabs.FirstOrDefault(t => t.Npc == npc);
            if (existingTab != null)
            {
                SelectedTab = existingTab;
                return;
            }

            // Ensure workspace tab exists before adding editor tab
            EnsureWorkspaceTabExists();

            var tab = new OpenElementTab { Npc = npc };
            _openTabs.Add(tab);
            SelectedTab = tab;
        }

        /// <summary>
        /// Closes a tab and handles workspace tab cleanup.
        /// </summary>
        /// <param name="tab">The tab to close.</param>
        /// <returns>True if the tab was closed successfully.</returns>
        public bool CloseTab(OpenElementTab tab)
        {
            // If closing workspace tab, check if other tabs exist
            if (tab.IsWorkspace)
            {
                var editorTabs = _openTabs.Where(t => !t.IsWorkspace).ToList();
                if (editorTabs.Count == 0)
                {
                    // No editor tabs exist, don't allow closing workspace tab
                    return false;
                }
            }

            // If closing the last editor tab, also close workspace tab
            if (!tab.IsWorkspace)
            {
                var editorTabs = _openTabs.Where(t => !t.IsWorkspace).ToList();
                if (editorTabs.Count == 1 && editorTabs[0] == tab)
                {
                    // This is the last editor tab, close workspace tab too
                    var workspaceTab = _openTabs.FirstOrDefault(t => t.IsWorkspace);
                    if (workspaceTab != null)
                    {
                        _openTabs.Remove(workspaceTab);
                    }
                }
            }

            if (tab == SelectedTab)
            {
                var index = _openTabs.IndexOf(tab);
                if (index > 0)
                {
                    SelectedTab = _openTabs[index - 1];
                }
                else if (_openTabs.Count > 1)
                {
                    SelectedTab = _openTabs[1];
                }
                else
                {
                    SelectedTab = null;
                }
            }
            _openTabs.Remove(tab);
            return true;
        }
    }
}
