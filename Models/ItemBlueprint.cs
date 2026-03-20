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
        private bool _clothingColorable = true;
        private ClothingColorOption _defaultClothingColor = ClothingColorOption.White;
        private string _displayMaterialResourcePath = string.Empty;
        private float _qualityChange;
        private float _yieldMultiplier = 1f;
        private float _instantGrowth;
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
        }

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
            set => SetProperty(ref _itemId, value);
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

                    OnPropertyChanged(nameof(EffectiveCategory));
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
                if (SetProperty(ref _category, value))
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
            set => SetProperty(ref _basePurchasePrice, value < 0f ? 0f : value);
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
            set => SetProperty(ref _useCustomShopPrice, value);
        }

        [JsonProperty("customShopPrice")]
        public float CustomShopPrice
        {
            get => _customShopPrice;
            set => SetProperty(ref _customShopPrice, value < 0f ? 0f : value);
        }

        [JsonProperty("shopNames")]
        public ObservableCollection<string> ShopNames { get; } = new ObservableCollection<string>();

        [JsonProperty("iconFileName")]
        public string IconFileName
        {
            get => _iconFileName;
            set => SetProperty(ref _iconFileName, value ?? string.Empty);
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
            set => SetProperty(ref _hasAvatarEquippable, value);
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
            set => SetProperty(ref _clothingApplicationType, value);
        }

        [JsonProperty("clothingAssetPath")]
        public string ClothingAssetPath
        {
            get => _clothingAssetPath;
            set => SetProperty(ref _clothingAssetPath, value ?? string.Empty);
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

        [JsonIgnore]
        public ItemCategoryOption EffectiveCategory => ItemType == ItemKindOption.Clothing ? ItemCategoryOption.Clothing : Category;

        [JsonIgnore]
        public bool SupportsCloneSource => ItemType != ItemKindOption.Generic;

        [JsonIgnore]
        public bool SupportsCategory => ItemType != ItemKindOption.Clothing;

        [JsonIgnore]
        public bool SupportsStackLimit => ItemType == ItemKindOption.Generic || ItemType == ItemKindOption.Buildable || ItemType == ItemKindOption.Additive;

        [JsonIgnore]
        public bool SupportsLegalStatus => ItemType == ItemKindOption.Generic || ItemType == ItemKindOption.Buildable || ItemType == ItemKindOption.Additive;

        [JsonIgnore]
        public bool SupportsDemoAvailability => ItemType == ItemKindOption.Generic || ItemType == ItemKindOption.Additive;

        [JsonIgnore]
        public bool SupportsEquippable => ItemType == ItemKindOption.Generic || ItemType == ItemKindOption.Buildable;

        [JsonIgnore]
        public bool UsesEquippable => SupportsEquippable && EquippableType != EquippableTypeOption.None;

        [JsonIgnore]
        public bool UsesViewmodelEquippable => EquippableType == EquippableTypeOption.Viewmodel;

        [JsonIgnore]
        public bool SupportsStoredItemPrefab => ItemType == ItemKindOption.Generic;

        [JsonIgnore]
        public bool SupportsStationItemPrefab => ItemType == ItemKindOption.Generic;

        [JsonIgnore]
        public bool SupportsBuildableOptions => ItemType == ItemKindOption.Buildable;

        [JsonIgnore]
        public bool SupportsClothingOptions => ItemType == ItemKindOption.Clothing;

        [JsonIgnore]
        public bool SupportsAdditiveOptions => ItemType == ItemKindOption.Additive;

        [JsonIgnore]
        public bool SupportsDisplayMaterial => ItemType == ItemKindOption.Additive;

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
        }

        private void RaiseShopModeProperties()
        {
            OnPropertyChanged(nameof(UsesCompatibleShops));
            OnPropertyChanged(nameof(UsesSpecificShops));
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
        Additive
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
