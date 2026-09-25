using Newtonsoft.Json;
using System.Collections.ObjectModel;

namespace Schedule1ModdingTool.Models
{
    /// <summary>
    /// Represents a custom S1API item blueprint and its editor-visible configuration.
    /// </summary>
    public class ItemBlueprint : ObservableObject
    {
        private string _className = "GeneratedItem";
        private string _namespace = "Schedule1Mods.Items";
        private string _itemId = "custom_item";
        private string _itemName = "Custom Item";
        private string _itemDescription = "A custom item created with S1API.";
        private ItemKindOption _itemType = ItemKindOption.Generic;
        private string _cloneSourceItemId = string.Empty;
        private ItemCategoryOption _category = ItemCategoryOption.Tools;
        private int _stackLimit = 10;
        private float _basePurchasePrice = 10f;
        private float _resellMultiplier = 0.5f;
        private ItemLegalStatusOption _legalStatus = ItemLegalStatusOption.Legal;
        private bool _availableInDemo;
        private ShopIntegrationModeOption _shopIntegrationMode = ShopIntegrationModeOption.Compatible;
        private bool _useCustomShopPrice;
        private float _customShopPrice;
        private string _iconFileName = string.Empty;
        private string _modelBundleResourcePath = string.Empty;
        private string _modelPrefabName = string.Empty;
        private EquippableTypeOption _equippableType = EquippableTypeOption.None;
        private string _equippableName = string.Empty;
        private bool _equippableCanInteract = true;
        private bool _equippableCanPickup = true;
        private bool _hasViewmodelTransform;
        private float _viewmodelPositionX;
        private float _viewmodelPositionY;
        private float _viewmodelPositionZ;
        private float _viewmodelRotationX;
        private float _viewmodelRotationY;
        private float _viewmodelRotationZ;
        private float _viewmodelScaleX = 1f;
        private float _viewmodelScaleY = 1f;
        private float _viewmodelScaleZ = 1f;
        private bool _hasAvatarEquippable;
        private string _avatarEquippableAssetPath = string.Empty;
        private AvatarHandOption _avatarHand = AvatarHandOption.Right;
        private string _avatarAnimationTrigger = "RightArm_Hold_ClosedHand";
        private string _storedItemResourcePath = string.Empty;
        private string _stationItemResourcePath = string.Empty;
        private bool _clearStationItem;
        private BuildSoundTypeOption _buildSoundType = BuildSoundTypeOption.Wood;
        private ClothingSlotOption _clothingSlot = ClothingSlotOption.Head;
        private ClothingApplicationTypeOption _clothingApplicationType = ClothingApplicationTypeOption.Accessory;
        private string _clothingAssetPath = string.Empty;
        private string _clothingTextureSourceAssetPath = string.Empty;
        private string _clothingTextureResourcePath = string.Empty;
        private string _accessoryTextureShaderPropertyName = "_MainTex";
        private bool _clothingColorable = true;
        private ClothingColorOption _defaultClothingColor = ClothingColorOption.White;
        private string _displayMaterialResourcePath = string.Empty;
        private float _qualityChange;
        private float _yieldMultiplier = 1f;
        private float _instantGrowth;
        private bool _allowOnGrowContainers;
        private string _drugEffects = string.Empty;
        private bool _useCustomWeedAppearance;
        private string _weedMainColor = "#FF689C52";
        private string _weedSecondaryColor = "#FF996CBD";
        private string _weedLeafColor = "#FF3F7C36";
        private string _weedStemColor = "#FF604A30";
        private string _productKindId = string.Empty;
        private string _productKindName = "Custom Product";
        private DrugCompatibilityOption _compatibilityDrugType = DrugCompatibilityOption.Marijuana;
        private bool _enableCustomProductMixing;
        private ProductMixingMapOption _customProductMixingMap = ProductMixingMapOption.Marijuana;
        private bool _usePropertyColorMixing;
        private string _representationTemplateItemId = "ogkush";
        private float _baseAddictiveness = 0.2f;
        private ProductQualityOption _defaultProductQuality = ProductQualityOption.Standard;
        private int _playerEffectDurationSeconds = 120;
        private int _npcEffectDurationSeconds = 180;
        private string _productPackagingIds = "baggie";
        private bool _discoverCustomProduct = true;
        private bool _listCustomProduct;
        private bool _showCustomProductKindInManager;
        private string _productKindColor = "#FF7EC8A1";
        private string _customDrugShopNames = string.Empty;
        private string _productConsoleAlias = string.Empty;
        private bool _enableUseCallbackHook;
        private bool _generateHookScaffold;
        private bool _registerAvatarEquippableFromEmbeddedBundle;
        private string _avatarBundleResourcePath = string.Empty;
        private string _avatarBundlePrefabName = string.Empty;
        private string _folderId = QuestProject.RootFolderId;
        private string _modName = "Schedule 1 Item Pack";
        private string _modAuthor = "Item Creator";
        private string _modVersion = "1.0.0";
        private string _gameDeveloper = "TVGS";
        private string _gameName = "Schedule I";

        public ItemBlueprint()
        {
            ShopNames.CollectionChanged += (_, _) =>
            {
                OnPropertyChanged(nameof(ShopNames));
                OnPropertyChanged(nameof(HasSpecificShops));
            };

            BlockedClothingSlots.CollectionChanged += (_, _) =>
            {
                OnPropertyChanged(nameof(BlockedClothingSlots));
                OnPropertyChanged(nameof(HasBlockedClothingSlots));
            };

            ChemistryRecipes.CollectionChanged += ChemistryRecipesOnCollectionChanged;
        }

        [JsonProperty("drugEffects")]
        public string DrugEffects
        {
            get => _drugEffects;
            set => SetProperty(ref _drugEffects, value ?? string.Empty);
        }

        [JsonProperty("useCustomWeedAppearance")]
        public bool UseCustomWeedAppearance
        {
            get => _useCustomWeedAppearance;
            set => SetProperty(ref _useCustomWeedAppearance, value);
        }

        [JsonProperty("weedMainColor")]
        public string WeedMainColor
        {
            get => _weedMainColor;
            set => SetProperty(ref _weedMainColor, value ?? string.Empty);
        }

