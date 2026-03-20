using Schedule1ModdingTool.Models;

namespace Schedule1ModdingTool.ViewModels
{
    /// <summary>
    /// Represents an open tab/editor for a mod element
    /// </summary>
    public class OpenElementTab : ObservableObject
    {
        private bool _isSelected;

        public QuestBlueprint? Quest { get; set; }
        public NpcBlueprint? Npc { get; set; }
        public ItemBlueprint? Item { get; set; }
        public PhoneAppBlueprint? PhoneApp { get; set; }
        public bool IsWorkspace { get; set; }

        public string Title => IsWorkspace 
            ? "Workspace" 
            : Quest?.DisplayName ?? Npc?.DisplayName ?? Item?.DisplayName ?? PhoneApp?.DisplayName ?? "Untitled";
        
        public string TabId => IsWorkspace 
            ? "Workspace" 
            : Quest != null
                ? $"Quest_{Quest.QuestId ?? "Unknown"}"
                : Npc != null
                    ? $"Npc_{Npc.NpcId ?? "Unknown"}"
                    : Item != null
                        ? $"Item_{Item.ItemId ?? "Unknown"}"
                        : $"PhoneApp_{PhoneApp?.AppName ?? "Unknown"}";

        public bool IsSelected
        {
            get => _isSelected;
            set => SetProperty(ref _isSelected, value);
        }
    }
}

