using Schedule1ModdingTool.Models;
using Schedule1ModdingTool.Services;
using Schedule1ModdingTool.Services.CodeGeneration.Abstractions;
using Schedule1ModdingTool.Services.CodeGeneration.Common;
using Schedule1ModdingTool.Services.CodeGeneration.Quest;

namespace Schedule1ModdingTool.Services.CodeGeneration.Triggers
{
    /// <summary>
    /// Generates the SubscribeToTriggers() method and trigger subscription code.
    /// Handles both quest-level and objective-level triggers.
    /// </summary>
    public class TriggerSubscriptionGenerator
    {
        private readonly QuestEntryFieldGenerator _entryFieldGenerator;

        public TriggerSubscriptionGenerator(QuestEntryFieldGenerator entryFieldGenerator)
        {
            _entryFieldGenerator = entryFieldGenerator ?? throw new ArgumentNullException(nameof(entryFieldGenerator));
        }

        /// <summary>
        /// Generates the complete SubscribeToTriggers() method.
        /// </summary>
        public void Generate(
            ICodeBuilder builder,
            QuestBlueprint quest,
            string className,
            string targetNamespace,
            List<TriggerHandlerInfo> handlerInfos)
        {
            if (builder == null)
                throw new ArgumentNullException(nameof(builder));
            if (quest == null)
                throw new ArgumentNullException(nameof(quest));

            var rootNamespace = GetRootNamespace(targetNamespace);
            var hasTriggers = (quest.QuestTriggers?.Any() == true)
                || (quest.QuestFinishTriggers?.Any() == true)
                || (quest.Objectives?.Any(o => o.StartTriggers?.Any() == true || o.FinishTriggers?.Any() == true) == true);
            var hasRewards = quest.QuestRewards && quest.QuestRewardsList != null && quest.QuestRewardsList.Count > 0;
            var hasCompletionGlobalStateSetters = quest.CompletionGlobalStateSetters.Count > 0;
            var hasFailureGlobalStateSetters = quest.FailureGlobalStateSetters.Count > 0;
            var hasHooks = quest.GenerateHookScaffold;

            builder.AppendComment("Generated from: Quest.QuestTriggers, Quest.QuestFinishTriggers, Quest.Objectives[].StartTriggers, Quest.Objectives[].FinishTriggers");
            builder.AppendBlockComment("Subscribes to triggers for this quest and its objectives.");

            builder.OpenBlock("private void SubscribeToTriggers()");

            if (!hasTriggers && !hasRewards && !hasCompletionGlobalStateSetters && !hasFailureGlobalStateSetters && !hasHooks)
            {
                builder.AppendComment("No triggers configured for this quest");
                builder.CloseBlock();
                builder.AppendLine();
                return;
            }

            GenerateQuestStartTriggers(builder, quest, handlerInfos, rootNamespace);
            GenerateQuestFinishTriggers(builder, quest, handlerInfos, rootNamespace);
            GenerateObjectiveTriggers(builder, quest, handlerInfos, rootNamespace);

            if (hasRewards)
            {
                GenerateQuestRewardSubscription(builder, quest);
            }

            if (hasCompletionGlobalStateSetters || hasFailureGlobalStateSetters)
            {
                GenerateQuestGlobalStateSetterSubscriptions(builder, quest);
            }

            if (hasHooks)
            {
                GenerateQuestHookSubscriptions(builder);
            }

            builder.CloseBlock();
            builder.AppendLine();
        }

        /// <summary>
        /// Generates subscriptions for quest start triggers.
        /// </summary>
        private void GenerateQuestStartTriggers(
            ICodeBuilder builder,
            QuestBlueprint quest,
            List<TriggerHandlerInfo> handlerInfos,
            string rootNamespace)
        {
            if (quest.QuestTriggers?.Any(t => t.TriggerTarget == QuestTriggerTarget.QuestStart) != true)
                return;

            builder.AppendComment("Generated from: Quest.QuestTriggers[] (where TriggerTarget = QuestStart)");
            foreach (var trigger in quest.QuestTriggers.Where(t => t.TriggerTarget == QuestTriggerTarget.QuestStart))
            {
                var handlerInfo = FindQuestHandlerInfo(handlerInfos, trigger, TriggerCategory.QuestStart);
                AppendTriggerSubscription(builder, trigger, handlerInfo?.FieldName, handlerInfo?.ActionMethod ?? "Begin()", rootNamespace);
            }
        }