        [JsonProperty("weedSecondaryColor")]
        public string WeedSecondaryColor
        {
            get => _weedSecondaryColor;
            set => SetProperty(ref _weedSecondaryColor, value ?? string.Empty);
        }

        [JsonProperty("weedLeafColor")]
        public string WeedLeafColor
        {
            get => _weedLeafColor;
            set => SetProperty(ref _weedLeafColor, value ?? string.Empty);
        }

        [JsonProperty("weedStemColor")]
        public string WeedStemColor
        {
            get => _weedStemColor;
            set => SetProperty(ref _weedStemColor, value ?? string.Empty);
        }

        [JsonProperty("productKindId")]
        public string ProductKindId { get => _productKindId; set => SetProperty(ref _productKindId, value ?? string.Empty); }

        [JsonProperty("productKindName")]
        public string ProductKindName { get => _productKindName; set => SetProperty(ref _productKindName, value ?? string.Empty); }

        [JsonProperty("compatibilityDrugType")]
        public DrugCompatibilityOption CompatibilityDrugType { get => _compatibilityDrugType; set => SetProperty(ref _compatibilityDrugType, value); }

        [JsonProperty("enableCustomProductMixing")]
        public bool EnableCustomProductMixing { get => _enableCustomProductMixing; set => SetProperty(ref _enableCustomProductMixing, value); }

        [JsonProperty("customProductMixingMap")]
        public ProductMixingMapOption CustomProductMixingMap { get => _customProductMixingMap; set => SetProperty(ref _customProductMixingMap, value); }

        [JsonProperty("usePropertyColorMixing")]
        public bool UsePropertyColorMixing { get => _usePropertyColorMixing; set => SetProperty(ref _usePropertyColorMixing, value); }

        [JsonProperty("representationTemplateItemId")]
        public string RepresentationTemplateItemId { get => _representationTemplateItemId; set => SetProperty(ref _representationTemplateItemId, value ?? string.Empty); }

        [JsonProperty("baseAddictiveness")]
        public float BaseAddictiveness { get => _baseAddictiveness; set => SetProperty(ref _baseAddictiveness, value); }

        [JsonProperty("defaultProductQuality")]
        public ProductQualityOption DefaultProductQuality { get => _defaultProductQuality; set => SetProperty(ref _defaultProductQuality, value); }

        [JsonProperty("playerEffectDurationSeconds")]
        public int PlayerEffectDurationSeconds { get => _playerEffectDurationSeconds; set => SetProperty(ref _playerEffectDurationSeconds, value); }

        [JsonProperty("npcEffectDurationSeconds")]
        public int NpcEffectDurationSeconds { get => _npcEffectDurationSeconds; set => SetProperty(ref _npcEffectDurationSeconds, value); }

        [JsonProperty("productPackagingIds")]
        public string ProductPackagingIds { get => _productPackagingIds; set => SetProperty(ref _productPackagingIds, value ?? string.Empty); }

        [JsonProperty("discoverCustomProduct")]
        public bool DiscoverCustomProduct { get => _discoverCustomProduct; set => SetProperty(ref _discoverCustomProduct, value); }

        [JsonProperty("listCustomProduct")]
        public bool ListCustomProduct { get => _listCustomProduct; set => SetProperty(ref _listCustomProduct, value); }

        [JsonProperty("showCustomProductKindInManager")]
        public bool ShowCustomProductKindInManager { get => _showCustomProductKindInManager; set => SetProperty(ref _showCustomProductKindInManager, value); }

        [JsonProperty("productKindColor")]
        public string ProductKindColor { get => _productKindColor; set => SetProperty(ref _productKindColor, value ?? string.Empty); }

        [JsonProperty("customDrugShopNames")]
        public string CustomDrugShopNames { get => _customDrugShopNames; set => SetProperty(ref _customDrugShopNames, value ?? string.Empty); }

        [JsonProperty("productConsoleAlias")]
        public string ProductConsoleAlias { get => _productConsoleAlias; set => SetProperty(ref _productConsoleAlias, value ?? string.Empty); }

        [JsonProperty("className")]
        public string ClassName
        {
            get => _className;
            set => SetProperty(ref _className, value);
        }

        [JsonProperty("namespace")]
        public string Namespace
        {
            get => _namespace;
            set => SetProperty(ref _namespace, value);
        }

        [JsonProperty("itemId")]
        public string ItemId
        {
            get => _itemId;
            set
            {
                if (SetProperty(ref _itemId, value))
                {
                    OnPropertyChanged(nameof(WorkspaceSubtitle));
                    OnPropertyChanged(nameof(SuggestedClothingAssetPath));
                }
            }
        }

        [JsonProperty("itemName")]
        public string ItemName
        {
            get => _itemName;
            set
            {
                if (SetProperty(ref _itemName, value))
                {
                    OnPropertyChanged(nameof(DisplayName));
                }
            }
        }

        [JsonProperty("itemDescription")]
        public string ItemDescription
        {
            get => _itemDescription;
            set => SetProperty(ref _itemDescription, value);
        }

        [JsonProperty("itemType")]
        public ItemKindOption ItemType
        {
            get => _itemType;
            set
            {
                if (SetProperty(ref _itemType, value))
                {
                    if (value == ItemKindOption.Clothing)
                    {
                        Category = ItemCategoryOption.Clothing;
                    }
                    else if (value == ItemKindOption.WeedDrug || value == ItemKindOption.CustomDrug)
                    {
                        var modId = new string((ModName ?? "mymod").ToLowerInvariant()
                            .Select(ch => ch < 128 && char.IsLetterOrDigit(ch) ? ch : '_').ToArray()).Trim('_');
                        var prefix = string.IsNullOrWhiteSpace(modId) ? "mymod" : modId;
                        if (!ItemId.Contains(':'))
                            ItemId = $"{prefix}:{ItemId}";
                        if (value == ItemKindOption.WeedDrug && string.IsNullOrWhiteSpace(DrugEffects))
                            DrugEffects = "Euphoric";
                        if (value == ItemKindOption.WeedDrug)
                        {
                            ModelBundleResourcePath = string.Empty;
                            ModelPrefabName = string.Empty;
                        }
                        if (value == ItemKindOption.CustomDrug && string.IsNullOrWhiteSpace(ProductKindId))
                            ProductKindId = $"{prefix}:products";
                        ShopIntegrationMode = ShopIntegrationModeOption.None;
                    }

                    OnPropertyChanged(nameof(EffectiveCategory));
                    OnPropertyChanged(nameof(WorkspaceSubtitle));
                    RaiseItemTypeCapabilityProperties();
                }
            }
        }

