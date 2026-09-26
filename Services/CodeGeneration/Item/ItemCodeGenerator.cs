using System.Linq;
using Schedule1ModdingTool.Models;
using Schedule1ModdingTool.Services.CodeGeneration.Abstractions;
using Schedule1ModdingTool.Services.CodeGeneration.Builders;
using Schedule1ModdingTool.Services.CodeGeneration.Common;

namespace Schedule1ModdingTool.Services.CodeGeneration.Item
{
    public class ItemCodeGenerator : ICodeGenerator<ItemBlueprint>
    {
        public string GenerateCode(ItemBlueprint item)
        {
            ArgumentNullException.ThrowIfNull(item);

            if (item.IsGlbModel)
            {
                var glbValidation = Validate(item);
                if (!glbValidation.IsValid)
                    throw new ArgumentException(string.Join(" ", glbValidation.Errors), nameof(item));
            }
            if (item.ItemType == ItemKindOption.WeedDrug)
            {
                var validation = Validate(item);
                if (!validation.IsValid)
                    throw new ArgumentException(string.Join(" ", validation.Errors), nameof(item));
                return GenerateWeedDrugCode(item);
            }
            if (item.ItemType == ItemKindOption.CustomDrug)
            {
                var validation = Validate(item);
                if (!validation.IsValid)
                    throw new ArgumentException(string.Join(" ", validation.Errors), nameof(item));
                return GenerateCustomDrugCode(item);
            }

            var builder = new CodeBuilder();
            var className = IdentifierSanitizer.MakeSafeIdentifier(item.ClassName, "GeneratedItem");
            var targetNamespace = NamespaceNormalizer.NormalizeForItem(item.Namespace);
            var usingsBuilder = new UsingStatementsBuilder();
            usingsBuilder.AddItemUsings();
            if (!string.IsNullOrWhiteSpace(item.ModelBundleResourcePath))
                usingsBuilder.Add("S1API.AssetBundles");

            builder.AppendComment("Auto-generated item registration class.");
            usingsBuilder.GenerateUsings(builder);
            builder.OpenBlock($"namespace {targetNamespace}");
            GenerateItemClass(builder, item, className);
            builder.CloseBlock();
            return builder.Build();
        }

        public CodeGenerationValidationResult Validate(ItemBlueprint blueprint)
        {
            var result = new CodeGenerationValidationResult { IsValid = true };
            if (blueprint == null)
            {
                result.IsValid = false;
                result.Errors.Add("Item blueprint cannot be null.");
                return result;
            }

            if (string.IsNullOrWhiteSpace(blueprint.ClassName))
                result.Warnings.Add("Class name is empty, will use default 'GeneratedItem'.");
            if (string.IsNullOrWhiteSpace(blueprint.ItemId))
                result.Errors.Add("Item ID is required.");
            if (string.IsNullOrWhiteSpace(blueprint.ItemName))
                result.Errors.Add("Item name is required.");
            if (!string.IsNullOrWhiteSpace(blueprint.ModelBundleResourcePath))
            {
                if (!blueprint.IsGlbModel && string.IsNullOrWhiteSpace(blueprint.ModelPrefabName))
                    result.Errors.Add("Select the prefab name for the 3D model bundle.");
                if (blueprint.ItemType == ItemKindOption.WeedDrug)
                    result.Errors.Add("Native weed strains do not support custom model profiles. Use a Custom Product for a custom 3D model.");
            }
            if (blueprint.IsGlbModel)
            {
                if (blueprint.ItemType is not (ItemKindOption.CustomDrug or ItemKindOption.Buildable))
                    result.Errors.Add("GLB models currently support Custom Products and Furniture only. Other item types require a Unity prefab bundle.");
                if (!float.IsFinite(blueprint.ModelScale) || blueprint.ModelScale <= 0 || blueprint.ModelScale > 1000)
                    result.Errors.Add("Model scale must be greater than zero and no larger than 1000.");
                if (new[] { blueprint.ModelRotationX, blueprint.ModelRotationY, blueprint.ModelRotationZ,
                    blueprint.ModelOffsetX, blueprint.ModelOffsetY, blueprint.ModelOffsetZ }.Any(value => !float.IsFinite(value) || Math.Abs(value) > 10000))
                    result.Errors.Add("Model rotation and offset values must be finite and between -10000 and 10000.");
            }
            ValidateChemistryRecipes(result, blueprint);
            if (blueprint.ItemType == ItemKindOption.WeedDrug)
            {
                if (!System.Text.RegularExpressions.Regex.IsMatch(blueprint.ItemId ?? string.Empty,
                    @"^[a-z0-9][a-z0-9_.-]*:[a-z0-9][a-z0-9_.-]*$"))
                    result.Errors.Add("Weed drug ID must be namespaced, for example mymod:blue_dream.");
                var effects = SplitDrugEffects(blueprint.DrugEffects).ToArray();
                if (effects.Length < 1 || effects.Length > 8)
                    result.Errors.Add("Native weed strains require 1 to 8 distinct effects.");
                foreach (var effect in effects)
                {
                    if (!ItemBlueprintOptions.DrugEffectNames.Contains(effect, StringComparer.OrdinalIgnoreCase))
                        result.Errors.Add($"Unknown weed effect '{effect}'. Use a name shown in the editor.");
                }
                if (blueprint.UseCustomWeedAppearance)
                {
                    foreach (var (name, color) in new[]
                    {
                        ("Main", blueprint.WeedMainColor), ("Secondary", blueprint.WeedSecondaryColor),
                        ("Leaf", blueprint.WeedLeafColor), ("Stem", blueprint.WeedStemColor)
                    })
                    {
                        if (!IsOpaqueColorHex(color))
                            result.Errors.Add($"{name} weed color must be #RRGGBB or a nontransparent #AARRGGBB value.");
                    }
                }
                result.IsValid = result.Errors.Count == 0;
                return result;
            }
            if (blueprint.ItemType == ItemKindOption.CustomDrug)
            {
                if (blueprint.EnableCustomProductMixing && !Enum.IsDefined(blueprint.CustomProductMixingMap))
                    result.Errors.Add("Choose a supported native mixer map for the custom product.");
                if (!IsNamespacedId(blueprint.ItemId))
                    result.Errors.Add("Custom product ID must be namespaced, for example mymod:focus_tablet.");
                if (!IsNamespacedId(blueprint.ProductKindId))
                    result.Errors.Add("Product kind ID must be namespaced, for example mymod:tablets.");
                if (string.IsNullOrWhiteSpace(blueprint.RepresentationTemplateItemId))
                    result.Errors.Add("A representation template item ID is required.");
                if (!float.IsFinite(blueprint.BasePurchasePrice) || blueprint.BasePurchasePrice < 1 || blueprint.BasePurchasePrice > 999)
                    result.Errors.Add("Product price must be between 1 and 999.");
                if (!float.IsFinite(blueprint.BaseAddictiveness) || blueprint.BaseAddictiveness < 0 || blueprint.BaseAddictiveness > 1)
                    result.Errors.Add("Base addictiveness must be between 0 and 1.");
                if (blueprint.PlayerEffectDurationSeconds < 0 || blueprint.NpcEffectDurationSeconds < 0)
                    result.Errors.Add("Effect durations cannot be negative.");
                if (blueprint.ListCustomProduct && !blueprint.DiscoverCustomProduct)
                    result.Errors.Add("A product must be discovered before it can be listed.");
                if (blueprint.ShopIntegrationMode != ShopIntegrationModeOption.None && !blueprint.DiscoverCustomProduct)
                    result.Errors.Add("Discover the product before adding it to shops.");
                if (blueprint.ShopIntegrationMode == ShopIntegrationModeOption.Specific &&
                    !SplitDrugEffects(blueprint.CustomDrugShopNames).Any())
                    result.Errors.Add("Specific shop mode requires at least one shop name.");
                if (blueprint.UseCustomShopPrice && (!float.IsFinite(blueprint.CustomShopPrice) || blueprint.CustomShopPrice < 0))
                    result.Errors.Add("Custom shop price must be nonnegative.");
                if (!string.IsNullOrWhiteSpace(blueprint.ProductConsoleAlias) &&
                    !System.Text.RegularExpressions.Regex.IsMatch(blueprint.ProductConsoleAlias, @"^[a-zA-Z0-9][a-zA-Z0-9_-]*$"))
                    result.Errors.Add("Console alias may contain letters, numbers, underscores, and hyphens.");
                var effects = SplitDrugEffects(blueprint.DrugEffects).ToArray();
                if (effects.Length > 8)
                    result.Errors.Add("Custom products support at most 8 distinct effects.");
                foreach (var effect in effects)
                    if (!ItemBlueprintOptions.DrugEffectNames.Contains(effect, StringComparer.OrdinalIgnoreCase))
                        result.Errors.Add($"Unknown product effect '{effect}'.");
                if (blueprint.ShowCustomProductKindInManager)
                {
                    if (string.IsNullOrWhiteSpace(blueprint.ProductKindName))
                        result.Errors.Add("Product Manager kind name is required when its section is shown.");
                    if (!IsOpaqueColorHex(blueprint.ProductKindColor))
                        result.Errors.Add("Product Manager kind color must be #RRGGBB or nontransparent #AARRGGBB.");
                }
                result.IsValid = result.Errors.Count == 0;
                return result;
            }
            if (blueprint.SupportsStackLimit && blueprint.StackLimit < 1)
                result.Errors.Add("Stack limit must be at least 1.");
            if (blueprint.ResellMultiplier < 0f || blueprint.ResellMultiplier > 1f)
                result.Errors.Add("Resell multiplier must be between 0 and 1.");
            if (blueprint.UseCustomShopPrice && blueprint.CustomShopPrice < 0f)
                result.Errors.Add("Custom shop price cannot be negative.");
            if (blueprint.UsesSpecificShops && blueprint.ShopNames.Count == 0)
                result.Errors.Add("Specific shop mode requires at least one shop name.");
            if (blueprint.ItemType == ItemKindOption.Clothing &&
                string.IsNullOrWhiteSpace(blueprint.CloneSourceItemId) &&
                string.IsNullOrWhiteSpace(blueprint.ClothingAssetPath) &&
                string.IsNullOrWhiteSpace(blueprint.ModelBundleResourcePath))
            {
                result.Errors.Add("Clothing items need either a clone source item ID or a clothing asset path.");
            }

            if (blueprint.ItemType == ItemKindOption.Clothing &&
                !string.IsNullOrWhiteSpace(blueprint.ClothingTextureResourcePath) &&
                string.IsNullOrWhiteSpace(blueprint.ClothingAssetPath))
            {
                result.Errors.Add("Clothing texture overrides need a clothing asset path to register against.");
            }

            if (blueprint.ItemType == ItemKindOption.Clothing &&
                blueprint.ClothingApplicationType == ClothingApplicationTypeOption.Accessory &&
                !string.IsNullOrWhiteSpace(blueprint.ClothingTextureResourcePath) &&
                string.IsNullOrWhiteSpace(blueprint.ClothingTextureSourceAssetPath))
            {
                result.Errors.Add("Accessory texture overrides need a source accessory asset path.");
            }

            if (!blueprint.SupportsEquippable && blueprint.EquippableType != EquippableTypeOption.None)
                result.Warnings.Add("Equippable settings are ignored for this item type.");
            if (blueprint.ClearStationItem && !blueprint.SupportsStationItemPrefab)
                result.Warnings.Add("Clear station item only applies to generic storable items.");
            if (blueprint.ItemType != ItemKindOption.Additive && !string.IsNullOrWhiteSpace(blueprint.DisplayMaterialResourcePath))
                result.Warnings.Add("Display material is only used by additive items.");
            if (blueprint.AllowOnGrowContainers && blueprint.ItemType != ItemKindOption.Additive)
                result.Warnings.Add("Grow-container integration only applies to additive items.");
            if (blueprint.EnableUseCallbackHook && !blueprint.SupportsUseCallbackHook)
                result.Warnings.Add("Use callback hooks only apply to viewmodel equippables.");

            if (blueprint.RegisterAvatarEquippableFromEmbeddedBundle)
            {
                if (!blueprint.UsesViewmodelEquippable || !blueprint.HasAvatarEquippable)
                    result.Warnings.Add("Embedded avatar equippable registration only applies to viewmodel equippables with avatar equippables enabled.");
                if (string.IsNullOrWhiteSpace(blueprint.AvatarBundleResourcePath))
                    result.Errors.Add("Avatar bundle resource path is required when embedded avatar equippable registration is enabled.");
                if (string.IsNullOrWhiteSpace(blueprint.AvatarBundlePrefabName))
                    result.Errors.Add("Avatar bundle prefab name is required when embedded avatar equippable registration is enabled.");
                if (string.IsNullOrWhiteSpace(blueprint.AvatarEquippableAssetPath))
                    result.Errors.Add("Avatar equippable asset path is required when embedded avatar equippable registration is enabled.");
            }

            result.IsValid = result.Errors.Count == 0;
            return result;
        }

