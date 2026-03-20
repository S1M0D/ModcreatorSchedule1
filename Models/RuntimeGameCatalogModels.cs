using Newtonsoft.Json;

namespace Schedule1ModdingTool.Models
{
    /// <summary>
    /// Runtime response from the connector mod containing live item and shop data.
    /// </summary>
    public class RuntimeGameCatalogResponse
    {
        [JsonProperty("success")]
        public bool Success { get; set; }

        [JsonProperty("items")]
        public List<GameItemCatalogEntry> Items { get; set; } = new List<GameItemCatalogEntry>();

        [JsonProperty("shops")]
        public List<GameShopCatalogEntry> Shops { get; set; } = new List<GameShopCatalogEntry>();

        [JsonProperty("error")]
        public string? Error { get; set; }
    }

    /// <summary>
    /// Represents one live item entry queried from the running game.
    /// </summary>
    public class GameItemCatalogEntry
    {
        [JsonProperty("itemId")]
        public string ItemId { get; set; } = string.Empty;

        [JsonProperty("name")]
        public string Name { get; set; } = string.Empty;

        [JsonProperty("category")]
        public string Category { get; set; } = string.Empty;

        [JsonProperty("itemType")]
        public string ItemType { get; set; } = string.Empty;

        [JsonProperty("basePurchasePrice")]
        public float BasePurchasePrice { get; set; }

        [JsonProperty("resellMultiplier")]
        public float ResellMultiplier { get; set; }

        [JsonProperty("legalStatus")]
        public string LegalStatus { get; set; } = string.Empty;

        [JsonProperty("availableInDemo")]
        public bool AvailableInDemo { get; set; }

        [JsonIgnore]
        public string DisplayLabel => string.IsNullOrWhiteSpace(Name) ? ItemId : $"{Name} ({ItemId})";

        [JsonIgnore]
        public string Summary => $"{ItemType} - {Category} - ${BasePurchasePrice:0.##}";
    }

    /// <summary>
    /// Represents one live shop entry queried from the running game.
    /// </summary>
    public class GameShopCatalogEntry
    {
        [JsonProperty("name")]
        public string Name { get; set; } = string.Empty;

        [JsonProperty("listingCount")]
        public int ListingCount { get; set; }

        [JsonProperty("categories")]
        public List<string> Categories { get; set; } = new List<string>();

        [JsonProperty("itemIds")]
        public List<string> ItemIds { get; set; } = new List<string>();

        [JsonIgnore]
        public string CategorySummary => Categories.Count == 0 ? "No categories" : string.Join(", ", Categories);

        [JsonIgnore]
        public string DisplayLabel => $"{Name} ({ListingCount} items)";
    }

    /// <summary>
    /// Shared editor-facing item reference used by rewards, NPC inventory, clone-from, and recipe inputs.
    /// </summary>
    public class ItemReferenceInfo
    {
        public string Id { get; set; } = string.Empty;

        public string DisplayName { get; set; } = string.Empty;

        public string Category { get; set; } = string.Empty;

        public string ItemType { get; set; } = string.Empty;

        public string Source { get; set; } = string.Empty;

        public bool IsProjectItem { get; set; }

        public string DisplayLabel => string.IsNullOrWhiteSpace(DisplayName) ? Id : $"{DisplayName} ({Id})";

        public string Summary => string.IsNullOrWhiteSpace(Source)
            ? $"{ItemType} - {Category}"
            : $"{Source} - {ItemType} - {Category}";
    }

    /// <summary>
    /// Editor-side projection of a live shop for the currently selected item.
    /// </summary>
    public class ShopCompatibilityPreview
    {
        public string Name { get; set; } = string.Empty;

        public int ListingCount { get; set; }

        public string CategorySummary { get; set; } = string.Empty;

        public bool SellsSelectedCategory { get; set; }

        public bool AlreadyStocksItem { get; set; }

        public bool IsSelectedForSpecificRouting { get; set; }

        public float FinalPrice { get; set; }

        public string RoutingLabel
        {
            get
            {
                if (IsSelectedForSpecificRouting)
                {
                    return "Selected";
                }

                if (AlreadyStocksItem)
                {
                    return "Already stocks";
                }

                return "Available";
            }
        }

        public string CompatibilityLabel => SellsSelectedCategory ? "Category match" : "Different category";

        public string Summary => $"{CompatibilityLabel} - {CategorySummary} - ${FinalPrice:0.##}";
    }
}