        [JsonProperty("cloneSourceItemId")]
        public string CloneSourceItemId
        {
            get => _cloneSourceItemId;
            set => SetProperty(ref _cloneSourceItemId, value ?? string.Empty);
        }

        [JsonProperty("category")]
        public ItemCategoryOption Category
        {
            get => _category;
            set
            {
                var normalizedValue = ItemType == ItemKindOption.Clothing
                    ? ItemCategoryOption.Clothing
                    : value;

                if (SetProperty(ref _category, normalizedValue))
                {
                    OnPropertyChanged(nameof(EffectiveCategory));
                }
            }
        }

        [JsonProperty("stackLimit")]
        public int StackLimit
        {
            get => _stackLimit;
            set => SetProperty(ref _stackLimit, value < 1 ? 1 : value);
        }

        [JsonProperty("basePurchasePrice")]
        public float BasePurchasePrice
        {
            get => _basePurchasePrice;
            set
            {
                if (SetProperty(ref _basePurchasePrice, value < 0f ? 0f : value))
                {
                    OnPropertyChanged(nameof(EffectiveShopPrice));
                    OnPropertyChanged(nameof(EffectiveShopPriceDisplay));
                }
            }
        }

        [JsonProperty("resellMultiplier")]
        public float ResellMultiplier
        {
            get => _resellMultiplier;
            set => SetProperty(ref _resellMultiplier, Math.Clamp(value, 0f, 1f));
        }

        [JsonProperty("legalStatus")]
        public ItemLegalStatusOption LegalStatus
        {
            get => _legalStatus;
            set => SetProperty(ref _legalStatus, value);
        }

        [JsonProperty("availableInDemo")]
        public bool AvailableInDemo
        {
            get => _availableInDemo;
            set => SetProperty(ref _availableInDemo, value);
        }

        [JsonProperty("shopIntegrationMode")]
        public ShopIntegrationModeOption ShopIntegrationMode
        {
            get => _shopIntegrationMode;
            set
            {
                if (SetProperty(ref _shopIntegrationMode, value))
                {
                    RaiseShopModeProperties();
                }
            }
        }

        [JsonProperty("useCustomShopPrice")]
        public bool UseCustomShopPrice
        {
            get => _useCustomShopPrice;
            set
            {
                if (SetProperty(ref _useCustomShopPrice, value))
                {
                    OnPropertyChanged(nameof(EffectiveShopPrice));
                    OnPropertyChanged(nameof(EffectiveShopPriceDisplay));
                }
            }
        }

        [JsonProperty("customShopPrice")]
        public float CustomShopPrice
        {
            get => _customShopPrice;
            set
            {
                if (SetProperty(ref _customShopPrice, value < 0f ? 0f : value))
                {
                    OnPropertyChanged(nameof(EffectiveShopPrice));
                    OnPropertyChanged(nameof(EffectiveShopPriceDisplay));
                }
            }
        }

        [JsonProperty("shopNames")]
        public ObservableCollection<string> ShopNames { get; } = new ObservableCollection<string>();

        [JsonProperty("iconFileName")]
        public string IconFileName
        {
            get => _iconFileName;
            set => SetProperty(ref _iconFileName, value ?? string.Empty);
        }

        [JsonProperty("modelBundleResourcePath")]
        public string ModelBundleResourcePath
        {
            get => _modelBundleResourcePath;
            set => SetProperty(ref _modelBundleResourcePath, value ?? string.Empty);
        }

        [JsonProperty("modelPrefabName")]
        public string ModelPrefabName
        {
            get => _modelPrefabName;
            set => SetProperty(ref _modelPrefabName, value ?? string.Empty);
        }

        [JsonProperty("equippableType")]
        public EquippableTypeOption EquippableType
        {
            get => _equippableType;
            set
            {
                if (SetProperty(ref _equippableType, value))
                {
                    OnPropertyChanged(nameof(UsesEquippable));
                    OnPropertyChanged(nameof(UsesViewmodelEquippable));
                    OnPropertyChanged(nameof(SupportsUseCallbackHook));
                    OnPropertyChanged(nameof(UsesAvatarBundleRegistration));
                }
            }
        }

        [JsonProperty("equippableName")]
        public string EquippableName
        {
            get => _equippableName;
            set => SetProperty(ref _equippableName, value ?? string.Empty);
        }

        [JsonProperty("equippableCanInteract")]
        public bool EquippableCanInteract
        {
            get => _equippableCanInteract;
            set => SetProperty(ref _equippableCanInteract, value);
        }

        [JsonProperty("equippableCanPickup")]
        public bool EquippableCanPickup
        {
            get => _equippableCanPickup;
            set => SetProperty(ref _equippableCanPickup, value);
        }

        [JsonProperty("hasViewmodelTransform")]
        public bool HasViewmodelTransform
        {
            get => _hasViewmodelTransform;
            set => SetProperty(ref _hasViewmodelTransform, value);
        }

        [JsonProperty("viewmodelPositionX")]
        public float ViewmodelPositionX
        {
            get => _viewmodelPositionX;
            set => SetProperty(ref _viewmodelPositionX, value);
        }

        [JsonProperty("viewmodelPositionY")]
        public float ViewmodelPositionY
        {
            get => _viewmodelPositionY;
            set => SetProperty(ref _viewmodelPositionY, value);
        }

        [JsonProperty("viewmodelPositionZ")]
        public float ViewmodelPositionZ
        {
            get => _viewmodelPositionZ;
            set => SetProperty(ref _viewmodelPositionZ, value);
        }

        [JsonProperty("viewmodelRotationX")]
        public float ViewmodelRotationX
        {
            get => _viewmodelRotationX;
            set => SetProperty(ref _viewmodelRotationX, value);
        }

        [JsonProperty("viewmodelRotationY")]
        public float ViewmodelRotationY
        {
            get => _viewmodelRotationY;
            set => SetProperty(ref _viewmodelRotationY, value);
        }

