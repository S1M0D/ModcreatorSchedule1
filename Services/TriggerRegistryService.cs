using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Schedule1ModdingTool.Models;

namespace Schedule1ModdingTool.Services
{
    /// <summary>
    /// Service for discovering and cataloging available triggers from S1API and mod elements
    /// </summary>
    public class TriggerRegistryService
    {
        private static readonly Lazy<List<TriggerMetadata>> _cachedTriggers = new Lazy<List<TriggerMetadata>>(BuildTriggerCatalog);

        /// <summary>
        /// Gets all available triggers from S1API
        /// </summary>
        public static List<TriggerMetadata> GetAvailableTriggers()
        {
            return _cachedTriggers.Value.ToList(); // Return a copy
        }

        /// <summary>
        /// Gets triggers available for a specific NPC
        /// </summary>
        public static List<TriggerMetadata> GetNpcTriggers(string npcId)
        {
            var allTriggers = GetAvailableTriggers();
            return allTriggers
                .Where(t => t.TriggerType == QuestTriggerType.NPCEventTrigger || 
                           t.TargetAction.Contains("NPC.") ||
                           t.TargetAction.Contains("NPCCustomer.") ||
                           t.TargetAction.Contains("NPCDealer.") ||
                           t.TargetAction.Contains("NPCRelationship."))
                .ToList();
        }