        /// <summary>
        /// Generates subscriptions for quest finish triggers.
        /// </summary>
        private void GenerateQuestFinishTriggers(
            ICodeBuilder builder,
            QuestBlueprint quest,
            List<TriggerHandlerInfo> handlerInfos,
            string rootNamespace)
        {
            if (quest.QuestFinishTriggers?.Any() != true)
                return;

            builder.AppendComment("Generated from: Quest.QuestFinishTriggers[]");
            foreach (var trigger in quest.QuestFinishTriggers)
            {
                var finishMethod = trigger.FinishType switch
                {
                    QuestFinishType.Complete => "Complete()",
                    QuestFinishType.Fail => "Fail()",
                    QuestFinishType.Cancel => "Cancel()",
                    QuestFinishType.Expire => "Expire()",
                    QuestFinishType.End => "End()",
                    _ => "Complete()"
                };

                var handlerInfo = FindQuestHandlerInfo(handlerInfos, trigger, TriggerCategory.QuestFinish);
                AppendTriggerSubscription(builder, trigger, handlerInfo?.FieldName, finishMethod, rootNamespace);
            }
        }

        /// <summary>
        /// Generates subscriptions for objective-level triggers.
        /// </summary>
        private void GenerateObjectiveTriggers(
            ICodeBuilder builder,
            QuestBlueprint quest,
            List<TriggerHandlerInfo> handlerInfos,
            string rootNamespace)
        {
            if (quest.Objectives?.Any() != true)
                return;

            builder.AppendComment("Generated from: Quest.Objectives[].StartTriggers, Quest.Objectives[].FinishTriggers");
            var objectiveNames = _entryFieldGenerator.GetAllObjectiveVariableNames(quest);

            for (int i = 0; i < quest.Objectives.Count; i++)
            {
                var objective = quest.Objectives[i];
                var objectiveVar = objectiveNames[i];

                if (objective.StartTriggers?.Any() == true)
                {
                    GenerateObjectiveTriggerGroup(
                        builder,
                        objective.StartTriggers,
                        i,
                        objectiveVar,
                        TriggerCategory.ObjectiveStart,
                        $"Generated from: Objectives[{i}].StartTriggers[]",
                        handlerInfos,
                        rootNamespace);
                }

                if (objective.FinishTriggers?.Any() == true)
                {
                    GenerateObjectiveTriggerGroup(
                        builder,
                        objective.FinishTriggers,
                        i,
                        objectiveVar,
                        TriggerCategory.ObjectiveFinish,
                        $"Generated from: Objectives[{i}].FinishTriggers[]",
                        handlerInfos,
                        rootNamespace);
                }
            }
        }

        private void GenerateObjectiveTriggerGroup(
            ICodeBuilder builder,
            IEnumerable<QuestObjectiveTrigger> triggers,
            int objectiveIndex,
            string objectiveVar,
            TriggerCategory triggerCategory,
            string sourceComment,
            List<TriggerHandlerInfo> handlerInfos,
            string rootNamespace)
        {
            builder.AppendComment(sourceComment);
            foreach (var trigger in triggers)
            {
                var handlerInfo = FindObjectiveHandlerInfo(handlerInfos, objectiveIndex, triggerCategory, trigger);
                AppendObjectiveTriggerSubscription(
                    builder,
                    trigger,
                    objectiveVar,
                    handlerInfo?.FieldName,
                    GetObjectiveActionMethod(trigger.ActionType),
                    rootNamespace);
            }
        }

        /// <summary>
        /// Appends a quest-level trigger subscription.
        /// </summary>
        private void AppendTriggerSubscription(
            ICodeBuilder builder,
            QuestTrigger trigger,
            string? handlerFieldName,
            string actionMethod,
            string rootNamespace)
        {
            if (string.IsNullOrWhiteSpace(trigger.TargetAction))
                return;

            builder.AppendComment($"Trigger: {CodeFormatter.EscapeString(trigger.TargetAction)} -> {trigger.TriggerTarget}");
            AppendSubscriptionWithErrorHandling(
                builder,
                $"Failed to subscribe to trigger {trigger.TargetAction}",
                () => GenerateTriggerSubscriptionCore(builder, trigger, handlerFieldName, actionMethod, null, rootNamespace));
        }