        [JsonProperty("viewmodelRotationZ")]
        public float ViewmodelRotationZ
        {
            get => _viewmodelRotationZ;
            set => SetProperty(ref _viewmodelRotationZ, value);
        }

        [JsonProperty("viewmodelScaleX")]
        public float ViewmodelScaleX
        {
            get => _viewmodelScaleX;
            set => SetProperty(ref _viewmodelScaleX, value == 0f ? 1f : value);
        }

        [JsonProperty("viewmodelScaleY")]
        public float ViewmodelScaleY
        {
            get => _viewmodelScaleY;
            set => SetProperty(ref _viewmodelScaleY, value == 0f ? 1f : value);
        }

        [JsonProperty("viewmodelScaleZ")]
        public float ViewmodelScaleZ
        {
            get => _viewmodelScaleZ;
            set => SetProperty(ref _viewmodelScaleZ, value == 0f ? 1f : value);
        }

        [JsonProperty("hasAvatarEquippable")]
        public bool HasAvatarEquippable
        {
            get => _hasAvatarEquippable;
            set
            {
                if (SetProperty(ref _hasAvatarEquippable, value))
                {
                    OnPropertyChanged(nameof(UsesAvatarBundleRegistration));
                }
            }
        }

        [JsonProperty("avatarEquippableAssetPath")]
        public string AvatarEquippableAssetPath
        {
            get => _avatarEquippableAssetPath;
            set => SetProperty(ref _avatarEquippableAssetPath, value ?? string.Empty);
        }

        [JsonProperty("avatarHand")]
        public AvatarHandOption AvatarHand
        {
            get => _avatarHand;
            set => SetProperty(ref _avatarHand, value);
        }

        [JsonProperty("avatarAnimationTrigger")]
        public string AvatarAnimationTrigger
        {
            get => _avatarAnimationTrigger;
            set => SetProperty(ref _avatarAnimationTrigger, string.IsNullOrWhiteSpace(value) ? "RightArm_Hold_ClosedHand" : value);
        }

        [JsonProperty("storedItemResourcePath")]
        public string StoredItemResourcePath
        {
            get => _storedItemResourcePath;
            set => SetProperty(ref _storedItemResourcePath, value ?? string.Empty);
        }

        [JsonProperty("stationItemResourcePath")]
        public string StationItemResourcePath
        {
            get => _stationItemResourcePath;
            set => SetProperty(ref _stationItemResourcePath, value ?? string.Empty);
        }

        [JsonProperty("clearStationItem")]
        public bool ClearStationItem
        {
            get => _clearStationItem;
            set => SetProperty(ref _clearStationItem, value);
        }

        [JsonProperty("buildSoundType")]
        public BuildSoundTypeOption BuildSoundType
        {
            get => _buildSoundType;
            set => SetProperty(ref _buildSoundType, value);
        }

        [JsonProperty("clothingSlot")]
        public ClothingSlotOption ClothingSlot
        {
            get => _clothingSlot;
            set => SetProperty(ref _clothingSlot, value);
        }

        [JsonProperty("clothingApplicationType")]
        public ClothingApplicationTypeOption ClothingApplicationType
        {
            get => _clothingApplicationType;
            set
            {
                if (SetProperty(ref _clothingApplicationType, value))
                {
                    OnPropertyChanged(nameof(SuggestedClothingAssetPath));
                    OnPropertyChanged(nameof(UsesAccessoryTextureRuntimeOverride));
                    OnPropertyChanged(nameof(UsesLayerTextureRuntimeOverride));
                }
            }
        }

        [JsonProperty("clothingAssetPath")]
        public string ClothingAssetPath
        {
            get => _clothingAssetPath;
            set
            {
                if (SetProperty(ref _clothingAssetPath, value ?? string.Empty))
                {
                    OnPropertyChanged(nameof(UsesAccessoryTextureRuntimeOverride));
                    OnPropertyChanged(nameof(UsesLayerTextureRuntimeOverride));
                }
            }
        }

        [JsonProperty("clothingTextureSourceAssetPath")]
        public string ClothingTextureSourceAssetPath
        {
            get => _clothingTextureSourceAssetPath;
            set
            {
                if (SetProperty(ref _clothingTextureSourceAssetPath, value ?? string.Empty))
                {
                    OnPropertyChanged(nameof(UsesAccessoryTextureRuntimeOverride));
                }
            }
        }

        [JsonProperty("clothingTextureResourcePath")]
        public string ClothingTextureResourcePath
        {
            get => _clothingTextureResourcePath;
            set
            {
                if (SetProperty(ref _clothingTextureResourcePath, value ?? string.Empty))
                {
                    OnPropertyChanged(nameof(HasClothingTextureResource));
                    OnPropertyChanged(nameof(UsesAccessoryTextureRuntimeOverride));
                    OnPropertyChanged(nameof(UsesLayerTextureRuntimeOverride));
                }
            }
        }

        [JsonProperty("accessoryTextureShaderPropertyName")]
        public string AccessoryTextureShaderPropertyName
        {
            get => _accessoryTextureShaderPropertyName;
            set => SetProperty(ref _accessoryTextureShaderPropertyName, string.IsNullOrWhiteSpace(value) ? "_MainTex" : value.Trim());
        }

        [JsonProperty("clothingColorable")]
        public bool ClothingColorable
        {
            get => _clothingColorable;
            set => SetProperty(ref _clothingColorable, value);
        }

        [JsonProperty("defaultClothingColor")]
        public ClothingColorOption DefaultClothingColor
        {
            get => _defaultClothingColor;
            set => SetProperty(ref _defaultClothingColor, value);
        }

        [JsonProperty("blockedClothingSlots")]
        public ObservableCollection<ClothingSlotOption> BlockedClothingSlots { get; } = new ObservableCollection<ClothingSlotOption>();

        [JsonProperty("displayMaterialResourcePath")]
        public string DisplayMaterialResourcePath
        {
            get => _displayMaterialResourcePath;
            set => SetProperty(ref _displayMaterialResourcePath, value ?? string.Empty);
        }

        [JsonProperty("qualityChange")]
        public float QualityChange
        {
            get => _qualityChange;
            set => SetProperty(ref _qualityChange, value);
        }

