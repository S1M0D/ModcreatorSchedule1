using System.Collections.ObjectModel;
using Newtonsoft.Json;
using Schedule1ModdingTool.Utils;

namespace Schedule1ModdingTool.Models
{
    /// <summary>
    /// Represents customer behavior defaults for an NPC.
    /// </summary>
    public class NpcCustomerDefaults : ObservableObject
    {
        private float _minWeeklySpending = 400f;
        private float _maxWeeklySpending = 900f;
        private int _minOrdersPerWeek = 1;
        private int _maxOrdersPerWeek = 3;
        private string _preferredOrderDay = "Monday";
        private int _orderTime = 900;
        private string _customerStandards = "Moderate";
        private bool _allowDirectApproach = true;
        private bool _guaranteeFirstSample;
        private float _mutualRelationMinAt50;
        private float _mutualRelationMaxAt100;
        private float _callPoliceChance;
        private float _baseAddiction;
        private float _dependenceMultiplier = 1.0f;

        [JsonProperty("minWeeklySpending")]
        public float MinWeeklySpending
        {
            get => _minWeeklySpending;
            set => SetProperty(ref _minWeeklySpending, value);
        }

        [JsonProperty("maxWeeklySpending")]
        public float MaxWeeklySpending
        {
            get => _maxWeeklySpending;
            set => SetProperty(ref _maxWeeklySpending, value);
        }

        [JsonProperty("minOrdersPerWeek")]
        public int MinOrdersPerWeek
        {
            get => _minOrdersPerWeek;
            set => SetProperty(ref _minOrdersPerWeek, value);
        }

        [JsonProperty("maxOrdersPerWeek")]
        public int MaxOrdersPerWeek
        {
            get => _maxOrdersPerWeek;
            set => SetProperty(ref _maxOrdersPerWeek, value);
        }

        [JsonProperty("preferredOrderDay")]
        public string PreferredOrderDay
        {
            get => _preferredOrderDay;
            set
            {
                // Strip ComboBoxItem prefix if present (for backward compatibility with old saved data)
                var cleanValue = value;
                if (!string.IsNullOrWhiteSpace(cleanValue) && cleanValue.Contains(":"))
                {
                    cleanValue = cleanValue.Substring(cleanValue.LastIndexOf(':') + 1).Trim();
                }
                SetProperty(ref _preferredOrderDay, cleanValue ?? "Monday");
            }
        }

        [JsonProperty("orderTime")]
        public int OrderTime
        {
            get => _orderTime;
            set => SetProperty(ref _orderTime, value);
        }

        [JsonProperty("customerStandards")]
        public string CustomerStandards
        {
            get => _customerStandards;
            set
            {
                // Strip ComboBoxItem prefix if present (for backward compatibility with old saved data)
                var cleanValue = value;
                if (!string.IsNullOrWhiteSpace(cleanValue) && cleanValue.Contains(":"))
                {
                    cleanValue = cleanValue.Substring(cleanValue.LastIndexOf(':') + 1).Trim();
                }
                SetProperty(ref _customerStandards, cleanValue ?? "Moderate");
            }
        }

        [JsonProperty("allowDirectApproach")]
        public bool AllowDirectApproach
        {
            get => _allowDirectApproach;
            set => SetProperty(ref _allowDirectApproach, value);
        }

        [JsonProperty("guaranteeFirstSample")]
        public bool GuaranteeFirstSample
        {
            get => _guaranteeFirstSample;
            set => SetProperty(ref _guaranteeFirstSample, value);
        }

        [JsonProperty("mutualRelationMinAt50")]
        public float MutualRelationMinAt50
        {
            get => _mutualRelationMinAt50;
            set => SetProperty(ref _mutualRelationMinAt50, value);
        }

        [JsonProperty("mutualRelationMaxAt100")]
        public float MutualRelationMaxAt100
        {
            get => _mutualRelationMaxAt100;
            set => SetProperty(ref _mutualRelationMaxAt100, value);
        }