        /// <summary>
        /// Appends an objective-level trigger subscription.
        /// </summary>
        private void AppendObjectiveTriggerSubscription(
            ICodeBuilder builder,
            QuestObjectiveTrigger trigger,
            string objectiveVar,
            string? handlerFieldName,
            string actionMethod,
            string rootNamespace)
        {
            if (string.IsNullOrWhiteSpace(trigger.TargetAction))
                return;

            builder.AppendComment($"Objective trigger: {CodeFormatter.EscapeString(trigger.TargetAction)} -> {objectiveVar}.{actionMethod}");
            AppendSubscriptionWithErrorHandling(
                builder,
                $"Failed to subscribe to objective trigger {trigger.TargetAction}",
                () => GenerateTriggerSubscriptionCore(builder, trigger, handlerFieldName, actionMethod, objectiveVar, rootNamespace));
        }

        private void GenerateTriggerSubscriptionCore(
            ICodeBuilder builder,
            QuestTrigger trigger,
            string? handlerFieldName,
            string actionMethod,
            string? objectiveVar,
            string rootNamespace)
        {
            if (trigger.TriggerType == QuestTriggerType.NPCEventTrigger && !string.IsNullOrWhiteSpace(trigger.TargetNpcId))
            {
                GenerateNpcTriggerSubscription(builder, trigger, handlerFieldName, actionMethod, objectiveVar);
                return;
            }

            if (trigger.TriggerType == QuestTriggerType.QuestEventTrigger && !string.IsNullOrWhiteSpace(trigger.TargetQuestId))
            {
                GenerateQuestTriggerSubscription(builder, trigger, handlerFieldName, actionMethod, objectiveVar, rootNamespace);
                return;
            }

            GenerateStaticActionTriggerSubscription(builder, trigger, actionMethod, objectiveVar);
        }

        /// <summary>
        /// Generates subscription code for NPC instance triggers.
        /// </summary>
        private void GenerateNpcTriggerSubscription(
            ICodeBuilder builder,
            QuestTrigger trigger,
            string? handlerFieldName,
            string actionMethod,
            string? objectiveVar)
        {
            var npcId = CodeFormatter.EscapeString(trigger.TargetNpcId);
            var eventPath = GetNpcEventPath(trigger.TargetAction);
            var lambdaSignature = GetLambdaSignature(trigger.TargetAction);

            builder.AppendLine($"var npc = NPC.All.FirstOrDefault(n => n.ID == \"{npcId}\");");
            builder.OpenBlock("if (npc == null)");
            builder.AppendLine($"MelonLogger.Warning($\"[Quest] NPC '{npcId}' not found when subscribing to trigger '{CodeFormatter.EscapeString(trigger.TargetAction)}'\");");
            builder.CloseBlock();
            builder.OpenBlock("else");
            AppendEventSubscription(builder, eventPath, lambdaSignature, handlerFieldName, actionMethod, objectiveVar);
            builder.CloseBlock();
        }

        /// <summary>
        /// Generates subscription code for Quest instance triggers.
        /// </summary>
        private void GenerateQuestTriggerSubscription(
            ICodeBuilder builder,
            QuestTrigger trigger,
            string? handlerFieldName,
            string actionMethod,
            string? objectiveVar,
            string rootNamespace)
        {
            var questId = CodeFormatter.EscapeString(trigger.TargetQuestId);
            var (componentType, eventName) = ParseQuestTargetAction(trigger.TargetAction);

            if (!IsSupportedQuestTrigger(componentType, eventName))
            {
                builder.AppendLine($"MelonLogger.Warning(\"[Quest] Trigger '{CodeFormatter.EscapeString(trigger.TargetAction)}' is not supported by S1API 3.0.0 and was skipped.\");");
                return;
            }

            var lambdaSignature = GetLambdaSignature(trigger.TargetAction);
            AppendQuestLookup(builder, trigger.TargetQuestId, questId, rootNamespace);

            builder.OpenBlock("if (quest == null)");
            builder.AppendLine($"MelonLogger.Warning($\"[Quest] Quest '{questId}' not found when subscribing to trigger '{CodeFormatter.EscapeString(trigger.TargetAction)}'\");");
            builder.CloseBlock();
            builder.OpenBlock("else");

            if (componentType == "QuestEntry")
            {
                AppendQuestEntryTriggerSubscription(
                    builder,
                    trigger,
                    questId,
                    eventName,
                    lambdaSignature,
                    handlerFieldName,
                    actionMethod,
                    objectiveVar);
            }
            else
            {
                AppendEventSubscription(builder, $"quest.{eventName}", lambdaSignature, handlerFieldName, actionMethod, objectiveVar);
            }

            builder.CloseBlock();
        }

