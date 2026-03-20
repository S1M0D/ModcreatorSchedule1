using Newtonsoft.Json;

namespace Schedule1ModdingTool.Models
{
    /// <summary>
    /// Runtime and prefab-side behavior options for an NPC.
    /// </summary>
    public class NpcRuntimeSettings : ObservableObject
    {
        private bool _setAggressiveness;
        private float _aggressiveness;
        private bool _setRegion;
        private string _region = "Northtown";
        private bool _setScale;
        private float _scale = 1f;
        private bool _overrideRequiresRegionUnlocked;
        private bool _requiresRegionUnlocked = true;
        private bool _enableSmokeBreak;
        private string _smokeBreakCigarettePath = string.Empty;
        private bool _enableSmokeBreakDebugMode;
        private bool _enableGraffiti;
        private string _sprayPaintEquippablePath = string.Empty;
        private bool _enableDrinking;
        private string _drinkEquippablePath = string.Empty;
        private bool _enableItemHolding;
        private string _heldItemEquippablePath = string.Empty;

        [JsonProperty("setAggressiveness")]
        public bool SetAggressiveness
        {
            get => _setAggressiveness;
            set => SetProperty(ref _setAggressiveness, value);
        }

        [JsonProperty("aggressiveness")]
        public float Aggressiveness
        {
            get => _aggressiveness;
            set => SetProperty(ref _aggressiveness, value);
        }

        [JsonProperty("setRegion")]
        public bool SetRegion
        {
            get => _setRegion;
            set => SetProperty(ref _setRegion, value);
        }

        [JsonProperty("region")]
        public string Region
        {
            get => _region;
            set => SetProperty(ref _region, string.IsNullOrWhiteSpace(value) ? "Northtown" : value);
        }

        [JsonProperty("setScale")]
        public bool SetScale
        {
            get => _setScale;
            set => SetProperty(ref _setScale, value);
        }

        [JsonProperty("scale")]
        public float Scale
        {
            get => _scale;
            set => SetProperty(ref _scale, value <= 0f ? 1f : value);
        }

        [JsonProperty("overrideRequiresRegionUnlocked")]
        public bool OverrideRequiresRegionUnlocked
        {
            get => _overrideRequiresRegionUnlocked;
            set => SetProperty(ref _overrideRequiresRegionUnlocked, value);
        }

        [JsonProperty("requiresRegionUnlocked")]
        public bool RequiresRegionUnlocked
        {
            get => _requiresRegionUnlocked;
            set => SetProperty(ref _requiresRegionUnlocked, value);
        }

        [JsonProperty("enableSmokeBreak")]
        public bool EnableSmokeBreak
        {
            get => _enableSmokeBreak;
            set => SetProperty(ref _enableSmokeBreak, value);
        }

        [JsonProperty("smokeBreakCigarettePath")]
        public string SmokeBreakCigarettePath
        {
            get => _smokeBreakCigarettePath;
            set => SetProperty(ref _smokeBreakCigarettePath, value ?? string.Empty);
        }

        [JsonProperty("enableSmokeBreakDebugMode")]
        public bool EnableSmokeBreakDebugMode
        {
            get => _enableSmokeBreakDebugMode;
            set => SetProperty(ref _enableSmokeBreakDebugMode, value);
        }

        [JsonProperty("enableGraffiti")]
        public bool EnableGraffiti
        {
            get => _enableGraffiti;
            set => SetProperty(ref _enableGraffiti, value);
        }

        [JsonProperty("sprayPaintEquippablePath")]
        public string SprayPaintEquippablePath
        {
            get => _sprayPaintEquippablePath;
            set => SetProperty(ref _sprayPaintEquippablePath, value ?? string.Empty);
        }

        [JsonProperty("enableDrinking")]
        public bool EnableDrinking
        {
            get => _enableDrinking;
            set => SetProperty(ref _enableDrinking, value);
        }

        [JsonProperty("drinkEquippablePath")]
        public string DrinkEquippablePath
        {
            get => _drinkEquippablePath;
            set => SetProperty(ref _drinkEquippablePath, value ?? string.Empty);
        }

        [JsonProperty("enableItemHolding")]
        public bool EnableItemHolding
        {
            get => _enableItemHolding;
            set => SetProperty(ref _enableItemHolding, value);
        }

        [JsonProperty("heldItemEquippablePath")]
        public string HeldItemEquippablePath
        {
            get => _heldItemEquippablePath;
            set => SetProperty(ref _heldItemEquippablePath, value ?? string.Empty);
        }

        public void CopyFrom(NpcRuntimeSettings source)
        {
            if (source == null)
                return;

            SetAggressiveness = source.SetAggressiveness;
            Aggressiveness = source.Aggressiveness;
            SetRegion = source.SetRegion;
            Region = source.Region;
            SetScale = source.SetScale;
            Scale = source.Scale;
            OverrideRequiresRegionUnlocked = source.OverrideRequiresRegionUnlocked;
            RequiresRegionUnlocked = source.RequiresRegionUnlocked;
            EnableSmokeBreak = source.EnableSmokeBreak;
            SmokeBreakCigarettePath = source.SmokeBreakCigarettePath;
            EnableSmokeBreakDebugMode = source.EnableSmokeBreakDebugMode;
            EnableGraffiti = source.EnableGraffiti;
            SprayPaintEquippablePath = source.SprayPaintEquippablePath;
            EnableDrinking = source.EnableDrinking;
            DrinkEquippablePath = source.DrinkEquippablePath;
            EnableItemHolding = source.EnableItemHolding;
            HeldItemEquippablePath = source.HeldItemEquippablePath;
        }

        public NpcRuntimeSettings DeepCopy()
        {
            var copy = new NpcRuntimeSettings();
            copy.CopyFrom(this);
            return copy;
        }
    }
}
