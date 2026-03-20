using Schedule1ModdingTool.Models;
using Schedule1ModdingTool.Services.CodeGeneration.Abstractions;
using Schedule1ModdingTool.Services.CodeGeneration.Builders;
using Schedule1ModdingTool.Services.CodeGeneration.Common;
using System.IO;

namespace Schedule1ModdingTool.Services.CodeGeneration.Item
{
    /// <summary>
    /// Generates runtime registration code for S1API items.
    /// </summary>
    public class ItemCodeGenerator : ICodeGenerator<ItemBlueprint>
    {
        public string GenerateCode(ItemBlueprint item)
        {
            if (item == null)
                throw new ArgumentNullException(nameof(item));

            var builder = new CodeBuilder();
            var className = IdentifierSanitizer.MakeSafeIdentifier(item.ClassName, "GeneratedItem");
            var targetNamespace = NamespaceNormalizer.NormalizeForItem(item.Namespace);

            builder.AppendComment("Auto-generated item registration class.");

            var usingsBuilder = new UsingStatementsBuilder();
            usingsBuilder.AddItemUsings();
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
            {
                result.Warnings.Add("Class name is empty, will use default 'GeneratedItem'.");
            }

            if (string.IsNullOrWhiteSpace(blueprint.ItemId))
            {
                result.Errors.Add("Item ID is required.");
            }

            if (string.IsNullOrWhiteSpace(blueprint.ItemName))
            {
                result.Errors.Add("Item name is required.");
            }

            if (blueprint.SupportsStackLimit && blueprint.StackLimit < 1)
            {
                result.Errors.Add("Stack limit must be at least 1.");
            }

            if (blueprint.ResellMultiplier < 0f || blueprint.ResellMultiplier > 1f)
            {
                result.Errors.Add("Resell multiplier must be between 0 and 1.");
            }

            if (blueprint.UseCustomShopPrice && blueprint.CustomShopPrice < 0f)
            {
                result.Errors.Add("Custom shop price cannot be negative.");
            }

            if (blueprint.UsesSpecificShops && blueprint.ShopNames.Count == 0)
            {
                result.Errors.Add("Specific shop mode requires at least one shop name.");
            }

            if (blueprint.ItemType == ItemKindOption.Clothing &&
                string.IsNullOrWhiteSpace(blueprint.CloneSourceItemId) &&
                string.IsNullOrWhiteSpace(blueprint.ClothingAssetPath))
            {
                result.Errors.Add("Clothing items need either a clone source item ID or a clothing asset path.");
            }

            if (!blueprint.SupportsEquippable && blueprint.EquippableType != EquippableTypeOption.None)
            {
                result.Warnings.Add("Equippable settings are ignored for this item type.");
            }

            if (blueprint.ClearStationItem && !blueprint.SupportsStationItemPrefab)
            {
                result.Warnings.Add("Clear station item only applies to generic storable items.");
            }

            if (blueprint.ItemType != ItemKindOption.Additive && !string.IsNullOrWhiteSpace(blueprint.DisplayMaterialResourcePath))
            {
                result.Warnings.Add("Display material is only used by additive items.");
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

            builder.OpenBlock($"public static class {className}");
            builder.AppendLine("public static StorableItemDefinition? Definition { get; private set; }");
            builder.AppendLine();

            GenerateRegisterMethod(builder, item);
            builder.AppendLine();
            GenerateBuildDefinitionMethod(builder, item);
            builder.AppendLine();
            GenerateShopIntegrationMethod(builder, item);
            builder.AppendLine();
            GenerateEquippableMethod(builder, item);
            builder.AppendLine();
            GenerateResourceLoadingHelpers(builder);
            builder.AppendLine();
            GenerateIconMethod(builder, item);

            builder.CloseBlock();
        }

        private void GenerateRegisterMethod(ICodeBuilder builder, ItemBlueprint item)
        {
            builder.AppendComment("Builds the item once, then re-integrates it with shops whenever Register is called.");
            builder.OpenBlock("public static StorableItemDefinition? Register()");
            builder.OpenBlock("if (Definition == null)");
            builder.AppendLine("Definition = BuildDefinition();");
            builder.CloseBlock();
            builder.AppendLine();
            builder.OpenBlock("if (Definition != null)");
            GenerateShopIntegrationInvocation(builder, item);
            builder.CloseBlock();
            builder.AppendLine();
            builder.AppendLine("return Definition;");
            builder.CloseBlock();
        }

        private void GenerateBuildDefinitionMethod(ICodeBuilder builder, ItemBlueprint item)
        {
            builder.AppendComment("Builds the underlying S1API definition for the configured item type.");
            builder.OpenBlock("private static StorableItemDefinition? BuildDefinition()");
            builder.AppendLine("var icon = LoadCustomIcon();");
            if (item.SupportsEquippable)
            {
                builder.AppendLine("var equippable = BuildEquippable();");
            }
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
            builder.AppendLine($"    .WithBasicInfo(\"{CodeFormatter.EscapeString(item.ItemId)}\", \"{CodeFormatter.EscapeString(item.ItemName)}\", \"{CodeFormatter.EscapeString(item.ItemDescription)}\", ItemCategory.{item.EffectiveCategory})")
                .AppendLine($"    .WithStackLimit({item.StackLimit})")
                .AppendLine($"    .WithPricing({CodeFormatter.FormatFloat(item.BasePurchasePrice)}f, {CodeFormatter.FormatFloat(item.ResellMultiplier)}f)")
                .AppendLine($"    .WithLegalStatus(LegalStatus.{item.LegalStatus})")
                .AppendLine($"    .WithDemoAvailability({item.AvailableInDemo.ToString().ToLowerInvariant()});");
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

            builder.AppendLine("return itemBuilder.Build();");
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
            builder.AppendLine("return itemBuilder.Build();");
        }

        private void GenerateClothingDefinition(ICodeBuilder builder, ItemBlueprint item)
        {
            builder.AppendLine($"var itemBuilder = {GetCloneCapableBuilderExpression("ClothingItemCreator", item.CloneSourceItemId)};");
            builder.AppendLine($"itemBuilder.WithBasicInfo(\"{CodeFormatter.EscapeString(item.ItemId)}\", \"{CodeFormatter.EscapeString(item.ItemName)}\", \"{CodeFormatter.EscapeString(item.ItemDescription)}\");");
            builder.AppendLine($"itemBuilder.WithSlot(ClothingSlot.{item.ClothingSlot});");
            builder.AppendLine($"itemBuilder.WithApplicationType(ClothingApplicationType.{item.ClothingApplicationType});");
            if (!string.IsNullOrWhiteSpace(item.ClothingAssetPath))
            {
                builder.AppendLine($"itemBuilder.WithClothingAsset(\"{CodeFormatter.EscapeString(item.ClothingAssetPath)}\");");
            }
            builder.AppendLine($"itemBuilder.WithColorable({item.ClothingColorable.ToString().ToLowerInvariant()});");
            builder.AppendLine($"itemBuilder.WithDefaultColor(ClothingColor.{item.DefaultClothingColor});");
            builder.AppendLine($"itemBuilder.WithPricing({CodeFormatter.FormatFloat(item.BasePurchasePrice)}f, {CodeFormatter.FormatFloat(item.ResellMultiplier)}f);");
            if (item.BlockedClothingSlots.Count > 0)
            {
                builder.AppendLine($"itemBuilder.WithBlockedSlots({string.Join(", ", item.BlockedClothingSlots.Select(slot => $"ClothingSlot.{slot}"))});");
            }
            builder.AppendLine();

            GenerateCommonItemEnhancements(builder, item);
            builder.AppendLine("return itemBuilder.Build();");
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

            builder.AppendLine("return itemBuilder.Build();");
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
            builder.AppendComment("Builds an optional equippable component for generic and buildable items.");
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
                builder.AppendLine($"equippableBuilder.WithViewmodelTransform(new Vector3({CodeFormatter.FormatFloat(item.ViewmodelPositionX)}f, {CodeFormatter.FormatFloat(item.ViewmodelPositionY)}f, {CodeFormatter.FormatFloat(item.ViewmodelPositionZ)}f), new Vector3({CodeFormatter.FormatFloat(item.ViewmodelRotationX)}f, {CodeFormatter.FormatFloat(item.ViewmodelRotationY)}f, {CodeFormatter.FormatFloat(item.ViewmodelRotationZ)}f), new Vector3({CodeFormatter.FormatFloat(item.ViewmodelScaleX)}f, {CodeFormatter.FormatFloat(item.ViewmodelScaleY)}f, {CodeFormatter.FormatFloat(item.ViewmodelScaleZ)}f));");
            }

            if (item.UsesViewmodelEquippable && item.HasAvatarEquippable && !string.IsNullOrWhiteSpace(item.AvatarEquippableAssetPath))
            {
                builder.AppendLine($"equippableBuilder.WithAvatarEquippable(\"{CodeFormatter.EscapeString(item.AvatarEquippableAssetPath)}\", AvatarHand.{item.AvatarHand}, \"{CodeFormatter.EscapeString(item.AvatarAnimationTrigger)}\");");
            }

            builder.AppendLine("return equippableBuilder.Build();");
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
        }