        private static void ValidateChemistryRecipes(CodeGenerationValidationResult result, ItemBlueprint blueprint)
        {
            if (!blueprint.SupportsChemistryRecipes)
                return;

            var recipeIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var legacyOutputQuantities = new HashSet<int>();
            foreach (var recipe in blueprint.ChemistryRecipes)
            {
                var label = recipe.DisplayName;
                if (string.IsNullOrWhiteSpace(recipe.Title))
                    result.Errors.Add("Chemistry recipe titles cannot be empty.");
                if (!string.IsNullOrWhiteSpace(recipe.RecipeId))
                {
                    if (!IsNamespacedId(recipe.RecipeId))
                        result.Errors.Add($"Chemistry recipe '{label}' needs a namespaced recipe ID, for example mymod:my_recipe.");
                    else if (!recipeIds.Add(recipe.RecipeId.Trim()))
                        result.Errors.Add($"Duplicate chemistry recipe ID '{recipe.RecipeId}'.");
                }
                else if (!legacyOutputQuantities.Add(recipe.ProductQuantity))
                {
                    result.Errors.Add($"Chemistry recipes for '{blueprint.ItemName}' with the same output quantity need distinct recipe IDs.");
                }
                if (recipe.IsUnlocked && !recipe.IsDiscovered)
                    result.Errors.Add($"Chemistry recipe '{label}' must be visible before it can be unlocked.");
                if (recipe.CookTimeMinutes < 1)
                    result.Errors.Add($"Chemistry recipe '{label}' must cook for at least 1 minute.");
                if (recipe.ProductQuantity < 1)
                    result.Errors.Add($"Chemistry recipe '{label}' must produce at least 1 item.");
                if (!IsOpaqueColorHex(recipe.FinalLiquidColorHex))
                    result.Errors.Add($"Chemistry recipe '{label}' needs a valid opaque liquid color.");
                if (recipe.Ingredients.Count == 0)
                    result.Errors.Add($"Chemistry recipe '{label}' needs at least one ingredient group.");
                foreach (var ingredient in recipe.Ingredients)
                {
                    if (ingredient.Quantity < 1)
                        result.Errors.Add($"Chemistry recipe '{label}' has an ingredient group with an invalid quantity.");
                    if (ingredient.ItemIds.Count == 0 || ingredient.ItemIds.All(string.IsNullOrWhiteSpace))
                        result.Errors.Add($"Chemistry recipe '{label}' has an ingredient group without any item IDs.");
                }
            }
        }

