using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Schedule1ModdingTool.Utils;

namespace Schedule1ModdingTool.Models
{
    /// <summary>
    /// Represents a customizable NPC blueprint with identity, behavior, and appearance settings.
    /// </summary>
    public class NpcBlueprint : ObservableObject
    {
        private string _className = "GeneratedNpc";
        private string _namespace = "Schedule1Mods.NPCs";
        private string _npcId = "npc_id";
        private string _firstName = "New";
        private string _lastName = "NPC";
        private string _modName = "Schedule 1 NPC Pack";
        private string _modAuthor = "NPC Creator";
        private string _modVersion = "1.0.0";
        private string _gameDeveloper = "TVGS";
        private string _gameName = "Schedule I";
        private bool _isPhysical = true;
        private bool _isDealer;
        private bool _enableCustomer = true;
        private bool _hasSpawnPosition;
        private float _spawnX;
        private float _spawnY;
        private float _spawnZ;
        private NpcAppearanceSettings _appearance = new NpcAppearanceSettings();
        private string _folderId = QuestProject.RootFolderId;

        [Required]
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

        [Required]
        [JsonProperty("npcId")]
        public string NpcId
        {
            get => _npcId;
            set => SetProperty(ref _npcId, value);
        }

        [JsonProperty("firstName")]
        public string FirstName
        {
            get => _firstName;
            set
            {
                if (SetProperty(ref _firstName, value))
                    OnPropertyChanged(nameof(DisplayName));
            }
        }

        [JsonProperty("lastName")]
        public string LastName
        {
            get => _lastName;
            set
            {
                if (SetProperty(ref _lastName, value))
                    OnPropertyChanged(nameof(DisplayName));
            }
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

        [JsonProperty("isPhysical")]
        public bool IsPhysical
        {
            get => _isPhysical;
            set => SetProperty(ref _isPhysical, value);
        }

        [JsonProperty("isDealer")]
        public bool IsDealer
        {
            get => _isDealer;
            set => SetProperty(ref _isDealer, value);
        }

        [JsonProperty("enableCustomer")]
        public bool EnableCustomer
        {
            get => _enableCustomer;
            set => SetProperty(ref _enableCustomer, value);
        }

        [JsonProperty("hasSpawnPosition")]
        public bool HasSpawnPosition
        {
            get => _hasSpawnPosition;
            set => SetProperty(ref _hasSpawnPosition, value);
        }

        [JsonProperty("spawnX")]
        public float SpawnX
        {
            get => _spawnX;
            set => SetProperty(ref _spawnX, value);
        }

        [JsonProperty("spawnY")]
        public float SpawnY
        {
            get => _spawnY;
            set => SetProperty(ref _spawnY, value);
        }

        [JsonProperty("spawnZ")]
        public float SpawnZ
        {
            get => _spawnZ;
            set => SetProperty(ref _spawnZ, value);
        }

        [JsonProperty("appearance")]
        public NpcAppearanceSettings Appearance
        {
            get => _appearance;
            set
            {
                if (_appearance != null)
                    _appearance.PropertyChanged -= AppearanceOnPropertyChanged;

                if (SetProperty(ref _appearance, value))
                {
                    if (_appearance != null)
                        _appearance.PropertyChanged += AppearanceOnPropertyChanged;
                }
            }
        }

        [JsonProperty("folderId")]
        public string FolderId
        {
            get => _folderId;
            set => SetProperty(ref _folderId, string.IsNullOrWhiteSpace(value) ? QuestProject.RootFolderId : value);
        }

        [JsonIgnore]
        public string DisplayName => string.IsNullOrWhiteSpace(FirstName)
            ? ClassName
            : $"{FirstName} {LastName}".Trim();

        public NpcBlueprint()
        {
            _appearance.PropertyChanged += AppearanceOnPropertyChanged;
        }

        private void AppearanceOnPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            OnPropertyChanged(nameof(Appearance));
        }

        public void CopyFrom(NpcBlueprint source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));