        [JsonProperty("yieldMultiplier")]
        public float YieldMultiplier
        {
            get => _yieldMultiplier;
            set => SetProperty(ref _yieldMultiplier, value < 0f ? 0f : value);
        }

        [JsonProperty("instantGrowth")]
        public float InstantGrowth
        {
            get => _instantGrowth;
            set => SetProperty(ref _instantGrowth, Math.Clamp(value, 0f, 1f));
        }

        [JsonProperty("allowOnGrowContainers")]
        public bool AllowOnGrowContainers
        {
            get => _allowOnGrowContainers;
            set => SetProperty(ref _allowOnGrowContainers, value);
        }

        [JsonProperty("enableUseCallbackHook")]
        public bool EnableUseCallbackHook
        {
            get => _enableUseCallbackHook;
            set
            {
                if (SetProperty(ref _enableUseCallbackHook, value) && value)
                {
                    GenerateHookScaffold = true;
                }
            }
        }

        [JsonProperty("generateHookScaffold")]
        public bool GenerateHookScaffold
        {
            get => _generateHookScaffold;
            set => SetProperty(ref _generateHookScaffold, value);
        }

        [JsonProperty("registerAvatarEquippableFromEmbeddedBundle")]
        public bool RegisterAvatarEquippableFromEmbeddedBundle
        {
            get => _registerAvatarEquippableFromEmbeddedBundle;
            set
            {
                if (SetProperty(ref _registerAvatarEquippableFromEmbeddedBundle, value))
                {
                    OnPropertyChanged(nameof(UsesAvatarBundleRegistration));
                }
            }
        }

        [JsonProperty("avatarBundleResourcePath")]
        public string AvatarBundleResourcePath
        {
            get => _avatarBundleResourcePath;
            set => SetProperty(ref _avatarBundleResourcePath, value ?? string.Empty);
        }

        [JsonProperty("avatarBundlePrefabName")]
        public string AvatarBundlePrefabName
        {
            get => _avatarBundlePrefabName;
            set => SetProperty(ref _avatarBundlePrefabName, value ?? string.Empty);
        }

        [JsonProperty("chemistryRecipes")]
        public ObservableCollection<ChemistryRecipeBlueprint> ChemistryRecipes { get; } = new ObservableCollection<ChemistryRecipeBlueprint>();

        [JsonProperty("folderId")]
        public string FolderId
        {
            get => _folderId;
            set => SetProperty(ref _folderId, string.IsNullOrWhiteSpace(value) ? QuestProject.RootFolderId : value);
        }

        [JsonProperty("modName")]
        public string ModName
        {
            get => _modName;
            set => SetProperty(ref _modName, value);
        }

        [JsonProperty("modAuthor")]
        public string ModAuthor
        {
            get => _modAuthor;
            set => SetProperty(ref _modAuthor, value);
        }

        [JsonProperty("modVersion")]
        public string ModVersion
        {
            get => _modVersion;
            set => SetProperty(ref _modVersion, value);
        }

        [JsonProperty("gameDeveloper")]
        public string GameDeveloper
        {
            get => _gameDeveloper;
            set => SetProperty(ref _gameDeveloper, value);
        }

        [JsonProperty("gameName")]
        public string GameName
        {
            get => _gameName;
            set => SetProperty(ref _gameName, value);
        }

        [JsonIgnore]
        public string DisplayName => string.IsNullOrWhiteSpace(ItemName) ? ClassName : ItemName;

        private bool IsGenericItem => ItemType == ItemKindOption.Generic;

        private bool IsBuildableItem => ItemType == ItemKindOption.Buildable;

        private bool IsClothingItem => ItemType == ItemKindOption.Clothing;

        private bool IsAdditiveItem => ItemType == ItemKindOption.Additive;

        [JsonIgnore]
        public bool IsWeedDrug => ItemType == ItemKindOption.WeedDrug;

        [JsonIgnore]
        public bool IsCustomDrug => ItemType == ItemKindOption.CustomDrug;

        [JsonIgnore]
        public bool IsStandardItem => !IsWeedDrug && !IsCustomDrug;

        [JsonIgnore]
        public string WorkspaceSubtitle => ItemType == ItemKindOption.Clothing
            ? $"Clothing · {ItemId}"
            : ItemId;

        [JsonIgnore]
        public ItemCategoryOption EffectiveCategory => IsClothingItem ? ItemCategoryOption.Clothing : Category;

        [JsonIgnore]
        public bool SupportsCloneSource => !IsGenericItem && !IsWeedDrug && !IsCustomDrug;

        [JsonIgnore]
        public bool SupportsCategory => !IsClothingItem && !IsWeedDrug && !IsCustomDrug;

        [JsonIgnore]
        public bool SupportsStackLimit => IsGenericItem || IsBuildableItem || IsAdditiveItem || IsClothingItem;

        [JsonIgnore]
        public bool SupportsLegalStatus => IsGenericItem || IsBuildableItem || IsAdditiveItem || IsClothingItem;

        [JsonIgnore]
        public bool SupportsDemoAvailability => IsGenericItem || IsAdditiveItem;

        [JsonIgnore]
        public bool SupportsEquippable => IsGenericItem || IsBuildableItem;

        [JsonIgnore]
        public bool UsesEquippable => SupportsEquippable && EquippableType != EquippableTypeOption.None;

        [JsonIgnore]
        public bool UsesViewmodelEquippable => EquippableType == EquippableTypeOption.Viewmodel;

        [JsonIgnore]
        public bool SupportsStoredItemPrefab => IsGenericItem;

        [JsonIgnore]
        public bool SupportsStationItemPrefab => IsGenericItem;

        [JsonIgnore]
        public bool SupportsBuildableOptions => IsBuildableItem;

        [JsonIgnore]
        public bool SupportsClothingOptions => IsClothingItem;

        [JsonIgnore]
        public bool SupportsAdditiveOptions => IsAdditiveItem;

        [JsonIgnore]
        public bool SupportsDisplayMaterial => IsAdditiveItem;

        [JsonIgnore]
        public bool SupportsGrowContainerIntegration => IsAdditiveItem;

        [JsonIgnore]
        public bool SupportsChemistryRecipes => !IsClothingItem && !IsWeedDrug;

        [JsonIgnore]
        public bool SupportsRuntimeEditor => !IsClothingItem;

