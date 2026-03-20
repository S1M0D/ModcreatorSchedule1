using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Schedule1ModdingTool.Models;
using Schedule1ModdingTool.Services;
using Schedule1ModdingTool.Utils;
using Schedule1ModdingTool.ViewModels;
using DrawingColor = System.Drawing.Color;
using GameAssetTextureCatalogEntry = Schedule1ModdingTool.Services.GameAssetTextureExtractorService.GameAssetTextureCatalogEntry;

namespace Schedule1ModdingTool.Views
{
    /// <summary>
    /// Interaction logic for ItemPropertiesControl.xaml.
    /// </summary>
    public partial class ItemPropertiesControl : UserControl
    {
        private readonly ClothingStudioService _clothingStudioService = new ClothingStudioService();
        private readonly GameAssetTextureExtractorService _gameAssetTextureExtractorService = new GameAssetTextureExtractorService();
        private readonly RuntimeClothingTextureService _runtimeClothingTextureService = new RuntimeClothingTextureService();
        private IReadOnlyList<GameAssetTextureCatalogEntry> _gameDataTextures = Array.Empty<GameAssetTextureCatalogEntry>();
        private string? _loadedTextureRelativePath;
        private string _loadedTextureLabel = "No texture loaded.";

        public ItemPropertiesControl()
        {
            InitializeComponent();
            UpdateTextureEditorDrawingAttributes();
            SetTextureEditorStatus(_loadedTextureLabel);
            SetGameDataTextureStatus("Scan Schedule I_Data to browse built-in clothing textures without the connector.");
        }

        private MainViewModel? ViewModel => DataContext as MainViewModel;

        private ItemBlueprint? SelectedItem => ViewModel?.SelectedItemBlueprint;

        private ChemistryRecipeBlueprint? SelectedRecipe => ChemistryRecipesListBox.SelectedItem as ChemistryRecipeBlueprint;

        private ChemistryRecipeIngredientBlueprint? SelectedIngredient => RecipeIngredientsListBox.SelectedItem as ChemistryRecipeIngredientBlueprint;

        private ResourceAsset? SelectedStudioResource => ClothingStudioResourcesListBox.SelectedItem as ResourceAsset;

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

        private void UseSuggestedClothingAssetPath_Click(object sender, RoutedEventArgs e)
        {
            var item = SelectedItem;
            if (item == null)
                return;

            item.ClothingAssetPath = item.SuggestedClothingAssetPath;
        }

        private void GenerateClothingTemplate_Click(object sender, RoutedEventArgs e)
        {
            var viewModel = ViewModel;
            var item = SelectedItem;
            if (viewModel?.CurrentProject == null || item == null)
                return;

            if (!viewModel.EnsureProjectDirectoryForEditor(out var projectDirectory))
                return;

            var result = _clothingStudioService.GenerateClothingTemplate(viewModel.CurrentProject, projectDirectory, item);
            if (!result.Success)
            {
                AppUtils.ShowError(result.Message, "Clothing Studio");
                return;
            }

            if (!string.IsNullOrWhiteSpace(result.RecommendedClothingAssetPath))
            {
                item.ClothingAssetPath = result.RecommendedClothingAssetPath;
            }

            if (result.PrimaryAsset != null)
            {
                item.ClothingTextureResourcePath = result.PrimaryAsset.RelativePath;
                viewModel.SelectedResource = result.PrimaryAsset;
                ClothingStudioResourcesListBox.SelectedItem = result.PrimaryAsset;
                LoadTextureEditorFromProjectResource(result.PrimaryAsset);
            }

            AppUtils.ShowInfo($"{result.Message}\n\nClothing Asset Path set to:\n{item.ClothingAssetPath}", "Clothing Studio");
        }

