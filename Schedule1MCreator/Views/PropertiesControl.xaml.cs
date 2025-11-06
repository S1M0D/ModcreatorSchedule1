using System.Windows;
using System.Windows.Controls;
using Schedule1ModdingTool.Models;
using Schedule1ModdingTool.ViewModels;

namespace Schedule1ModdingTool.Views
{
    /// <summary>
    /// Interaction logic for PropertiesControl.xaml
    /// </summary>
    public partial class PropertiesControl : UserControl
    {
        public PropertiesControl()
        {
            InitializeComponent();
        }

        private void AddObjective_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is MainViewModel vm && vm.SelectedQuest != null)
            {
                vm.SelectedQuest.AddObjective();
                vm.CurrentProject.MarkAsModified();
            }
        }

        private void RemoveObjective_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.DataContext is QuestObjective objective)
            {
                if (DataContext is MainViewModel vm && vm.SelectedQuest != null)
                {
                    vm.SelectedQuest.RemoveObjective(objective);
                    vm.CurrentProject.MarkAsModified();
                }
            }
        }
    }
}