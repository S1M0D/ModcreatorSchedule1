using Schedule1ModdingTool.Models;
using System.Windows.Controls;

namespace Schedule1ModdingTool.Views
{
    public partial class ItemModelPicker : UserControl
    {
        public ItemModelPicker() => InitializeComponent();

        private void ModelSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DataContext is ItemBlueprint item && e.AddedItems.OfType<ModelAsset>().FirstOrDefault() is { } model)
            {
                item.ModelPrefabName = model.PrefabName;
                if (item.ItemType == ItemKindOption.Clothing && string.IsNullOrWhiteSpace(item.ClothingAssetPath))
                    item.ClothingAssetPath = item.SuggestedClothingAssetPath;
            }
        }

        private void ClearModel_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (DataContext is not ItemBlueprint item) return;
            item.ModelBundleResourcePath = string.Empty;
            item.ModelPrefabName = string.Empty;
        }
    }
}
