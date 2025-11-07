using Newtonsoft.Json;

namespace Schedule1ModdingTool.Models
{
    /// <summary>
    /// Configuration for quest start conditions based on trigger type
    /// </summary>
    public class QuestStartCondition : ObservableObject
    {
        private QuestStartTrigger _triggerType = QuestStartTrigger.AutoStart;
        private string _npcId = "";
        private string _sceneName = "Main";

        [JsonProperty("triggerType")]
        public QuestStartTrigger TriggerType
        {
            get => _triggerType;
            set => SetProperty(ref _triggerType, value);
        }

        /// <summary>
        /// NPC ID for NPCDealCompleted trigger type
        /// </summary>
        [JsonProperty("npcId")]
        public string NpcId
        {
            get => _npcId;
            set => SetProperty(ref _npcId, value);
        }

        /// <summary>
        /// Scene name for SceneInit trigger type (default: "Main")
        /// </summary>
        [JsonProperty("sceneName")]
        public string SceneName
        {
            get => _sceneName;
            set => SetProperty(ref _sceneName, value);
        }
    }
}

