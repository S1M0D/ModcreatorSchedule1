using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Schedule1ModdingTool.Models;
using Schedule1ModdingTool.ViewModels;

namespace Schedule1ModdingTool.Views
{
    /// <summary>
    /// Interaction logic for WorkspaceControl.xaml
    /// </summary>
    public partial class WorkspaceControl : UserControl
    {
        private System.Windows.Threading.DispatcherTimer _doubleClickTimer;
        private QuestBlueprint? _lastClickedQuest;
        private const int DoubleClickDelay = 300; // milliseconds

        public WorkspaceControl()
        {
            InitializeComponent();
            _doubleClickTimer = new System.Windows.Threading.DispatcherTimer
            {
                Interval = System.TimeSpan.FromMilliseconds(DoubleClickDelay)
            };
            _doubleClickTimer.Tick += DoubleClickTimer_Tick;
        }

        private void NewQuest_Click(object sender, RoutedEventArgs e)
        {
            var mainWindow = Window.GetWindow(this);
            if (mainWindow?.DataContext is MainViewModel vm && vm.AvailableBlueprints.Count > 0)
            {
                vm.AddQuestCommand.Execute(vm.AvailableBlueprints[0]);
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

                    // Handle double-click detection
                    if (_lastClickedQuest == quest && _doubleClickTimer.IsEnabled)
                    {
                        _doubleClickTimer.Stop();
                        _lastClickedQuest = null;
                        // Double-click detected
                        vm.OpenQuestInTab(quest);
                    }
                    else
                    {
                        _lastClickedQuest = quest;
                        _doubleClickTimer.Start();
                    }
                }
            }
        }

        private void DoubleClickTimer_Tick(object? sender, System.EventArgs e)
        {
            _doubleClickTimer.Stop();
            _lastClickedQuest = null;
        }

        private void QuestTile_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Context menu is handled by the Grid's ContextMenu
        }
    }
}
