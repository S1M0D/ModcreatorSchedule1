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