        private void GenerateIconMethod(ICodeBuilder builder, ItemBlueprint item)
        {
            builder.AppendComment("Loads an embedded PNG resource as the item icon.");
            builder.OpenBlock("private static Sprite? LoadCustomIcon()");

            if (string.IsNullOrWhiteSpace(item.IconFileName))
            {
                builder.AppendLine("return null;");
            }
            else
            {
                var normalizedResourcePath = (item.IconFileName ?? string.Empty).Replace('\\', '/').TrimStart('/');
                var fileName = Path.GetFileName(normalizedResourcePath);
                var manifestSuffix = normalizedResourcePath.StartsWith("Resources/", StringComparison.OrdinalIgnoreCase)
                    ? $".{normalizedResourcePath.Replace('/', '.')}"
                    : $".Resources.{fileName}";
                var fileNameSuffix = $".{fileName}";

                builder.OpenBlock("try");
                builder.AppendLine("var assembly = Assembly.GetExecutingAssembly();");
                builder.AppendLine($"var resourceName = assembly.GetManifestResourceNames().FirstOrDefault(name => name.EndsWith(\"{CodeFormatter.EscapeString(manifestSuffix)}\", StringComparison.OrdinalIgnoreCase) || name.EndsWith(\"{CodeFormatter.EscapeString(fileNameSuffix)}\", StringComparison.OrdinalIgnoreCase));");
                builder.OpenBlock("if (resourceName == null)");
                builder.AppendLine("return null;");
                builder.CloseBlock();
                builder.AppendLine();
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
            }

            builder.CloseBlock();
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
