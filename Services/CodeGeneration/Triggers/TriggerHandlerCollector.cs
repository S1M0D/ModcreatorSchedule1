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

            CollectQuestStartTriggers(quest, handlers, usedNames, ref handlerIndex);
            CollectQuestFinishTriggers(quest, handlers, usedNames, ref handlerIndex);
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

            CollectQuestTriggerGroup(
                quest.QuestTriggers.Where(t => t.TriggerTarget == QuestTriggerTarget.QuestStart),
                handlers,
                usedNames,
                ref handlerIndex,
                TriggerCategory.QuestStart,
                _ => "Begin()");
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

            CollectQuestTriggerGroup(
                quest.QuestFinishTriggers,
                handlers,
                usedNames,
                ref handlerIndex,
                TriggerCategory.QuestFinish,
                trigger => GetFinishActionMethod(trigger.FinishType));
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

                CollectObjectiveTriggerGroup(
                    objective.StartTriggers,
                    i,
                    TriggerCategory.ObjectiveStart,
                    handlers,
                    usedNames,
                    ref handlerIndex);

                CollectObjectiveTriggerGroup(
                    objective.FinishTriggers,
                    i,
                    TriggerCategory.ObjectiveFinish,
                    handlers,
                    usedNames,
                    ref handlerIndex);
            }
        }

        private void CollectQuestTriggerGroup<TTrigger>(
            IEnumerable<TTrigger> triggers,
            List<TriggerHandlerInfo> handlers,
            HashSet<string> usedNames,
            ref int handlerIndex,
            TriggerCategory triggerCategory,
            Func<TTrigger, string> actionMethodSelector)
            where TTrigger : QuestTrigger
        {
            foreach (var trigger in triggers)
            {
                if (!RequiresHandlerField(trigger))
                {
                    continue;
                }

                handlers.Add(new TriggerHandlerInfo
                {
                    Trigger = trigger,
                    FieldName = GenerateHandlerFieldName(trigger, usedNames, ref handlerIndex),
                    ActionMethod = actionMethodSelector(trigger),
                    TriggerCategory = triggerCategory
                });
            }
        }

        private void CollectObjectiveTriggerGroup(
            IEnumerable<QuestObjectiveTrigger>? triggers,
            int objectiveIndex,
            TriggerCategory triggerCategory,
            List<TriggerHandlerInfo> handlers,
            HashSet<string> usedNames,
            ref int handlerIndex)
        {
            if (triggers?.Any() != true)
                return;

            foreach (var trigger in triggers)
            {
                if (!RequiresHandlerField(trigger))
                {
                    continue;
                }

                handlers.Add(new TriggerHandlerInfo
                {
                    Trigger = trigger,
                    FieldName = GenerateHandlerFieldName(trigger, usedNames, ref handlerIndex),
                    ActionMethod = GetObjectiveActionMethod(trigger.ActionType),
                    ObjectiveIndex = objectiveIndex,
                    TriggerCategory = triggerCategory
                });
            }
        }

        private static bool RequiresHandlerField(QuestTrigger trigger)
        {
            return !string.IsNullOrWhiteSpace(trigger.TargetAction)
                && (trigger.TriggerType == QuestTriggerType.NPCEventTrigger
                    || trigger.TriggerType == QuestTriggerType.QuestEventTrigger);
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

        private static string GetFinishActionMethod(QuestFinishType finishType)
        {
            return finishType switch
            {
                QuestFinishType.Complete => "Complete()",
                QuestFinishType.Fail => "Fail()",
                QuestFinishType.Cancel => "Cancel()",
                QuestFinishType.Expire => "Expire()",
                QuestFinishType.End => "End()",
                _ => "Complete()"
            };
        }

        private static string GetObjectiveActionMethod(QuestObjectiveTriggerActionType actionType)
        {
            return actionType switch
            {
                QuestObjectiveTriggerActionType.Begin => "Begin()",
                QuestObjectiveTriggerActionType.Complete => "Complete()",
                QuestObjectiveTriggerActionType.SetInactive => "SetState(QuestState.Inactive)",
                QuestObjectiveTriggerActionType.Fail => "SetState(QuestState.Failed)",
                QuestObjectiveTriggerActionType.Cancel => "SetState(QuestState.Cancelled)",
                QuestObjectiveTriggerActionType.Expire => "SetState(QuestState.Expired)",
                _ => "Begin()"
            };
        }
    }
}
