using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using Newtonsoft.Json;

namespace Schedule1ModdingTool.Models
{
    /// <summary>
    /// Represents a custom S1API phone app and the editor-visible settings used to generate it.
    /// </summary>
    public class PhoneAppBlueprint : ObservableObject
    {
        private string _className = "GeneratedPhoneApp";
        private string _namespace = "Schedule1Mods.PhoneApps";
        private string _appName = "generated_phone_app";
        private string _appTitle = "Generated Phone App";
        private string _iconLabel = "APP";
        private string _iconFileName = string.Empty;
        private PhoneAppOrientationOption _orientation = PhoneAppOrientationOption.Horizontal;
        private PhoneAppLayoutPresetOption _layoutPreset = PhoneAppLayoutPresetOption.InformationPanel;
        private string _headerText = "Welcome";
        private string _subheaderText = "Generated with S1 Mod Creator";
        private string _bodyText = "Use this screen for notes, instructions, or lightweight app interactions.";
        private string _footerText = "Advanced S1API UI can be added in the generated hook file.";
        private bool _useScrollableBody = true;
        private bool _showPrimaryButton = true;
        private string _primaryButtonLabel = "Confirm";
        private string _primaryButtonResultText = "Primary action pressed.";
        private bool _primaryButtonClosesApp;
        private bool _showSecondaryButton;
        private string _secondaryButtonLabel = "Cancel";
        private string _secondaryButtonResultText = "Secondary action pressed.";
        private bool _secondaryButtonClosesApp = true;
        private bool _useCustomUiBuilder;
        private bool _generateHookScaffold = true;
        private string _folderId = QuestProject.RootFolderId;
        private string _modName = "Schedule 1 Phone Apps";
        private string _modAuthor = "Phone App Creator";
        private string _modVersion = "1.0.0";
        private string _gameDeveloper = "TVGS";
        private string _gameName = "Schedule I";

