using System.Windows;
using System.Windows.Controls;
using Schedule1ModdingTool.Models;
using Schedule1ModdingTool.ViewModels;

namespace Schedule1ModdingTool.Views
{
    /// <summary>
    /// Interaction logic for GlobalStatePropertiesControl.xaml.
    /// </summary>
    public partial class GlobalStatePropertiesControl : UserControl
    {
        public GlobalStatePropertiesControl()
        {
            InitializeComponent();
        }

        private MainViewModel? ViewModel => DataContext as MainViewModel;

        private GlobalStateBlueprint? CurrentGlobalState => ViewModel?.SelectedGlobalState;

        private void AddField_Click(object sender, RoutedEventArgs e)
        {
            var globalState = CurrentGlobalState;
            if (globalState == null)
            {
                return;
            }

            globalState.Fields.Add(new GlobalStateFieldBlueprint
            {
                FieldName = $"Field{globalState.Fields.Count + 1}",
                FieldType = DataClassFieldType.Bool
            });
        }

        private void DuplicateField_Click(object sender, RoutedEventArgs e)
        {
            var globalState = CurrentGlobalState;
            var field = GetTag<GlobalStateFieldBlueprint>(sender);
            if (globalState == null || field == null)
            {
                return;
            }

            var index = globalState.Fields.IndexOf(field);
            if (index < 0)
            {
                return;
            }

            var copy = field.DeepCopy();
            if (!string.IsNullOrWhiteSpace(copy.FieldName))
            {
                copy.FieldName += "Copy";
            }

            globalState.Fields.Insert(index + 1, copy);
        }

        private void RemoveField_Click(object sender, RoutedEventArgs e)
        {
            var globalState = CurrentGlobalState;
            var field = GetTag<GlobalStateFieldBlueprint>(sender);
            if (globalState == null || field == null)
            {
                return;
            }

            globalState.Fields.Remove(field);
        }

        private static T? GetTag<T>(object sender) where T : class
        {
            return (sender as FrameworkElement)?.Tag as T;
        }
    }
}
