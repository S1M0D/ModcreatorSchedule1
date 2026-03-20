using System;
using System.Drawing;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Schedule1ModdingTool.Models;
using Schedule1ModdingTool.Services;
using Schedule1ModdingTool.Utils;
using Schedule1ModdingTool.ViewModels;

namespace Schedule1ModdingTool.Views
{
    /// <summary>
    /// Interaction logic for ItemPropertiesControl.xaml.
    /// </summary>
    public partial class ItemPropertiesControl : UserControl
    {
        public ItemPropertiesControl()
        {
            InitializeComponent();
        }

        private MainViewModel? ViewModel => DataContext as MainViewModel;

        private ItemBlueprint? SelectedItem => ViewModel?.SelectedItemBlueprint;

        private ChemistryRecipeBlueprint? SelectedRecipe => ChemistryRecipesListBox.SelectedItem as ChemistryRecipeBlueprint;

        private ChemistryRecipeIngredientBlueprint? SelectedIngredient => RecipeIngredientsListBox.SelectedItem as ChemistryRecipeIngredientBlueprint;

        private void AddSpecificShop_Click(object sender, RoutedEventArgs e)
        {
            var item = SelectedItem;
            var shopName = SpecificShopNameComboBox.Text?.Trim();
            if (item == null || string.IsNullOrWhiteSpace(shopName))
                return;

            if (!item.ShopNames.Any(existing => string.Equals(existing, shopName, StringComparison.OrdinalIgnoreCase)))
            {
                item.ShopNames.Add(shopName);
            }

            SpecificShopNameComboBox.Text = string.Empty;
        }

        private void RemoveSpecificShop_Click(object sender, RoutedEventArgs e)
        {
            var item = SelectedItem;
            if (item == null || SpecificShopsListBox.SelectedItem is not string selectedShop)
                return;

            item.ShopNames.Remove(selectedShop);
        }

        private void AddBlockedSlot_Click(object sender, RoutedEventArgs e)
        {
            var item = SelectedItem;
            if (item == null || BlockedSlotComboBox.SelectedItem is not ClothingSlotOption slot)
                return;

            if (!item.BlockedClothingSlots.Contains(slot))
            {
                item.BlockedClothingSlots.Add(slot);
            }
        }

        private void RemoveBlockedSlot_Click(object sender, RoutedEventArgs e)
        {
            var item = SelectedItem;
            if (item == null || BlockedSlotsListBox.SelectedItem is not ClothingSlotOption slot)
                return;

            item.BlockedClothingSlots.Remove(slot);
        }

        private void UseLiveItemAsCloneSource_Click(object sender, RoutedEventArgs e)
        {
            var item = SelectedItem;
            if (item == null || !item.SupportsCloneSource || LiveItemBrowserListBox.SelectedItem is not GameItemCatalogEntry selectedCatalogItem)
                return;

            item.CloneSourceItemId = selectedCatalogItem.ItemId;
        }

        private void AddLiveShop_Click(object sender, RoutedEventArgs e)
        {
            var item = SelectedItem;
            if (item == null || LiveShopBrowserListBox.SelectedItem is not ShopCompatibilityPreview preview)
                return;

            item.ShopIntegrationMode = ShopIntegrationModeOption.Specific;
            if (!item.ShopNames.Any(existing => string.Equals(existing, preview.Name, StringComparison.OrdinalIgnoreCase)))
            {
                item.ShopNames.Add(preview.Name);
            }
        }