        private void GenerateClothingIconTemplate_Click(object sender, RoutedEventArgs e)
        {
            var viewModel = ViewModel;
            var item = SelectedItem;
            if (viewModel?.CurrentProject == null || item == null)
                return;

            if (!viewModel.EnsureProjectDirectoryForEditor(out var projectDirectory))
                return;

            var result = _clothingStudioService.GenerateIconTemplate(viewModel.CurrentProject, projectDirectory, item);
            if (!result.Success)
            {
                AppUtils.ShowError(result.Message, "Clothing Studio");
                return;
            }

            if (result.PrimaryAsset != null)
            {
                item.IconFileName = result.PrimaryAsset.RelativePath;
                viewModel.SelectedResource = result.PrimaryAsset;
                ClothingStudioResourcesListBox.SelectedItem = result.PrimaryAsset;
            }

            AppUtils.ShowInfo($"{result.Message}\n\nIcon Resource set to:\n{item.IconFileName}", "Clothing Studio");
        }

        private void CreateAccessoryStarterPack_Click(object sender, RoutedEventArgs e)
        {
            var viewModel = ViewModel;
            var item = SelectedItem;
            if (viewModel?.CurrentProject == null || item == null)
                return;

            if (item.ClothingApplicationType != ClothingApplicationTypeOption.Accessory)
            {
                AppUtils.ShowWarning("Switch Application Type to Accessory before generating an accessory starter pack.", "Clothing Studio");
                return;
            }

            if (!viewModel.EnsureProjectDirectoryForEditor(out var projectDirectory))
                return;

            var result = _clothingStudioService.GenerateAccessoryStarterPack(viewModel.CurrentProject, projectDirectory, item);
            if (!result.Success)
            {
                AppUtils.ShowError(result.Message, "Clothing Studio");
                return;
            }

            if (!string.IsNullOrWhiteSpace(result.RecommendedClothingAssetPath))
            {
                item.ClothingAssetPath = result.RecommendedClothingAssetPath;
            }

            if (result.PrimaryAsset != null)
            {
                item.ClothingTextureResourcePath = result.PrimaryAsset.RelativePath;
                viewModel.SelectedResource = result.PrimaryAsset;
                ClothingStudioResourcesListBox.SelectedItem = result.PrimaryAsset;
                LoadTextureEditorFromProjectResource(result.PrimaryAsset);
            }

            AppUtils.ShowInfo($"{result.Message}\n\nClothing Asset Path set to:\n{item.ClothingAssetPath}", "Clothing Studio");
        }

        private void ImportClothingStudioPng_Click(object sender, RoutedEventArgs e)
        {
            if (ViewModel?.AddResourceCommand?.CanExecute(null) == true)
            {
                ViewModel.AddResourceCommand.Execute(null);
            }
        }

        private void OpenClothingResourcesFolder_Click(object sender, RoutedEventArgs e)
        {
            var viewModel = ViewModel;
            if (viewModel == null)
                return;

            if (!viewModel.EnsureProjectDirectoryForEditor(out var projectDirectory))
                return;

            var resourcesDirectory = Path.Combine(projectDirectory, "Resources");
            Directory.CreateDirectory(resourcesDirectory);

            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = resourcesDirectory,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                AppUtils.ShowError($"Failed to open the project Resources folder: {ex.Message}", "Clothing Studio");
            }
        }

