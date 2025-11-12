using System;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;

namespace Schedule1ModdingTool.Models
{
    /// <summary>
    /// Represents a quest objective with validation attributes
    /// </summary>
    public class QuestObjective : ObservableObject
    {
        private string _name = "";
        private string _title = "";
        private int _requiredProgress = 1;
        private bool _hasLocation = false;
        private float _locationX = 0f;
        private float _locationY = 0f;
        private float _locationZ = 0f;
        private ObservableCollection<QuestObjectiveTrigger> _startTriggers = new ObservableCollection<QuestObjectiveTrigger>();
        private ObservableCollection<QuestObjectiveTrigger> _finishTriggers = new ObservableCollection<QuestObjectiveTrigger>();
        private bool _autoStart = true;
        private bool _createPOI = true;
        private bool _useNpcLocation = false;
        private string _npcId = "";

        [Required(ErrorMessage = "Objective name is required")]
        [JsonProperty("name")]
        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        [Required(ErrorMessage = "Objective title is required")]
        [JsonProperty("title")]
        public string Title
        {
            get => _title;
            set => SetProperty(ref _title, value);
        }

        [Range(1, int.MaxValue, ErrorMessage = "Required progress must be at least 1")]
        [JsonProperty("requiredProgress")]
        public int RequiredProgress
        {
            get => _requiredProgress;
            set => SetProperty(ref _requiredProgress, value);
        }

        [JsonProperty("hasLocation")]
        public bool HasLocation
        {
            get => _hasLocation;
            set
            {
                if (SetProperty(ref _hasLocation, value))
                {
                    // Automatically enable POI creation when HasLocation is true
                    if (value && !_createPOI)
                    {
                        CreatePOI = true;
                    }
                }
            }
        }

        [JsonProperty("locationX")]
        public float LocationX
        {
            get => _locationX;
            set => SetProperty(ref _locationX, value);
        }

        [JsonProperty("locationY")]
        public float LocationY
        {
            get => _locationY;
            set => SetProperty(ref _locationY, value);
        }

        [JsonProperty("locationZ")]
        public float LocationZ
        {
            get => _locationZ;
            set => SetProperty(ref _locationZ, value);
        }

        /// <summary>
        /// Whether to use an NPC as the location source instead of static coordinates
        /// </summary>
        [JsonProperty("useNpcLocation")]
        public bool UseNpcLocation
        {
            get => _useNpcLocation;
            set => SetProperty(ref _useNpcLocation, value);
        }

        /// <summary>
        /// The NPC ID to use as the location source (when UseNpcLocation is true)
        /// </summary>
        [JsonProperty("npcId")]
        public string NpcId
        {
            get => _npcId;
            set => SetProperty(ref _npcId, value);
        }

        [JsonIgnore]
        public string LocationText
        {
            get
            {
                if (!HasLocation)
                    return "No Location";
                
                if (UseNpcLocation && !string.IsNullOrWhiteSpace(NpcId))
                    return $"NPC: {NpcId}";
                
                return $"({LocationX:F2}, {LocationY:F2}, {LocationZ:F2})";
            }
        }

        /// <summary>
        /// Triggers that start this objective
        /// </summary>
        [JsonProperty("startTriggers")]
        public ObservableCollection<QuestObjectiveTrigger> StartTriggers
        {
            get => _startTriggers;
            set => SetProperty(ref _startTriggers, value);
        }

        /// <summary>
        /// Triggers that finish this objective
        /// </summary>
        [JsonProperty("finishTriggers")]
        public ObservableCollection<QuestObjectiveTrigger> FinishTriggers
        {
            get => _finishTriggers;
            set => SetProperty(ref _finishTriggers, value);
        }

        /// <summary>
        /// Whether to automatically start this objective when created (if false, requires trigger)
        /// </summary>
        [JsonProperty("autoStart")]
        public bool AutoStart
        {
            get => _autoStart;
            set => SetProperty(ref _autoStart, value);
        }

        /// <summary>
        /// Whether to create a POI (Point of Interest) marker for this objective
        /// </summary>
        [JsonProperty("createPOI")]
        public bool CreatePOI
        {
            get => _createPOI;
            set => SetProperty(ref _createPOI, value);
        }

        public QuestObjective()
        {
        }

        public QuestObjective(string name, string title)
        {
            Name = name;
            Title = title;
        }

        public override string ToString()
        {
            return $"{Title} ({Name})";
        }

        public void CopyFrom(QuestObjective source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));

            Name = source.Name;
            Title = source.Title;
            RequiredProgress = source.RequiredProgress;
            HasLocation = source.HasLocation;
            LocationX = source.LocationX;
            LocationY = source.LocationY;
            LocationZ = source.LocationZ;
            AutoStart = source.AutoStart;
            CreatePOI = source.CreatePOI;
            UseNpcLocation = source.UseNpcLocation;
            NpcId = source.NpcId;

            StartTriggers.Clear();
            foreach (var trigger in source.StartTriggers)
            {
                StartTriggers.Add(trigger.DeepCopy());
            }

            FinishTriggers.Clear();
            foreach (var trigger in source.FinishTriggers)
            {
                FinishTriggers.Add(trigger.DeepCopy());
            }
        }

        public QuestObjective DeepCopy()
        {
            var copy = new QuestObjective();
            copy.CopyFrom(this);
            return copy;
        }
    }
}
