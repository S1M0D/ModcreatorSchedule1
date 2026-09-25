using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Schedule1ModdingTool.Models;
using Schedule1ModdingTool.Utils;
using DrawingColor = System.Drawing.Color;

namespace Schedule1ModdingTool.Views
{
    public partial class ChemistryRecipesEditor : UserControl
    {
        public ChemistryRecipesEditor()
        {
            InitializeComponent();
        }

        private ItemBlueprint? SelectedItem => DataContext as ItemBlueprint;
        private ChemistryRecipeBlueprint? SelectedRecipe => ChemistryRecipesListBox.SelectedItem as ChemistryRecipeBlueprint;
        private ChemistryRecipeIngredientBlueprint? SelectedIngredient => RecipeIngredientsListBox.SelectedItem as ChemistryRecipeIngredientBlueprint;

        private void AddChemistryRecipe_Click(object sender, RoutedEventArgs e)
        {
            var item = SelectedItem;
            if (item == null)
                return;

            var recipe = new ChemistryRecipeBlueprint();
            if (item.ItemId.Contains(':'))
            {
                var prefix = item.ItemId.Split(':')[0];
                var name = item.ItemId.Split(':')[1].Replace('/', '_');
                var index = 1;
                string candidate;
                do
                {
                    candidate = $"{prefix}:{name}_recipe_{index++}";
                } while (item.ChemistryRecipes.Any(existing =>
                    string.Equals(existing.RecipeId, candidate, StringComparison.OrdinalIgnoreCase)));
                recipe.RecipeId = candidate;
            }
            recipe.Ingredients.Add(new ChemistryRecipeIngredientBlueprint());
            item.ChemistryRecipes.Add(recipe);
            ChemistryRecipesListBox.SelectedItem = recipe;
        }

        private void RemoveChemistryRecipe_Click(object sender, RoutedEventArgs e)
        {
            var recipe = SelectedRecipe;
            if (SelectedItem == null || recipe == null)
                return;
            SelectedItem.ChemistryRecipes.Remove(recipe);
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
            if (recipe != null && ingredient != null)
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
                ingredient.ItemIds.Add(itemId);
            RecipeIngredientItemComboBox.Text = string.Empty;
        }

        private void RemoveIngredientItemOption_Click(object sender, RoutedEventArgs e)
        {
            var ingredient = SelectedIngredient;
            if (ingredient != null && IngredientItemIdsListBox.SelectedItem is string itemId)
                ingredient.ItemIds.Remove(itemId);
        }

        private void PickRecipeColor_Click(object sender, RoutedEventArgs e)
        {
            var recipe = SelectedRecipe;
            if (recipe == null)
                return;
            var (a, r, g, b) = ColorUtils.ParseHex(recipe.FinalLiquidColorHex);
            using var dialog = new System.Windows.Forms.ColorDialog
            {
                AllowFullOpen = true,
                FullOpen = true,
                Color = DrawingColor.FromArgb(a, r, g, b)
            };
            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                recipe.FinalLiquidColorHex = $"#{dialog.Color.A:X2}{dialog.Color.R:X2}{dialog.Color.G:X2}{dialog.Color.B:X2}";
        }
    }
}