        private void AppendQuestEntryTriggerSubscription(
            ICodeBuilder builder,
            QuestTrigger trigger,
            string questId,
            string eventName,
            string lambdaSignature,
            string? handlerFieldName,
            string actionMethod,
            string? objectiveVar)
        {
            var eventPath = $"entry.{eventName}";

            if (trigger.TargetQuestEntryIndex.HasValue)
            {
                var entryIndex = trigger.TargetQuestEntryIndex.Value;
                builder.AppendLine($"// Subscribe to quest entry at index {entryIndex}");
                builder.OpenBlock($"if (quest.QuestEntries.Count > {entryIndex})");
                builder.AppendLine($"var entry = quest.QuestEntries[{entryIndex}];");
                AppendEventSubscription(builder, eventPath, lambdaSignature, handlerFieldName, actionMethod, objectiveVar);
                builder.CloseBlock();
                builder.OpenBlock("else");
                builder.AppendLine($"MelonLogger.Warning($\"[Quest] Quest '{questId}' does not have entry index {entryIndex} for trigger '{CodeFormatter.EscapeString(trigger.TargetAction)}'\");");
                builder.CloseBlock();
                return;
            }

            builder.AppendLine("// Subscribe to all quest entries");
            builder.OpenBlock("foreach (var entry in quest.QuestEntries)");
            AppendEventSubscription(builder, eventPath, lambdaSignature, handlerFieldName, actionMethod, objectiveVar);
            builder.CloseBlock();
        }

        private static bool IsSupportedQuestTrigger(string componentType, string eventName)
        {
            if (componentType == "Quest")
            {
                return eventName == "OnComplete" || eventName == "OnFail";
            }

            if (componentType == "QuestEntry")
            {
                return eventName == "OnComplete";
            }

            return false;
        }

        private static string GetRootNamespace(string targetNamespace)
        {
            var normalizedNamespace = NamespaceNormalizer.Normalize(targetNamespace);
            const string questSuffix = ".Quests";

            return normalizedNamespace.EndsWith(questSuffix, StringComparison.Ordinal)
                ? normalizedNamespace.Substring(0, normalizedNamespace.Length - questSuffix.Length)
                : normalizedNamespace;
        }

        private static bool TryResolveBaseGameQuest(string? targetQuestId, out BaseGameQuestDefinition questDefinition)
        {
            return BaseGameQuestCatalogService.TryResolve(targetQuestId, out questDefinition);
        }

        /// <summary>
        /// Gets the lambda signature string for a trigger action based on its parameters.
        /// Returns "()" for no parameters, or parameter list like "(delta)" or "(type, notify)".
        /// </summary>
        private string GetLambdaSignature(string targetAction)
        {
            if (string.IsNullOrWhiteSpace(targetAction))
                return "()";

            if (targetAction == "NPCRelationship.OnChanged")
                return "(delta)";

            if (targetAction == "NPCRelationship.OnUnlocked")
                return "(type, notify)";

            if (targetAction == "NPCCustomer.OnContractAssigned")
                return "(payment, quantity, windowStart, windowEnd)";

            if (targetAction == "TimeManager.OnSleepEnd")
                return "(minutes)";

            return "()";
        }

