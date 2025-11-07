using System;

namespace Schedule1ModdingTool.Models
{
    /// <summary>
    /// Represents a navigation item in the left navigation panel
    /// </summary>
    public class NavigationItem : ObservableObject
    {
        private bool _isSelected;
        private bool _isEnabled;

        public string Id { get; set; } = "";
        public string DisplayName { get; set; } = "";
        public string IconKey { get; set; } = "";
        public bool IsEnabled
        {
            get => _isEnabled;
            set => SetProperty(ref _isEnabled, value);
        }
        public bool IsSelected
        {
            get => _isSelected;
            set => SetProperty(ref _isSelected, value);
        }
        public string Tooltip { get; set; } = "";
    }
}