        private void UseStudioResourceForTexture_Click(object sender, RoutedEventArgs e)
        {
            var item = SelectedItem;
            var asset = SelectedStudioResource;
            if (item == null || asset == null)
                return;

            if (!asset.RelativePath.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
            {
                AppUtils.ShowWarning("Texture resources must be PNG files.", "Clothing Studio");
                return;
            }

            item.ClothingTextureResourcePath = asset.RelativePath.Replace('\\', '/');
            ApplyTexturePathDefaults(item, item.ClothingTextureSourceAssetPath);
        }

        private void UseStudioResourceForIcon_Click(object sender, RoutedEventArgs e)
        {
            var item = SelectedItem;
            var asset = SelectedStudioResource;
            if (item == null || asset == null)
                return;

            if (!asset.RelativePath.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
            {
                AppUtils.ShowWarning("Icon resources must be PNG files.", "Clothing Studio");
                return;
            }

            item.IconFileName = asset.RelativePath.Replace('\\', '/');
        }

        private void UseLiveItemAsCloneSource_Click(object sender, RoutedEventArgs e)
        {
            var item = SelectedItem;
            if (item == null || !item.SupportsCloneSource || LiveItemBrowserListBox.SelectedItem is not GameItemCatalogEntry selectedCatalogItem)
                return;

            item.CloneSourceItemId = selectedCatalogItem.ItemId;
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
                Color = DrawingColor.FromArgb(a, r, g, b)
            };

            return dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK
                ? $"#{dialog.Color.A:X2}{dialog.Color.R:X2}{dialog.Color.G:X2}{dialog.Color.B:X2}"
                : null;
        }

        private async void ScanGameDataTextures_Click(object sender, RoutedEventArgs e)
        {
            var viewModel = ViewModel;
            if (viewModel == null)
                return;

            SetGameDataTextureStatus("Scanning Schedule I data files...");
            Mouse.OverrideCursor = System.Windows.Input.Cursors.Wait;

            try
            {
                var result = await Task.Run(() => _gameAssetTextureExtractorService.ScanTextureCatalog(viewModel.Settings.GameInstallPath));
                if (!result.Success)
                {
                    _gameDataTextures = Array.Empty<GameAssetTextureCatalogEntry>();
                    ApplyGameDataTextureFilter();
                    SetGameDataTextureStatus(result.Message);
                    AppUtils.ShowError(result.Message, "Game Data Extractor");
                    return;
                }

                _gameDataTextures = result.Textures;
                ApplyGameDataTextureFilter();
                SetGameDataTextureStatus(result.Message);
            }
            catch (Exception ex)
            {
                _gameDataTextures = Array.Empty<GameAssetTextureCatalogEntry>();
                ApplyGameDataTextureFilter();
                SetGameDataTextureStatus("Game data scan failed.");
                AppUtils.ShowError($"Failed to scan Schedule I data files: {ex.Message}", "Game Data Extractor");
            }
            finally
            {
                Mouse.OverrideCursor = null;
            }
        }

        private async void ImportClothingTextureFromGameData_Click(object sender, RoutedEventArgs e)
        {
            var viewModel = ViewModel;
            var item = SelectedItem;
            if (viewModel?.CurrentProject == null || item == null)
                return;

            if (GameDataTexturesListBox.SelectedItem is not GameAssetTextureCatalogEntry selectedTexture)
            {
                AppUtils.ShowWarning("Select a scanned game texture first.", "Game Data Extractor");
                return;
            }

            if (!viewModel.EnsureProjectDirectoryForEditor(out var projectDirectory))
                return;

            SetGameDataTextureStatus($"Extracting {selectedTexture.TextureName}...");
            Mouse.OverrideCursor = System.Windows.Input.Cursors.Wait;

            try
            {
                var extractedTexture = await Task.Run(() => _gameAssetTextureExtractorService.ExtractTexturePng(viewModel.Settings.GameInstallPath, selectedTexture));
                if (!extractedTexture.Success || extractedTexture.PngBytes == null)
                {
                    SetGameDataTextureStatus(extractedTexture.Message);
                    AppUtils.ShowError(extractedTexture.Message, "Game Data Extractor");
                    return;
                }

                var importResult = _clothingStudioService.ImportTexturePng(
                    viewModel.CurrentProject,
                    projectDirectory,
                    item,
                    extractedTexture.PngBytes,
                    $"game_{selectedTexture.TextureName}",
                    $"{item.DisplayName} {selectedTexture.TextureName}",
                    $"Extracted from Schedule I data file '{selectedTexture.AssetFileRelativePath}'.");

                if (!importResult.Success || importResult.PrimaryAsset == null)
                {
                    SetGameDataTextureStatus(importResult.Message);
                    AppUtils.ShowError(importResult.Message, "Game Data Extractor");
                    return;
                }

                item.ClothingTextureResourcePath = importResult.PrimaryAsset.RelativePath;
                ApplyTexturePathDefaults(item, item.ClothingTextureSourceAssetPath);
                viewModel.SelectedResource = importResult.PrimaryAsset;
                ClothingStudioResourcesListBox.SelectedItem = importResult.PrimaryAsset;
                LoadTextureEditorFromBytes(
                    extractedTexture.PngBytes,
                    importResult.PrimaryAsset.RelativePath,
                    $"{selectedTexture.TextureName} ({extractedTexture.Width}x{extractedTexture.Height})");

                SetGameDataTextureStatus($"Imported {selectedTexture.TextureName} from {selectedTexture.AssetFileRelativePath}.");
                AppUtils.ShowInfo(
                    $"{importResult.Message}\n\nTexture: {selectedTexture.TextureName}\nSource file: {selectedTexture.AssetFileRelativePath}",
                    "Game Data Extractor");
            }
            catch (Exception ex)
            {
                SetGameDataTextureStatus("Game data import failed.");
                AppUtils.ShowError($"Failed to extract the selected game texture: {ex.Message}", "Game Data Extractor");
            }
            finally
            {
                Mouse.OverrideCursor = null;
            }
        }

        private void ImportClothingTextureFromRunningGame_Click(object sender, RoutedEventArgs e)
        {
            var viewModel = ViewModel;
            var item = SelectedItem;
            if (viewModel?.CurrentProject == null || item == null)
                return;

            var sourceAssetPath = GetRequestedTextureSourceAssetPath(item);
            if (string.IsNullOrWhiteSpace(sourceAssetPath))
            {
                AppUtils.ShowWarning("Enter a source game asset path first, or use one of the preset source paths.", "Texture Lab");
                return;
            }

            if (!viewModel.EnsureProjectDirectoryForEditor(out var projectDirectory))
                return;

            var response = _runtimeClothingTextureService.ImportTexture(sourceAssetPath, item.ClothingApplicationType.ToString());
            if (!response.Success || string.IsNullOrWhiteSpace(response.TextureBytesBase64))
            {
                AppUtils.ShowError(response.Error ?? "Connector did not return a texture.", "Texture Lab");
                return;
            }

            byte[] rawTextureBytes;
            try
            {
                rawTextureBytes = Convert.FromBase64String(response.TextureBytesBase64);
            }
            catch (Exception ex)
            {
                AppUtils.ShowError($"Connector returned an invalid texture payload: {ex.Message}", "Texture Lab");
                return;
            }

            var pngBytes = ConvertRawTextureToPng(rawTextureBytes, response.Width, response.Height);
            if (pngBytes == null)
            {
                AppUtils.ShowError("Failed to convert the imported texture into a PNG editor resource.", "Texture Lab");
                return;
            }

            var importSuffix = item.ClothingApplicationType == ClothingApplicationTypeOption.Accessory ? "source_accessory" : "source_layer";
            var result = _clothingStudioService.ImportTexturePng(
                viewModel.CurrentProject,
                projectDirectory,
                item,
                pngBytes,
                importSuffix,
                $"{item.DisplayName} Source Texture",
                $"Imported from running game asset '{sourceAssetPath}'.");

            if (!result.Success || result.PrimaryAsset == null)
            {
                AppUtils.ShowError(result.Message, "Texture Lab");
                return;
            }

            item.ClothingTextureSourceAssetPath = sourceAssetPath;
            item.ClothingTextureResourcePath = result.PrimaryAsset.RelativePath;
            if (!string.IsNullOrWhiteSpace(response.ResolvedShaderProperty))
            {
                item.AccessoryTextureShaderPropertyName = response.ResolvedShaderProperty;
            }

            ApplyTexturePathDefaults(item, sourceAssetPath);
            viewModel.SelectedResource = result.PrimaryAsset;
            ClothingStudioResourcesListBox.SelectedItem = result.PrimaryAsset;
            LoadTextureEditorFromBytes(pngBytes, result.PrimaryAsset.RelativePath, $"{result.PrimaryAsset.DisplayName} ({response.Width}x{response.Height})");

            var summary = string.IsNullOrWhiteSpace(response.ResolvedTextureName)
                ? result.Message
                : $"{result.Message}\n\nImported texture: {response.ResolvedTextureName}";
            AppUtils.ShowInfo(summary, "Texture Lab");
        }

        private void LoadStudioResourceIntoEditor_Click(object sender, RoutedEventArgs e)
        {
            var asset = SelectedStudioResource;
            if (asset == null)
                return;

            LoadTextureEditorFromProjectResource(asset);
        }

        private void SaveTextureOverCurrentResource_Click(object sender, RoutedEventArgs e)
        {
            var viewModel = ViewModel;
            var item = SelectedItem;
            if (viewModel?.CurrentProject == null || item == null)
                return;

            if (!viewModel.EnsureProjectDirectoryForEditor(out var projectDirectory))
                return;

            var targetRelativePath = _loadedTextureRelativePath;
            if (string.IsNullOrWhiteSpace(targetRelativePath) && SelectedStudioResource?.RelativePath.EndsWith(".png", StringComparison.OrdinalIgnoreCase) == true)
            {
                targetRelativePath = SelectedStudioResource.RelativePath;
            }

            if (string.IsNullOrWhiteSpace(targetRelativePath))
            {
                AppUtils.ShowWarning("Load a PNG into the editor first, or select a PNG resource to overwrite.", "Texture Lab");
                return;
            }

            var pngBytes = RenderTextureEditorToPng();
            if (pngBytes == null)
                return;

            var result = _clothingStudioService.SaveTexturePng(
                viewModel.CurrentProject,
                projectDirectory,
                targetRelativePath,
                pngBytes,
                Path.GetFileNameWithoutExtension(targetRelativePath),
                "Edited in Clothing Studio.");

            if (!result.Success || result.PrimaryAsset == null)
            {
                AppUtils.ShowError(result.Message, "Texture Lab");
                return;
            }

            item.ClothingTextureResourcePath = result.PrimaryAsset.RelativePath;
            viewModel.SelectedResource = result.PrimaryAsset;
            ClothingStudioResourcesListBox.SelectedItem = result.PrimaryAsset;
            LoadTextureEditorFromBytes(pngBytes, result.PrimaryAsset.RelativePath, result.PrimaryAsset.DisplayName);
            SetTextureEditorStatus($"Saved over {result.PrimaryAsset.RelativePath}");
        }

        private void SaveTextureAsNewResource_Click(object sender, RoutedEventArgs e)
        {
            var viewModel = ViewModel;
            var item = SelectedItem;
            if (viewModel?.CurrentProject == null || item == null)
                return;

            if (!viewModel.EnsureProjectDirectoryForEditor(out var projectDirectory))
                return;

            var pngBytes = RenderTextureEditorToPng();
            if (pngBytes == null)
                return;

            var result = _clothingStudioService.ImportTexturePng(
                viewModel.CurrentProject,
                projectDirectory,
                item,
                pngBytes,
                $"edited_{DateTime.Now:yyyyMMdd_HHmmss}",
                $"{item.DisplayName} Edited Texture",
                "Edited in Clothing Studio.");

            if (!result.Success || result.PrimaryAsset == null)
            {
                AppUtils.ShowError(result.Message, "Texture Lab");
                return;
            }

            item.ClothingTextureResourcePath = result.PrimaryAsset.RelativePath;
            ApplyTexturePathDefaults(item, item.ClothingTextureSourceAssetPath);
            viewModel.SelectedResource = result.PrimaryAsset;
            ClothingStudioResourcesListBox.SelectedItem = result.PrimaryAsset;
            LoadTextureEditorFromBytes(pngBytes, result.PrimaryAsset.RelativePath, result.PrimaryAsset.DisplayName);
            SetTextureEditorStatus($"Saved as {result.PrimaryAsset.RelativePath}");
        }

        private void ClearTextureEditor_Click(object sender, RoutedEventArgs e)
        {
            if (TextureEditorInkCanvas == null)
                return;

            TextureEditorInkCanvas.Strokes.Clear();
        }

        private void UndoTextureStroke_Click(object sender, RoutedEventArgs e)
        {
            if (TextureEditorInkCanvas == null)
                return;

            if (TextureEditorInkCanvas.Strokes.Count == 0)
                return;

            var lastStroke = TextureEditorInkCanvas.Strokes[TextureEditorInkCanvas.Strokes.Count - 1];
            TextureEditorInkCanvas.Strokes.Remove(lastStroke);
        }

        private void TextureBrushColorTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            UpdateTextureEditorDrawingAttributes();
        }