        /// <summary>
        /// Generates subscription code for static Action triggers.
        /// </summary>
        private void GenerateStaticActionTriggerSubscription(
            ICodeBuilder builder,
            QuestTrigger trigger,
            string actionMethod,
            string? objectiveVar = null)
        {
            var actionParts = trigger.TargetAction.Split('.');
            if (actionParts.Length != 2)
            {
                return;
            }

            var targetClass = actionParts[0];
            var actionName = actionParts[1];
            var eventPath = $"{targetClass}.{actionName}";

            if (targetClass == "Player" && actionName == "OnDeath")
            {
                builder.OpenBlock("if (Player.Local != null)");
                AppendEventSubscription(builder, "Player.Local.OnDeath", "()", null, actionMethod, objectiveVar);
                builder.CloseBlock();
                return;
            }

            if (targetClass == "Player"
                && (actionName == "PlayerSpawned" || actionName == "LocalPlayerSpawned" || actionName == "PlayerDespawned"))
            {
                AppendEventSubscription(builder, eventPath, "(player)", null, actionMethod, objectiveVar);
                return;
            }

            AppendEventSubscription(builder, eventPath, GetLambdaSignature(trigger.TargetAction), null, actionMethod, objectiveVar);
        }

        /// <summary>
        /// Generates subscription to quest completion event for rewards.
        /// </summary>
        private void GenerateQuestRewardSubscription(ICodeBuilder builder, QuestBlueprint quest)
        {
            if (builder == null)
                throw new ArgumentNullException(nameof(builder));
            if (quest == null)
                throw new ArgumentNullException(nameof(quest));

            builder.AppendComment("Generated from: Quest.QuestRewards = true");
            builder.AppendBlockComment("Subscribe to quest completion event to grant rewards.");
            builder.AppendLine("// Subscribe to quest completion event for rewards");
            AppendSubscriptionWithErrorHandling(
                builder,
                "Failed to subscribe to quest completion event",
                () => AppendMethodHandlerSubscription(builder, "OnComplete", "_onQuestCompletedHandler", "GrantQuestRewards"));
        }

        private void GenerateQuestGlobalStateSetterSubscriptions(ICodeBuilder builder, QuestBlueprint quest)
        {
            if (quest.CompletionGlobalStateSetters.Count > 0)
            {
                builder.AppendComment("Generated from: Quest.CompletionGlobalStateSetters[]");
                AppendSubscriptionWithErrorHandling(
                    builder,
                    "Failed to subscribe generated quest completion save setters",
                    () => AppendMethodHandlerSubscription(builder, "OnComplete", "_onQuestCompletedGlobalStateHandler", "ApplyGeneratedCompletionGlobalStateSetters"));
            }

            if (quest.FailureGlobalStateSetters.Count > 0)
            {
                builder.AppendComment("Generated from: Quest.FailureGlobalStateSetters[]");
                AppendSubscriptionWithErrorHandling(
                    builder,
                    "Failed to subscribe generated quest failure save setters",
                    () => AppendMethodHandlerSubscription(builder, "OnFail", "_onQuestFailedGlobalStateHandler", "ApplyGeneratedFailureGlobalStateSetters"));
            }
        }

        private void GenerateQuestHookSubscriptions(ICodeBuilder builder)
        {
            builder.AppendComment("Generated from: Quest.GenerateHookScaffold = true");
            builder.AppendBlockComment("Wire quest lifecycle events into generated partial hooks.");

            AppendSubscriptionWithErrorHandling(
                builder,
                "Failed to subscribe generated quest completion hook",
                () => AppendMethodHandlerSubscription(builder, "OnComplete", "_onQuestCompletedGeneratedHandler", "OnQuestCompletedGenerated"));

            AppendSubscriptionWithErrorHandling(
                builder,
                "Failed to subscribe generated quest fail hook",
                () => AppendMethodHandlerSubscription(builder, "OnFail", "_onQuestFailedGeneratedHandler", "OnQuestFailedGenerated"));

            builder.AppendLine("OnAfterTriggerSubscriptionsGenerated();");
            builder.AppendLine();
        }

        private void AppendSubscriptionWithErrorHandling(
            ICodeBuilder builder,
            string failureMessagePrefix,
            System.Action subscriptionWriter)
        {
            builder.OpenBlock("try");
            subscriptionWriter();
            builder.CloseBlock();
            builder.OpenBlock("catch (Exception ex)");
            builder.AppendLine($"MelonLogger.Warning($\"{CodeFormatter.EscapeString(failureMessagePrefix)}: {{ex.Message}}\");");
            builder.CloseBlock();
            builder.AppendLine();
        }

