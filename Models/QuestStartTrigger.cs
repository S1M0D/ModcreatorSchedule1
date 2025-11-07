using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Schedule1ModdingTool.Models
{
    /// <summary>
    /// Defines how a quest should be started
    /// </summary>
    [JsonConverter(typeof(StringEnumConverter))]
    public enum QuestStartTrigger
    {
        /// <summary>
        /// Quest starts automatically if not already completed (checks save data)
        /// </summary>
        AutoStart,

        /// <summary>
        /// Quest starts when selling to a specific NPC customer
        /// </summary>
        NPCDealCompleted,

        /// <summary>
        /// Quest starts when Main scene initializes (if not completed)
        /// </summary>
        SceneInit,

        /// <summary>
        /// Quest must be started manually via code
        /// </summary>
        Manual
    }
}

