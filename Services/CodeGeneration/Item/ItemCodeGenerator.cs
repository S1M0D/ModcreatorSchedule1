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

            var builder = new CodeBuilder();
            var className = IdentifierSanitizer.MakeSafeIdentifier(item.ClassName, "GeneratedItem");
            var targetNamespace = NamespaceNormalizer.NormalizeForItem(item.Namespace);
            var usingsBuilder = new UsingStatementsBuilder();
            usingsBuilder.AddItemUsings();

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
                string.IsNullOrWhiteSpace(blueprint.ClothingAssetPath))
            {
                result.Errors.Add("Clothing items need either a clone source item ID or a clothing asset path.");
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

            foreach (var recipe in blueprint.ChemistryRecipes)
            {
                if (string.IsNullOrWhiteSpace(recipe.Title))
                    result.Errors.Add("Chemistry recipe titles cannot be empty.");
                if (recipe.CookTimeMinutes < 1)
                    result.Errors.Add($"Chemistry recipe '{recipe.DisplayName}' must cook for at least 1 minute.");
                if (recipe.ProductQuantity < 1)
                    result.Errors.Add($"Chemistry recipe '{recipe.DisplayName}' must produce at least 1 item.");
                if (recipe.Ingredients.Count == 0)
                    result.Errors.Add($"Chemistry recipe '{recipe.DisplayName}' needs at least one ingredient group.");
                foreach (var ingredient in recipe.Ingredients)
                {
                    if (ingredient.Quantity < 1)
                        result.Errors.Add($"Chemistry recipe '{recipe.DisplayName}' has an ingredient group with an invalid quantity.");
                    if (ingredient.ItemIds.Count == 0 || ingredient.ItemIds.All(string.IsNullOrWhiteSpace))
                        result.Errors.Add($"Chemistry recipe '{recipe.DisplayName}' has an ingredient group without any item IDs.");
                }
            }

            result.IsValid = result.Errors.Count == 0;
            return result;
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
            GenerateResourceLoadingHelpers(builder);
            builder.AppendLine();
            GenerateIconMethod(builder, item);
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
            builder.AppendLine("EnsureAvatarEquippableRegistered();");
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
            builder.AppendLine($"var itemBuilder = {GetCloneCapableBuilderExpression("BuildableItemCreator", item.CloneSourceItemId)};");
            builder.AppendLine($"itemBuilder.WithBasicInfo(\"{CodeFormatter.EscapeString(item.ItemId)}\", \"{CodeFormatter.EscapeString(item.ItemName)}\", \"{CodeFormatter.EscapeString(item.ItemDescription)}\");");
            builder.AppendLine($"itemBuilder.WithBuildSound(BuildSoundType.{item.BuildSoundType});");
            builder.AppendLine($"itemBuilder.WithPricing({CodeFormatter.FormatFloat(item.BasePurchasePrice)}f, {CodeFormatter.FormatFloat(item.ResellMultiplier)}f);");
            builder.AppendLine($"itemBuilder.WithCategory(ItemCategory.{item.EffectiveCategory});");
            builder.AppendLine($"itemBuilder.WithStackLimit({item.StackLimit});");
            builder.AppendLine($"itemBuilder.WithLegalStatus(LegalStatus.{item.LegalStatus});");
            builder.AppendLine();
            GenerateCommonItemEnhancements(builder, item);
            FinalizeDefinitionBuild(builder);
        }

        private void GenerateClothingDefinition(ICodeBuilder builder, ItemBlueprint item)
        {
            builder.AppendLine($"var itemBuilder = {GetCloneCapableBuilderExpression("ClothingItemCreator", item.CloneSourceItemId)};");
            builder.AppendLine($"itemBuilder.WithBasicInfo(\"{CodeFormatter.EscapeString(item.ItemId)}\", \"{CodeFormatter.EscapeString(item.ItemName)}\", \"{CodeFormatter.EscapeString(item.ItemDescription)}\");");
            builder.AppendLine($"itemBuilder.WithSlot(ClothingSlot.{item.ClothingSlot});");
            builder.AppendLine($"itemBuilder.WithApplicationType(ClothingApplicationType.{item.ClothingApplicationType});");
            if (!string.IsNullOrWhiteSpace(item.ClothingAssetPath))
                builder.AppendLine($"itemBuilder.WithClothingAsset(\"{CodeFormatter.EscapeString(item.ClothingAssetPath)}\");");
            builder.AppendLine($"itemBuilder.WithColorable({item.ClothingColorable.ToString().ToLowerInvariant()});");
            builder.AppendLine($"itemBuilder.WithDefaultColor(ClothingColor.{item.DefaultClothingColor});");
            builder.AppendLine($"itemBuilder.WithPricing({CodeFormatter.FormatFloat(item.BasePurchasePrice)}f, {CodeFormatter.FormatFloat(item.ResellMultiplier)}f);");
            if (item.BlockedClothingSlots.Count > 0)
                builder.AppendLine($"itemBuilder.WithBlockedSlots({string.Join(", ", item.BlockedClothingSlots.Select(slot => $"ClothingSlot.{slot}"))});");
            builder.AppendLine();
            GenerateCommonItemEnhancements(builder, item);
            FinalizeDefinitionBuild(builder);
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

        private void GenerateChemistryRecipeMethod(ICodeBuilder builder, ItemBlueprint item)
        {
            builder.AppendComment("Registers Chemistry Station recipes that produce this item.");
            builder.OpenBlock("private static void RegisterChemistryRecipes()");
            if (item.ChemistryRecipes.Count == 0)
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
                builder.AppendLine($"recipeBuilder.WithTitle(\"{CodeFormatter.EscapeString(recipe.Title)}\");");
                builder.AppendLine($"recipeBuilder.WithCookTimeMinutes({recipe.CookTimeMinutes});");
                builder.AppendLine($"recipeBuilder.WithFinalLiquidColor({CodeFormatter.FormatColorFromHex(recipe.FinalLiquidColorHex)});");

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