        [JsonIgnore]
        public bool SupportsLiveCatalogEditor => !IsClothingItem;

        [JsonIgnore]
        public bool CanEditItemType => !IsClothingItem;

        [JsonIgnore]
        public string SuggestedClothingAssetPath => ClothingApplicationType switch
        {
            ClothingApplicationTypeOption.BodyLayer => $"avatar/bodylayers/{BuildClothingAssetKey(ItemId)}",
            ClothingApplicationTypeOption.FaceLayer => $"avatar/facelayers/{BuildClothingAssetKey(ItemId)}",
            _ => $"avatar/accessories/{BuildClothingAssetKey(ItemId)}"
        };

        [JsonIgnore]
        public bool SupportsUseCallbackHook => UsesViewmodelEquippable;

        [JsonIgnore]
        public bool HasClothingTextureResource => !string.IsNullOrWhiteSpace(ClothingTextureResourcePath);

        [JsonIgnore]
        public bool UsesAccessoryTextureRuntimeOverride => IsClothingItem
            && ClothingApplicationType == ClothingApplicationTypeOption.Accessory
            && !string.IsNullOrWhiteSpace(ClothingTextureResourcePath)
            && !string.IsNullOrWhiteSpace(ClothingTextureSourceAssetPath)
            && !string.IsNullOrWhiteSpace(ClothingAssetPath);

        [JsonIgnore]
        public bool UsesLayerTextureRuntimeOverride => IsClothingItem
            && ClothingApplicationType != ClothingApplicationTypeOption.Accessory
            && !string.IsNullOrWhiteSpace(ClothingTextureResourcePath)
            && !string.IsNullOrWhiteSpace(ClothingAssetPath);

        [JsonIgnore]
        public bool UsesAvatarBundleRegistration => UsesViewmodelEquippable
            && HasAvatarEquippable
            && RegisterAvatarEquippableFromEmbeddedBundle;

        [JsonIgnore]
        public float EffectiveShopPrice => UseCustomShopPrice ? CustomShopPrice : BasePurchasePrice;

        [JsonIgnore]
        public string EffectiveShopPriceDisplay => $"{EffectiveShopPrice:0.##}";

        [JsonIgnore]
        public bool UsesCompatibleShops => ShopIntegrationMode == ShopIntegrationModeOption.Compatible;

        [JsonIgnore]
        public bool UsesSpecificShops => ShopIntegrationMode == ShopIntegrationModeOption.Specific;

        [JsonIgnore]
        public bool HasSpecificShops => ShopNames.Count > 0;

        [JsonIgnore]
        public bool HasBlockedClothingSlots => BlockedClothingSlots.Count > 0;

        public void CopyFrom(ItemBlueprint source)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            ClassName = source.ClassName;
            Namespace = source.Namespace;
            ItemId = source.ItemId;
            ItemName = source.ItemName;
            ItemDescription = source.ItemDescription;
            ItemType = source.ItemType;
            CloneSourceItemId = source.CloneSourceItemId;
            Category = source.Category;
            StackLimit = source.StackLimit;
            BasePurchasePrice = source.BasePurchasePrice;
            ResellMultiplier = source.ResellMultiplier;
            LegalStatus = source.LegalStatus;
            AvailableInDemo = source.AvailableInDemo;
            ShopIntegrationMode = source.ShopIntegrationMode;
            UseCustomShopPrice = source.UseCustomShopPrice;
            CustomShopPrice = source.CustomShopPrice;

            ShopNames.Clear();
            foreach (var shopName in source.ShopNames)
            {
                ShopNames.Add(shopName);
            }

            IconFileName = source.IconFileName;
            ModelBundleResourcePath = source.ModelBundleResourcePath;
            ModelPrefabName = source.ModelPrefabName;
            EquippableType = source.EquippableType;
            EquippableName = source.EquippableName;
            EquippableCanInteract = source.EquippableCanInteract;
            EquippableCanPickup = source.EquippableCanPickup;
            HasViewmodelTransform = source.HasViewmodelTransform;
            ViewmodelPositionX = source.ViewmodelPositionX;
            ViewmodelPositionY = source.ViewmodelPositionY;
            ViewmodelPositionZ = source.ViewmodelPositionZ;
            ViewmodelRotationX = source.ViewmodelRotationX;
            ViewmodelRotationY = source.ViewmodelRotationY;
            ViewmodelRotationZ = source.ViewmodelRotationZ;
            ViewmodelScaleX = source.ViewmodelScaleX;
            ViewmodelScaleY = source.ViewmodelScaleY;
            ViewmodelScaleZ = source.ViewmodelScaleZ;
            HasAvatarEquippable = source.HasAvatarEquippable;
            AvatarEquippableAssetPath = source.AvatarEquippableAssetPath;
            AvatarHand = source.AvatarHand;
            AvatarAnimationTrigger = source.AvatarAnimationTrigger;
            StoredItemResourcePath = source.StoredItemResourcePath;
            StationItemResourcePath = source.StationItemResourcePath;
            ClearStationItem = source.ClearStationItem;
            BuildSoundType = source.BuildSoundType;
            ClothingSlot = source.ClothingSlot;
            ClothingApplicationType = source.ClothingApplicationType;
            ClothingAssetPath = source.ClothingAssetPath;
            ClothingTextureSourceAssetPath = source.ClothingTextureSourceAssetPath;
            ClothingTextureResourcePath = source.ClothingTextureResourcePath;
            AccessoryTextureShaderPropertyName = source.AccessoryTextureShaderPropertyName;
            ClothingColorable = source.ClothingColorable;
            DefaultClothingColor = source.DefaultClothingColor;

            BlockedClothingSlots.Clear();
            foreach (var blockedSlot in source.BlockedClothingSlots)
            {
                BlockedClothingSlots.Add(blockedSlot);
            }