        private static IEnumerable<string> SplitDrugEffects(string? effects) =>
            (effects ?? string.Empty).Split(new[] { ',', ';', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(effect => effect.Trim()).Where(effect => effect.Length > 0)
                .Distinct(StringComparer.OrdinalIgnoreCase);

        private static bool IsOpaqueColorHex(string? color) =>
            color != null &&
            (System.Text.RegularExpressions.Regex.IsMatch(color, @"^#[0-9a-fA-F]{6}$") ||
             (System.Text.RegularExpressions.Regex.IsMatch(color, @"^#[0-9a-fA-F]{8}$") &&
              !color.Substring(1, 2).Equals("00", StringComparison.OrdinalIgnoreCase)));

        private static bool IsNamespacedId(string? id) =>
            id != null && System.Text.RegularExpressions.Regex.IsMatch(id,
                @"^[a-zA-Z0-9][a-zA-Z0-9_.-]*:[a-zA-Z0-9][a-zA-Z0-9_./-]*$");

        private static string GenerateCustomDrugCode(ItemBlueprint item)
        {
            var className = IdentifierSanitizer.MakeSafeIdentifier(item.ClassName, "GeneratedItem");
            var targetNamespace = NamespaceNormalizer.NormalizeForItem(item.Namespace);
            var builder = new CodeBuilder();
            builder.AppendLine("using System;");
            if (!string.IsNullOrWhiteSpace(item.ModelBundleResourcePath))
            {
                builder.AppendLine("using System.IO;");
                builder.AppendLine("using System.Linq;");
                builder.AppendLine("using System.Reflection;");
                builder.AppendLine("using MelonLoader;");
                builder.AppendLine("using S1API.AssetBundles;");
            }
            builder.AppendLine("using S1API.Items;");
            builder.AppendLine("using S1API.Products;");
            builder.AppendLine("using S1API.Properties.Tokens;");
            if (item.ChemistryRecipes.Count > 0)
                builder.AppendLine("using S1API.Stations;");
            if (!string.IsNullOrWhiteSpace(item.ProductConsoleAlias))
                builder.AppendLine("using S1API.Console;");
            if (item.ShopIntegrationMode != ShopIntegrationModeOption.None)
                builder.AppendLine("using S1API.Shops;");
            if (item.ShowCustomProductKindInManager || !string.IsNullOrWhiteSpace(item.ModelBundleResourcePath) || item.ChemistryRecipes.Count > 0)
                builder.AppendLine("using UnityEngine;");
            builder.AppendLine();
            builder.OpenBlock($"namespace {targetNamespace}");
            builder.OpenBlock($"public static class {className}");
            builder.AppendLine("public static CustomProductDefinition? Definition { get; private set; }");
            builder.AppendLine("private static bool _providerRegistered;");
            if (item.ChemistryRecipes.Count > 0)
                builder.AppendLine("private static bool _recipesRegistered;");
            if (!string.IsNullOrWhiteSpace(item.ModelBundleResourcePath))
            {
                builder.AppendLine("private static bool _presentationRegistered;");
                builder.AppendLine("private static GameObject? _modelPrefab;");
            }
            builder.AppendLine();
            builder.AppendLine($"private const string ProviderId = \"{CodeFormatter.EscapeString(item.ItemId)}.provider\";");
            builder.AppendLine();
            builder.AppendComment("Register the save provider during mod initialization, before any save is read.");
            builder.OpenBlock("public static void RegisterProvider()");
            builder.OpenBlock("if (_providerRegistered)");
            builder.AppendLine("return;");
            builder.CloseBlock();
            builder.AppendLine("CustomProductSaveProviderRegistry.Register(new SaveProvider());");
            builder.AppendLine("_providerRegistered = true;");
            builder.CloseBlock();
            builder.AppendLine();
            builder.OpenBlock("private sealed class SaveProvider : ICustomProductSaveProvider");
            builder.AppendLine($"public string ProviderId => {className}.ProviderId;");
            builder.AppendLine("public int MaximumDescriptorVersion => 1;");
            builder.OpenBlock("public CustomProductDefinitionBuilder? Restore(CustomProductSaveDescriptor descriptor)");
            builder.OpenBlock($"if (!string.Equals(descriptor.ProductId, \"{CodeFormatter.EscapeString(item.ItemId)}\", StringComparison.OrdinalIgnoreCase))");
            builder.AppendLine("return null;");
            builder.CloseBlock();
            builder.AppendLine("return CreateBuilder();");
            builder.CloseBlock();
            builder.CloseBlock();
            builder.AppendLine();
            builder.AppendComment("Register during GameLifecycle.OnPreLoad, before saved items are restored.");
            builder.OpenBlock("public static CustomProductDefinition Register()");
            builder.AppendLine("RegisterProvider();");
            builder.OpenBlock("if (Definition != null)");
            builder.AppendLine("return Definition;");
            builder.CloseBlock();
            builder.AppendLine($"Definition = ItemManager.GetDefinition(\"{CodeFormatter.EscapeString(item.ItemId)}\") as CustomProductDefinition;");
            builder.OpenBlock("if (Definition != null)");
            builder.AppendLine("return Definition;");
            builder.CloseBlock();
            builder.AppendLine("Definition = CreateBuilder().Build();");
            builder.AppendLine("return Definition;");
            builder.CloseBlock();
            builder.AppendLine();
            builder.OpenBlock("private static CustomProductDefinitionBuilder CreateBuilder()");
            if (!string.IsNullOrWhiteSpace(item.ModelBundleResourcePath))
                builder.AppendLine("RegisterPresentation();");
            builder.AppendLine($"var template = ItemManager.GetDefinition(\"{CodeFormatter.EscapeString(item.RepresentationTemplateItemId)}\") as ProductDefinition");
            builder.AppendLine($"    ?? throw new InvalidOperationException(\"Product representation template '{CodeFormatter.EscapeString(item.RepresentationTemplateItemId)}' is unavailable during OnPreLoad.\");");
            builder.AppendLine($"var kind = new ProductKindBuilder(\"{CodeFormatter.EscapeString(item.ProductKindId)}\")");
            builder.AppendLine($"    .WithCompatibilityDrugType(DrugType.{item.CompatibilityDrugType})");
            builder.AppendLine("    .Build();");
            if (item.EnableCustomProductMixing)
            {
                var map = item.CustomProductMixingMap;
                var factoryIdentity = CodeFormatter.EscapeString(item.ProductKindId + "/modcreator-output-v1");
                builder.AppendLine("var existingMixingProfile = ProductMixingProfiles.Get(kind);");
                builder.OpenBlock("if (existingMixingProfile == null)");
                builder.AppendLine("var mixingProfile = new ProductMixingProfileBuilder(kind)");
                builder.AppendLine($"    .WithMixerMap(ProductMixingMap.{map})");
                builder.AppendLine("    .WithOutputFactory(output => new ProductMixingOutputDefinition(output.MixName, output.SourceKind, output.SourcePrice))");
                builder.AppendLine($"    .WithOutputFactoryCompatibility(\"{factoryIdentity}\", 1);");
                if (item.UsePropertyColorMixing)
                    builder.AppendLine("mixingProfile.WithPropertyColorMixing();");
                builder.AppendLine("mixingProfile.Build();");
                builder.CloseBlock();
                builder.OpenBlock($"else if (existingMixingProfile.MixerMap != ProductMixingMap.{map} || existingMixingProfile.UsePropertyColorMixing != {item.UsePropertyColorMixing.ToString().ToLowerInvariant()} || existingMixingProfile.OutputFactoryIdentity != \"{factoryIdentity}\" || existingMixingProfile.OutputFactoryVersion != 1)");
                builder.AppendLine($"throw new InvalidOperationException(\"Conflicting mixing profile for product kind '{CodeFormatter.EscapeString(item.ProductKindId)}'.\");");
                builder.CloseBlock();
            }
            if (item.ShowCustomProductKindInManager)
            {
                builder.AppendLine("var kindIcon = template.Icon ?? throw new InvalidOperationException(\"The representation template has no icon for the Product Manager.\");");
                builder.AppendLine("new ProductKindMetadataBuilder(kind)");
                builder.AppendLine($"    .WithDisplayName(\"{CodeFormatter.EscapeString(item.ProductKindName)}\")");
                builder.AppendLine($"    .WithColor({CodeFormatter.FormatColorFromHex(item.ProductKindColor)})");
                builder.AppendLine("    .WithIcon(kindIcon)");
                builder.AppendLine("    .WithProductManagerVisibility(true)");
                builder.AppendLine("    .Build();");
            }
            builder.AppendLine($"var product = CustomProductItemCreator.CreateBuilder(\"{CodeFormatter.EscapeString(item.ItemId)}\", kind)");
            builder.AppendLine($"    .WithName(\"{CodeFormatter.EscapeString(item.ItemName)}\")");
            builder.AppendLine($"    .WithDescription(\"{CodeFormatter.EscapeString(item.ItemDescription)}\")");
            builder.AppendLine($"    .WithProductPrice({CodeFormatter.FormatFloat(item.BasePurchasePrice)}f)");
            builder.AppendLine($"    .WithLegalStatus(LegalStatus.{item.LegalStatus})");
            builder.AppendLine($"    .WithBaseAddictiveness({CodeFormatter.FormatFloat(item.BaseAddictiveness)}f)");
            builder.AppendLine($"    .WithDefaultQuality(Quality.{item.DefaultProductQuality})");
            builder.AppendLine("    .WithRepresentationsFrom(template)");
            if (item.EnableCustomProductMixing)
                builder.AppendLine($"    .WithNativeMixerMap(ProductMixingMap.{item.CustomProductMixingMap})");
            builder.AppendLine($"    .WithEffectDurations({item.PlayerEffectDurationSeconds}, {item.NpcEffectDurationSeconds})");
            builder.AppendLine("    .WithSaveProvider(ProviderId, 1, \"\");");
            foreach (var effect in SplitDrugEffects(item.DrugEffects))
            {
                var known = ItemBlueprintOptions.DrugEffectNames.First(value => value.Equals(effect, StringComparison.OrdinalIgnoreCase));
                builder.AppendLine($"product.WithProperty(new {known}());");
            }
            var packagingIds = SplitDrugEffects(item.ProductPackagingIds).ToArray();
            for (var index = 0; index < packagingIds.Length; index++)
            {
                var packagingId = CodeFormatter.EscapeString(packagingIds[index]);
                builder.AppendLine($"var packaging{index} = ProductPopulator.GetPackaging(\"{packagingId}\")");
                builder.AppendLine($"    ?? throw new InvalidOperationException(\"Packaging '{packagingId}' is unavailable during OnPreLoad.\");");
            }
            if (packagingIds.Length > 0)
                builder.AppendLine($"product.WithValidPackaging({string.Join(", ", Enumerable.Range(0, packagingIds.Length).Select(index => $"packaging{index}"))});");
            builder.AppendLine("return product;");
            builder.CloseBlock();
            builder.AppendLine();
            builder.AppendComment("Call on the host after GameLifecycle.OnLoadComplete.");
            builder.OpenBlock("public static void Discover()");
            builder.OpenBlock("if (Definition == null)");
            builder.AppendLine("return;");
            builder.CloseBlock();
            if (item.ChemistryRecipes.Count > 0)
                builder.AppendLine("RegisterChemistryRecipes();");
            if (!string.IsNullOrWhiteSpace(item.ProductConsoleAlias))
                builder.AppendLine($"ConsoleItemAliases.Register(\"{CodeFormatter.EscapeString(item.ProductConsoleAlias)}\", Definition.ID);");
            if (item.DiscoverCustomProduct)
            {
                builder.AppendLine($"Definition.Discover({item.ListCustomProduct.ToString().ToLowerInvariant()});");
            }
            if (item.ShopIntegrationMode == ShopIntegrationModeOption.Compatible)
            {
                builder.AppendLine("var shops = ShopManager.FindShopsByCategory(Definition.Category);");
                builder.OpenBlock("foreach (var shop in shops)");
                builder.OpenBlock("if (!shop.HasItem(Definition.ID))");
                builder.AppendLine($"shop.AddItem(Definition, {(item.UseCustomShopPrice ? $"{CodeFormatter.FormatFloat(item.CustomShopPrice)}f" : "null")});");
                builder.CloseBlock();
                builder.CloseBlock();
                builder.AppendLine("ShopManager.RefreshItemIcon(Definition);");
            }
            else if (item.ShopIntegrationMode == ShopIntegrationModeOption.Specific)
            {
                var shopIndex = 0;
                foreach (var shopName in SplitDrugEffects(item.CustomDrugShopNames))
                {
                    var variable = $"shop{shopIndex++}";
                    builder.AppendLine($"var {variable} = ShopManager.GetShopByName(\"{CodeFormatter.EscapeString(shopName)}\");");
                    builder.OpenBlock($"if ({variable} != null && !{variable}.HasItem(Definition.ID))");
                    builder.AppendLine($"{variable}.AddItem(Definition, {(item.UseCustomShopPrice ? $"{CodeFormatter.FormatFloat(item.CustomShopPrice)}f" : "null")});");
                    builder.CloseBlock();
                }
                builder.AppendLine("ShopManager.RefreshItemIcon(Definition);");
            }
            builder.CloseBlock();
            if (item.ChemistryRecipes.Count > 0)
            {
                builder.AppendLine();
                GenerateChemistryRecipeMethod(builder, item);
            }
            if (!string.IsNullOrWhiteSpace(item.ModelBundleResourcePath))
            {
                builder.AppendLine();
                builder.OpenBlock("private static void RegisterPresentation()");
                builder.OpenBlock("if (_presentationRegistered)");
                builder.AppendLine("return;");
                builder.CloseBlock();
                builder.AppendLine("_modelPrefab = LoadModelPrefab();");
                builder.OpenBlock("if (_modelPrefab == null)");
                builder.AppendLine("return;");
                builder.CloseBlock();
                builder.AppendLine("var profile = new ProductPresentationProfileBuilder()");
                builder.AppendLine("    .WithLooseVisual(() => _modelPrefab)");
                builder.AppendLine("    .WithGeneratedIconFromLooseVisual(512)");
                builder.AppendLine("    .Build();");
                builder.AppendLine($"ProductPresentationProfileRegistry.RegisterForProduct(\"{CodeFormatter.EscapeString(item.ItemId.Split(':')[0])}\", \"{CodeFormatter.EscapeString(item.ItemId)}\", profile);");
                foreach (var packaging in SplitDrugEffects(item.ProductPackagingIds).Where(id => !id.Equals("brick", StringComparison.OrdinalIgnoreCase)))
                {
                    builder.AppendLine($"ProductPackagingContentProfileRegistry.Register(\"{CodeFormatter.EscapeString(item.ItemId.Split(':')[0])}\", \"{CodeFormatter.EscapeString(item.ItemId)}\", \"{CodeFormatter.EscapeString(packaging)}\", new ProductPackagingContentProfileBuilder().WithContent(() => _modelPrefab).Build());");
                }
                builder.AppendLine("_presentationRegistered = true;");
                builder.CloseBlock();
                builder.AppendLine();
                GenerateModelLoader(builder, item);
            }
            builder.CloseBlock();
            builder.CloseBlock();
            return builder.Build();
        }

        private static string GenerateWeedDrugCode(ItemBlueprint item)
        {
            var className = IdentifierSanitizer.MakeSafeIdentifier(item.ClassName, "GeneratedItem");
            var targetNamespace = NamespaceNormalizer.NormalizeForItem(item.Namespace);
            var builder = new CodeBuilder();
            builder.AppendLine("using S1API.Products;");
            builder.AppendLine("using S1API.Properties.Tokens;");
            if (item.UseCustomWeedAppearance)
                builder.AppendLine("using UnityEngine;");
            builder.AppendLine();
            builder.OpenBlock($"namespace {targetNamespace}");
            builder.OpenBlock($"public static class {className}");
            builder.AppendLine("public static WeedDefinition? Definition { get; private set; }");
            builder.AppendLine();
            builder.AppendComment("Call after GameLifecycle.OnLoadComplete; native creation handles saves, mixing and discovery.");
            builder.OpenBlock("public static WeedDefinition Register()");
            builder.AppendLine($"var weed = WeedItemCreator.CreateBuilder(\"{CodeFormatter.EscapeString(item.ItemId)}\")");
            builder.AppendLine($"    .WithName(\"{CodeFormatter.EscapeString(item.ItemName)}\");");
            foreach (var effect in SplitDrugEffects(item.DrugEffects))
            {
                var known = ItemBlueprintOptions.DrugEffectNames.FirstOrDefault(value => value.Equals(effect, StringComparison.OrdinalIgnoreCase));
                if (known != null)
                    builder.AppendLine($"weed.WithProperty(new {known}());");
            }
            if (item.UseCustomWeedAppearance)
            {
                builder.AppendLine("weed.WithAppearance(new WeedAppearanceSettings(");
                builder.AppendLine($"    {CodeFormatter.FormatColor32FromHex(item.WeedMainColor)},");
                builder.AppendLine($"    {CodeFormatter.FormatColor32FromHex(item.WeedSecondaryColor)},");
                builder.AppendLine($"    {CodeFormatter.FormatColor32FromHex(item.WeedLeafColor)},");
                builder.AppendLine($"    {CodeFormatter.FormatColor32FromHex(item.WeedStemColor)}));");
            }
            builder.AppendLine("Definition = weed.Build();");
            builder.AppendLine("return Definition;");
            builder.CloseBlock();
            builder.CloseBlock();
            builder.CloseBlock();
            return builder.Build();
        }

        private void GenerateItemClass(ICodeBuilder builder, ItemBlueprint item, string className)
        {
            builder.AppendBlockComment(
                $"Registers the custom item \"{CodeFormatter.EscapeString(item.DisplayName)}\".",
                "Call Register() after the Main scene is available."
            );
            builder.OpenBlock($"public static partial class {className}");
            builder.AppendLine("public static StorableItemDefinition? Definition { get; private set; }");
            builder.AppendLine("private static bool _growContainerRegistered;");
            builder.AppendLine("private static bool _recipesRegistered;");
            builder.AppendLine("private static bool _avatarEquippableRegistered;");
            builder.AppendLine("private static bool _clothingRuntimeAssetsRegistered;");
            if (!string.IsNullOrWhiteSpace(item.ModelBundleResourcePath))
                builder.AppendLine("private static GameObject? _modelPrefab;");
            builder.AppendLine();
            GenerateRegisterMethod(builder, item);
            builder.AppendLine();
            GenerateBuildDefinitionMethod(builder, item);
            builder.AppendLine();
            GenerateShopIntegrationMethod(builder, item);
            builder.AppendLine();
            GenerateGrowContainerIntegrationMethod(builder, item);
            builder.AppendLine();
            GenerateChemistryRecipeMethod(builder, item);
            builder.AppendLine();
            GenerateEquippableMethod(builder, item);
            builder.AppendLine();
            GenerateAvatarEquippableMethod(builder, item);
            builder.AppendLine();
            GenerateClothingRuntimeRegistrationMethod(builder, item);
            builder.AppendLine();
            GenerateResourceLoadingHelpers(builder);
            builder.AppendLine();
            GenerateIconMethod(builder, item);
            if (!string.IsNullOrWhiteSpace(item.ModelBundleResourcePath))
            {
                builder.AppendLine();
                GenerateModelLoader(builder, item);
            }
            builder.AppendLine();
            GeneratePartialHookMembers(builder);
            builder.CloseBlock();
        }

        private void GenerateRegisterMethod(ICodeBuilder builder, ItemBlueprint item)
        {
            builder.AppendComment("Builds the item once, then re-applies integrations whenever Register is called.");
            builder.OpenBlock("public static StorableItemDefinition? Register()");
            builder.OpenBlock("if (Definition == null)");
            builder.AppendLine("Definition = BuildDefinition();");
            builder.CloseBlock();
            builder.AppendLine();
            builder.OpenBlock("if (Definition == null)");
            builder.AppendLine("return null;");
            builder.CloseBlock();
            builder.AppendLine();
            builder.AppendLine("RegisterGrowContainerIntegration();");
            builder.AppendLine("RegisterChemistryRecipes();");
            GenerateShopIntegrationInvocation(builder, item);
            builder.AppendLine("OnAfterRegister(Definition);");
            builder.AppendLine("return Definition;");
            builder.CloseBlock();
        }

        private void GenerateBuildDefinitionMethod(ICodeBuilder builder, ItemBlueprint item)
        {
            builder.AppendComment("Builds the underlying S1API definition for the configured item type.");
            builder.OpenBlock("private static StorableItemDefinition? BuildDefinition()");
            builder.AppendLine("var icon = LoadCustomIcon();");
            if (!string.IsNullOrWhiteSpace(item.ModelBundleResourcePath))
                builder.AppendLine("var customModel = LoadModelPrefab();");
            builder.AppendLine("EnsureAvatarEquippableRegistered();");
            builder.AppendLine("EnsureClothingRuntimeAssetsRegistered();");
            if (item.SupportsEquippable)
                builder.AppendLine("var equippable = BuildEquippable();");
            builder.AppendLine();

            switch (item.ItemType)
            {
                case ItemKindOption.Buildable:
                    GenerateBuildableDefinition(builder, item);
                    break;
                case ItemKindOption.Clothing:
                    GenerateClothingDefinition(builder, item);
                    break;
                case ItemKindOption.Additive:
                    GenerateAdditiveDefinition(builder, item);
                    break;
                default:
                    GenerateGenericDefinition(builder, item);
                    break;
            }

            builder.CloseBlock();
        }

        private void GenerateGenericDefinition(ICodeBuilder builder, ItemBlueprint item)
        {
            builder.AppendLine("var itemBuilder = ItemCreator.CreateBuilder()");
            builder.AppendLine($"    .WithBasicInfo(\"{CodeFormatter.EscapeString(item.ItemId)}\", \"{CodeFormatter.EscapeString(item.ItemName)}\", \"{CodeFormatter.EscapeString(item.ItemDescription)}\", ItemCategory.{item.EffectiveCategory})");
            builder.AppendLine($"    .WithStackLimit({item.StackLimit})");
            builder.AppendLine($"    .WithPricing({CodeFormatter.FormatFloat(item.BasePurchasePrice)}f, {CodeFormatter.FormatFloat(item.ResellMultiplier)}f)");
            builder.AppendLine($"    .WithLegalStatus(LegalStatus.{item.LegalStatus})");
            builder.AppendLine($"    .WithDemoAvailability({item.AvailableInDemo.ToString().ToLowerInvariant()});");
            builder.AppendLine();
            GenerateCommonItemEnhancements(builder, item);
            if (!string.IsNullOrWhiteSpace(item.ModelBundleResourcePath))
            {
                builder.OpenBlock("if (customModel != null)");
                builder.AppendLine("itemBuilder.WithStoredItem(customModel);");
                builder.CloseBlock();
            }
            if (!string.IsNullOrWhiteSpace(item.StoredItemResourcePath))
            {
                builder.AppendLine($"var storedItemPrefab = LoadGameObjectResource(\"{CodeFormatter.EscapeString(item.StoredItemResourcePath)}\", \"stored item prefab\");");
                builder.OpenBlock("if (storedItemPrefab != null)");
                builder.AppendLine("itemBuilder.WithStoredItem(storedItemPrefab);");
                builder.CloseBlock();
                builder.AppendLine();
            }
            if (item.ClearStationItem)
            {
                builder.AppendLine("itemBuilder.WithoutStationItem();");
                builder.AppendLine();
            }
            if (!string.IsNullOrWhiteSpace(item.StationItemResourcePath))
            {
                builder.AppendLine($"var stationItemPrefab = LoadGameObjectResource(\"{CodeFormatter.EscapeString(item.StationItemResourcePath)}\", \"station item prefab\");");
                builder.OpenBlock("if (stationItemPrefab != null)");
                builder.AppendLine("itemBuilder.WithStationItem(stationItemPrefab);");
                builder.CloseBlock();
                builder.AppendLine();
            }
            FinalizeDefinitionBuild(builder);
        }

        private void GenerateBuildableDefinition(ICodeBuilder builder, ItemBlueprint item)
        {
            if (!string.IsNullOrWhiteSpace(item.ModelBundleResourcePath))
            {
                var furnitureFactory = string.IsNullOrWhiteSpace(item.CloneSourceItemId)
                    ? "S1API.Items.Buildable.FurnitureCreator.CreateBuilder()"
                    : $"S1API.Items.Buildable.FurnitureCreator.CloneFrom(\"{CodeFormatter.EscapeString(item.CloneSourceItemId)}\")";
                builder.AppendLine($"var furniture = {furnitureFactory}");
                builder.AppendLine($"    .WithBasicInfo(\"{CodeFormatter.EscapeString(item.ItemId)}\", \"{CodeFormatter.EscapeString(item.ItemName)}\", \"{CodeFormatter.EscapeString(item.ItemDescription)}\")");
                builder.AppendLine($"    .WithBuildSound(BuildSoundType.{item.BuildSoundType})");
                builder.AppendLine($"    .WithStackLimit({item.StackLimit})");
                builder.AppendLine($"    .WithPricing({CodeFormatter.FormatFloat(item.BasePurchasePrice)}f, {CodeFormatter.FormatFloat(item.ResellMultiplier)}f);");
                builder.OpenBlock("if (customModel == null)");
                builder.AppendLine("throw new InvalidOperationException(\"The configured furniture model could not be loaded.\");");
                builder.CloseBlock();
                builder.AppendLine("furniture.WithModel(customModel);");
                builder.OpenBlock("if (icon != null)");
                builder.AppendLine("furniture.WithIcon(icon);");
                builder.CloseBlock();
                builder.AppendLine("else furniture.WithGeneratedIcon();");
                builder.AppendLine("var definition = furniture.Build();");
                builder.AppendLine($"definition.LegalStatus = LegalStatus.{item.LegalStatus};");
                builder.AppendLine("ConfigureDefinition(definition);");
                builder.AppendLine("return definition;");
                return;
            }
            builder.AppendLine($"var itemBuilder = {GetCloneCapableBuilderExpression("BuildableItemCreator", item.CloneSourceItemId)};");
            builder.AppendLine($"itemBuilder.WithBasicInfo(\"{CodeFormatter.EscapeString(item.ItemId)}\", \"{CodeFormatter.EscapeString(item.ItemName)}\", \"{CodeFormatter.EscapeString(item.ItemDescription)}\", ItemCategory.{item.EffectiveCategory});");
            builder.AppendLine($"itemBuilder.WithBuildSound(BuildSoundType.{item.BuildSoundType});");
            builder.AppendLine($"itemBuilder.WithPricing({CodeFormatter.FormatFloat(item.BasePurchasePrice)}f, {CodeFormatter.FormatFloat(item.ResellMultiplier)}f);");
            builder.AppendLine($"itemBuilder.WithStackLimit({item.StackLimit});");
            builder.AppendLine($"itemBuilder.WithLegalStatus(LegalStatus.{item.LegalStatus});");
            builder.AppendLine();
            GenerateCommonItemEnhancements(builder, item);
            FinalizeDefinitionBuild(builder);
        }

        private void GenerateClothingDefinition(ICodeBuilder builder, ItemBlueprint item)
        {
            builder.AppendLine($"var itemBuilder = {GetCloneCapableBuilderExpression("ClothingItemCreator", item.CloneSourceItemId)};");
            builder.AppendLine($"itemBuilder.WithBasicInfo(\"{CodeFormatter.EscapeString(item.ItemId)}\", \"{CodeFormatter.EscapeString(item.ItemName)}\", \"{CodeFormatter.EscapeString(item.ItemDescription)}\", ItemCategory.{item.EffectiveCategory});");
            builder.AppendLine($"itemBuilder.WithSlot(ClothingSlot.{item.ClothingSlot});");
            builder.AppendLine($"itemBuilder.WithApplicationType(ClothingApplicationType.{item.ClothingApplicationType});");
            if (!string.IsNullOrWhiteSpace(item.ClothingAssetPath) || !string.IsNullOrWhiteSpace(item.ModelBundleResourcePath))
                builder.AppendLine($"itemBuilder.WithClothingAsset(\"{CodeFormatter.EscapeString(string.IsNullOrWhiteSpace(item.ClothingAssetPath) ? item.SuggestedClothingAssetPath : item.ClothingAssetPath)}\");");
            builder.AppendLine($"itemBuilder.WithColorable({item.ClothingColorable.ToString().ToLowerInvariant()});");
            builder.AppendLine($"itemBuilder.WithDefaultColor(ClothingColor.{item.DefaultClothingColor});");
            builder.AppendLine($"itemBuilder.WithPricing({CodeFormatter.FormatFloat(item.BasePurchasePrice)}f, {CodeFormatter.FormatFloat(item.ResellMultiplier)}f);");
            if (item.BlockedClothingSlots.Count > 0)
                builder.AppendLine($"itemBuilder.WithBlockedSlots({string.Join(", ", item.BlockedClothingSlots.Select(slot => $"ClothingSlot.{slot}"))});");
            builder.AppendLine();
            GenerateCommonItemEnhancements(builder, item);
            if (!string.IsNullOrWhiteSpace(item.ModelBundleResourcePath))
            {
                builder.OpenBlock("if (customModel != null)");
                builder.AppendLine("itemBuilder.WithStoredItem(customModel);");
                builder.CloseBlock();
            }
            builder.AppendLine("var definition = itemBuilder.Build();");
            builder.AppendLine($"definition.StackLimit = {item.StackLimit};");
            builder.AppendLine($"definition.LegalStatus = LegalStatus.{item.LegalStatus};");
            builder.AppendLine("ConfigureDefinition(definition);");
            builder.AppendLine("return definition;");
        }

        private void GenerateAdditiveDefinition(ICodeBuilder builder, ItemBlueprint item)
        {
            builder.AppendLine($"var itemBuilder = {GetCloneCapableBuilderExpression("AdditiveItemCreator", item.CloneSourceItemId)};");
            builder.AppendLine($"itemBuilder.WithBasicInfo(\"{CodeFormatter.EscapeString(item.ItemId)}\", \"{CodeFormatter.EscapeString(item.ItemName)}\", \"{CodeFormatter.EscapeString(item.ItemDescription)}\", ItemCategory.{item.EffectiveCategory});");
            builder.AppendLine($"itemBuilder.WithStackLimit({item.StackLimit});");
            builder.AppendLine($"itemBuilder.WithPricing({CodeFormatter.FormatFloat(item.BasePurchasePrice)}f, {CodeFormatter.FormatFloat(item.ResellMultiplier)}f);");
            builder.AppendLine($"itemBuilder.WithLegalStatus(LegalStatus.{item.LegalStatus});");
            builder.AppendLine($"itemBuilder.WithDemoAvailability({item.AvailableInDemo.ToString().ToLowerInvariant()});");
            builder.AppendLine($"itemBuilder.WithEffects({CodeFormatter.FormatFloat(item.YieldMultiplier)}f, {CodeFormatter.FormatFloat(item.InstantGrowth)}f, {CodeFormatter.FormatFloat(item.QualityChange)}f);");
            builder.AppendLine();
            GenerateCommonItemEnhancements(builder, item);
            if (!string.IsNullOrWhiteSpace(item.ModelBundleResourcePath))
            {
                builder.OpenBlock("if (customModel != null)");
                builder.AppendLine("itemBuilder.WithStoredItem(customModel);");
                builder.CloseBlock();
            }
            if (!string.IsNullOrWhiteSpace(item.DisplayMaterialResourcePath))
            {
                builder.AppendLine($"var displayMaterial = LoadMaterialResource(\"{CodeFormatter.EscapeString(item.DisplayMaterialResourcePath)}\", \"additive display material\");");
                builder.OpenBlock("if (displayMaterial != null)");
                builder.AppendLine("itemBuilder.WithDisplayMaterial(displayMaterial);");
                builder.CloseBlock();
                builder.AppendLine();
            }
            FinalizeDefinitionBuild(builder);
        }

        private static void FinalizeDefinitionBuild(ICodeBuilder builder)
        {
            builder.AppendLine("var definition = itemBuilder.Build();");
            builder.AppendLine("ConfigureDefinition(definition);");
            builder.AppendLine("return definition;");
        }

        private void GenerateCommonItemEnhancements(ICodeBuilder builder, ItemBlueprint item)
        {
            builder.OpenBlock("if (icon != null)");
            builder.AppendLine("itemBuilder.WithIcon(icon);");
            builder.CloseBlock();
            builder.AppendLine();
            if (item.SupportsEquippable)
            {
                builder.OpenBlock("if (equippable != null)");
                builder.AppendLine("itemBuilder.WithEquippable(equippable);");
                builder.CloseBlock();
                builder.AppendLine();
            }
        }

        private void GenerateShopIntegrationMethod(ICodeBuilder builder, ItemBlueprint item)
        {
            builder.AppendComment("Adds the item to shops after the Main scene is ready, without duplicating existing listings.");
            builder.OpenBlock("private static void IntegrateWithShops()");
            builder.OpenBlock("if (Definition == null)");
            builder.AppendLine("return;");
            builder.CloseBlock();
            builder.AppendLine();

            switch (item.ShopIntegrationMode)
            {
                case ShopIntegrationModeOption.Compatible:
                    builder.AppendLine("var shops = ShopManager.FindShopsByCategory(Definition.Category);");
                    builder.OpenBlock("foreach (var shop in shops)");
                    builder.OpenBlock("if (!shop.HasItem(Definition.ID))");
                    builder.AppendLine($"shop.AddItem(Definition, {GetShopPriceLiteral(item)});");
                    builder.CloseBlock();
                    builder.CloseBlock();
                    builder.AppendLine("ShopManager.RefreshItemIcon(Definition);");
                    break;
                case ShopIntegrationModeOption.Specific:
                    builder.AppendLine($"var shopNames = new[] {{ {string.Join(", ", item.ShopNames.Select(shop => $"\"{CodeFormatter.EscapeString(shop)}\""))} }};");
                    builder.OpenBlock("foreach (var shopName in shopNames)");
                    builder.AppendLine("var shop = ShopManager.GetShopByName(shopName);");
                    builder.OpenBlock("if (shop == null)");
                    builder.AppendLine("MelonLogger.Warning($\"Shop '{shopName}' was not found while registering item '{Definition.ID}'.\");");
                    builder.AppendLine("continue;");
                    builder.CloseBlock();
                    builder.AppendLine();
                    builder.OpenBlock("if (!shop.HasItem(Definition.ID))");
                    builder.AppendLine($"shop.AddItem(Definition, {GetShopPriceLiteral(item)});");
                    builder.CloseBlock();
                    builder.CloseBlock();
                    builder.AppendLine("ShopManager.RefreshItemIcon(Definition);");
                    break;
                default:
                    builder.AppendComment("No shop integration configured for this item.");
                    break;
            }

