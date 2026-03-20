using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Schedule1ModdingTool.Models
{
    /// <summary>
    /// Represents a trigger specifically for quest completion/finish events
    /// </summary>
    public class QuestFinishTrigger : QuestTrigger
    {
        private QuestFinishType _finishType = QuestFinishType.Complete;

        /// <summary>
        /// How the quest should finish (Complete, Fail, Cancel, Expire)
        /// </summary>
        [JsonProperty("finishType")]
        [JsonConverter(typeof(StringEnumConverter))]
        public QuestFinishType FinishType
        {
            get => _finishType;
            set => SetProperty(ref _finishType, value);
        }

        public QuestFinishTrigger() : base()
        {
            TriggerTarget = QuestTriggerTarget.QuestFinish;
        }

        public QuestFinishTrigger(QuestTriggerType triggerType, string targetAction, QuestFinishType finishType)
            : base(triggerType, targetAction, QuestTriggerTarget.QuestFinish)
        {
            FinishType = finishType;
        }

        public new QuestFinishTrigger DeepCopy()
        {
            return new QuestFinishTrigger
            {
                TriggerType = TriggerType,
                TargetAction = TargetAction,
                TargetNpcId = TargetNpcId,
                TargetQuestId = TargetQuestId,
                TargetQuestEntryIndex = TargetQuestEntryIndex,
                TriggerTarget = TriggerTarget,
                ObjectiveIndex = ObjectiveIndex,
                FinishType = FinishType
            };
        }
    }

    /// <summary>
    /// Types of quest finish actions
    /// </summary>
    public enum QuestFinishType
    {
        /// <summary>
        /// Complete the quest successfully
        /// </summary>
        Complete,

        /// <summary>
        /// Fail the quest
        /// </summary>
        Fail,

        /// <summary>
        /// Cancel the quest
        /// </summary>
        Cancel,

        /// <summary>
        /// Expire the quest
        /// </summary>
        Expire,

        /// <summary>
        /// End the quest immediately using Quest.End()
        /// </summary>
        End
    }
}