            DisplayMaterialResourcePath = source.DisplayMaterialResourcePath;
            QualityChange = source.QualityChange;
            YieldMultiplier = source.YieldMultiplier;
            InstantGrowth = source.InstantGrowth;
            AllowOnGrowContainers = source.AllowOnGrowContainers;
            DrugEffects = source.DrugEffects;
            UseCustomWeedAppearance = source.UseCustomWeedAppearance;
            WeedMainColor = source.WeedMainColor;
            WeedSecondaryColor = source.WeedSecondaryColor;
            WeedLeafColor = source.WeedLeafColor;
            WeedStemColor = source.WeedStemColor;
            ProductKindId = source.ProductKindId;
            ProductKindName = source.ProductKindName;
            CompatibilityDrugType = source.CompatibilityDrugType;
            EnableCustomProductMixing = source.EnableCustomProductMixing;
            CustomProductMixingMap = source.CustomProductMixingMap;
            UsePropertyColorMixing = source.UsePropertyColorMixing;
            RepresentationTemplateItemId = source.RepresentationTemplateItemId;
            BaseAddictiveness = source.BaseAddictiveness;
            DefaultProductQuality = source.DefaultProductQuality;
            PlayerEffectDurationSeconds = source.PlayerEffectDurationSeconds;
            NpcEffectDurationSeconds = source.NpcEffectDurationSeconds;
            ProductPackagingIds = source.ProductPackagingIds;
            DiscoverCustomProduct = source.DiscoverCustomProduct;
            ListCustomProduct = source.ListCustomProduct;
            ShowCustomProductKindInManager = source.ShowCustomProductKindInManager;
            ProductKindColor = source.ProductKindColor;
            CustomDrugShopNames = source.CustomDrugShopNames;
            ProductConsoleAlias = source.ProductConsoleAlias;
            EnableUseCallbackHook = source.EnableUseCallbackHook;
            GenerateHookScaffold = source.GenerateHookScaffold;
            RegisterAvatarEquippableFromEmbeddedBundle = source.RegisterAvatarEquippableFromEmbeddedBundle;
            AvatarBundleResourcePath = source.AvatarBundleResourcePath;
            AvatarBundlePrefabName = source.AvatarBundlePrefabName;

            ChemistryRecipes.Clear();
            foreach (var recipe in source.ChemistryRecipes)
            {
                ChemistryRecipes.Add(recipe.DeepCopy());
            }

            FolderId = source.FolderId;
            ModName = source.ModName;
            ModAuthor = source.ModAuthor;
            ModVersion = source.ModVersion;
            GameDeveloper = source.GameDeveloper;
            GameName = source.GameName;
        }

        public ItemBlueprint DeepCopy()
        {
            var copy = new ItemBlueprint();
            copy.CopyFrom(this);
            return copy;
        }

        private void RaiseItemTypeCapabilityProperties()
        {
            OnPropertyChanged(nameof(SupportsCloneSource));
            OnPropertyChanged(nameof(IsWeedDrug));
            OnPropertyChanged(nameof(IsCustomDrug));
            OnPropertyChanged(nameof(IsStandardItem));
            OnPropertyChanged(nameof(SupportsCategory));
            OnPropertyChanged(nameof(SupportsStackLimit));
            OnPropertyChanged(nameof(SupportsLegalStatus));
            OnPropertyChanged(nameof(SupportsDemoAvailability));
            OnPropertyChanged(nameof(SupportsEquippable));
            OnPropertyChanged(nameof(UsesEquippable));
            OnPropertyChanged(nameof(UsesViewmodelEquippable));
            OnPropertyChanged(nameof(SupportsStoredItemPrefab));
            OnPropertyChanged(nameof(SupportsStationItemPrefab));
            OnPropertyChanged(nameof(SupportsBuildableOptions));
            OnPropertyChanged(nameof(SupportsClothingOptions));
            OnPropertyChanged(nameof(SupportsAdditiveOptions));
            OnPropertyChanged(nameof(SupportsDisplayMaterial));
            OnPropertyChanged(nameof(SupportsGrowContainerIntegration));
            OnPropertyChanged(nameof(SupportsChemistryRecipes));
            OnPropertyChanged(nameof(SupportsRuntimeEditor));
            OnPropertyChanged(nameof(SupportsLiveCatalogEditor));
            OnPropertyChanged(nameof(CanEditItemType));
            OnPropertyChanged(nameof(SuggestedClothingAssetPath));
            OnPropertyChanged(nameof(SupportsUseCallbackHook));
            OnPropertyChanged(nameof(UsesAvatarBundleRegistration));
            OnPropertyChanged(nameof(HasClothingTextureResource));
            OnPropertyChanged(nameof(UsesAccessoryTextureRuntimeOverride));
            OnPropertyChanged(nameof(UsesLayerTextureRuntimeOverride));
        }

        private void RaiseShopModeProperties()
        {
            OnPropertyChanged(nameof(UsesCompatibleShops));
            OnPropertyChanged(nameof(UsesSpecificShops));
        }

        private void ChemistryRecipesOnCollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems != null)
            {
                foreach (var recipe in e.NewItems.OfType<ChemistryRecipeBlueprint>())
                {
                    recipe.PropertyChanged += ChemistryRecipeOnPropertyChanged;
                }
            }

            if (e.OldItems != null)
            {
                foreach (var recipe in e.OldItems.OfType<ChemistryRecipeBlueprint>())
                {
                    recipe.PropertyChanged -= ChemistryRecipeOnPropertyChanged;
                }
            }

            OnPropertyChanged(nameof(ChemistryRecipes));
        }

        private void ChemistryRecipeOnPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            OnPropertyChanged(nameof(ChemistryRecipes));
        }

        private static string BuildClothingAssetKey(string source)
        {
            if (string.IsNullOrWhiteSpace(source))
                return "custom_clothing";

            var filteredCharacters = source
                .Trim()
                .ToLowerInvariant()
                .Select(character => char.IsLetterOrDigit(character) || character == '_' || character == '-'
                    ? character
                    : '_')
                .ToArray();

            var normalized = new string(filteredCharacters).Trim('_');
            return string.IsNullOrWhiteSpace(normalized) ? "custom_clothing" : normalized;
        }
    }

    /// <summary>
    /// Supported item generation modes exposed by the editor.
    /// </summary>
    public enum ItemKindOption
    {
        Generic,
        Buildable,
        Clothing,
        Additive,
        WeedDrug,
        CustomDrug
    }

    public enum DrugCompatibilityOption
    {
        Marijuana,
        Methamphetamine,
        Cocaine,
        Shrooms
    }

    public enum ProductMixingMapOption
    {
        Marijuana,
        Methamphetamine,
        Cocaine,
        Shrooms
    }

    public enum ProductQualityOption
    {
        Trash,
        Poor,
        Standard,
        Premium,
        Heavenly
    }

    /// <summary>
    /// Local mirror of S1API.Items.ItemCategory for editor serialization and UI binding.
    /// </summary>
    public enum ItemCategoryOption
    {
        Product,
        Packaging,
        Growing,
        Tools,
        Furniture,
        Lighting,
        Cash,
        Consumable,
        Equipment,
        Ingredient,
        Decoration,
        Clothing
    }

    /// <summary>
    /// Local mirror of S1API.Items.LegalStatus for editor serialization and UI binding.
    /// </summary>
    public enum ItemLegalStatusOption
    {
        Legal,
        Illegal
    }

    /// <summary>
    /// Controls how generated items are pushed into shop inventories.
    /// </summary>
    public enum ShopIntegrationModeOption
    {
        None,
        Compatible,
        Specific
    }

    /// <summary>
    /// Supported equippable modes for the visual editor.
    /// </summary>
    public enum EquippableTypeOption
    {
        None,
        Basic,
        Viewmodel
    }

    /// <summary>
    /// Local mirror of S1API.Items.AvatarHand.
    /// </summary>
    public enum AvatarHandOption
    {
        Left,
        Right
    }

    /// <summary>
    /// Local mirror of S1API.Items.BuildSoundType.
    /// </summary>
    public enum BuildSoundTypeOption
    {
        Wood,
        Metal,
        Plastic,
        Cardboard
    }

    /// <summary>
    /// Local mirror of S1API.Items.ClothingSlot.
    /// </summary>
    public enum ClothingSlotOption
    {
        Feet,
        Bottom,
        Waist,
        Top,
        Outerwear,
        Hands,
        Neck,
        Eyes,
        Head,
        Wrist
    }

    /// <summary>
    /// Local mirror of S1API.Items.ClothingApplicationType.
    /// </summary>
    public enum ClothingApplicationTypeOption
    {
        BodyLayer,
        FaceLayer,
        Accessory
    }

    /// <summary>
    /// Local mirror of S1API.Items.ClothingColor.
    /// </summary>
    public enum ClothingColorOption
    {
        White,
        LightGrey,
        DarkGrey,
        Charcoal,
        Black,
        LightRed,
        Red,
        Crimson,
        Orange,
        Tan,
        Brown,
        Coral,
        Beige,
        Yellow,
        Lime,
        LightGreen,
        DarkGreen,
        Cyan,
        SkyBlue,
        Blue,
        DeepBlue,
        Navy,
        DeepPurple,
        Purple,
        Magenta,
        BrightPink,
        HotPink
    }

    /// <summary>
    /// Shared enum value sources for item editor controls.
    /// </summary>
    public static class ItemBlueprintOptions
    {
        public static IReadOnlyList<string> DrugEffectNames { get; } = new[]
        {
            "AntiGravity", "Athletic", "Balding", "BrightEyed", "Calming", "CalorieDense",
            "Cyclopean", "Disorienting", "Electrifying", "Energizing", "Euphoric",
            "Explosive", "Focused", "Foggy", "Gingeritis", "Glowie", "Jennerising", "Laxative", "Lethal",
            "LongFaced", "Munchies", "Paranoia", "Refreshing", "Schizophrenic",
            "Sedating", "Seizure", "Shrinking", "Slippery", "Smelly", "Sneaky",
            "Spicy", "ThoughtProvoking", "Toxic", "TropicThunder", "Zombifying"
        };

        public static IReadOnlyList<DrugCompatibilityOption> DrugCompatibilityTypes { get; } =
            Enum.GetValues(typeof(DrugCompatibilityOption)).Cast<DrugCompatibilityOption>().ToArray();

        public static IReadOnlyList<ProductMixingMapOption> ProductMixingMaps { get; } =
            Enum.GetValues(typeof(ProductMixingMapOption)).Cast<ProductMixingMapOption>().ToArray();

        public static IReadOnlyList<ProductQualityOption> ProductQualities { get; } =
            Enum.GetValues(typeof(ProductQualityOption)).Cast<ProductQualityOption>().ToArray();

        public static IReadOnlyList<ItemKindOption> ItemTypes { get; } =
            Enum.GetValues(typeof(ItemKindOption)).Cast<ItemKindOption>().ToArray();

        public static IReadOnlyList<ItemCategoryOption> Categories { get; } =
            Enum.GetValues(typeof(ItemCategoryOption)).Cast<ItemCategoryOption>().ToArray();

        public static IReadOnlyList<ItemLegalStatusOption> LegalStatuses { get; } =
            Enum.GetValues(typeof(ItemLegalStatusOption)).Cast<ItemLegalStatusOption>().ToArray();

        public static IReadOnlyList<ShopIntegrationModeOption> ShopIntegrationModes { get; } =
            Enum.GetValues(typeof(ShopIntegrationModeOption)).Cast<ShopIntegrationModeOption>().ToArray();

        public static IReadOnlyList<EquippableTypeOption> EquippableTypes { get; } =
            Enum.GetValues(typeof(EquippableTypeOption)).Cast<EquippableTypeOption>().ToArray();

        public static IReadOnlyList<AvatarHandOption> AvatarHands { get; } =
            Enum.GetValues(typeof(AvatarHandOption)).Cast<AvatarHandOption>().ToArray();

        public static IReadOnlyList<BuildSoundTypeOption> BuildSounds { get; } =
            Enum.GetValues(typeof(BuildSoundTypeOption)).Cast<BuildSoundTypeOption>().ToArray();

        public static IReadOnlyList<ClothingSlotOption> ClothingSlots { get; } =
            Enum.GetValues(typeof(ClothingSlotOption)).Cast<ClothingSlotOption>().ToArray();

        public static IReadOnlyList<ClothingApplicationTypeOption> ClothingApplicationTypes { get; } =
            Enum.GetValues(typeof(ClothingApplicationTypeOption)).Cast<ClothingApplicationTypeOption>().ToArray();

        public static IReadOnlyList<ClothingColorOption> ClothingColors { get; } =
            Enum.GetValues(typeof(ClothingColorOption)).Cast<ClothingColorOption>().ToArray();
    }
}