        public PhoneAppBlueprint()
        {
            UiNodes.CollectionChanged += UiNodesOnCollectionChanged;
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

        [JsonProperty("appName")]
        public string AppName
        {
            get => _appName;
            set
            {
                if (SetProperty(ref _appName, value))
                {
                    OnPropertyChanged(nameof(DisplayName));
                }
            }
        }

        [JsonProperty("appTitle")]
        public string AppTitle
        {
            get => _appTitle;
            set
            {
                if (SetProperty(ref _appTitle, value))
                {
                    OnPropertyChanged(nameof(DisplayName));
                }
            }
        }

        [JsonProperty("iconLabel")]
        public string IconLabel
        {
            get => _iconLabel;
            set => SetProperty(ref _iconLabel, value);
        }

        [JsonProperty("iconFileName")]
        public string IconFileName
        {
            get => _iconFileName;
            set => SetProperty(ref _iconFileName, value ?? string.Empty);
        }

        [JsonProperty("orientation")]
        public PhoneAppOrientationOption Orientation
        {
            get => _orientation;
            set => SetProperty(ref _orientation, value);
        }

        [JsonProperty("layoutPreset")]
        public PhoneAppLayoutPresetOption LayoutPreset
        {
            get => _layoutPreset;
            set
            {
                if (SetProperty(ref _layoutPreset, value))
                {
                    OnPropertyChanged(nameof(IsBlankCanvas));
                    OnPropertyChanged(nameof(UsesGeneratedLayout));
                }
            }
        }

        [JsonProperty("headerText")]
        public string HeaderText
        {
            get => _headerText;
            set => SetProperty(ref _headerText, value ?? string.Empty);
        }

        [JsonProperty("subheaderText")]
        public string SubheaderText
        {
            get => _subheaderText;
            set => SetProperty(ref _subheaderText, value ?? string.Empty);
        }

        [JsonProperty("bodyText")]
        public string BodyText
        {
            get => _bodyText;
            set => SetProperty(ref _bodyText, value ?? string.Empty);
        }

        [JsonProperty("footerText")]
        public string FooterText
        {
            get => _footerText;
            set => SetProperty(ref _footerText, value ?? string.Empty);
        }

        [JsonProperty("useScrollableBody")]
        public bool UseScrollableBody
        {
            get => _useScrollableBody;
            set => SetProperty(ref _useScrollableBody, value);
        }

        [JsonProperty("showPrimaryButton")]
        public bool ShowPrimaryButton
        {
            get => _showPrimaryButton;
            set => SetProperty(ref _showPrimaryButton, value);
        }

        [JsonProperty("primaryButtonLabel")]
        public string PrimaryButtonLabel
        {
            get => _primaryButtonLabel;
            set => SetProperty(ref _primaryButtonLabel, value ?? string.Empty);
        }

        [JsonProperty("primaryButtonResultText")]
        public string PrimaryButtonResultText
        {
            get => _primaryButtonResultText;
            set => SetProperty(ref _primaryButtonResultText, value ?? string.Empty);
        }

        [JsonProperty("primaryButtonClosesApp")]
        public bool PrimaryButtonClosesApp
        {
            get => _primaryButtonClosesApp;
            set => SetProperty(ref _primaryButtonClosesApp, value);
        }

        [JsonProperty("showSecondaryButton")]
        public bool ShowSecondaryButton
        {
            get => _showSecondaryButton;
            set => SetProperty(ref _showSecondaryButton, value);
        }

        [JsonProperty("secondaryButtonLabel")]
        public string SecondaryButtonLabel
        {
            get => _secondaryButtonLabel;
            set => SetProperty(ref _secondaryButtonLabel, value ?? string.Empty);
        }

        [JsonProperty("secondaryButtonResultText")]
        public string SecondaryButtonResultText
        {
            get => _secondaryButtonResultText;
            set => SetProperty(ref _secondaryButtonResultText, value ?? string.Empty);
        }

        [JsonProperty("secondaryButtonClosesApp")]
        public bool SecondaryButtonClosesApp
        {
            get => _secondaryButtonClosesApp;
            set => SetProperty(ref _secondaryButtonClosesApp, value);
        }

        [JsonProperty("generateHookScaffold")]
        public bool GenerateHookScaffold
        {
            get => _generateHookScaffold;
            set => SetProperty(ref _generateHookScaffold, value);
        }

        [JsonProperty("useCustomUiBuilder")]
        public bool UseCustomUiBuilder
        {
            get => _useCustomUiBuilder;
            set
            {
                if (SetProperty(ref _useCustomUiBuilder, value))
                {
                    OnPropertyChanged(nameof(UsesGeneratedLayout));
                    OnPropertyChanged(nameof(IsBlankCanvas));
                    OnPropertyChanged(nameof(UsesLiveBuilder));
                }
            }
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

        [JsonProperty("uiNodes")]
        public ObservableCollection<PhoneAppUiNodeBlueprint> UiNodes { get; } = new();

        [JsonIgnore]
        public string DisplayName => string.IsNullOrWhiteSpace(AppTitle) ? AppName : AppTitle;

        [JsonIgnore]
        public bool UsesGeneratedLayout => !UseCustomUiBuilder && LayoutPreset != PhoneAppLayoutPresetOption.BlankCanvas;

        [JsonIgnore]
        public bool IsBlankCanvas => !UseCustomUiBuilder && LayoutPreset == PhoneAppLayoutPresetOption.BlankCanvas;

        [JsonIgnore]
        public bool UsesLiveBuilder => UseCustomUiBuilder;

        public void CopyFrom(PhoneAppBlueprint source)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            ClassName = source.ClassName;
            Namespace = source.Namespace;
            AppName = source.AppName;
            AppTitle = source.AppTitle;
            IconLabel = source.IconLabel;
            IconFileName = source.IconFileName;
            Orientation = source.Orientation;
            LayoutPreset = source.LayoutPreset;
            HeaderText = source.HeaderText;
            SubheaderText = source.SubheaderText;
            BodyText = source.BodyText;
            FooterText = source.FooterText;
            UseScrollableBody = source.UseScrollableBody;
            ShowPrimaryButton = source.ShowPrimaryButton;
            PrimaryButtonLabel = source.PrimaryButtonLabel;
            PrimaryButtonResultText = source.PrimaryButtonResultText;
            PrimaryButtonClosesApp = source.PrimaryButtonClosesApp;
            ShowSecondaryButton = source.ShowSecondaryButton;
            SecondaryButtonLabel = source.SecondaryButtonLabel;
            SecondaryButtonResultText = source.SecondaryButtonResultText;
            SecondaryButtonClosesApp = source.SecondaryButtonClosesApp;
            UseCustomUiBuilder = source.UseCustomUiBuilder;
            GenerateHookScaffold = source.GenerateHookScaffold;
            FolderId = source.FolderId;
            ModName = source.ModName;
            ModAuthor = source.ModAuthor;
            ModVersion = source.ModVersion;
            GameDeveloper = source.GameDeveloper;
            GameName = source.GameName;

            UiNodes.Clear();
            foreach (var node in source.UiNodes)
            {
                UiNodes.Add(node.DeepCopy());
            }
        }

        public PhoneAppBlueprint DeepCopy()
        {
            var copy = new PhoneAppBlueprint();
            copy.CopyFrom(this);
            return copy;
        }

        private void UiNodesOnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.OldItems != null)
            {
                foreach (var removedNode in e.OldItems.OfType<PhoneAppUiNodeBlueprint>())
                {
                    removedNode.PropertyChanged -= UiNodeOnPropertyChanged;
                }
            }

            if (e.NewItems != null)
            {
                foreach (var addedNode in e.NewItems.OfType<PhoneAppUiNodeBlueprint>())
                {
                    addedNode.PropertyChanged += UiNodeOnPropertyChanged;
                }
            }

            OnPropertyChanged(nameof(UiNodes));
        }

        private void UiNodeOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            OnPropertyChanged(nameof(UiNodes));
        }
    }

    /// <summary>
    /// Editor-side mirror of the S1API phone orientation enum.
    /// </summary>
    public enum PhoneAppOrientationOption
    {
        Horizontal,
        Vertical
    }

    /// <summary>
    /// Controls how much of the phone app UI is generated by the editor.
    /// </summary>
    public enum PhoneAppLayoutPresetOption
    {
        BlankCanvas,
        InformationPanel,
        ActionPanel
    }

    /// <summary>
    /// Supplies binding-friendly option lists for phone app editors.
    /// </summary>
    public static class PhoneAppBlueprintOptions
    {
        public static IReadOnlyList<PhoneAppOrientationOption> Orientations { get; } =
            Enum.GetValues(typeof(PhoneAppOrientationOption)).Cast<PhoneAppOrientationOption>().ToArray();

        public static IReadOnlyList<PhoneAppLayoutPresetOption> LayoutPresets { get; } =
            Enum.GetValues(typeof(PhoneAppLayoutPresetOption)).Cast<PhoneAppLayoutPresetOption>().ToArray();
    }
}
