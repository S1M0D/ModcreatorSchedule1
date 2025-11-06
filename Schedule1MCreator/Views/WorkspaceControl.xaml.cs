using System.Windows;
using System.Windows.Controls;
using Schedule1ModdingTool.Models;
using Schedule1ModdingTool.ViewModels;

namespace Schedule1ModdingTool.Views
{
    /// <summary>
    /// Interaction logic for WorkspaceControl.xaml
    /// </summary>
    public partial class WorkspaceControl : UserControl
    {
        public WorkspaceControl()
        {
            InitializeComponent();
        }

        private void QuestListBox_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(typeof(QuestBlueprint)))
            {
                var template = (QuestBlueprint)e.Data.GetData(typeof(QuestBlueprint));
                if (DataContext is MainViewModel vm)
                {
                    vm.AddQuestCommand.Execute(template);
                }
            }
        }

        private void QuestListBox_DragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(typeof(QuestBlueprint)))
            {
                e.Effects = DragDropEffects.Copy;
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }
        }
    }
}