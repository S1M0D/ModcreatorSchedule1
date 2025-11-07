using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Schedule1ModdingTool.Models;
using Schedule1ModdingTool.ViewModels;

namespace Schedule1ModdingTool.Views
{
    /// <summary>
    /// Interaction logic for QuestItemsView.xaml
    /// </summary>
    public partial class QuestItemsView : UserControl
    {
        public QuestItemsView()
        {
            InitializeComponent();
        }

        private void BackToCategories_Click(object sender, RoutedEventArgs e)
        {
            var mainWindow = Window.GetWindow(this);
            if (mainWindow?.DataContext is MainViewModel vm)
            {
                vm.WorkspaceViewModel.SelectedCategory = null;
            }
        }

        private void QuestTile_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement element && element.DataContext is QuestBlueprint quest)
            {
                var mainWindow = Window.GetWindow(this);
                if (mainWindow?.DataContext is MainViewModel vm)
                {
                    vm.SelectedQuest = quest;
                }
            }
        }

        private void QuestTile_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement element && element.DataContext is QuestBlueprint quest)
            {
                var mainWindow = Window.GetWindow(this);
                if (mainWindow?.DataContext is MainViewModel vm)
                {
                    vm.SelectedQuest = quest;
                    // Show context menu
                    var contextMenu = this.Resources["QuestContextMenu"] as ContextMenu;
                    if (contextMenu != null)
                    {
                        contextMenu.PlacementTarget = element;
                        contextMenu.IsOpen = true;
                    }
                }
            }
        }

        private void AddQuest_Click(object sender, RoutedEventArgs e)
        {
            var mainWindow = Window.GetWindow(this);
            if (mainWindow?.DataContext is MainViewModel vm && vm.AvailableBlueprints.Count > 0)
            {
                vm.AddQuestCommand.Execute(vm.AvailableBlueprints[0]);
            }
        }

        private void RemoveQuest_Click(object sender, RoutedEventArgs e)
        {
            var mainWindow = Window.GetWindow(this);
            if (mainWindow?.DataContext is MainViewModel vm)
            {
                // Remove the currently selected quest
                if (vm.SelectedQuest != null)
                {
                    vm.RemoveQuestCommand.Execute(null);
                }
            }
        }
    }
}

