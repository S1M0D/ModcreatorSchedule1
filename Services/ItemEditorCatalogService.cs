using System;
using System.Collections.Generic;
using System.Linq;

namespace Schedule1ModdingTool.Services
{
    /// <summary>
    /// Provides editor-side suggestion lists for item-related S1API inputs.
    /// </summary>
    public static class ItemEditorCatalogService
    {
        public static IReadOnlyList<string> SuggestedShopNames { get; } = BuildSuggestedShopNames();

        public static IReadOnlyList<string> AvatarEquippablePaths { get; } = new[]
        {
            "avatar/equippables/Baton",
            "avatar/equippables/Beer",
            "avatar/equippables/BrokenBottle",
            "avatar/equippables/Coffee",
            "avatar/equippables/Cuke",
            "avatar/equippables/Hammer",
            "avatar/equippables/Joint",
            "avatar/equippables/Knife",
            "avatar/equippables/M1911",
            "avatar/equippables/PhoneLowered",
            "avatar/equippables/PhoneRaised",
            "avatar/equippables/Pipe",
            "avatar/equippables/Revolver",
            "avatar/equippables/Taser",
            "avatar/equippables/TrashBag"
        };

        private static IReadOnlyList<string> BuildSuggestedShopNames()
        {
            var names = BuildingRegistryService.GetAllBuildings()
                .Select(building => building.DisplayName)
                .Where(IsLikelyShopName)
                .Concat(new[] { "General Store", "Hardware Store" })
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
                .ToArray();

            return names;
        }

        private static bool IsLikelyShopName(string displayName)
        {
            if (string.IsNullOrWhiteSpace(displayName))
                return false;

            return displayName.Contains("Store", StringComparison.OrdinalIgnoreCase)
                || displayName.Contains("Shop", StringComparison.OrdinalIgnoreCase)
                || displayName.Contains("Mart", StringComparison.OrdinalIgnoreCase)
                || displayName.Contains("Hardware", StringComparison.OrdinalIgnoreCase)
                || displayName.Equals("Pillville", StringComparison.OrdinalIgnoreCase)
                || displayName.Equals("Supermarket", StringComparison.OrdinalIgnoreCase);
        }
    }
}