        private void ApplyEquippablePreset_Click(object sender, RoutedEventArgs e)
        {
            var item = SelectedItem;
            if (item == null || EquippablePresetComboBox.SelectedItem is not ItemEquippablePreset preset)
                return;

            item.EquippableType = preset.EquippableType;
            item.HasViewmodelTransform = preset.EquippableType == EquippableTypeOption.Viewmodel;
            item.ViewmodelPositionX = preset.PositionX;
            item.ViewmodelPositionY = preset.PositionY;
            item.ViewmodelPositionZ = preset.PositionZ;
            item.ViewmodelRotationX = preset.RotationX;
            item.ViewmodelRotationY = preset.RotationY;
            item.ViewmodelRotationZ = preset.RotationZ;
            item.ViewmodelScaleX = preset.ScaleX;
            item.ViewmodelScaleY = preset.ScaleY;
            item.ViewmodelScaleZ = preset.ScaleZ;
            item.HasAvatarEquippable = !string.IsNullOrWhiteSpace(preset.AvatarEquippableAssetPath);
            item.AvatarEquippableAssetPath = preset.AvatarEquippableAssetPath;
            item.AvatarHand = preset.AvatarHand;
            item.AvatarAnimationTrigger = preset.AvatarAnimationTrigger;
        }

        private void AddChemistryRecipe_Click(object sender, RoutedEventArgs e)
        {
            var item = SelectedItem;
            if (item == null)
                return;

            var recipe = new ChemistryRecipeBlueprint();
            recipe.Ingredients.Add(new ChemistryRecipeIngredientBlueprint());
            item.ChemistryRecipes.Add(recipe);
            ChemistryRecipesListBox.SelectedItem = recipe;
        }

        private void RemoveChemistryRecipe_Click(object sender, RoutedEventArgs e)
        {
            var item = SelectedItem;
            var recipe = SelectedRecipe;
            if (item == null || recipe == null)
                return;

            item.ChemistryRecipes.Remove(recipe);
        }

        private void AddChemistryIngredient_Click(object sender, RoutedEventArgs e)
        {
            var recipe = SelectedRecipe;
            if (recipe == null)
                return;

            var ingredient = new ChemistryRecipeIngredientBlueprint();
            recipe.Ingredients.Add(ingredient);
            RecipeIngredientsListBox.SelectedItem = ingredient;
        }

        private void RemoveChemistryIngredient_Click(object sender, RoutedEventArgs e)
        {
            var recipe = SelectedRecipe;
            var ingredient = SelectedIngredient;
            if (recipe == null || ingredient == null)
                return;

            recipe.Ingredients.Remove(ingredient);
        }

        private void AddIngredientItemOption_Click(object sender, RoutedEventArgs e)
        {
            var ingredient = SelectedIngredient;
            if (ingredient == null)
                return;

            var itemId = (RecipeIngredientItemComboBox.SelectedItem as ItemReferenceInfo)?.Id
                ?? RecipeIngredientItemComboBox.Text?.Trim();

            if (string.IsNullOrWhiteSpace(itemId))
                return;

            if (!ingredient.ItemIds.Any(existing => string.Equals(existing, itemId, StringComparison.OrdinalIgnoreCase)))
            {
                ingredient.ItemIds.Add(itemId);
            }

            RecipeIngredientItemComboBox.Text = string.Empty;
        }

        private void RemoveIngredientItemOption_Click(object sender, RoutedEventArgs e)
        {
            var ingredient = SelectedIngredient;
            if (ingredient == null || IngredientItemIdsListBox.SelectedItem is not string itemId)
                return;

            ingredient.ItemIds.Remove(itemId);
        }

        private void PickRecipeColor_Click(object sender, RoutedEventArgs e)
        {
            var recipe = SelectedRecipe;
            if (recipe == null)
                return;

            var pickedColor = PickColor(recipe.FinalLiquidColorHex);
            if (!string.IsNullOrWhiteSpace(pickedColor))
            {
                recipe.FinalLiquidColorHex = pickedColor;
            }
        }

        private static string? PickColor(string currentHex)
        {
            var (a, r, g, b) = ColorUtils.ParseHex(currentHex);
            using var dialog = new System.Windows.Forms.ColorDialog
            {
                AllowFullOpen = true,
                FullOpen = true,
                Color = Color.FromArgb(a, r, g, b)
            };

            return dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK
                ? $"#{dialog.Color.A:X2}{dialog.Color.R:X2}{dialog.Color.G:X2}{dialog.Color.B:X2}"
                : null;
        }
    }
}
