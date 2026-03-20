using System.Collections.ObjectModel;
using System.Collections.Specialized;
using Newtonsoft.Json;

namespace Schedule1ModdingTool.Models
{
    public enum PhoneAppUiNodeType
    {
        Panel,
        Text,
        Button,
        Spacer
    }

    public enum PhoneAppUiLayoutDirection
    {
        Vertical,
        Horizontal
    }

    public enum PhoneAppUiTextAlignment
    {
        UpperLeft,
        UpperCenter,
        MiddleLeft,
        MiddleCenter,
        MiddleRight,
        LowerCenter
    }

    /// <summary>
    /// Represents a hierarchy-authored UI node inside a generated phone app.
    /// </summary>
    public class PhoneAppUiNodeBlueprint : ObservableObject
    {
        private string _id = Guid.NewGuid().ToString("N");
        private string _name = "Container";
        private PhoneAppUiNodeType _nodeType = PhoneAppUiNodeType.Panel;
        private PhoneAppUiLayoutDirection _layoutDirection = PhoneAppUiLayoutDirection.Vertical;
        private PhoneAppUiTextAlignment _textAlignment = PhoneAppUiTextAlignment.MiddleLeft;
        private string _text = "New text";
        private string _statusMessage = string.Empty;
        private bool _closeAppOnClick;
        private string _backgroundColorHex = "#331C2430";
        private string _textColorHex = "#FFFFFFFF";
        private double _fontSize = 16d;
        private double _preferredWidth;
        private double _preferredHeight;
        private bool _expandWidth = true;
        private bool _expandHeight;
        private double _spacing = 10d;
        private int _padding = 12;
        private bool _bold;

        public PhoneAppUiNodeBlueprint()
        {
            Children.CollectionChanged += ChildrenOnCollectionChanged;
        }

        [JsonProperty("id")]
        public string Id
        {
            get => _id;
            set => SetProperty(ref _id, string.IsNullOrWhiteSpace(value) ? Guid.NewGuid().ToString("N") : value);
        }

        [JsonProperty("name")]
        public string Name
        {
            get => _name;
            set
            {
                if (SetProperty(ref _name, value ?? string.Empty))
                {
                    OnPropertyChanged(nameof(DisplayName));
                }
            }
        }

        [JsonProperty("nodeType")]
        public PhoneAppUiNodeType NodeType
        {
            get => _nodeType;
            set
            {
                if (SetProperty(ref _nodeType, value))
                {
                    OnPropertyChanged(nameof(DisplayName));
                    OnPropertyChanged(nameof(SupportsChildren));
                    OnPropertyChanged(nameof(SupportsLayout));
                    OnPropertyChanged(nameof(SupportsText));
                    OnPropertyChanged(nameof(SupportsTextColor));
                    OnPropertyChanged(nameof(SupportsBackgroundColor));
                    OnPropertyChanged(nameof(SupportsFontSize));
                    OnPropertyChanged(nameof(SupportsTextAlignment));
                    OnPropertyChanged(nameof(SupportsPadding));
                    OnPropertyChanged(nameof(SupportsSpacing));
                    OnPropertyChanged(nameof(SupportsButtonAction));
                    OnPropertyChanged(nameof(SupportsBold));
                }
            }
        }

        [JsonProperty("layoutDirection")]
        public PhoneAppUiLayoutDirection LayoutDirection
        {
            get => _layoutDirection;
            set => SetProperty(ref _layoutDirection, value);
        }

        [JsonProperty("textAlignment")]
        public PhoneAppUiTextAlignment TextAlignment
        {
            get => _textAlignment;
            set => SetProperty(ref _textAlignment, value);
        }

        [JsonProperty("text")]
        public string Text
        {
            get => _text;
            set
            {
                if (SetProperty(ref _text, value ?? string.Empty))
                {
                    OnPropertyChanged(nameof(DisplayName));
                }
            }
        }

