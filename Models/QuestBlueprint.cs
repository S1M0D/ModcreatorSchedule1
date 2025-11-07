using System;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Newtonsoft.Json;

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

        [JsonProperty("questRewards")]
        public bool QuestRewards
        {
            get => _questRewards;
            set => SetProperty(ref _questRewards, value);
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

        [JsonIgnore]
        public string DisplayName => string.IsNullOrEmpty(QuestTitle) ? ClassName : QuestTitle;

        [JsonIgnore]
        public string Summary => $"{DisplayName} ({Objectives.Count} objectives)";

        public QuestBlueprint()
        {
            // Add default objective
            Objectives.Add(new QuestObjective("objective_1", "Complete objective"));
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

        public void CopyFrom(QuestBlueprint source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));

            ClassName = source.ClassName;
            QuestId = source.QuestId;
            QuestTitle = source.QuestTitle;
            QuestDescription = source.QuestDescription;
            AutoBegin = source.AutoBegin;
            CustomIcon = source.CustomIcon;
            QuestRewards = source.QuestRewards;
            GenerateDataClass = source.GenerateDataClass;
            BlueprintType = source.BlueprintType;
            Namespace = source.Namespace;
            ModName = source.ModName;
            ModVersion = source.ModVersion;
            ModAuthor = source.ModAuthor;
            GameDeveloper = source.GameDeveloper;
            GameName = source.GameName;
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