        private void TextureBrushSizeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            UpdateTextureEditorDrawingAttributes();
        }

        private void TextureDrawMode_Checked(object sender, RoutedEventArgs e)
        {
            if (TextureEditorInkCanvas == null)
                return;

            TextureEditorInkCanvas.EditingMode = InkCanvasEditingMode.Ink;
        }

        private void TextureEraseMode_Checked(object sender, RoutedEventArgs e)
        {
            if (TextureEditorInkCanvas == null)
                return;

            TextureEditorInkCanvas.EditingMode = InkCanvasEditingMode.EraseByPoint;
        }

        private void GameDataTextureSearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            ApplyGameDataTextureFilter();
        }

        private void GameDataTextureFilterMode_Changed(object sender, RoutedEventArgs e)
        {
            ApplyGameDataTextureFilter();
        }

        private void LoadTextureEditorFromProjectResource(ResourceAsset asset)
        {
            if (ViewModel == null || string.IsNullOrWhiteSpace(asset.RelativePath))
                return;

            if (!asset.RelativePath.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
            {
                AppUtils.ShowWarning("Only PNG resources can be opened in the clothing texture editor.", "Texture Lab");
                return;
            }

            if (!ViewModel.EnsureProjectDirectoryForEditor(out var projectDirectory))
                return;

            var absolutePath = Path.Combine(projectDirectory, asset.RelativePath.Replace('/', Path.DirectorySeparatorChar));
            if (!File.Exists(absolutePath))
            {
                AppUtils.ShowError($"Could not find resource file:\n{absolutePath}", "Texture Lab");
                return;
            }

            var pngBytes = File.ReadAllBytes(absolutePath);
            LoadTextureEditorFromBytes(pngBytes, asset.RelativePath, asset.DisplayName);
        }

        private void LoadTextureEditorFromBytes(byte[] pngBytes, string? relativePath, string label)
        {
            using var stream = new MemoryStream(pngBytes);
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.StreamSource = stream;
            bitmap.EndInit();
            bitmap.Freeze();

            TextureEditorBackgroundImage.Source = bitmap;
            TextureEditorSurfaceGrid.Width = bitmap.PixelWidth;
            TextureEditorSurfaceGrid.Height = bitmap.PixelHeight;
            TextureEditorInkCanvas.Width = bitmap.PixelWidth;
            TextureEditorInkCanvas.Height = bitmap.PixelHeight;
            TextureEditorInkCanvas.Strokes.Clear();
            TextureEditorPlaceholderText.Visibility = Visibility.Collapsed;
            _loadedTextureRelativePath = relativePath;
            _loadedTextureLabel = label;
            SetTextureEditorStatus($"Editing {label}");
        }

        private byte[]? RenderTextureEditorToPng()
        {
            if (TextureEditorBackgroundImage.Source == null)
            {
                AppUtils.ShowWarning("Load or import a texture first.", "Texture Lab");
                return null;
            }

            var width = (int)Math.Max(1, TextureEditorSurfaceGrid.Width);
            var height = (int)Math.Max(1, TextureEditorSurfaceGrid.Height);
            var renderTarget = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
            renderTarget.Render(TextureEditorSurfaceGrid);

            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(renderTarget));
            using var outputStream = new MemoryStream();
            encoder.Save(outputStream);
            return outputStream.ToArray();
        }

        private static byte[]? ConvertRawTextureToPng(byte[] rawTextureBytes, int width, int height)
        {
            if (rawTextureBytes.Length == 0 || width < 1 || height < 1)
                return null;

            var stride = width * 4;
            if (rawTextureBytes.Length < stride * height)
                return null;

            var bitmap = BitmapSource.Create(
                width,
                height,
                96,
                96,
                PixelFormats.Bgra32,
                null,
                rawTextureBytes,
                stride);

            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(bitmap));
            using var outputStream = new MemoryStream();
            encoder.Save(outputStream);
            return outputStream.ToArray();
        }

        private void UpdateTextureEditorDrawingAttributes()
        {
            if (TextureEditorInkCanvas == null || TextureBrushColorTextBox == null || TextureBrushSizeSlider == null)
                return;

            var brushColor = Colors.White;
            try
            {
                var parsedColor = System.Windows.Media.ColorConverter.ConvertFromString(TextureBrushColorTextBox.Text?.Trim() ?? "#FFFFFFFF");
                if (parsedColor is System.Windows.Media.Color resolvedColor)
                {
                    brushColor = resolvedColor;
                }
            }
            catch
            {
                brushColor = Colors.White;
            }

            var brushSize = TextureBrushSizeSlider.Value <= 0 ? 12d : TextureBrushSizeSlider.Value;
            TextureEditorInkCanvas.DefaultDrawingAttributes = new DrawingAttributes
            {
                Color = brushColor,
                Width = brushSize,
                Height = brushSize,
                FitToCurve = false,
                IgnorePressure = true,
                StylusTip = StylusTip.Ellipse
            };
        }

        private void SetTextureEditorStatus(string status)
        {
            if (TextureEditorStatusTextBlock != null)
            {
                TextureEditorStatusTextBlock.Text = status;
            }
        }

        private void ApplyGameDataTextureFilter()
        {
            if (GameDataTexturesListBox == null)
                return;

            var query = GameDataTextureSearchTextBox?.Text?.Trim().ToLowerInvariant() ?? string.Empty;
            var showAllTextures = GameDataShowAllTexturesCheckBox?.IsChecked == true;

            var filteredTextures = _gameDataTextures
                .Where(texture => showAllTextures || texture.LooksLikeClothing);

            if (!string.IsNullOrWhiteSpace(query))
            {
                filteredTextures = filteredTextures.Where(texture => texture.SearchKey.Contains(query));
            }

            var filteredList = filteredTextures.Take(500).ToArray();
            GameDataTexturesListBox.ItemsSource = filteredList;

            if (GameDataTextureResultCountTextBlock != null)
            {
                if (_gameDataTextures.Count == 0)
                {
                    GameDataTextureResultCountTextBlock.Text = "No game textures scanned yet.";
                }
                else
                {
                    GameDataTextureResultCountTextBlock.Text =
                        $"Showing {filteredList.Length} of {_gameDataTextures.Count} scanned textures" +
                        (showAllTextures ? "." : " (clothing-like only).");
                }
            }
        }

        private void SetGameDataTextureStatus(string status)
        {
            if (GameDataTextureStatusTextBlock != null)
            {
                GameDataTextureStatusTextBlock.Text = status;
            }
        }

        private static string GetRequestedTextureSourceAssetPath(ItemBlueprint item)
        {
            if (!string.IsNullOrWhiteSpace(item.ClothingTextureSourceAssetPath))
                return item.ClothingTextureSourceAssetPath.Trim();

            return item.ClothingAssetPath?.Trim() ?? string.Empty;
        }

        private static void ApplyTexturePathDefaults(ItemBlueprint item, string? importedSourceAssetPath)
        {
            if (item.ClothingApplicationType == ClothingApplicationTypeOption.Accessory &&
                string.IsNullOrWhiteSpace(item.ClothingTextureSourceAssetPath) &&
                LooksLikeBaseGameAvatarPath(item.ClothingAssetPath))
            {
                item.ClothingTextureSourceAssetPath = item.ClothingAssetPath;
            }

            if (!string.IsNullOrWhiteSpace(importedSourceAssetPath))
            {
                item.ClothingTextureSourceAssetPath = importedSourceAssetPath;
            }

            if (string.IsNullOrWhiteSpace(item.ClothingAssetPath) ||
                (!string.IsNullOrWhiteSpace(importedSourceAssetPath) &&
                 string.Equals(item.ClothingAssetPath, importedSourceAssetPath, StringComparison.OrdinalIgnoreCase)))
            {
                item.ClothingAssetPath = item.SuggestedClothingAssetPath;
            }
        }

        private static bool LooksLikeBaseGameAvatarPath(string? path)
        {
            return !string.IsNullOrWhiteSpace(path) &&
                   path.Replace('\\', '/').StartsWith("Avatar/", StringComparison.OrdinalIgnoreCase);
        }
    }
}