        [JsonProperty("statusMessage")]
        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value ?? string.Empty);
        }

        [JsonProperty("closeAppOnClick")]
        public bool CloseAppOnClick
        {
            get => _closeAppOnClick;
            set => SetProperty(ref _closeAppOnClick, value);
        }

        [JsonProperty("backgroundColorHex")]
        public string BackgroundColorHex
        {
            get => _backgroundColorHex;
            set => SetProperty(ref _backgroundColorHex, string.IsNullOrWhiteSpace(value) ? "#00000000" : value);
        }

        [JsonProperty("textColorHex")]
        public string TextColorHex
        {
            get => _textColorHex;
            set => SetProperty(ref _textColorHex, string.IsNullOrWhiteSpace(value) ? "#FFFFFFFF" : value);
        }

        [JsonProperty("fontSize")]
        public double FontSize
        {
            get => _fontSize;
            set => SetProperty(ref _fontSize, Math.Max(8d, value));
        }

        [JsonProperty("preferredWidth")]
        public double PreferredWidth
        {
            get => _preferredWidth;
            set => SetProperty(ref _preferredWidth, Math.Max(0d, value));
        }

        [JsonProperty("preferredHeight")]
        public double PreferredHeight
        {
            get => _preferredHeight;
            set => SetProperty(ref _preferredHeight, Math.Max(0d, value));
        }

        [JsonProperty("expandWidth")]
        public bool ExpandWidth
        {
            get => _expandWidth;
            set => SetProperty(ref _expandWidth, value);
        }

        [JsonProperty("expandHeight")]
        public bool ExpandHeight
        {
            get => _expandHeight;
            set => SetProperty(ref _expandHeight, value);
        }

        [JsonProperty("spacing")]
        public double Spacing
        {
            get => _spacing;
            set => SetProperty(ref _spacing, Math.Max(0d, value));
        }

        [JsonProperty("padding")]
        public int Padding
        {
            get => _padding;
            set => SetProperty(ref _padding, Math.Max(0, value));
        }

        [JsonProperty("bold")]
        public bool Bold
        {
            get => _bold;
            set => SetProperty(ref _bold, value);
        }

        [JsonProperty("children")]
        public ObservableCollection<PhoneAppUiNodeBlueprint> Children { get; } = new();

        [JsonIgnore]
        public bool SupportsChildren => NodeType == PhoneAppUiNodeType.Panel;

        [JsonIgnore]
        public bool SupportsLayout => NodeType == PhoneAppUiNodeType.Panel;

        [JsonIgnore]
        public bool SupportsText => NodeType == PhoneAppUiNodeType.Text || NodeType == PhoneAppUiNodeType.Button;

        [JsonIgnore]
        public bool SupportsTextColor => NodeType == PhoneAppUiNodeType.Text || NodeType == PhoneAppUiNodeType.Button;

        [JsonIgnore]
        public bool SupportsBackgroundColor => NodeType == PhoneAppUiNodeType.Panel || NodeType == PhoneAppUiNodeType.Button;

        [JsonIgnore]
        public bool SupportsFontSize => NodeType == PhoneAppUiNodeType.Text || NodeType == PhoneAppUiNodeType.Button;

        [JsonIgnore]
        public bool SupportsTextAlignment => NodeType == PhoneAppUiNodeType.Text;

        [JsonIgnore]
        public bool SupportsPadding => NodeType == PhoneAppUiNodeType.Panel;

        [JsonIgnore]
        public bool SupportsSpacing => NodeType == PhoneAppUiNodeType.Panel;

        [JsonIgnore]
        public bool SupportsButtonAction => NodeType == PhoneAppUiNodeType.Button;

        [JsonIgnore]
        public bool SupportsBold => NodeType == PhoneAppUiNodeType.Text || NodeType == PhoneAppUiNodeType.Button;

        [JsonIgnore]
        public string DisplayName => NodeType switch
        {
            PhoneAppUiNodeType.Panel => $"Panel: {GetDisplayFallback(Name, "Container")} ({Children.Count})",
            PhoneAppUiNodeType.Text => $"Text: {GetDisplayFallback(Text, "Text")}",
            PhoneAppUiNodeType.Button => $"Button: {GetDisplayFallback(Text, "Button")}",
            PhoneAppUiNodeType.Spacer => $"Spacer: {PreferredWidth:0} x {PreferredHeight:0}",
            _ => GetDisplayFallback(Name, "Node")
        };

        public PhoneAppUiNodeBlueprint DeepCopy(bool preserveIdentity = false)
        {
            var copy = new PhoneAppUiNodeBlueprint
            {
                Id = preserveIdentity ? Id : Guid.NewGuid().ToString("N"),
                Name = Name,
                NodeType = NodeType,
                LayoutDirection = LayoutDirection,
                TextAlignment = TextAlignment,
                Text = Text,
                StatusMessage = StatusMessage,
                CloseAppOnClick = CloseAppOnClick,
                BackgroundColorHex = BackgroundColorHex,
                TextColorHex = TextColorHex,
                FontSize = FontSize,
                PreferredWidth = PreferredWidth,
                PreferredHeight = PreferredHeight,
                ExpandWidth = ExpandWidth,
                ExpandHeight = ExpandHeight,
                Spacing = Spacing,
                Padding = Padding,
                Bold = Bold
            };

            foreach (var child in Children)
            {
                copy.Children.Add(child.DeepCopy(preserveIdentity));
            }

            return copy;
        }

        private void ChildrenOnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.OldItems != null)
            {
                foreach (var removedNode in e.OldItems.OfType<PhoneAppUiNodeBlueprint>())
                {
                    removedNode.PropertyChanged -= ChildNodeOnPropertyChanged;
                }
            }

            if (e.NewItems != null)
            {
                foreach (var addedNode in e.NewItems.OfType<PhoneAppUiNodeBlueprint>())
                {
                    addedNode.PropertyChanged += ChildNodeOnPropertyChanged;
                }
            }

            OnPropertyChanged(nameof(Children));
            OnPropertyChanged(nameof(DisplayName));
        }

        private void ChildNodeOnPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            OnPropertyChanged(nameof(Children));
            OnPropertyChanged(nameof(DisplayName));
        }

        private static string GetDisplayFallback(string value, string fallback)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return fallback;
            }

            var trimmed = value.Trim();
            return trimmed.Length <= 32 ? trimmed : trimmed[..29] + "...";
        }
    }

    public static class PhoneAppUiNodeOptions
    {
        public static IReadOnlyList<PhoneAppUiNodeType> NodeTypes { get; } =
            Enum.GetValues(typeof(PhoneAppUiNodeType)).Cast<PhoneAppUiNodeType>().ToArray();

        public static IReadOnlyList<PhoneAppUiLayoutDirection> LayoutDirections { get; } =
            Enum.GetValues(typeof(PhoneAppUiLayoutDirection)).Cast<PhoneAppUiLayoutDirection>().ToArray();

        public static IReadOnlyList<PhoneAppUiTextAlignment> TextAlignments { get; } =
            Enum.GetValues(typeof(PhoneAppUiTextAlignment)).Cast<PhoneAppUiTextAlignment>().ToArray();
    }
}