        private void AppendEventSubscription(
            ICodeBuilder builder,
            string eventPath,
            string lambdaSignature,
            string? handlerFieldName,
            string actionMethod,
            string? objectiveVar)
        {
            if (!string.IsNullOrWhiteSpace(handlerFieldName))
            {
                builder.AppendLine($"{handlerFieldName} ??= {lambdaSignature} =>");
                builder.OpenBlock();
                AppendActionInvocation(builder, actionMethod, objectiveVar);
                builder.CloseBlock(semicolon: true);

                builder.AppendLine($"{eventPath} -= {handlerFieldName};");
                builder.AppendLine($"{eventPath} += {handlerFieldName};");
                return;
            }

            builder.AppendLine($"{eventPath} += {lambdaSignature} =>");
            builder.OpenBlock();
            AppendActionInvocation(builder, actionMethod, objectiveVar);
            builder.CloseBlock(semicolon: true);
        }

        private void AppendActionInvocation(ICodeBuilder builder, string actionMethod, string? objectiveVar)
        {
            if (!string.IsNullOrWhiteSpace(objectiveVar))
            {
                builder.OpenBlock($"if ({objectiveVar} != null)");
                builder.AppendLine($"{objectiveVar}.{actionMethod};");
                builder.CloseBlock();
                return;
            }

            builder.AppendLine($"{actionMethod};");
        }

        private void AppendMethodHandlerSubscription(
            ICodeBuilder builder,
            string eventPath,
            string handlerFieldName,
            string handlerMethodName)
        {
            builder.AppendLine($"{handlerFieldName} ??= {handlerMethodName};");
            builder.AppendLine($"{eventPath} -= {handlerFieldName};");
            builder.AppendLine($"{eventPath} += {handlerFieldName};");
        }

        private static string GetNpcEventPath(string targetAction)
        {
            var actionParts = targetAction.Split('.');
            var componentType = actionParts.Length >= 2 ? actionParts[0] : "NPC";
            var eventName = actionParts.Length >= 2 ? actionParts[1] : targetAction;

            return componentType switch
            {
                "NPCCustomer" => $"npc.Customer.{eventName}",
                "NPCDealer" => $"npc.Dealer.{eventName}",
                "NPCRelationship" => $"npc.Relationship.{eventName}",
                _ => $"npc.{eventName}"
            };
        }

        private static (string ComponentType, string EventName) ParseQuestTargetAction(string targetAction)
        {
            var actionParts = targetAction.Split('.');
            var componentType = actionParts.Length >= 2 ? actionParts[0] : "Quest";
            var eventName = actionParts.Length >= 2 ? actionParts[1] : targetAction;
            return (componentType, eventName);
        }

        private static void AppendQuestLookup(ICodeBuilder builder, string? targetQuestId, string escapedQuestId, string rootNamespace)
        {
            if (TryResolveBaseGameQuest(targetQuestId, out var baseGameQuest))
            {
                builder.AppendLine($"var quest = QuestManager.Get<{baseGameQuest.IdentifierName}>();");
                return;
            }

            builder.AppendLine($"var quest = global::{rootNamespace}.Core.GetRegisteredQuest(\"{escapedQuestId}\") ?? QuestManager.GetQuestByGuid(\"{escapedQuestId}\") ?? QuestManager.GetQuestByName(\"{escapedQuestId}\");");
        }

        private static TriggerHandlerInfo? FindQuestHandlerInfo(
            IEnumerable<TriggerHandlerInfo> handlerInfos,
            QuestTrigger trigger,
            TriggerCategory triggerCategory)
        {
            return handlerInfos.FirstOrDefault(h => h.Trigger == trigger && h.TriggerCategory == triggerCategory);
        }

        private static TriggerHandlerInfo? FindObjectiveHandlerInfo(
            IEnumerable<TriggerHandlerInfo> handlerInfos,
            int objectiveIndex,
            TriggerCategory triggerCategory,
            QuestTrigger trigger)
        {
            return handlerInfos.FirstOrDefault(h =>
                h.ObjectiveIndex == objectiveIndex
                && h.TriggerCategory == triggerCategory
                && h.Trigger == trigger);
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
