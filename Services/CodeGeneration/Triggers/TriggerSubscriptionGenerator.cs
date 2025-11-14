using Schedule1ModdingTool.Models;
using Schedule1ModdingTool.Services.CodeGeneration.Abstractions;
using Schedule1ModdingTool.Services.CodeGeneration.Common;
using Schedule1ModdingTool.Services.CodeGeneration.Quest;
using System.Linq;

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
            List<TriggerHandlerInfo> handlerInfos)
        {
            if (builder == null)
                throw new ArgumentNullException(nameof(builder));
            if (quest == null)
                throw new ArgumentNullException(nameof(quest));

            var questId = string.IsNullOrWhiteSpace(quest.QuestId) ? className : quest.QuestId.Trim();
            var hasTriggers = (quest.QuestTriggers?.Any() == true) ||
                             (quest.QuestFinishTriggers?.Any() == true) ||
                             (quest.Objectives?.Any(o => o.StartTriggers?.Any() == true || o.FinishTriggers?.Any() == true) == true);
            var hasRewards = quest.QuestRewards && quest.QuestRewardsList != null && quest.QuestRewardsList.Count > 0;

            // Check if we need delayed subscription for quest triggers
            bool needsDelayedQuestSubscription = HasQuestEventTriggers(quest);

            builder.AppendComment("🔧 Generated from: Quest.QuestTriggers, Quest.QuestFinishTriggers, Quest.Objectives[].StartTriggers, Quest.Objectives[].FinishTriggers");
            builder.AppendBlockComment(
                "Subscribes to triggers for this quest and its objectives."
            );

            builder.OpenBlock("private void SubscribeToTriggers()");

            if (!hasTriggers && !hasRewards)
            {
                builder.AppendComment("No triggers configured for this quest");
                builder.CloseBlock();
                builder.AppendLine();
                return;
            }

            // If we have quest event triggers, use delayed subscription
            if (needsDelayedQuestSubscription)
            {
                builder.AppendComment("Quest event triggers require delayed subscription - start coroutine to wait for quests");
                builder.AppendLine("MelonCoroutines.Start(WaitForQuestsAndSubscribe());");
                builder.AppendLine("return;");
                builder.CloseBlock();
                builder.AppendLine();
                
                // Generate the coroutine method
                GenerateWaitForQuestsCoroutine(builder, quest, className, handlerInfos, hasRewards);
                return;
            }

            // Quest start triggers
            GenerateQuestStartTriggers(builder, quest, className, questId, handlerInfos);

            // Quest finish triggers
            GenerateQuestFinishTriggers(builder, quest, className, questId, handlerInfos);

            // Objective triggers
            GenerateObjectiveTriggers(builder, quest, className, handlerInfos);

            // Quest completion reward subscription
            if (hasRewards)
            {
                GenerateQuestRewardSubscription(builder, quest);
            }

            builder.CloseBlock();
            builder.AppendLine();
        }

        /// <summary>
        /// Checks if the quest has any QuestEventTrigger triggers that need delayed subscription.
        /// </summary>
        private bool HasQuestEventTriggers(QuestBlueprint quest)
        {
            // Check quest-level triggers
            if (quest.QuestTriggers?.Any(t => t.TriggerType == QuestTriggerType.QuestEventTrigger) == true)
                return true;
            
            if (quest.QuestFinishTriggers?.Any(t => t.TriggerType == QuestTriggerType.QuestEventTrigger) == true)
                return true;

            // Check objective-level triggers
            if (quest.Objectives?.Any(o => 
                o.StartTriggers?.Any(t => t.TriggerType == QuestTriggerType.QuestEventTrigger) == true ||
                o.FinishTriggers?.Any(t => t.TriggerType == QuestTriggerType.QuestEventTrigger) == true) == true)
                return true;

            return false;
        }

        /// <summary>
        /// Generates subscriptions for quest start triggers.
        /// </summary>
        private void GenerateQuestStartTriggers(
            ICodeBuilder builder,
            QuestBlueprint quest,
            string className,
            string questId,
            List<TriggerHandlerInfo> handlerInfos)
        {
            if (quest.QuestTriggers?.Any(t => t.TriggerTarget == QuestTriggerTarget.QuestStart) != true)
                return;

            builder.AppendComment("🔧 Generated from: Quest.QuestTriggers[] (where TriggerTarget = QuestStart)");
            foreach (var trigger in quest.QuestTriggers.Where(t => t.TriggerTarget == QuestTriggerTarget.QuestStart))
            {
                var handlerInfo = handlerInfos.FirstOrDefault(h =>
                    h.Trigger == trigger && h.TriggerCategory == TriggerCategory.QuestStart);

                AppendTriggerSubscription(builder, trigger, handlerInfo?.FieldName, handlerInfo?.ActionMethod ?? "Begin()");
            }
        }

        /// <summary>
        /// Generates subscriptions for quest finish triggers.
        /// </summary>
        private void GenerateQuestFinishTriggers(
            ICodeBuilder builder,
            QuestBlueprint quest,
            string className,
            string questId,
            List<TriggerHandlerInfo> handlerInfos)
        {
            if (quest.QuestFinishTriggers?.Any() != true)
                return;

            builder.AppendComment("🔧 Generated from: Quest.QuestFinishTriggers[]");
            foreach (var trigger in quest.QuestFinishTriggers)
            {
                var finishMethod = trigger.FinishType switch
                {
                    QuestFinishType.Complete => "Complete()",
                    QuestFinishType.Fail => "Fail()",
                    QuestFinishType.Cancel => "Cancel()",
                    QuestFinishType.Expire => "Expire()",
                    _ => "Complete()"
                };

                var handlerInfo = handlerInfos.FirstOrDefault(h =>
                    h.Trigger == trigger && h.TriggerCategory == TriggerCategory.QuestFinish);

                AppendTriggerSubscription(builder, trigger, handlerInfo?.FieldName, finishMethod);
            }
        }

        /// <summary>
        /// Generates subscriptions for objective-level triggers.
        /// </summary>
        private void GenerateObjectiveTriggers(
            ICodeBuilder builder,
            QuestBlueprint quest,
            string className,
            List<TriggerHandlerInfo> handlerInfos)
        {
            if (quest.Objectives?.Any() != true)
                return;

            builder.AppendComment("🔧 Generated from: Quest.Objectives[].StartTriggers, Quest.Objectives[].FinishTriggers");
            var objectiveNames = _entryFieldGenerator.GetAllObjectiveVariableNames(quest);

            for (int i = 0; i < quest.Objectives.Count; i++)
            {
                var objective = quest.Objectives[i];
                var objectiveVar = objectiveNames[i];

                // Objective start triggers
                if (objective.StartTriggers?.Any() == true)
                {
                    builder.AppendComment($"🔧 From: Objectives[{i}].StartTriggers[]");
                    foreach (var trigger in objective.StartTriggers)
                    {
                        var handlerInfo = handlerInfos.FirstOrDefault(h =>
                            h.ObjectiveIndex == i && h.TriggerCategory == TriggerCategory.ObjectiveStart &&
                            h.Trigger?.TargetAction == trigger.TargetAction);

                        AppendObjectiveTriggerSubscription(builder, trigger, objectiveVar, handlerInfo?.FieldName, "Begin()");
                    }
                }

                // Objective finish triggers
                if (objective.FinishTriggers?.Any() == true)
                {
                    builder.AppendComment($"🔧 From: Objectives[{i}].FinishTriggers[]");
                    foreach (var trigger in objective.FinishTriggers)
                    {
                        var handlerInfo = handlerInfos.FirstOrDefault(h =>
                            h.ObjectiveIndex == i && h.TriggerCategory == TriggerCategory.ObjectiveFinish &&
                            h.Trigger?.TargetAction == trigger.TargetAction);

                        AppendObjectiveTriggerSubscription(builder, trigger, objectiveVar, handlerInfo?.FieldName, "Complete()");
                    }
                }
            }
        }

        /// <summary>
        /// Appends a quest-level trigger subscription.
        /// </summary>
        private void AppendTriggerSubscription(
            ICodeBuilder builder,
            QuestTrigger trigger,
            string? handlerFieldName,
            string actionMethod)
        {
            if (string.IsNullOrWhiteSpace(trigger.TargetAction))
                return;

            builder.AppendComment($"Trigger: {CodeFormatter.EscapeString(trigger.TargetAction)} -> {trigger.TriggerTarget}");
            builder.OpenBlock("try");

            if (trigger.TriggerType == QuestTriggerType.NPCEventTrigger && !string.IsNullOrWhiteSpace(trigger.TargetNpcId))
            {
                GenerateNpcTriggerSubscription(builder, trigger, handlerFieldName, actionMethod, null);
            }
            else if (trigger.TriggerType == QuestTriggerType.QuestEventTrigger && !string.IsNullOrWhiteSpace(trigger.TargetQuestId))
            {
                GenerateQuestTriggerSubscription(builder, trigger, handlerFieldName, actionMethod, null);
            }
            else
            {
                GenerateStaticActionTriggerSubscription(builder, trigger, actionMethod);
            }

            builder.CloseBlock();
            builder.OpenBlock("catch (Exception ex)");
            builder.AppendLine($"MelonLogger.Warning($\"Failed to subscribe to trigger {CodeFormatter.EscapeString(trigger.TargetAction)}: {{ex.Message}}\");");
            builder.CloseBlock();
            builder.AppendLine();
        }

        /// <summary>
        /// Appends an objective-level trigger subscription.
        /// </summary>
        private void AppendObjectiveTriggerSubscription(
            ICodeBuilder builder,
            QuestObjectiveTrigger trigger,
            string objectiveVar,
            string? handlerFieldName,
            string actionMethod)
        {
            if (string.IsNullOrWhiteSpace(trigger.TargetAction))
                return;

            builder.AppendComment($"Objective trigger: {CodeFormatter.EscapeString(trigger.TargetAction)} -> {objectiveVar}.{actionMethod}");
            builder.OpenBlock("try");

            if (trigger.TriggerType == QuestTriggerType.NPCEventTrigger && !string.IsNullOrWhiteSpace(trigger.TargetNpcId))
            {
                GenerateNpcTriggerSubscription(builder, trigger, handlerFieldName, actionMethod, objectiveVar);
            }
            else if (trigger.TriggerType == QuestTriggerType.QuestEventTrigger && !string.IsNullOrWhiteSpace(trigger.TargetQuestId))
            {
                GenerateQuestTriggerSubscription(builder, trigger, handlerFieldName, actionMethod, objectiveVar);
            }
            else
            {
                GenerateStaticActionTriggerSubscription(builder, trigger, actionMethod, objectiveVar);
            }

            builder.CloseBlock();
            builder.OpenBlock("catch (Exception ex)");
            builder.AppendLine($"MelonLogger.Warning($\"Failed to subscribe to objective trigger {CodeFormatter.EscapeString(trigger.TargetAction)}: {{ex.Message}}\");");
            builder.CloseBlock();
            builder.AppendLine();
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
            var actionParts = trigger.TargetAction.Split('.');
            string eventPath;

            if (actionParts.Length >= 2)
            {
                var componentType = actionParts[0]; // NPCCustomer, NPCDealer, NPCRelationship, or NPC
                var eventName = actionParts[1]; // OnDealCompleted, OnRecruited, OnChanged, etc.

                if (componentType == "NPCCustomer")
                {
                    eventPath = $"npc.Customer.{eventName}";
                }
                else if (componentType == "NPCDealer")
                {
                    eventPath = $"npc.Dealer.{eventName}";
                }
                else if (componentType == "NPCRelationship")
                {
                    eventPath = $"npc.Relationship.{eventName}";
                }
                else
                {
                    eventPath = $"npc.{eventName}";
                }
            }
            else
            {
                var actionName = trigger.TargetAction.Contains(".") ? trigger.TargetAction.Split('.')[1] : trigger.TargetAction;
                eventPath = $"npc.{actionName}";
            }

            // Get lambda signature for parameterized events
            var lambdaSignature = GetLambdaSignature(trigger.TargetAction);

            builder.AppendLine($"var npc = NPC.All.FirstOrDefault(n => n.ID == \"{npcId}\");");
            builder.OpenBlock("if (npc == null)");
            builder.AppendLine($"MelonLogger.Warning($\"[Quest] NPC '{npcId}' not found when subscribing to trigger '{CodeFormatter.EscapeString(trigger.TargetAction)}'\");");
            builder.CloseBlock();
            builder.OpenBlock("else");

            if (!string.IsNullOrWhiteSpace(handlerFieldName))
            {
                // Use Action field pattern
                builder.AppendLine($"{handlerFieldName} ??= {lambdaSignature} =>");
                builder.OpenBlock();
                if (objectiveVar != null)
                {
                    builder.OpenBlock($"if ({objectiveVar} != null)");
                    builder.AppendLine($"{objectiveVar}.{actionMethod};");
                    builder.CloseBlock();
                }
                else
                {
                    builder.AppendLine($"{actionMethod};");
                }
                builder.CloseBlock(semicolon: true);

                builder.AppendLine($"{eventPath} -= {handlerFieldName};");
                builder.AppendLine($"{eventPath} += {handlerFieldName};");
            }
            else
            {
                // Fallback inline lambda
                builder.AppendLine($"{eventPath} += {lambdaSignature} =>");
                builder.OpenBlock();
                if (objectiveVar != null)
                {
                    builder.OpenBlock($"if ({objectiveVar} != null)");
                    builder.AppendLine($"{objectiveVar}.{actionMethod};");
                    builder.CloseBlock();
                }
                else
                {
                    builder.AppendLine($"{actionMethod};");
                }
                builder.CloseBlock(semicolon: true);
            }

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
            string? objectiveVar)
        {
            // Check if this is a base game quest with typed identifier
            bool useTypedIdentifier = !string.IsNullOrWhiteSpace(trigger.TargetQuestIdentifierType);
            var identifierTypeName = useTypedIdentifier ? trigger.TargetQuestIdentifierType.Trim() : null;
            var questTitle = CodeFormatter.EscapeString(trigger.TargetQuestId);
            var actionParts = trigger.TargetAction.Split('.');
            string eventPath;
            string questLookupCode;

            if (actionParts.Length >= 2)
            {
                var componentType = actionParts[0]; // Quest or QuestEntry
                var eventName = actionParts[1]; // OnComplete, OnFail, OnBegin, etc.

                if (componentType == "QuestEntry")
                {
                    // For QuestEntry events, we need to access the quest's entries
                    if (useTypedIdentifier)
                    {
                        builder.AppendLine($"MelonLogger.Msg($\"[Quest] Attempting to subscribe to quest trigger '{CodeFormatter.EscapeString(trigger.TargetAction)}' using identifier '{identifierTypeName}'\");");
                        builder.AppendLine($"var questWrapper = QuestManager.Get<S1API.Quests.Identifiers.{identifierTypeName}>();");
                        builder.OpenBlock("if (questWrapper == null)");
                        builder.AppendLine($"MelonLogger.Warning($\"[Quest] Quest '{questTitle}' (identifier: {identifierTypeName}) not found when subscribing to trigger '{CodeFormatter.EscapeString(trigger.TargetAction)}'. Quest may not be initialized yet.\");");
                        builder.CloseBlock();
                        builder.OpenBlock("else");
                        builder.AppendLine($"MelonLogger.Msg($\"[Quest] Successfully found quest '{questTitle}' (identifier: {identifierTypeName}) for trigger subscription\");");
                        
                        if (trigger.TargetQuestEntryIndex.HasValue)
                        {
                            // Subscribe to specific entry
                            builder.AppendLine($"// Subscribe to quest entry at index {trigger.TargetQuestEntryIndex.Value}");
                            builder.OpenBlock($"if (questWrapper.QuestEntries.Count > {trigger.TargetQuestEntryIndex.Value})");
                            builder.AppendLine($"var entry = questWrapper.QuestEntries[{trigger.TargetQuestEntryIndex.Value}];");
                            eventPath = $"entry.{eventName}";
                        }
                        else
                        {
                            // Subscribe to all entries
                            builder.AppendLine("// Subscribe to all quest entries");
                            builder.OpenBlock("foreach (var entry in questWrapper.QuestEntries)");
                            eventPath = $"entry.{eventName}";
                        }
                    }
                    else
                    {
                        builder.AppendLine($"var quest = QuestManager.GetQuestByName(\"{questTitle}\");");
                        builder.OpenBlock("if (quest == null)");
                        builder.AppendLine($"MelonLogger.Warning($\"[Quest] Quest '{questTitle}' not found when subscribing to trigger '{CodeFormatter.EscapeString(trigger.TargetAction)}'\");");
                        builder.CloseBlock();
                        builder.OpenBlock("else");
                        
                        if (trigger.TargetQuestEntryIndex.HasValue)
                        {
                            // Subscribe to specific entry
                            builder.AppendLine($"// Subscribe to quest entry at index {trigger.TargetQuestEntryIndex.Value}");
                            builder.OpenBlock($"if (quest.QuestEntries.Count > {trigger.TargetQuestEntryIndex.Value})");
                            builder.AppendLine($"var entry = quest.QuestEntries[{trigger.TargetQuestEntryIndex.Value}];");
                            eventPath = $"entry.{eventName}";
                        }
                        else
                        {
                            // Subscribe to all entries
                            builder.AppendLine("// Subscribe to all quest entries");
                            builder.OpenBlock("foreach (var entry in quest.QuestEntries)");
                            eventPath = $"entry.{eventName}";
                        }
                    }
                }
                else
                {
                    // Quest events
                    if (useTypedIdentifier)
                    {
                        builder.AppendLine($"MelonLogger.Msg($\"[Quest] Attempting to subscribe to quest trigger '{CodeFormatter.EscapeString(trigger.TargetAction)}' using identifier '{identifierTypeName}'\");");
                        builder.AppendLine($"var questWrapper = QuestManager.Get<S1API.Quests.Identifiers.{identifierTypeName}>();");
                        builder.AppendLine("var quest = questWrapper;");
                        builder.OpenBlock("if (quest == null)");
                        builder.AppendLine($"MelonLogger.Warning($\"[Quest] Quest '{questTitle}' (identifier: {identifierTypeName}) not found when subscribing to trigger '{CodeFormatter.EscapeString(trigger.TargetAction)}'. Quest may not be initialized yet.\");");
                        builder.CloseBlock();
                        builder.OpenBlock("else");
                        builder.AppendLine($"MelonLogger.Msg($\"[Quest] Successfully found quest '{questTitle}' (identifier: {identifierTypeName}) for trigger subscription\");");
                    }
                    else
                    {
                        builder.AppendLine($"MelonLogger.Msg($\"[Quest] Attempting to subscribe to quest trigger '{CodeFormatter.EscapeString(trigger.TargetAction)}' using quest title '{questTitle}'\");");
                        builder.AppendLine($"var quest = QuestManager.GetQuestByName(\"{questTitle}\");");
                        builder.OpenBlock("if (quest == null)");
                        builder.AppendLine($"MelonLogger.Warning($\"[Quest] Quest '{questTitle}' not found when subscribing to trigger '{CodeFormatter.EscapeString(trigger.TargetAction)}'. Quest may not be initialized yet.\");");
                        builder.CloseBlock();
                        builder.OpenBlock("else");
                        builder.AppendLine($"MelonLogger.Msg($\"[Quest] Successfully found quest '{questTitle}' for trigger subscription\");");
                    }
                    eventPath = $"quest.{eventName}";
                }
            }
            else
            {
                var actionName = trigger.TargetAction.Contains(".") ? trigger.TargetAction.Split('.')[1] : trigger.TargetAction;
                if (useTypedIdentifier)
                {
                    builder.AppendLine($"MelonLogger.Msg($\"[Quest] Attempting to subscribe to quest trigger '{CodeFormatter.EscapeString(trigger.TargetAction)}' using identifier '{identifierTypeName}'\");");
                    builder.AppendLine($"var questWrapper = QuestManager.Get<S1API.Quests.Identifiers.{identifierTypeName}>();");
                    builder.AppendLine("var quest = questWrapper;");
                    builder.OpenBlock("if (quest == null)");
                    builder.AppendLine($"MelonLogger.Warning($\"[Quest] Quest '{questTitle}' (identifier: {identifierTypeName}) not found when subscribing to trigger '{CodeFormatter.EscapeString(trigger.TargetAction)}'. Quest may not be initialized yet.\");");
                    builder.CloseBlock();
                    builder.OpenBlock("else");
                    builder.AppendLine($"MelonLogger.Msg($\"[Quest] Successfully found quest '{questTitle}' (identifier: {identifierTypeName}) for trigger subscription\");");
                }
                else
                {
                    builder.AppendLine($"MelonLogger.Msg($\"[Quest] Attempting to subscribe to quest trigger '{CodeFormatter.EscapeString(trigger.TargetAction)}' using quest title '{questTitle}'\");");
                    builder.AppendLine($"var quest = QuestManager.GetQuestByName(\"{questTitle}\");");
                    builder.OpenBlock("if (quest == null)");
                    builder.AppendLine($"MelonLogger.Warning($\"[Quest] Quest '{questTitle}' not found when subscribing to trigger '{CodeFormatter.EscapeString(trigger.TargetAction)}'. Quest may not be initialized yet.\");");
                    builder.CloseBlock();
                    builder.OpenBlock("else");
                    builder.AppendLine($"MelonLogger.Msg($\"[Quest] Successfully found quest '{questTitle}' for trigger subscription\");");
                }
                eventPath = $"quest.{actionName}";
            }

            // Get lambda signature for parameterized events
            var lambdaSignature = GetLambdaSignature(trigger.TargetAction);

            if (actionParts.Length >= 2 && actionParts[0] == "QuestEntry")
            {
                // QuestEntry subscription - either specific entry or foreach loop
                if (!string.IsNullOrWhiteSpace(handlerFieldName))
                {
                    builder.AppendLine($"{handlerFieldName} ??= {lambdaSignature} =>");
                    builder.OpenBlock();
                    if (objectiveVar != null)
                    {
                        builder.OpenBlock($"if ({objectiveVar} != null)");
                        builder.AppendLine($"{objectiveVar}.{actionMethod};");
                        builder.CloseBlock();
                    }
                    else
                    {
                        builder.AppendLine($"{actionMethod};");
                    }
                    builder.CloseBlock(semicolon: true);

                    builder.AppendLine($"{eventPath} -= {handlerFieldName};");
                    builder.AppendLine($"{eventPath} += {handlerFieldName};");
                }
                else
                {
                    builder.AppendLine($"{eventPath} += {lambdaSignature} =>");
                    builder.OpenBlock();
                    if (objectiveVar != null)
                    {
                        builder.OpenBlock($"if ({objectiveVar} != null)");
                        builder.AppendLine($"{objectiveVar}.{actionMethod};");
                        builder.CloseBlock();
                    }
                    else
                    {
                        builder.AppendLine($"{actionMethod};");
                    }
                    builder.CloseBlock(semicolon: true);
                }
                
                // Close the if block for specific entry, or foreach block for all entries
                if (trigger.TargetQuestEntryIndex.HasValue)
                {
                    builder.CloseBlock(); // if (questWrapper/quest.QuestEntries.Count > index)
                }
                else
                {
                    builder.CloseBlock(); // foreach
                }
                
                // Close the outer if/else block (questWrapper/quest found)
                builder.CloseBlock();
            }
            else
            {
                // Quest subscription
                if (!string.IsNullOrWhiteSpace(handlerFieldName))
                {
                    builder.AppendLine($"{handlerFieldName} ??= {lambdaSignature} =>");
                    builder.OpenBlock();
                    if (objectiveVar != null)
                    {
                        builder.OpenBlock($"if ({objectiveVar} != null)");
                        builder.AppendLine($"{objectiveVar}.{actionMethod};");
                        builder.CloseBlock();
                    }
                    else
                    {
                        builder.AppendLine($"{actionMethod};");
                    }
                    builder.CloseBlock(semicolon: true);

                    builder.AppendLine($"{eventPath} -= {handlerFieldName};");
                    builder.AppendLine($"{eventPath} += {handlerFieldName};");
                }
                else
                {
                    builder.AppendLine($"{eventPath} += {lambdaSignature} =>");
                    builder.OpenBlock();
                    if (objectiveVar != null)
                    {
                        builder.OpenBlock($"if ({objectiveVar} != null)");
                        builder.AppendLine($"{objectiveVar}.{actionMethod};");
                        builder.CloseBlock();
                    }
                    else
                    {
                        builder.AppendLine($"{actionMethod};");
                    }
                    builder.CloseBlock(semicolon: true);
                }
            }

            builder.CloseBlock(); // else (quest found)
        }

        /// <summary>
        /// Gets the lambda signature string for a trigger action based on its parameters.
        /// Returns "()" for no parameters, or parameter list like "(delta)" or "(type, notify)".
        /// </summary>
        private string GetLambdaSignature(string targetAction)
        {
            if (string.IsNullOrWhiteSpace(targetAction))
                return "()";

            // NPCRelationship.OnChanged -> Action<float>
            if (targetAction == "NPCRelationship.OnChanged")
                return "(delta)";

            // NPCRelationship.OnUnlocked -> Action<UnlockType, bool>
            if (targetAction == "NPCRelationship.OnUnlocked")
                return "(type, notify)";

            // NPCCustomer.OnContractAssigned -> Action<float, int, int, int>
            if (targetAction == "NPCCustomer.OnContractAssigned")
                return "(payment, quantity, windowStart, windowEnd)";

            // TimeManager.OnSleepEnd -> Action<int>
            if (targetAction == "TimeManager.OnSleepEnd")
                return "(minutes)";

            // Default: no parameters
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
            if (actionParts.Length == 2)
            {
                var targetClass = actionParts[0];
                var actionName = actionParts[1];

                // Special handling for Player.OnDeath (instance event on Player.Local)
                if (targetClass == "Player" && actionName == "OnDeath")
                {
                    builder.OpenBlock("if (Player.Local != null)");
                    builder.AppendLine("Player.Local.OnDeath += () =>");
                    builder.OpenBlock();
                    if (objectiveVar != null)
                    {
                        builder.OpenBlock($"if ({objectiveVar} != null)");
                        builder.AppendLine($"{objectiveVar}.{actionMethod};");
                        builder.CloseBlock();
                    }
                    else
                    {
                        builder.AppendLine($"{actionMethod};");
                    }
                    builder.CloseBlock(semicolon: true);
                    builder.CloseBlock();
                    return;
                }

                // Handle Player static events with parameters (PlayerSpawned, LocalPlayerSpawned, PlayerDespawned)
                if (targetClass == "Player" && (actionName == "PlayerSpawned" || actionName == "LocalPlayerSpawned" || actionName == "PlayerDespawned"))
                {
                    builder.AppendLine($"{targetClass}.{actionName} += (player) =>");
                    builder.OpenBlock();
                    if (objectiveVar != null)
                    {
                        builder.OpenBlock($"if ({objectiveVar} != null)");
                        builder.AppendLine($"{objectiveVar}.{actionMethod};");
                        builder.CloseBlock();
                    }
                    else
                    {
                        builder.AppendLine($"{actionMethod};");
                    }
                    builder.CloseBlock(semicolon: true);
                    return;
                }

                // Get lambda signature for parameterized events
                var lambdaSignature = GetLambdaSignature(trigger.TargetAction);

                // Standard static Action triggers
                builder.AppendLine($"{targetClass}.{actionName} += {lambdaSignature} =>");
                builder.OpenBlock();
                if (objectiveVar != null)
                {
                    builder.OpenBlock($"if ({objectiveVar} != null)");
                    builder.AppendLine($"{objectiveVar}.{actionMethod};");
                    builder.CloseBlock();
                }
                else
                {
                    builder.AppendLine($"{actionMethod};");
                }
                builder.CloseBlock(semicolon: true);
            }
        }

        /// <summary>
        /// Generates a coroutine that waits for quests to be available before subscribing to triggers.
        /// </summary>
        private void GenerateWaitForQuestsCoroutine(
            ICodeBuilder builder,
            QuestBlueprint quest,
            string className,
            List<TriggerHandlerInfo> handlerInfos,
            bool hasRewards)
        {
            builder.AppendComment("🔧 Generated from: Quest.QuestTriggers (delayed subscription)");
            builder.AppendBlockComment(
                "Coroutine that waits for target quests to be available before subscribing to their events.",
                "This prevents subscription failures when quests haven't been initialized yet."
            );
            builder.OpenBlock("private System.Collections.IEnumerator WaitForQuestsAndSubscribe()");
            
            builder.AppendLine("float timeout = 10f;");
            builder.AppendLine("float waited = 0f;");
            builder.AppendLine();
            builder.AppendComment("Wait for target quests to be available");
            builder.OpenBlock("while (waited < timeout)");
            
            // Collect all quest identifiers/titles that need to be checked
            var questIdentifiers = new System.Collections.Generic.HashSet<string>();
            var questTitles = new System.Collections.Generic.HashSet<string>();
            
            if (quest.QuestTriggers != null)
            {
                foreach (var trigger in quest.QuestTriggers.Where(t => t.TriggerType == QuestTriggerType.QuestEventTrigger))
                {
                    if (!string.IsNullOrWhiteSpace(trigger.TargetQuestIdentifierType))
                        questIdentifiers.Add(trigger.TargetQuestIdentifierType);
                    else if (!string.IsNullOrWhiteSpace(trigger.TargetQuestId))
                        questTitles.Add(trigger.TargetQuestId);
                }
            }
            
            if (quest.QuestFinishTriggers != null)
            {
                foreach (var trigger in quest.QuestFinishTriggers.Where(t => t.TriggerType == QuestTriggerType.QuestEventTrigger))
                {
                    if (!string.IsNullOrWhiteSpace(trigger.TargetQuestIdentifierType))
                        questIdentifiers.Add(trigger.TargetQuestIdentifierType);
                    else if (!string.IsNullOrWhiteSpace(trigger.TargetQuestId))
                        questTitles.Add(trigger.TargetQuestId);
                }
            }
            
            if (quest.Objectives != null)
            {
                foreach (var objective in quest.Objectives)
                {
                    if (objective.StartTriggers != null)
                    {
                        foreach (var trigger in objective.StartTriggers.Where(t => t.TriggerType == QuestTriggerType.QuestEventTrigger))
                        {
                            if (!string.IsNullOrWhiteSpace(trigger.TargetQuestIdentifierType))
                                questIdentifiers.Add(trigger.TargetQuestIdentifierType);
                            else if (!string.IsNullOrWhiteSpace(trigger.TargetQuestId))
                                questTitles.Add(trigger.TargetQuestId);
                        }
                    }
                    if (objective.FinishTriggers != null)
                    {
                        foreach (var trigger in objective.FinishTriggers.Where(t => t.TriggerType == QuestTriggerType.QuestEventTrigger))
                        {
                            if (!string.IsNullOrWhiteSpace(trigger.TargetQuestIdentifierType))
                                questIdentifiers.Add(trigger.TargetQuestIdentifierType);
                            else if (!string.IsNullOrWhiteSpace(trigger.TargetQuestId))
                                questTitles.Add(trigger.TargetQuestId);
                        }
                    }
                }
            }

            // Check typed identifiers
            foreach (var identifierType in questIdentifiers)
            {
                builder.AppendLine($"MelonLogger.Msg($\"[Quest] Waiting for quest with identifier '{identifierType}'...\");");
                builder.AppendLine($"var questWrapper_{identifierType} = QuestManager.Get<S1API.Quests.Identifiers.{identifierType}>();");
                builder.AppendLine($"if (questWrapper_{identifierType} != null) MelonLogger.Msg($\"[Quest] Found quest with identifier '{identifierType}'\");");
            }
            
            // Check quest titles
            foreach (var questTitle in questTitles)
            {
                var safeVarName = IdentifierSanitizer.MakeSafeIdentifier(questTitle.Replace(" ", ""), "quest");
                builder.AppendLine($"MelonLogger.Msg($\"[Quest] Waiting for quest with title '{CodeFormatter.EscapeString(questTitle)}'...\");");
                builder.AppendLine($"var {safeVarName} = QuestManager.GetQuestByName(\"{CodeFormatter.EscapeString(questTitle)}\");");
                builder.AppendLine($"if ({safeVarName} != null) MelonLogger.Msg($\"[Quest] Found quest with title '{CodeFormatter.EscapeString(questTitle)}'\");");
            }
            
            builder.AppendLine();
            
            // Build condition to check if all quests are found
            var conditions = new List<string>();
            foreach (var identifierType in questIdentifiers)
            {
                conditions.Add($"questWrapper_{identifierType} != null");
            }
            foreach (var questTitle in questTitles)
            {
                var safeVarName = IdentifierSanitizer.MakeSafeIdentifier(questTitle.Replace(" ", ""), "quest");
                conditions.Add($"{safeVarName} != null");
            }
            
            if (conditions.Count > 0)
            {
                builder.AppendLine($"if ({string.Join(" && ", conditions)})");
                builder.OpenBlock();
                builder.AppendLine("MelonLogger.Msg(\"[Quest] All target quests found, proceeding with trigger subscriptions\");");
                builder.AppendLine("break; // All quests found");
                builder.CloseBlock();
            }
            else
            {
                builder.AppendLine("// No quest triggers to wait for");
                builder.AppendLine("break;");
            }
            
            builder.AppendLine();
            builder.AppendLine("yield return null; // Wait one frame");
            builder.AppendLine("waited += UnityEngine.Time.deltaTime;");
            builder.CloseBlock(); // while
            
            builder.AppendLine();
            builder.AppendComment("Now subscribe to triggers");
            
            // Generate the actual trigger subscriptions
            var questId = string.IsNullOrWhiteSpace(quest.QuestId) ? className : quest.QuestId.Trim();
            GenerateQuestStartTriggers(builder, quest, className, questId, handlerInfos);
            GenerateQuestFinishTriggers(builder, quest, className, questId, handlerInfos);
            GenerateObjectiveTriggers(builder, quest, className, handlerInfos);
            
            if (hasRewards)
            {
                GenerateQuestRewardSubscription(builder, quest);
            }
            
            builder.CloseBlock(); // coroutine
            builder.AppendLine();
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

            builder.AppendComment("🔧 Generated from: Quest.QuestRewards = true");
            builder.AppendBlockComment(
                "Subscribe to quest completion event to grant rewards."
            );

            builder.AppendLine("// Subscribe to quest completion event for rewards");
            builder.OpenBlock("try");
            builder.AppendLine("_onQuestCompletedHandler ??= GrantQuestRewards;");
            builder.AppendLine("OnComplete -= _onQuestCompletedHandler;");
            builder.AppendLine("OnComplete += _onQuestCompletedHandler;");
            builder.CloseBlock();
            builder.OpenBlock("catch (Exception ex)");
            builder.AppendLine("MelonLogger.Warning($\"Failed to subscribe to quest completion event: {{ex.Message}}\");");
            builder.CloseBlock();
            builder.AppendLine();
        }
    }
}
