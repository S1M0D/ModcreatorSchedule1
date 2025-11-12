using System;
using System.Collections.Generic;
using System.Linq;
using Schedule1ModdingTool.Models;
using Schedule1ModdingTool.Services.CodeGeneration.Common;

namespace Schedule1ModdingTool.Services.CodeGeneration.Triggers
{
    /// <summary>
    /// Collects and organizes trigger handlers from a quest blueprint.
    /// Scans quest-level and objective-level triggers to build handler metadata.
    /// </summary>
    public class TriggerHandlerCollector
    {
        /// <summary>
        /// Collects all trigger handlers from a quest blueprint.
        /// </summary>
        /// <param name="quest">The quest blueprint to scan.</param>
        /// <returns>List of trigger handler metadata.</returns>
        public List<TriggerHandlerInfo> CollectHandlers(QuestBlueprint quest)
        {
            if (quest == null)
                throw new ArgumentNullException(nameof(quest));

            var handlers = new List<TriggerHandlerInfo>();
            var usedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            int handlerIndex = 0;

            // Quest start triggers
            CollectQuestStartTriggers(quest, handlers, usedNames, ref handlerIndex);

            // Quest finish triggers
            CollectQuestFinishTriggers(quest, handlers, usedNames, ref handlerIndex);

            // Objective triggers
            CollectObjectiveTriggers(quest, handlers, usedNames, ref handlerIndex);

            return handlers;
        }

        /// <summary>
        /// Collects triggers that start the quest.
        /// </summary>
        private void CollectQuestStartTriggers(
            QuestBlueprint quest,
            List<TriggerHandlerInfo> handlers,
            HashSet<string> usedNames,
            ref int handlerIndex)
        {
            if (quest.QuestTriggers?.Any(t => t.TriggerTarget == QuestTriggerTarget.QuestStart) != true)
                return;

            foreach (var trigger in quest.QuestTriggers.Where(t => t.TriggerTarget == QuestTriggerTarget.QuestStart))
            {
                if ((trigger.TriggerType == QuestTriggerType.NPCEventTrigger || trigger.TriggerType == QuestTriggerType.QuestEventTrigger) &&
                    !string.IsNullOrWhiteSpace(trigger.TargetAction))
                {
                    var handlerName = GenerateHandlerFieldName(trigger, usedNames, ref handlerIndex);
                    handlers.Add(new TriggerHandlerInfo
                    {
                        Trigger = trigger,
                        FieldName = handlerName,
                        ActionMethod = "Begin()",
                        TriggerCategory = TriggerCategory.QuestStart
                    });
                }
            }
        }

        /// <summary>
        /// Collects triggers that finish the quest (complete, fail, cancel, expire).
        /// </summary>
        private void CollectQuestFinishTriggers(
            QuestBlueprint quest,
            List<TriggerHandlerInfo> handlers,
            HashSet<string> usedNames,
            ref int handlerIndex)
        {
            if (quest.QuestFinishTriggers?.Any() != true)
                return;

            foreach (var trigger in quest.QuestFinishTriggers)
            {
                if ((trigger.TriggerType == QuestTriggerType.NPCEventTrigger || trigger.TriggerType == QuestTriggerType.QuestEventTrigger) &&
                    !string.IsNullOrWhiteSpace(trigger.TargetAction))
                {
                    var finishMethod = trigger.FinishType switch
                    {
                        QuestFinishType.Complete => "Complete()",
                        QuestFinishType.Fail => "Fail()",
                        QuestFinishType.Cancel => "Cancel()",
                        QuestFinishType.Expire => "Expire()",
                        _ => "Complete()"
                    };

                    var handlerName = GenerateHandlerFieldName(trigger, usedNames, ref handlerIndex);
                    handlers.Add(new TriggerHandlerInfo
                    {
                        Trigger = trigger,
                        FieldName = handlerName,
                        ActionMethod = finishMethod,
                        TriggerCategory = TriggerCategory.QuestFinish
                    });
                }
            }
        }

        /// <summary>
        /// Collects triggers for quest objectives (start and finish).
        /// </summary>
        private void CollectObjectiveTriggers(
            QuestBlueprint quest,
            List<TriggerHandlerInfo> handlers,
            HashSet<string> usedNames,
            ref int handlerIndex)
        {
            if (quest.Objectives?.Any() != true)
                return;

            for (int i = 0; i < quest.Objectives.Count; i++)
            {
                var objective = quest.Objectives[i];

                // Objective start triggers
                if (objective.StartTriggers?.Any() == true)
                {
                    foreach (var trigger in objective.StartTriggers)
                    {
                        if ((trigger.TriggerType == QuestTriggerType.NPCEventTrigger || trigger.TriggerType == QuestTriggerType.QuestEventTrigger) &&
                            !string.IsNullOrWhiteSpace(trigger.TargetAction))
                        {
                            var handlerName = GenerateHandlerFieldName(trigger, usedNames, ref handlerIndex);
                            handlers.Add(new TriggerHandlerInfo
                            {
                                Trigger = trigger,
                                FieldName = handlerName,
                                ActionMethod = "Begin()",
                                ObjectiveIndex = i,
                                TriggerCategory = TriggerCategory.ObjectiveStart
                            });
                        }
                    }
                }

                // Objective finish triggers
                if (objective.FinishTriggers?.Any() == true)
                {
                    foreach (var trigger in objective.FinishTriggers)
                    {
                        if ((trigger.TriggerType == QuestTriggerType.NPCEventTrigger || trigger.TriggerType == QuestTriggerType.QuestEventTrigger) &&
                            !string.IsNullOrWhiteSpace(trigger.TargetAction))
                        {
                            var handlerName = GenerateHandlerFieldName(trigger, usedNames, ref handlerIndex);
                            handlers.Add(new TriggerHandlerInfo
                            {
                                Trigger = trigger,
                                FieldName = handlerName,
                                ActionMethod = "Complete()",
                                ObjectiveIndex = i,
                                TriggerCategory = TriggerCategory.ObjectiveFinish
                            });
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Generates a unique field name for a trigger handler.
        /// Extracts the event name from the TargetAction and creates a handler field name.
        /// </summary>
        private string GenerateHandlerFieldName(QuestTrigger trigger, HashSet<string> usedNames, ref int handlerIndex)
        {
            if (string.IsNullOrWhiteSpace(trigger.TargetAction))
                return IdentifierSanitizer.EnsureUniqueIdentifier("_triggerHandler", usedNames, ++handlerIndex);

            var actionParts = trigger.TargetAction.Split('.');
            var eventName = actionParts.Length >= 2 ? actionParts[1] : trigger.TargetAction;
            var baseName = $"_{IdentifierSanitizer.MakeSafeIdentifier(eventName, "trigger")}Handler";
            return IdentifierSanitizer.EnsureUniqueIdentifier(baseName, usedNames, ++handlerIndex);
        }
    }
}
