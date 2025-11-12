using System;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace Schedule1ModdingTool.Models
{
    /// <summary>
    /// Represents a quest blueprint with all its properties and objectives
    /// </summary>
    public class QuestBlueprint : ObservableObject
    {
        private string _className = "";
        private string _questId = "";
        private string _questTitle = "";
        private string _questDescription = "";
        private bool _autoBegin = true;
        private bool _customIcon = false;
        private string _iconFileName = string.Empty;
        private bool _questRewards = true;
        private bool _generateDataClass = false;
        private QuestBlueprintType _blueprintType = QuestBlueprintType.Standard;
        private string _namespace = "Schedule1Mods.Quests";
        private string _modName = "Schedule 1 Quest Pack";
        private string _modVersion = "1.0.0";
        private string _modAuthor = "Quest Creator";
        private string _gameDeveloper = "TVGS";
        private string _gameName = "Schedule I";
        private QuestStartCondition _startCondition = new QuestStartCondition();
        private string _folderId = QuestProject.RootFolderId;
        private ObservableCollection<QuestTrigger> _questTriggers = new ObservableCollection<QuestTrigger>();
        private ObservableCollection<QuestFinishTrigger> _questFinishTriggers = new ObservableCollection<QuestFinishTrigger>();
        private bool _trackOnBegin = true;
        private bool _autoCompleteOnAllEntriesComplete = true;
        private ObservableCollection<QuestReward> _questRewardsList = new ObservableCollection<QuestReward>();
        private ObservableCollection<DataClassField> _dataClassFields = new ObservableCollection<DataClassField>();

        [Required(ErrorMessage = "Class name is required")]
        [JsonProperty("className")]
        public string ClassName
        {
            get => _className;
            set => SetProperty(ref _className, value);
        }

        [JsonProperty("questId")]
        public string QuestId
        {
            get => _questId;
            set => SetProperty(ref _questId, value);
        }

        [Required(ErrorMessage = "Quest title is required")]
        [JsonProperty("questTitle")]
        public string QuestTitle
        {
            get => _questTitle;
            set => SetProperty(ref _questTitle, value);
        }

        [Required(ErrorMessage = "Quest description is required")]
        [JsonProperty("questDescription")]
        public string QuestDescription
        {
            get => _questDescription;
            set => SetProperty(ref _questDescription, value);
        }

        [Required(ErrorMessage = "Namespace is required")]
        [JsonProperty("namespace")]
        public string Namespace
        {
            get => _namespace;
            set => SetProperty(ref _namespace, value);
        }

        [Required(ErrorMessage = "Mod name is required")]
        [JsonProperty("modName")]
        public string ModName
        {
            get => _modName;
            set => SetProperty(ref _modName, value);
        }

        [Required(ErrorMessage = "Mod version is required")]
        [JsonProperty("modVersion")]
        public string ModVersion
        {
            get => _modVersion;
            set => SetProperty(ref _modVersion, value);
        }

        [Required(ErrorMessage = "Mod author is required")]
        [JsonProperty("modAuthor")]
        public string ModAuthor
        {
            get => _modAuthor;
            set => SetProperty(ref _modAuthor, value);
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

        [JsonProperty("autoBegin")]
        public bool AutoBegin
        {
            get => _autoBegin;
            set => SetProperty(ref _autoBegin, value);
        }

        [JsonProperty("customIcon")]
        public bool CustomIcon
        {
            get => _customIcon;
            set => SetProperty(ref _customIcon, value);
        }

        [JsonProperty("iconFileName")]
        public string IconFileName
        {
            get => _iconFileName;
            set => SetProperty(ref _iconFileName, value);
        }

        [JsonProperty("questRewards")]
        public bool QuestRewards
        {
            get => _questRewards;
            set
            {
                if (SetProperty(ref _questRewards, value))
                {
                    // If enabling rewards and list is empty, add a default reward
                    if (value && (_questRewardsList == null || _questRewardsList.Count == 0))
                    {
                        if (_questRewardsList == null)
                        {
                            _questRewardsList = new ObservableCollection<QuestReward>();
                        }
                        _questRewardsList.Add(new QuestReward
                        {
                            RewardType = QuestRewardType.Money,
                            Amount = 100
                        });
                    }
                }
            }
        }

        /// <summary>
        /// Collection of quest rewards (XP, Money, Items)
        /// </summary>
        [JsonProperty("questRewardsList")]
        public ObservableCollection<QuestReward> QuestRewardsList
        {
            get => _questRewardsList;
            set
            {
                if (SetProperty(ref _questRewardsList, value))
                {
                    // Update the boolean flag based on whether there are any rewards
                    var hasRewards = _questRewardsList != null && _questRewardsList.Count > 0;
                    if (_questRewards != hasRewards)
                    {
                        _questRewards = hasRewards;
                        OnPropertyChanged(nameof(QuestRewards));
                    }
                }
            }
        }

        /// <summary>
        /// Collection of custom data class fields
        /// </summary>
        [JsonProperty("dataClassFields")]
        public ObservableCollection<DataClassField> DataClassFields
        {
            get => _dataClassFields;
            set => SetProperty(ref _dataClassFields, value);
        }

        [JsonProperty("generateDataClass")]
        public bool GenerateDataClass
        {
            get => _generateDataClass;
            set => SetProperty(ref _generateDataClass, value);
        }

        [JsonProperty("blueprintType")]
        public QuestBlueprintType BlueprintType
        {
            get => _blueprintType;
            set => SetProperty(ref _blueprintType, value);
        }

        [JsonProperty("objectives")]
        public ObservableCollection<QuestObjective> Objectives { get; } = new ObservableCollection<QuestObjective>();

        [JsonProperty("startCondition")]
        public QuestStartCondition StartCondition
        {
            get => _startCondition;
            set => SetProperty(ref _startCondition, value);
        }

        [JsonProperty("folderId")]
        public string FolderId
        {
            get => _folderId;
            set => SetProperty(ref _folderId, string.IsNullOrWhiteSpace(value) ? QuestProject.RootFolderId : value);
        }

        /// <summary>
        /// Collection of triggers that can start this quest
        /// </summary>
        [JsonProperty("questTriggers")]
        public ObservableCollection<QuestTrigger> QuestTriggers
        {
            get => _questTriggers;
            set => SetProperty(ref _questTriggers, value);
        }

        /// <summary>
        /// Collection of triggers that can finish this quest
        /// </summary>
        [JsonProperty("questFinishTriggers")]
        public ObservableCollection<QuestFinishTrigger> QuestFinishTriggers
        {
            get => _questFinishTriggers;
            set => SetProperty(ref _questFinishTriggers, value);
        }

        /// <summary>
        /// Whether to automatically track the quest when it begins
        /// </summary>
        [JsonProperty("trackOnBegin")]
        public bool TrackOnBegin
        {
            get => _trackOnBegin;
            set => SetProperty(ref _trackOnBegin, value);
        }

        /// <summary>
        /// Whether to automatically complete the quest when all entries are complete
        /// </summary>
        [JsonProperty("autoCompleteOnAllEntriesComplete")]
        public bool AutoCompleteOnAllEntriesComplete
        {
            get => _autoCompleteOnAllEntriesComplete;
            set => SetProperty(ref _autoCompleteOnAllEntriesComplete, value);
        }

        [JsonIgnore]
        public string DisplayName => string.IsNullOrEmpty(QuestTitle) ? ClassName : QuestTitle;

        [JsonIgnore]
        public string Summary => $"{DisplayName} ({Objectives.Count} objectives)";

        public QuestBlueprint()
        {
            // Add default objective
            Objectives.Add(new QuestObjective("objective_1", "Complete objective"));
            
            // Initialize collections
            _questRewardsList = new ObservableCollection<QuestReward>();
            _dataClassFields = new ObservableCollection<DataClassField>();
        }

        public QuestBlueprint(QuestBlueprintType type) : this()
        {
            BlueprintType = type;
            if (type == QuestBlueprintType.Advanced)
            {
                // Advanced blueprints have more default objectives
                Objectives.Add(new QuestObjective("objective_2", "Advanced objective"));
            }
        }

        [OnDeserialized]
        private void OnDeserialized(StreamingContext context)
        {
            // When deserializing from JSON, the constructor adds default objectives,
            // then JSON deserializer adds objectives from the file.
            // We need to remove the constructor's defaults if objectives were loaded from JSON.
            
            // Expected default count: 1 for Standard, 2 for Advanced
            int expectedDefaultCount = BlueprintType == QuestBlueprintType.Advanced ? 2 : 1;
            
            // If we have more objectives than the constructor would add, it means objectives were loaded from JSON
            // In that case, remove the default objectives added by the constructor
            if (Objectives.Count > expectedDefaultCount)
            {
                // Remove default objective_1 if it matches the constructor's pattern
                var defaultObj1 = Objectives
                    .FirstOrDefault(obj => obj.Name == "objective_1" && obj.Title == "Complete objective");
                if (defaultObj1 != null)
                {
                    Objectives.Remove(defaultObj1);
                }
                
                // Remove default objective_2 for Advanced blueprints if it matches the constructor's pattern
                if (BlueprintType == QuestBlueprintType.Advanced)
                {
                    var defaultObj2 = Objectives
                        .FirstOrDefault(obj => obj.Name == "objective_2" && obj.Title == "Advanced objective");
                    if (defaultObj2 != null)
                    {
                        Objectives.Remove(defaultObj2);
                    }
                }
            }

            // Backward compatibility: If QuestRewards boolean is true but QuestRewardsList is empty,
            // create a default money reward
            if (_questRewards && (_questRewardsList == null || _questRewardsList.Count == 0))
            {
                if (_questRewardsList == null)
                {
                    _questRewardsList = new ObservableCollection<QuestReward>();
                }
                _questRewardsList.Add(new QuestReward
                {
                    RewardType = QuestRewardType.Money,
                    Amount = 100
                });
            }

            // Initialize DataClassFields if null
            if (_dataClassFields == null)
            {
                _dataClassFields = new ObservableCollection<DataClassField>();
            }
        }

        public void AddObjective()
        {
            int nextIndex = Objectives.Count + 1;
            var objective = new QuestObjective($"objective_{nextIndex}", $"Objective {nextIndex}");
            Objectives.Add(objective);
        }

        public void RemoveObjective(QuestObjective objective)
        {
            if (Objectives.Count > 1) // Always keep at least one objective
            {
                Objectives.Remove(objective);
            }
        }

        public void AddReward()
        {
            QuestRewardsList.Add(new QuestReward
            {
                RewardType = QuestRewardType.Money,
                Amount = 100
            });
            QuestRewards = true;
        }

        public void RemoveReward(QuestReward reward)
        {
            QuestRewardsList.Remove(reward);
            if (QuestRewardsList.Count == 0)
            {
                QuestRewards = false;
            }
        }

        public void AddDataClassField()
        {
            DataClassFields.Add(new DataClassField
            {
                FieldName = $"Field{DataClassFields.Count + 1}",
                FieldType = DataClassFieldType.Bool
            });
        }

        public void RemoveDataClassField(DataClassField field)
        {
            DataClassFields.Remove(field);
        }

        public void CopyFrom(QuestBlueprint source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));

            ClassName = source.ClassName;
            QuestId = source.QuestId;
            QuestTitle = source.QuestTitle;
            QuestDescription = source.QuestDescription;
            AutoBegin = source.AutoBegin;
            CustomIcon = source.CustomIcon;
            IconFileName = source.IconFileName;
            QuestRewards = source.QuestRewards;
            GenerateDataClass = source.GenerateDataClass;
            BlueprintType = source.BlueprintType;
            TrackOnBegin = source.TrackOnBegin;
            AutoCompleteOnAllEntriesComplete = source.AutoCompleteOnAllEntriesComplete;
            Namespace = source.Namespace;
            ModName = source.ModName;
            ModVersion = source.ModVersion;
            ModAuthor = source.ModAuthor;
            GameDeveloper = source.GameDeveloper;
            GameName = source.GameName;
            FolderId = source.FolderId;
            StartCondition = source.StartCondition != null ? new QuestStartCondition
            {
                TriggerType = source.StartCondition.TriggerType,
                NpcId = source.StartCondition.NpcId,
                SceneName = source.StartCondition.SceneName
            } : new QuestStartCondition();

            Objectives.Clear();
            foreach (var objective in source.Objectives)
            {
                Objectives.Add(objective.DeepCopy());
            }

            QuestTriggers.Clear();
            foreach (var trigger in source.QuestTriggers)
            {
                QuestTriggers.Add(trigger.DeepCopy());
            }

            QuestFinishTriggers.Clear();
            foreach (var finishTrigger in source.QuestFinishTriggers)
            {
                QuestFinishTriggers.Add(finishTrigger.DeepCopy());
            }

            QuestRewardsList.Clear();
            if (source.QuestRewardsList != null)
            {
                foreach (var reward in source.QuestRewardsList)
                {
                    QuestRewardsList.Add(reward.DeepCopy());
                }
            }

            DataClassFields.Clear();
            if (source.DataClassFields != null)
            {
                foreach (var field in source.DataClassFields)
                {
                    DataClassFields.Add(field.DeepCopy());
                }
            }
        }

        public QuestBlueprint DeepCopy()
        {
            var copy = new QuestBlueprint();
            copy.CopyFrom(this);
            return copy;
        }

        public override string ToString()
        {
            return DisplayName;
        }
    }

    public enum QuestBlueprintType
    {
        Standard,
        Advanced
    }
}