            ClassName = source.ClassName;
            Namespace = source.Namespace;
            NpcId = source.NpcId;
            FirstName = source.FirstName;
            LastName = source.LastName;
            ModName = source.ModName;
            ModAuthor = source.ModAuthor;
            ModVersion = source.ModVersion;
            GameDeveloper = source.GameDeveloper;
            GameName = source.GameName;
            IsPhysical = source.IsPhysical;
            IsDealer = source.IsDealer;
            EnableCustomer = source.EnableCustomer;
            HasSpawnPosition = source.HasSpawnPosition;
            SpawnX = source.SpawnX;
            SpawnY = source.SpawnY;
            SpawnZ = source.SpawnZ;
            Appearance.CopyFrom(source.Appearance);
            FolderId = source.FolderId;
        }

        public NpcBlueprint DeepCopy()
        {
            var copy = new NpcBlueprint();
            copy.CopyFrom(this);
            return copy;
        }
    }

    /// <summary>
    /// Configurable appearance values mirrored from AvatarDefaultsBuilder.
    /// </summary>
    public class NpcAppearanceSettings : ObservableObject
    {
        private double _gender = 0.5;
        private double _height = 1.0;
        private double _weight = 0.5;
        private double _leftEyeTop = 0.5;
        private double _leftEyeBottom = 0.5;
        private double _rightEyeTop = 0.5;
        private double _rightEyeBottom = 0.5;
        private double _pupilDilation = 0.5;
        private double _eyebrowScale = 1.0;
        private double _eyebrowThickness = 1.0;
        private double _eyebrowRestingHeight;
        private double _eyebrowRestingAngle;
        private string _skinColor = "#FFD3B58F";
        private string _leftEyeLidColor = "#FFD3B58F";
        private string _rightEyeLidColor = "#FFD3B58F";
        private string _eyeBallTint = "#FFFFFFFF";
        private string _hairColor = "#FF2D2013";
        private string _hairPath = "Avatar/Hair/Spiky/Spiky";
        private string _eyeballMaterialIdentifier = "Default";

        [JsonProperty("faceLayers")]
        public ObservableCollection<NpcAppearanceLayer> FaceLayers { get; } = new();

        [JsonProperty("bodyLayers")]
        public ObservableCollection<NpcAppearanceLayer> BodyLayers { get; } = new();

        [JsonProperty("accessoryLayers")]
        public ObservableCollection<NpcAppearanceLayer> AccessoryLayers { get; } = new();

        public NpcAppearanceSettings()
        {
            // Subscribe to collection changes to trigger PropertyChanged events
            // This ensures appearance preview updates when layers are added/removed
            FaceLayers.CollectionChanged += OnLayerCollectionChanged;
            BodyLayers.CollectionChanged += OnLayerCollectionChanged;
            AccessoryLayers.CollectionChanged += OnLayerCollectionChanged;

            // Subscribe to any existing items (in case of deserialization)
            SubscribeToExistingLayers();
        }

        [OnDeserialized]
        internal void OnDeserializedMethod(StreamingContext context)
        {
            // Re-subscribe to layers after JSON deserialization
            SubscribeToExistingLayers();
        }

        private void SubscribeToExistingLayers()
        {
            foreach (var layer in FaceLayers)
                layer.PropertyChanged += OnLayerPropertyChanged;
            foreach (var layer in BodyLayers)
                layer.PropertyChanged += OnLayerPropertyChanged;
            foreach (var layer in AccessoryLayers)
                layer.PropertyChanged += OnLayerPropertyChanged;
        }

        private void OnLayerCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            Debug.WriteLine($"[NpcAppearanceSettings] Layer collection changed: {e.Action}");

            // Unsubscribe from old items
            if (e.OldItems != null)
            {
                foreach (var item in e.OldItems)
                {
                    if (item is NpcAppearanceLayer layer)
                    {
                        layer.PropertyChanged -= OnLayerPropertyChanged;
                        Debug.WriteLine($"[NpcAppearanceSettings] Unsubscribed from layer: {layer.LayerPath}");
                    }
                }
            }

            // Subscribe to new items
            if (e.NewItems != null)
            {
                foreach (var item in e.NewItems)
                {
                    if (item is NpcAppearanceLayer layer)
                    {
                        layer.PropertyChanged += OnLayerPropertyChanged;
                        Debug.WriteLine($"[NpcAppearanceSettings] Subscribed to layer: {layer.LayerPath}");
                    }
                }
            }

            // Notify that layers changed
            if (sender == FaceLayers)
            {
                Debug.WriteLine("[NpcAppearanceSettings] Raising PropertyChanged for FaceLayers");
                OnPropertyChanged(nameof(FaceLayers));
            }
            else if (sender == BodyLayers)
            {
                Debug.WriteLine("[NpcAppearanceSettings] Raising PropertyChanged for BodyLayers");
                OnPropertyChanged(nameof(BodyLayers));
            }
            else if (sender == AccessoryLayers)
            {
                Debug.WriteLine("[NpcAppearanceSettings] Raising PropertyChanged for AccessoryLayers");
                OnPropertyChanged(nameof(AccessoryLayers));
            }
        }

        private void OnLayerPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            Debug.WriteLine($"[NpcAppearanceSettings] Layer property changed: {e.PropertyName}");

            // When any layer's properties change, notify that the collection changed
            // This triggers appearance preview updates for layer color/path changes
            if (FaceLayers.Contains(sender))
            {
                Debug.WriteLine("[NpcAppearanceSettings] Raising PropertyChanged for FaceLayers (property change)");
                OnPropertyChanged(nameof(FaceLayers));
            }
            else if (BodyLayers.Contains(sender))
            {
                Debug.WriteLine("[NpcAppearanceSettings] Raising PropertyChanged for BodyLayers (property change)");
                OnPropertyChanged(nameof(BodyLayers));
            }
            else if (AccessoryLayers.Contains(sender))
            {
                Debug.WriteLine("[NpcAppearanceSettings] Raising PropertyChanged for AccessoryLayers (property change)");
                OnPropertyChanged(nameof(AccessoryLayers));
            }
        }

        [JsonProperty("gender")]
        public double Gender
        {
            get => _gender;
            set => SetProperty(ref _gender, Clamp01(value));
        }

        [JsonProperty("height")]
        public double Height
        {
            get => _height;
            set => SetProperty(ref _height, Clamp(value, 0.5, 1.5));
        }

        [JsonProperty("weight")]
        public double Weight
        {
            get => _weight;
            set => SetProperty(ref _weight, Clamp(value, 0.1, 2.0));
        }

        [JsonProperty("skinColor")]
        public string SkinColor
        {
            get => _skinColor;
            set => SetProperty(ref _skinColor, ColorUtils.NormalizeHex(value));
        }

        [JsonProperty("leftEyeLidColor")]
        public string LeftEyeLidColor
        {
            get => _leftEyeLidColor;
            set => SetProperty(ref _leftEyeLidColor, ColorUtils.NormalizeHex(value));
        }

        [JsonProperty("rightEyeLidColor")]
        public string RightEyeLidColor
        {
            get => _rightEyeLidColor;
            set => SetProperty(ref _rightEyeLidColor, ColorUtils.NormalizeHex(value));
        }

        [JsonProperty("eyeBallTint")]
        public string EyeBallTint
        {
            get => _eyeBallTint;
            set => SetProperty(ref _eyeBallTint, ColorUtils.NormalizeHex(value));
        }

        [JsonProperty("hairColor")]
        public string HairColor
        {
            get => _hairColor;
            set => SetProperty(ref _hairColor, ColorUtils.NormalizeHex(value));
        }

        [JsonProperty("hairPath")]
        public string HairPath
        {
            get => _hairPath;
            set => SetProperty(ref _hairPath, value ?? string.Empty);
        }

        [JsonProperty("eyeballMaterialId")]
        public string EyeballMaterialIdentifier
        {
            get => _eyeballMaterialIdentifier;
            set => SetProperty(ref _eyeballMaterialIdentifier, value ?? "Default");
        }

        [JsonProperty("pupilDilation")]
        public double PupilDilation
        {
            get => _pupilDilation;
            set => SetProperty(ref _pupilDilation, Clamp01(value));
        }

        [JsonProperty("eyebrowScale")]
        public double EyebrowScale
        {
            get => _eyebrowScale;
            set => SetProperty(ref _eyebrowScale, Clamp(value, 0.1, 3.0));
        }

        [JsonProperty("eyebrowThickness")]
        public double EyebrowThickness
        {
            get => _eyebrowThickness;
            set => SetProperty(ref _eyebrowThickness, Clamp(value, 0.1, 3.0));
        }

        [JsonProperty("eyebrowRestingHeight")]
        public double EyebrowRestingHeight
        {
            get => _eyebrowRestingHeight;
            set => SetProperty(ref _eyebrowRestingHeight, Clamp(value, -1.0, 1.0));
        }

        [JsonProperty("eyebrowRestingAngle")]
        public double EyebrowRestingAngle
        {
            get => _eyebrowRestingAngle;
            set => SetProperty(ref _eyebrowRestingAngle, Clamp(value, -1.0, 1.0));
        }

        [JsonProperty("leftEye")]
        public double LeftEyeTop
        {
            get => _leftEyeTop;
            set => SetProperty(ref _leftEyeTop, Clamp01(value));
        }

        [JsonProperty("leftEyeBottom")]
        public double LeftEyeBottom
        {
            get => _leftEyeBottom;
            set => SetProperty(ref _leftEyeBottom, Clamp01(value));
        }

        [JsonProperty("rightEyeTop")]
        public double RightEyeTop
        {
            get => _rightEyeTop;
            set => SetProperty(ref _rightEyeTop, Clamp01(value));
        }

        [JsonProperty("rightEyeBottom")]
        public double RightEyeBottom
        {
            get => _rightEyeBottom;
            set => SetProperty(ref _rightEyeBottom, Clamp01(value));
        }

        public void CopyFrom(NpcAppearanceSettings source)
        {
            if (source == null) return;

            Gender = source.Gender;
            Height = source.Height;
            Weight = source.Weight;
            SkinColor = source.SkinColor;
            LeftEyeLidColor = source.LeftEyeLidColor;
            RightEyeLidColor = source.RightEyeLidColor;
            EyeBallTint = source.EyeBallTint;
            HairColor = source.HairColor;
            HairPath = source.HairPath;
            EyeballMaterialIdentifier = source.EyeballMaterialIdentifier;
            PupilDilation = source.PupilDilation;
            EyebrowScale = source.EyebrowScale;
            EyebrowThickness = source.EyebrowThickness;
            EyebrowRestingHeight = source.EyebrowRestingHeight;
            EyebrowRestingAngle = source.EyebrowRestingAngle;
            LeftEyeTop = source.LeftEyeTop;
            LeftEyeBottom = source.LeftEyeBottom;
            RightEyeTop = source.RightEyeTop;
            RightEyeBottom = source.RightEyeBottom;

            CopyLayers(FaceLayers, source.FaceLayers);
            CopyLayers(BodyLayers, source.BodyLayers);
            CopyLayers(AccessoryLayers, source.AccessoryLayers);
        }

        private static void CopyLayers(ObservableCollection<NpcAppearanceLayer> target, ObservableCollection<NpcAppearanceLayer> source)
        {
            target.Clear();
            foreach (var layer in source)
            {
                target.Add(layer.DeepCopy());
            }
        }

        private static double Clamp(double value, double min, double max) =>
            Math.Max(min, Math.Min(max, value));

        private static double Clamp01(double value) => Clamp(value, 0.0, 1.0);
    }

    public class NpcAppearanceLayer : ObservableObject
    {
        private string _layerPath = string.Empty;
        private string _colorHex = "#FFFFFFFF";

        [JsonProperty("path")]
        public string LayerPath
        {
            get => _layerPath;
            set => SetProperty(ref _layerPath, value ?? string.Empty);
        }

        [JsonProperty("color")]
        public string ColorHex
        {
            get => _colorHex;
            set => SetProperty(ref _colorHex, ColorUtils.NormalizeHex(value));
        }

        public NpcAppearanceLayer DeepCopy()
        {
            return new NpcAppearanceLayer
            {
                LayerPath = LayerPath,
                ColorHex = ColorHex
            };
        }
    }
}