        [JsonProperty("callPoliceChance")]
        public float CallPoliceChance
        {
            get => _callPoliceChance;
            set => SetProperty(ref _callPoliceChance, value);
        }

        [JsonProperty("baseAddiction")]
        public float BaseAddiction
        {
            get => _baseAddiction;
            set => SetProperty(ref _baseAddiction, value);
        }

        [JsonProperty("dependenceMultiplier")]
        public float DependenceMultiplier
        {
            get => _dependenceMultiplier;
            set => SetProperty(ref _dependenceMultiplier, value);
        }

        [JsonProperty("drugAffinities")]
        public ObservableCollection<DrugAffinity> DrugAffinities { get; } = new();

        [JsonProperty("preferredProperties")]
        public ObservableCollection<string> PreferredProperties { get; } = new();

        public void CopyFrom(NpcCustomerDefaults source)
        {
            if (source == null) return;

            MinWeeklySpending = source.MinWeeklySpending;
            MaxWeeklySpending = source.MaxWeeklySpending;
            MinOrdersPerWeek = source.MinOrdersPerWeek;
            MaxOrdersPerWeek = source.MaxOrdersPerWeek;
            PreferredOrderDay = source.PreferredOrderDay;
            OrderTime = source.OrderTime;
            CustomerStandards = source.CustomerStandards;
            AllowDirectApproach = source.AllowDirectApproach;
            GuaranteeFirstSample = source.GuaranteeFirstSample;
            MutualRelationMinAt50 = source.MutualRelationMinAt50;
            MutualRelationMaxAt100 = source.MutualRelationMaxAt100;
            CallPoliceChance = source.CallPoliceChance;
            BaseAddiction = source.BaseAddiction;
            DependenceMultiplier = source.DependenceMultiplier;

            DrugAffinities.Clear();
            foreach (var affinity in source.DrugAffinities)
            {
                DrugAffinities.Add(affinity.DeepCopy());
            }

            PreferredProperties.Clear();
            foreach (var prop in source.PreferredProperties)
            {
                PreferredProperties.Add(prop);
            }
        }

        public NpcCustomerDefaults DeepCopy()
        {
            var copy = new NpcCustomerDefaults
            {
                MinWeeklySpending = MinWeeklySpending,
                MaxWeeklySpending = MaxWeeklySpending,
                MinOrdersPerWeek = MinOrdersPerWeek,
                MaxOrdersPerWeek = MaxOrdersPerWeek,
                PreferredOrderDay = PreferredOrderDay,
                OrderTime = OrderTime,
                CustomerStandards = CustomerStandards,
                AllowDirectApproach = AllowDirectApproach,
                GuaranteeFirstSample = GuaranteeFirstSample,
                MutualRelationMinAt50 = MutualRelationMinAt50,
                MutualRelationMaxAt100 = MutualRelationMaxAt100,
                CallPoliceChance = CallPoliceChance,
                BaseAddiction = BaseAddiction,
                DependenceMultiplier = DependenceMultiplier
            };

            foreach (var affinity in DrugAffinities)
            {
                copy.DrugAffinities.Add(affinity.DeepCopy());
            }

            foreach (var prop in PreferredProperties)
            {
                copy.PreferredProperties.Add(prop);
            }

            return copy;
        }
    }

    /// <summary>
    /// Represents affinity for a specific drug type.
    /// </summary>
    public class DrugAffinity : ObservableObject
    {
        private string _drugType = "Marijuana";
        private float _affinityValue;

        [JsonProperty("drugType")]
        public string DrugType
        {
            get => _drugType;
            set => SetProperty(ref _drugType, value ?? "Marijuana");
        }

        [JsonProperty("affinityValue")]
        public float AffinityValue
        {
            get => _affinityValue;
            set => SetProperty(ref _affinityValue, value);
        }

        public DrugAffinity DeepCopy()
        {
            return new DrugAffinity
            {
                DrugType = DrugType,
                AffinityValue = AffinityValue
            };
        }
    }
}
