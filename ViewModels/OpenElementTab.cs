using Schedule1ModdingTool.Models;
using Schedule1ModdingTool.Utils;

namespace Schedule1ModdingTool.ViewModels
{
    /// <summary>
    /// Represents an open tab/editor for a mod element
    /// </summary>
    public class OpenElementTab : ObservableObject
    {
        private bool _isSelected;

        public QuestBlueprint Quest { get; set; }
        public string Title => Quest?.DisplayName ?? "Untitled";
        public string TabId => $"Quest_{Quest?.QuestId ?? "Unknown"}";

        public bool IsSelected
        {
            get => _isSelected;
            set => SetProperty(ref _isSelected, value);
        }
    }
}

