using Schedule1ModdingTool.Models;
using Schedule1ModdingTool.ViewModels;
using System.Windows;

namespace Schedule1ModdingTool.Views
{
    public partial class ModelsView : UserControl
    {
        public ModelsView() => InitializeComponent();

        private void PreviewModel_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button { DataContext: ModelAsset model } && DataContext is MainViewModel viewModel)
                GlbPreviewWindow.ShowModel(Window.GetWindow(this), viewModel.CurrentProject, model.RelativePath);
        }

        private void PrefabName_LostFocus(object sender, RoutedEventArgs e)
        {
            if (sender is not TextBox { DataContext: ModelAsset model } || DataContext is not MainViewModel viewModel)
                return;
            foreach (var item in viewModel.CurrentProject.Items)
                if (string.Equals(item.ModelBundleResourcePath, model.RelativePath, StringComparison.OrdinalIgnoreCase))
                    item.ModelPrefabName = model.PrefabName;
        }
    }
}
