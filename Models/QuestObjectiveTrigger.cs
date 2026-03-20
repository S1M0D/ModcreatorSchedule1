using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.Runtime.Serialization;

namespace Schedule1ModdingTool.Models
{
    /// <summary>
    /// Represents a trigger specifically for quest objectives
    /// Extends QuestTrigger with objective-specific context
    /// </summary>
    public class QuestObjectiveTrigger : QuestTrigger
    {
        private string _objectiveName = "";
        private QuestObjectiveTriggerActionType _actionType = QuestObjectiveTriggerActionType.Begin;
        [JsonIgnore]
        private bool _actionTypeWasExplicitlySet;

        /// <summary>
        /// The name of the objective this trigger targets (alternative to ObjectiveIndex)
        /// </summary>
        [JsonProperty("objectiveName")]
        public string ObjectiveName
        {
            get => _objectiveName;
            set => SetProperty(ref _objectiveName, value);
        }

        /// <summary>
        /// The quest-entry action to perform when this trigger fires.
        /// </summary>
        [JsonProperty("actionType")]
        [JsonConverter(typeof(StringEnumConverter))]
        public QuestObjectiveTriggerActionType ActionType
        {
            get => _actionType;
            set
            {
                _actionTypeWasExplicitlySet = true;
                SetProperty(ref _actionType, value);
            }
        }

        public QuestObjectiveTrigger() : base()
        {
        }

        public QuestObjectiveTrigger(QuestTriggerType triggerType, string targetAction, QuestTriggerTarget triggerTarget, string objectiveName)
            : base(triggerType, targetAction, triggerTarget)
        {
            ObjectiveName = objectiveName;
        }

        public new QuestObjectiveTrigger DeepCopy()
        {
            return new QuestObjectiveTrigger
            {
                TriggerType = TriggerType,
                TargetAction = TargetAction,
                TargetNpcId = TargetNpcId,
                TargetQuestId = TargetQuestId,
                TargetQuestEntryIndex = TargetQuestEntryIndex,
                TriggerTarget = TriggerTarget,
                ObjectiveIndex = ObjectiveIndex,
                ObjectiveName = ObjectiveName,
                ActionType = ActionType
            };
        }

        [OnDeserialized]
        private void OnDeserialized(StreamingContext context)
        {
            if (_actionTypeWasExplicitlySet)
            {
                return;
            }

            _actionType = TriggerTarget == QuestTriggerTarget.ObjectiveFinish
                ? QuestObjectiveTriggerActionType.Complete
                : QuestObjectiveTriggerActionType.Begin;
        }
    }

    /// <summary>
    /// Quest-entry actions supported by S1API quest entry runtime methods.
    /// </summary>
    public enum QuestObjectiveTriggerActionType
    {
        Begin,
        Complete,
        SetInactive,
        Fail,
        Cancel,
        Expire
    }
}