            builder.CloseBlock();
        }

        private void GenerateGrowContainerIntegrationMethod(ICodeBuilder builder, ItemBlueprint item)
        {
            builder.AppendComment("Allows additive items on grow containers when configured.");
            builder.OpenBlock("private static void RegisterGrowContainerIntegration()");
            if (!item.AllowOnGrowContainers || item.ItemType != ItemKindOption.Additive)
            {
                builder.AppendLine("return;");
                builder.CloseBlock();
                return;
            }

            builder.OpenBlock("if (Definition == null || _growContainerRegistered)");
            builder.AppendLine("return;");
            builder.CloseBlock();
            builder.AppendLine();
            builder.AppendLine("GrowContainerAdditives.AllowAdditive(Definition.ID);");
            builder.AppendLine("_growContainerRegistered = true;");
            builder.CloseBlock();
        }

        private static void GenerateChemistryRecipeMethod(ICodeBuilder builder, ItemBlueprint item)
        {
            builder.AppendComment("Registers Chemistry Station recipes that produce this item.");
            builder.OpenBlock("private static void RegisterChemistryRecipes()");
            if (!item.SupportsChemistryRecipes || item.ChemistryRecipes.Count == 0)
            {
                builder.AppendLine("return;");
                builder.CloseBlock();
                return;
            }

            builder.OpenBlock("if (Definition == null || _recipesRegistered)");
            builder.AppendLine("return;");
            builder.CloseBlock();
            builder.AppendLine();

            foreach (var recipe in item.ChemistryRecipes)
            {
                builder.AppendLine("ChemistryStationRecipes.CreateAndRegister(recipeBuilder =>");
                builder.OpenBlock();
                if (!string.IsNullOrWhiteSpace(recipe.RecipeId))
                    builder.AppendLine($"recipeBuilder.WithRecipeId(\"{CodeFormatter.EscapeString(recipe.RecipeId.Trim())}\");");
                builder.AppendLine($"recipeBuilder.WithTitle(\"{CodeFormatter.EscapeString(recipe.Title)}\");");
                builder.AppendLine($"recipeBuilder.WithCookTimeMinutes({recipe.CookTimeMinutes});");
                builder.AppendLine($"recipeBuilder.WithFinalLiquidColor({CodeFormatter.FormatColorFromHex(recipe.FinalLiquidColorHex)});");
                builder.AppendLine($"recipeBuilder.WithInitialAvailability({recipe.IsDiscovered.ToString().ToLowerInvariant()}, {recipe.IsUnlocked.ToString().ToLowerInvariant()});");

                foreach (var ingredient in recipe.Ingredients)
                {
                    var itemIds = ingredient.ItemIds.Where(id => !string.IsNullOrWhiteSpace(id))
                        .Select(id => $"\"{CodeFormatter.EscapeString(id)}\"").ToList();
                    if (itemIds.Count == 1)
                        builder.AppendLine($"recipeBuilder.WithIngredient({itemIds[0]}, {ingredient.Quantity});");
                    else if (itemIds.Count > 1)
                        builder.AppendLine($"recipeBuilder.WithIngredientOptions(new[] {{ {string.Join(", ", itemIds)} }}, {ingredient.Quantity});");
                }

                builder.AppendLine($"recipeBuilder.WithProduct(Definition.ID, {recipe.ProductQuantity});");
                builder.CloseBlock();
                builder.AppendLine(");");
                builder.AppendLine();
            }

            builder.AppendLine("_recipesRegistered = true;");
            builder.CloseBlock();
        }

        private void GenerateShopIntegrationInvocation(ICodeBuilder builder, ItemBlueprint item)
        {
            if (item.ShopIntegrationMode == ShopIntegrationModeOption.None)
            {
                builder.AppendComment("No shop integration configured for this item.");
                return;
            }

            builder.AppendLine("IntegrateWithShops();");
        }
        private void GenerateEquippableMethod(ICodeBuilder builder, ItemBlueprint item)
        {
            builder.AppendComment("Builds an optional equippable component for item types that support it.");
            builder.OpenBlock("private static Equippable? BuildEquippable()");
            if (!item.SupportsEquippable || item.EquippableType == EquippableTypeOption.None)
            {
                builder.AppendLine("return null;");
                builder.CloseBlock();
                return;
            }

            builder.AppendLine("var equippableBuilder = ItemCreator.CreateEquippableBuilder();");
            switch (item.EquippableType)
            {
                case EquippableTypeOption.Viewmodel:
                    builder.AppendLine($"equippableBuilder.CreateViewmodelEquippable({FormatNullableString(item.EquippableName)});");
                    break;
                case EquippableTypeOption.Basic:
                    builder.AppendLine($"equippableBuilder.CreateBasicEquippable({FormatNullableString(item.EquippableName)});");
                    break;
            }

            builder.AppendLine($"equippableBuilder.WithInteraction({item.EquippableCanInteract.ToString().ToLowerInvariant()}, {item.EquippableCanPickup.ToString().ToLowerInvariant()});");
            if (item.UsesViewmodelEquippable && item.HasViewmodelTransform)
            {
                builder.AppendLine($"equippableBuilder.WithViewmodelTransform({CodeFormatter.FormatVector3(item.ViewmodelPositionX, item.ViewmodelPositionY, item.ViewmodelPositionZ)}, {CodeFormatter.FormatVector3(item.ViewmodelRotationX, item.ViewmodelRotationY, item.ViewmodelRotationZ)}, {CodeFormatter.FormatVector3(item.ViewmodelScaleX, item.ViewmodelScaleY, item.ViewmodelScaleZ)});");
            }
            if (item.UsesViewmodelEquippable && item.HasAvatarEquippable && !string.IsNullOrWhiteSpace(item.AvatarEquippableAssetPath))
            {
                builder.AppendLine($"equippableBuilder.WithAvatarEquippable(\"{CodeFormatter.EscapeString(item.AvatarEquippableAssetPath)}\", AvatarHand.{item.AvatarHand}, \"{CodeFormatter.EscapeString(item.AvatarAnimationTrigger)}\");");
            }
            if (item.EnableUseCallbackHook && item.UsesViewmodelEquippable)
            {
                builder.AppendLine("equippableBuilder.WithUseCallback(HandleUseCallback);");
            }

            builder.AppendLine("ConfigureEquippableBuilder(equippableBuilder);");
            builder.AppendLine("return equippableBuilder.Build();");
            builder.CloseBlock();
        }

        private void GenerateAvatarEquippableMethod(ICodeBuilder builder, ItemBlueprint item)
        {
            builder.AppendComment("Registers a custom avatar equippable from an embedded bundle when configured.");
            builder.OpenBlock("private static void EnsureAvatarEquippableRegistered()");
            if (!item.UsesAvatarBundleRegistration ||
                string.IsNullOrWhiteSpace(item.AvatarBundleResourcePath) ||
                string.IsNullOrWhiteSpace(item.AvatarBundlePrefabName) ||
                string.IsNullOrWhiteSpace(item.AvatarEquippableAssetPath))
            {
                builder.AppendLine("return;");
                builder.CloseBlock();
                return;
            }

            builder.OpenBlock("if (_avatarEquippableRegistered)");
            builder.AppendLine("return;");
            builder.CloseBlock();
            builder.AppendLine();
            builder.AppendLine($"const string assetPath = \"{CodeFormatter.EscapeString(item.AvatarEquippableAssetPath)}\";");
            builder.OpenBlock("if (AvatarEquippableRegistry.IsRegistered(assetPath))");
            builder.AppendLine("_avatarEquippableRegistered = true;");
            builder.AppendLine("return;");
            builder.CloseBlock();
            builder.AppendLine();
            builder.AppendLine($"var resourceName = ResolveEmbeddedResourceName(\"{CodeFormatter.EscapeString(item.AvatarBundleResourcePath)}\");");
            builder.OpenBlock("if (resourceName == null)");
            builder.AppendLine($"MelonLogger.Warning(\"Could not find embedded avatar bundle '{CodeFormatter.EscapeString(item.AvatarBundleResourcePath)}'.\");");
            builder.AppendLine("return;");
            builder.CloseBlock();
            builder.AppendLine();
            builder.AppendLine("var assembly = Assembly.GetExecutingAssembly();");
            builder.AppendLine($"var success = AvatarEquippableRegistry.LoadAndRegisterFromEmbeddedBundle(resourceName, \"{CodeFormatter.EscapeString(item.AvatarBundlePrefabName)}\", assetPath, assembly);");
            builder.OpenBlock("if (!success)");
            builder.AppendLine($"MelonLogger.Warning(\"Failed to register embedded avatar equippable '{CodeFormatter.EscapeString(item.AvatarBundlePrefabName)}'.\");");
            builder.AppendLine("return;");
            builder.CloseBlock();
            builder.AppendLine();
            builder.AppendLine("_avatarEquippableRegistered = true;");
            builder.CloseBlock();
        }

        private void GenerateClothingRuntimeRegistrationMethod(ICodeBuilder builder, ItemBlueprint item)
        {
            builder.AppendComment("Registers runtime clothing assets generated from embedded texture resources.");
            builder.OpenBlock("private static void EnsureClothingRuntimeAssetsRegistered()");
            if (item.ItemType != ItemKindOption.Clothing ||
                (string.IsNullOrWhiteSpace(item.ClothingTextureResourcePath) && string.IsNullOrWhiteSpace(item.ModelBundleResourcePath)) ||
                (string.IsNullOrWhiteSpace(item.ClothingAssetPath) && string.IsNullOrWhiteSpace(item.ModelBundleResourcePath)))
            {
                builder.AppendLine("return;");
                builder.CloseBlock();
                return;
            }

            builder.OpenBlock("if (_clothingRuntimeAssetsRegistered)");
            builder.AppendLine("return;");
            builder.CloseBlock();
            builder.AppendLine();
            if (!string.IsNullOrWhiteSpace(item.ModelBundleResourcePath))
            {
                var clothingPath = string.IsNullOrWhiteSpace(item.ClothingAssetPath) ? item.SuggestedClothingAssetPath : item.ClothingAssetPath;
                builder.AppendLine("var model = LoadModelPrefab();");
                builder.OpenBlock("if (model != null)");
                builder.AppendLine($"RuntimeResourceRegistry.RegisterGameObject(\"{CodeFormatter.EscapeString(clothingPath)}\", model);");
                builder.AppendLine("_clothingRuntimeAssetsRegistered = true;");
                builder.AppendLine("return;");
                builder.CloseBlock();
                if (string.IsNullOrWhiteSpace(item.ClothingTextureResourcePath))
                {
                    builder.AppendLine("return;");
                    builder.CloseBlock();
                    return;
                }
            }
            builder.AppendLine($"var texture = LoadEmbeddedTexture(\"{CodeFormatter.EscapeString(item.ClothingTextureResourcePath)}\", \"clothing texture\");");
            builder.OpenBlock("if (texture == null)");
            builder.AppendLine("return;");
            builder.CloseBlock();
            builder.AppendLine();

            if (item.ClothingApplicationType == ClothingApplicationTypeOption.Accessory)
            {
                if (string.IsNullOrWhiteSpace(item.ClothingTextureSourceAssetPath))
                {
                    builder.AppendLine($"MelonLogger.Warning(\"Accessory texture override for '{CodeFormatter.EscapeString(item.DisplayName)}' is missing a source accessory asset path.\");");
                    builder.AppendLine("return;");
                    builder.CloseBlock();
                    return;
                }

                builder.AppendLine("var textureReplacements = new Dictionary<string, Texture2D>");
                builder.AppendLine("{");
                builder.AppendLine($"    [\"{CodeFormatter.EscapeString(item.AccessoryTextureShaderPropertyName)}\"] = texture");
                builder.AppendLine("};");
                builder.AppendLine($"var accessoryRegistered = AccessoryFactory.CreateAndRegisterAccessory(\"{CodeFormatter.EscapeString(item.ClothingTextureSourceAssetPath)}\", \"{CodeFormatter.EscapeString(item.ClothingAssetPath)}\", \"{CodeFormatter.EscapeString(item.DisplayName)}\", textureReplacements, null);");
                builder.OpenBlock("if (!accessoryRegistered)");
                builder.AppendLine($"MelonLogger.Warning(\"Failed to create runtime accessory override '{CodeFormatter.EscapeString(item.ClothingAssetPath)}'.\");");
                builder.AppendLine("return;");
                builder.CloseBlock();
            }
            else
            {
                builder.AppendLine($"RuntimeResourceRegistry.RegisterAsset(\"{CodeFormatter.EscapeString(item.ClothingAssetPath)}\", texture);");
                builder.AppendLine($"RuntimeResourceRegistry.RegisterAssetForType(\"{CodeFormatter.EscapeString(item.ClothingAssetPath)}\", texture, typeof(Texture2D));");
            }

            builder.AppendLine("_clothingRuntimeAssetsRegistered = true;");
            builder.CloseBlock();
        }

        private void GenerateResourceLoadingHelpers(ICodeBuilder builder)
        {
            builder.AppendComment("Loads optional prefab and material resources from Unity's runtime Resources registry.");
            builder.OpenBlock("private static GameObject? LoadGameObjectResource(string path, string label)");
            builder.OpenBlock("if (string.IsNullOrWhiteSpace(path))");
            builder.AppendLine("return null;");
            builder.CloseBlock();
            builder.AppendLine();
            builder.AppendLine("var prefab = Resources.Load<GameObject>(path);");
            builder.OpenBlock("if (prefab == null)");
            builder.AppendLine("MelonLogger.Warning($\"Could not load {label} at resource path '{path}'.\");");
            builder.CloseBlock();
            builder.AppendLine();
            builder.AppendLine("return prefab;");
            builder.CloseBlock();
            builder.AppendLine();

            builder.OpenBlock("private static Material? LoadMaterialResource(string path, string label)");
            builder.OpenBlock("if (string.IsNullOrWhiteSpace(path))");
            builder.AppendLine("return null;");
            builder.CloseBlock();
            builder.AppendLine();
            builder.AppendLine("var material = Resources.Load<Material>(path);");
            builder.OpenBlock("if (material == null)");
            builder.AppendLine("MelonLogger.Warning($\"Could not load {label} at resource path '{path}'.\");");
            builder.CloseBlock();
            builder.AppendLine();
            builder.AppendLine("return material;");
            builder.CloseBlock();
            builder.AppendLine();

            builder.OpenBlock("private static Texture2D? LoadEmbeddedTexture(string relativePath, string label)");
            builder.OpenBlock("if (string.IsNullOrWhiteSpace(relativePath))");
            builder.AppendLine("return null;");
            builder.CloseBlock();
            builder.AppendLine();
            builder.OpenBlock("try");
            builder.AppendLine("var resourceName = ResolveEmbeddedResourceName(relativePath);");
            builder.OpenBlock("if (resourceName == null)");
            builder.AppendLine("MelonLogger.Warning($\"Could not find embedded {label} '{relativePath}'.\");");
            builder.AppendLine("return null;");
            builder.CloseBlock();
            builder.AppendLine();
            builder.AppendLine("var assembly = Assembly.GetExecutingAssembly();");
            builder.AppendLine("return TextureUtils.LoadTextureFromResource(assembly, resourceName, FilterMode.Bilinear, TextureWrapMode.Clamp);");
            builder.CloseBlock();
            builder.OpenBlock("catch (Exception ex)");
            builder.AppendLine("MelonLogger.Warning($\"Failed to load embedded {label} '{relativePath}': {ex.Message}\");");
            builder.AppendLine("return null;");
            builder.CloseBlock();
            builder.CloseBlock();
            builder.AppendLine();

            builder.AppendComment("Resolves an embedded resource by relative path or filename.");
            builder.OpenBlock("private static string? ResolveEmbeddedResourceName(string path)");
            builder.OpenBlock("if (string.IsNullOrWhiteSpace(path))");
            builder.AppendLine("return null;");
            builder.CloseBlock();
            builder.AppendLine();
            builder.AppendLine("var normalizedPath = path.Replace('\\\\', '/').TrimStart('/');");
            builder.AppendLine("var fileName = Path.GetFileName(normalizedPath);");
            builder.AppendLine("var exactSuffix = \".\" + normalizedPath.Replace('/', '.');");
            builder.AppendLine("var fileNameSuffix = \".\" + fileName;");
            builder.AppendLine("var assembly = Assembly.GetExecutingAssembly();");
            builder.AppendLine("return assembly.GetManifestResourceNames().FirstOrDefault(name =>");
            builder.AppendLine("    name.EndsWith(exactSuffix, StringComparison.OrdinalIgnoreCase)");
            builder.AppendLine("    || name.EndsWith(fileNameSuffix, StringComparison.OrdinalIgnoreCase));");
            builder.CloseBlock();
        }

        private void GenerateIconMethod(ICodeBuilder builder, ItemBlueprint item)
        {
            builder.AppendComment("Loads an embedded PNG resource as the item icon.");
            builder.OpenBlock("private static Sprite? LoadCustomIcon()");
            if (string.IsNullOrWhiteSpace(item.IconFileName))
            {
                builder.AppendLine("return null;");
                builder.CloseBlock();
                return;
            }

            builder.OpenBlock("try");
            builder.AppendLine($"var resourceName = ResolveEmbeddedResourceName(\"{CodeFormatter.EscapeString(item.IconFileName)}\");");
            builder.OpenBlock("if (resourceName == null)");
            builder.AppendLine("return null;");
            builder.CloseBlock();
            builder.AppendLine();
            builder.AppendLine("var assembly = Assembly.GetExecutingAssembly();");
            builder.AppendLine("using var stream = assembly.GetManifestResourceStream(resourceName);");
            builder.OpenBlock("if (stream == null)");
            builder.AppendLine("return null;");
            builder.CloseBlock();
            builder.AppendLine();
            builder.AppendLine("byte[] data = new byte[stream.Length];");
            builder.AppendLine("stream.Read(data, 0, data.Length);");
            builder.AppendLine("return ImageUtils.LoadImageRaw(data);");
            builder.CloseBlock();
            builder.OpenBlock("catch (Exception ex)");
            builder.AppendLine($"MelonLogger.Warning($\"Failed to load item icon '{CodeFormatter.EscapeString(item.IconFileName)}': {{ex.Message}}\");");
            builder.AppendLine("return null;");
            builder.CloseBlock();
            builder.CloseBlock();
        }

        private static void GenerateModelLoader(ICodeBuilder builder, ItemBlueprint item)
        {
            builder.AppendComment("Loads and caches the selected embedded model.");
            builder.OpenBlock("private static GameObject? LoadModelPrefab()");
            builder.OpenBlock("if (_modelPrefab != null)");
            builder.AppendLine("return _modelPrefab;");
            builder.CloseBlock();
            builder.OpenBlock("try");
            builder.AppendLine("var assembly = Assembly.GetExecutingAssembly();");
            builder.AppendLine($"const string path = \"{CodeFormatter.EscapeString(item.ModelBundleResourcePath)}\";");
            builder.AppendLine("var suffix = \".\" + path.Replace('\\\\', '/').Replace('/', '.');");
            builder.AppendLine("var resourceName = assembly.GetManifestResourceNames().FirstOrDefault(name => name.EndsWith(suffix, StringComparison.OrdinalIgnoreCase));");
            builder.OpenBlock("if (resourceName == null)");
            builder.AppendLine("MelonLogger.Warning($\"Model '{path}' is not embedded in the mod.\");");
            builder.AppendLine("return null;");
            builder.CloseBlock();
            if (item.IsGlbModel)
            {
                builder.AppendLine("using var stream = assembly.GetManifestResourceStream(resourceName);");
                builder.AppendLine("if (stream == null) return null;");
                builder.AppendLine("using var bytes = new System.IO.MemoryStream();");
                builder.AppendLine("stream.CopyTo(bytes);");
                builder.AppendLine("var imported = new S1MAPI.Gltf.GltfImporter().ImportAnimations(false).ImportSkins(false).ImportBlendShapes(false).ImportCameras(false).LoadGlb(bytes.ToArray());");
                builder.AppendLine("if (imported == null) return null;");
                builder.AppendLine("_modelPrefab = new GameObject(\"GLB model template\");");
                builder.AppendLine("_modelPrefab.SetActive(false);");
                builder.AppendLine("UnityEngine.Object.DontDestroyOnLoad(_modelPrefab);");
                builder.AppendLine("imported.transform.SetParent(_modelPrefab.transform, false);");
                builder.AppendLine($"imported.transform.localScale = Vector3.one * {CodeFormatter.FormatFloat(item.ModelScale)}f;");
                builder.AppendLine($"imported.transform.localRotation = Quaternion.Euler({CodeFormatter.FormatFloat(item.ModelRotationX)}f, {CodeFormatter.FormatFloat(item.ModelRotationY)}f, {CodeFormatter.FormatFloat(item.ModelRotationZ)}f);");
                builder.AppendLine($"imported.transform.localPosition = new Vector3({CodeFormatter.FormatFloat(item.ModelOffsetX)}f, {CodeFormatter.FormatFloat(item.ModelOffsetY)}f, {CodeFormatter.FormatFloat(item.ModelOffsetZ)}f);");
            }
            else
            {
                builder.AppendLine($"_modelPrefab = AssetLoader.EasyLoad<GameObject>(resourceName, \"{CodeFormatter.EscapeString(item.ModelPrefabName)}\", assembly);");
            }
            builder.OpenBlock("if (_modelPrefab == null)");
            builder.AppendLine($"MelonLogger.Warning(\"Prefab '{CodeFormatter.EscapeString(item.ModelPrefabName)}' was not found in model bundle '{CodeFormatter.EscapeString(item.ModelBundleResourcePath)}'.\");");
            builder.CloseBlock();
            builder.AppendLine("return _modelPrefab;");
            builder.CloseBlock();
            builder.OpenBlock("catch (Exception ex)");
            builder.AppendLine("MelonLogger.Warning($\"Failed to load model: {ex.Message}\");");
            builder.AppendLine("return null;");
            builder.CloseBlock();
            builder.CloseBlock();
        }

        private void GeneratePartialHookMembers(ICodeBuilder builder)
        {
            builder.AppendComment("Optional hook points for generated partial companion files.");
            builder.OpenBlock("private static void HandleUseCallback(ItemInstance itemInstance)");
            builder.AppendLine("OnUse(itemInstance);");
            builder.CloseBlock();
            builder.AppendLine();
            builder.AppendLine("static partial void ConfigureDefinition(StorableItemDefinition definition);");
            builder.AppendLine("static partial void ConfigureEquippableBuilder(EquippableBuilder equippableBuilder);");
            builder.AppendLine("static partial void OnAfterRegister(StorableItemDefinition definition);");
            builder.AppendLine("static partial void OnUse(ItemInstance itemInstance);");
        }

        private static string GetCloneCapableBuilderExpression(string creatorType, string cloneSourceItemId)
        {
            return string.IsNullOrWhiteSpace(cloneSourceItemId)
                ? $"{creatorType}.CreateBuilder()"
                : $"{creatorType}.CloneFrom(\"{CodeFormatter.EscapeString(cloneSourceItemId)}\")";
        }

        private static string GetShopPriceLiteral(ItemBlueprint item)
        {
            return item.UseCustomShopPrice
                ? $"{CodeFormatter.FormatFloat(item.CustomShopPrice)}f"
                : "null";
        }

        private static string FormatNullableString(string? value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? "null"
                : $"\"{CodeFormatter.EscapeString(value)}\"";
        }
    }
}
