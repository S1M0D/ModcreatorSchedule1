namespace Schedule1ModdingTool.Models
{
    /// <summary>
    /// Represents a mod element category (Quests, NPCs, Phone Apps, etc.)
    /// </summary>
    public enum ModCategory
    {
        Quests,
        NPCs,
        PhoneApps,
        Items
    }
    
    /// <summary>
    /// Model for displaying a mod category tile
    /// </summary>
    public class ModCategoryInfo : ObservableObject
    {
        private bool _isEnabled;
        private int _count;

        public ModCategory Category { get; set; }
        public string DisplayName { get; set; } = "";
        public string IconKey { get; set; } = "";
        public string Description { get; set; } = "";
        public bool IsEnabled
        {
            get => _isEnabled;
            set => SetProperty(ref _isEnabled, value);
        }
        public int Count
        {
            get => _count;
            set => SetProperty(ref _count, value);
        }
        public string ComingSoonText { get; set; } = "Coming Soon";
    }
}