        /// <summary>
        /// Gets triggers available for a specific Quest
        /// </summary>
        public static List<TriggerMetadata> GetQuestTriggers(string questId)
        {
            var allTriggers = GetAvailableTriggers();
            return allTriggers
                .Where(t => t.TriggerType == QuestTriggerType.QuestEventTrigger || 
                           t.TargetAction.StartsWith("Quest.", StringComparison.OrdinalIgnoreCase) ||
                           t.TargetAction.StartsWith("QuestEntry.", StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        /// <summary>
        /// Validates a trigger configuration
        /// </summary>
        public static bool ValidateTrigger(QuestTrigger trigger)
        {
            if (trigger == null)
                return false;

            if (string.IsNullOrWhiteSpace(trigger.TargetAction))
                return false;

            // Validate NPC triggers require NPC ID
            if (trigger.TriggerType == QuestTriggerType.NPCEventTrigger && string.IsNullOrWhiteSpace(trigger.TargetNpcId))
                return false;

            // Validate Quest triggers require Quest ID
            if (trigger.TriggerType == QuestTriggerType.QuestEventTrigger && string.IsNullOrWhiteSpace(trigger.TargetQuestId))
                return false;

            // Validate objective triggers require objective index
            if ((trigger.TriggerTarget == QuestTriggerTarget.ObjectiveStart || 
                 trigger.TriggerTarget == QuestTriggerTarget.ObjectiveFinish) && 
                !trigger.ObjectiveIndex.HasValue)
                return false;

            // Check if trigger exists in catalog
            var availableTriggers = GetAvailableTriggers();
            return availableTriggers.Any(t => t.TargetAction == trigger.TargetAction);
        }

        /// <summary>
        /// Builds the catalog of available triggers by scanning S1API
        /// </summary>
        private static List<TriggerMetadata> BuildTriggerCatalog()
        {
            var catalog = new List<TriggerMetadata>();

            // Add known S1API triggers manually (since we can't reflect into S1API at design time)
            // These are documented triggers from S1API

            // TimeManager triggers
            catalog.Add(new TriggerMetadata
            {
                TriggerType = QuestTriggerType.ActionTrigger,
                TargetAction = "TimeManager.OnDayPass",
                Description = "Triggered when a new in-game day starts",
                SourceClass = "S1API.GameTime.TimeManager",
                Parameters = Array.Empty<string>()
            });

            catalog.Add(new TriggerMetadata
            {
                TriggerType = QuestTriggerType.ActionTrigger,
                TargetAction = "TimeManager.OnWeekPass",
                Description = "Triggered when a new in-game week starts",
                SourceClass = "S1API.GameTime.TimeManager",
                Parameters = Array.Empty<string>()
            });

            catalog.Add(new TriggerMetadata
            {
                TriggerType = QuestTriggerType.ActionTrigger,
                TargetAction = "TimeManager.OnSleepStart",
                Description = "Triggered when the player starts sleeping",
                SourceClass = "S1API.GameTime.TimeManager",
                Parameters = Array.Empty<string>()
            });

            catalog.Add(new TriggerMetadata
            {
                TriggerType = QuestTriggerType.ActionTrigger,
                TargetAction = "TimeManager.OnSleepEnd",
                Description = "Triggered when the player finishes sleeping (parameter: minutes skipped)",
                SourceClass = "S1API.GameTime.TimeManager",
                Parameters = new[] { "int minutes" }
            });

            catalog.Add(new TriggerMetadata
            {
                TriggerType = QuestTriggerType.ActionTrigger,
                TargetAction = "TimeManager.OnTick",
                Description = "Triggered at every tick of gametime",
                SourceClass = "S1API.GameTime.TimeManager",
                Parameters = Array.Empty<string>()
            });

            // NPC triggers (instance-based, require NPC ID)
            catalog.Add(new TriggerMetadata
            {
                TriggerType = QuestTriggerType.NPCEventTrigger,
                TargetAction = "NPC.OnDeath",
                Description = "Triggered when an NPC dies",
                SourceClass = "S1API.Entities.NPC",
                Parameters = Array.Empty<string>(),
                RequiresNpcId = true
            });

            catalog.Add(new TriggerMetadata
            {
                TriggerType = QuestTriggerType.NPCEventTrigger,
                TargetAction = "NPC.OnInventoryChanged",
                Description = "Triggered when an NPC's inventory contents change",
                SourceClass = "S1API.Entities.NPC",
                Parameters = Array.Empty<string>(),
                RequiresNpcId = true
            });

            // NPCRelationship triggers
            catalog.Add(new TriggerMetadata
            {
                TriggerType = QuestTriggerType.NPCEventTrigger,
                TargetAction = "NPCRelationship.OnChanged",
                Description = "Triggered when an NPC's relationship delta changes (parameter: float delta)",
                SourceClass = "S1API.Entities.NPCRelationship",
                Parameters = new[] { "float delta" },
                RequiresNpcId = true
            });

            catalog.Add(new TriggerMetadata
            {
                TriggerType = QuestTriggerType.NPCEventTrigger,
                TargetAction = "NPCRelationship.OnUnlocked",
                Description = "Triggered when an NPC is unlocked (parameters: UnlockType type, bool notify)",
                SourceClass = "S1API.Entities.NPCRelationship",
                Parameters = new[] { "UnlockType type", "bool notify" },
                RequiresNpcId = true
            });

            // NPCCustomer triggers
            catalog.Add(new TriggerMetadata
            {
                TriggerType = QuestTriggerType.NPCEventTrigger,
                TargetAction = "NPCCustomer.OnUnlocked",
                Description = "Triggered when a customer NPC is unlocked",
                SourceClass = "S1API.Entities.NPCCustomer",
                Parameters = Array.Empty<string>(),
                RequiresNpcId = true
            });

            catalog.Add(new TriggerMetadata
            {
                TriggerType = QuestTriggerType.NPCEventTrigger,
                TargetAction = "NPCCustomer.OnDealCompleted",
                Description = "Triggered when a customer completes a deal",
                SourceClass = "S1API.Entities.NPCCustomer",
                Parameters = Array.Empty<string>(),
                RequiresNpcId = true
            });

            catalog.Add(new TriggerMetadata
            {
                TriggerType = QuestTriggerType.NPCEventTrigger,
                TargetAction = "NPCCustomer.OnContractAssigned",
                Description = "Triggered when a contract is assigned to a customer (parameters: payment, quantity, windowStart, windowEnd)",
                SourceClass = "S1API.Entities.NPCCustomer",
                Parameters = new[] { "float payment", "int quantity", "int windowStart", "int windowEnd" },
                RequiresNpcId = true
            });

            // NPCDealer triggers
            catalog.Add(new TriggerMetadata
            {
                TriggerType = QuestTriggerType.NPCEventTrigger,
                TargetAction = "NPCDealer.OnRecruited",
                Description = "Triggered when a dealer NPC is recruited",
                SourceClass = "S1API.Entities.NPCDealer",
                Parameters = Array.Empty<string>(),
                RequiresNpcId = true
            });

            catalog.Add(new TriggerMetadata
            {
                TriggerType = QuestTriggerType.NPCEventTrigger,
                TargetAction = "NPCDealer.OnContractAccepted",
                Description = "Triggered when a dealer accepts a contract",
                SourceClass = "S1API.Entities.NPCDealer",
                Parameters = Array.Empty<string>(),
                RequiresNpcId = true
            });

            catalog.Add(new TriggerMetadata
            {
                TriggerType = QuestTriggerType.NPCEventTrigger,
                TargetAction = "NPCDealer.OnRecommended",
                Description = "Triggered when a dealer is recommended",
                SourceClass = "S1API.Entities.NPCDealer",
                Parameters = Array.Empty<string>(),
                RequiresNpcId = true
            });

            // Player triggers
            catalog.Add(new TriggerMetadata
            {
                TriggerType = QuestTriggerType.ActionTrigger,
                TargetAction = "Player.OnDeath",
                Description = "Triggered when the local player dies",
                SourceClass = "S1API.Entities.Player",
                Parameters = Array.Empty<string>()
            });

            catalog.Add(new TriggerMetadata
            {
                TriggerType = QuestTriggerType.ActionTrigger,
                TargetAction = "Player.PlayerSpawned",
                Description = "Triggered when any player spawns (parameter: Player player)",
                SourceClass = "S1API.Entities.Player",
                Parameters = new[] { "Player player" }
            });

            catalog.Add(new TriggerMetadata
            {
                TriggerType = QuestTriggerType.ActionTrigger,
                TargetAction = "Player.LocalPlayerSpawned",
                Description = "Triggered when the local player spawns (parameter: Player player)",
                SourceClass = "S1API.Entities.Player",
                Parameters = new[] { "Player player" }
            });

            catalog.Add(new TriggerMetadata
            {
                TriggerType = QuestTriggerType.ActionTrigger,
                TargetAction = "Player.PlayerDespawned",
                Description = "Triggered when any player despawns (parameter: Player player)",
                SourceClass = "S1API.Entities.Player",
                Parameters = new[] { "Player player" }
            });

            // Quest triggers (instance-based, require Quest ID)
            catalog.Add(new TriggerMetadata
            {
                TriggerType = QuestTriggerType.QuestEventTrigger,
                TargetAction = "Quest.OnComplete",
                Description = "Triggered when a quest completes",
                SourceClass = "S1API.Quests.Quest",
                Parameters = Array.Empty<string>(),
                RequiresQuestId = true
            });

            catalog.Add(new TriggerMetadata
            {
                TriggerType = QuestTriggerType.QuestEventTrigger,
                TargetAction = "Quest.OnFail",
                Description = "Triggered when a quest fails",
                SourceClass = "S1API.Quests.Quest",
                Parameters = Array.Empty<string>(),
                RequiresQuestId = true
            });

            catalog.Add(new TriggerMetadata
            {
                TriggerType = QuestTriggerType.QuestEventTrigger,
                TargetAction = "Quest.OnCancel",
                Description = "Triggered when a quest is cancelled",
                SourceClass = "S1API.Quests.Quest",
                Parameters = Array.Empty<string>(),
                RequiresQuestId = true
            });

            catalog.Add(new TriggerMetadata
            {
                TriggerType = QuestTriggerType.QuestEventTrigger,
                TargetAction = "Quest.OnExpire",
                Description = "Triggered when a quest expires",
                SourceClass = "S1API.Quests.Quest",
                Parameters = Array.Empty<string>(),
                RequiresQuestId = true
            });

            catalog.Add(new TriggerMetadata
            {
                TriggerType = QuestTriggerType.QuestEventTrigger,
                TargetAction = "Quest.OnBegin",
                Description = "Triggered when a quest begins",
                SourceClass = "S1API.Quests.Quest",
                Parameters = Array.Empty<string>(),
                RequiresQuestId = true
            });

            // QuestEntry triggers (instance-based, require Quest ID)
            catalog.Add(new TriggerMetadata
            {
                TriggerType = QuestTriggerType.QuestEventTrigger,
                TargetAction = "QuestEntry.OnComplete",
                Description = "Triggered when a quest entry/objective completes",
                SourceClass = "S1API.Quests.QuestEntry",
                Parameters = Array.Empty<string>(),
                RequiresQuestId = true
            });

            catalog.Add(new TriggerMetadata
            {
                TriggerType = QuestTriggerType.QuestEventTrigger,
                TargetAction = "QuestEntry.OnBegin",
                Description = "Triggered when a quest entry/objective begins",
                SourceClass = "S1API.Quests.QuestEntry",
                Parameters = Array.Empty<string>(),
                RequiresQuestId = true
            });

            return catalog;
        }
    }

    /// <summary>
    /// Metadata about an available trigger
    /// </summary>
    public class TriggerMetadata
    {
        /// <summary>
        /// The type of trigger
        /// </summary>
        public QuestTriggerType TriggerType { get; set; }

        /// <summary>
        /// The target action string (e.g., "TimeManager.OnDayPass")
        /// </summary>
        public string TargetAction { get; set; } = "";

        /// <summary>
        /// Human-readable description of what the trigger does
        /// </summary>
        public string Description { get; set; } = "";

        /// <summary>
        /// The source class that provides this trigger
        /// </summary>
        public string SourceClass { get; set; } = "";

        /// <summary>
        /// Parameters that the trigger action accepts (if any)
        /// </summary>
        public string[] Parameters { get; set; } = Array.Empty<string>();

        /// <summary>
        /// Whether this trigger requires an NPC ID to be specified
        /// </summary>
        public bool RequiresNpcId { get; set; }

        /// <summary>
        /// Whether this trigger requires a Quest ID to be specified
        /// </summary>
        public bool RequiresQuestId { get; set; }

        public override string ToString()
        {
            return $"{TargetAction} - {Description}";
        }
    }
}

