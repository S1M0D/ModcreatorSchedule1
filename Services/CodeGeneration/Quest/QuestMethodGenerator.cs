using System.IO;
using Schedule1ModdingTool.Models;
using Schedule1ModdingTool.Services.CodeGeneration.Abstractions;
using Schedule1ModdingTool.Services.CodeGeneration.Common;

namespace Schedule1ModdingTool.Services.CodeGeneration.Quest
{
    /// <summary>
    /// Generates quest methods: OnCreated, OnLoaded, reward stubs, and icon loading.
    /// Handles the quest lifecycle and objective initialization logic.
    /// </summary>
    public class QuestMethodGenerator
    {
        private readonly QuestEntryFieldGenerator _entryFieldGenerator;

        public QuestMethodGenerator(QuestEntryFieldGenerator entryFieldGenerator)
        {
            _entryFieldGenerator = entryFieldGenerator ?? throw new ArgumentNullException(nameof(entryFieldGenerator));
        }

        /// <summary>
        /// Generates the OnCreated method which initializes quest entries on first creation.
        /// </summary>
        public void GenerateOnCreatedMethod(ICodeBuilder builder, QuestBlueprint quest)
        {
            if (builder == null)
                throw new ArgumentNullException(nameof(builder));
            if (quest == null)
                throw new ArgumentNullException(nameof(quest));

            builder.AppendComment("🔧 Generated from: Quest.Objectives[]");
            builder.AppendBlockComment(
                "Called when the quest is created (Unity Start method).",
                "For LOADED quests: OnLoaded() runs first, then OnCreated(). QuestEntries will already be populated.",
                "For NEW quests: Only OnCreated() runs. QuestEntries will be empty.",
                "Check QuestEntries.Count to avoid creating duplicate entries."
            );

            builder.OpenBlock("protected override void OnCreated()");
            builder.AppendLine("base.OnCreated();");
            builder.AppendLine();

            if (quest.Objectives?.Any() != true)
            {
                builder.AppendComment("Define at least one objective so the quest has progress steps.");
                builder.OpenBlock("if (QuestEntries.Count == 0)");
                builder.AppendLine("var defaultEntry = AddEntry(\"Describe your first objective\");");
                builder.AppendLine("defaultEntry.Begin();");
                builder.CloseBlock();
            }
            else
            {
                builder.AppendComment("Only create entries if they haven't been created yet (avoids duplicates for loaded quests)");
                builder.OpenBlock("if (QuestEntries.Count == 0)");
                GenerateObjectiveInitialization(builder, quest);
                builder.CloseBlock();
                builder.OpenBlock("else");
                GenerateEntryReferenceRestoration(builder, quest);
                builder.CloseBlock();
            }

            builder.AppendLine();
            builder.AppendComment("Subscribe to triggers after entries are set up (only once, in OnCreated)");
            builder.AppendLine("SubscribeToTriggers();");
            if (quest.GenerateHookScaffold)
            {
                builder.AppendLine("OnAfterCreatedGenerated();");
            }
            builder.CloseBlock();
            builder.AppendLine();
        }

        /// <summary>
        /// Generates objective initialization code for OnCreated.
        /// </summary>
        private void GenerateObjectiveInitialization(ICodeBuilder builder, QuestBlueprint quest)
        {
            var objectiveNames = _entryFieldGenerator.GetAllObjectiveVariableNames(quest);

            for (int i = 0; i < quest.Objectives.Count; i++)
            {
                var objective = quest.Objectives[i];
                var objectiveVar = objectiveNames[i];

                builder.AppendComment($"🔧 Generated from: Quest.Objectives[{i}]");
                builder.AppendComment($"Objective \"{CodeFormatter.EscapeString(objective.Title)}\" ({objective.Name})");

                // Create entry
                // HasLocation automatically implies POI creation
                if (objective.HasLocation)
                {
                    if (objective.UseNpcLocation && !string.IsNullOrWhiteSpace(objective.NpcId))
                    {
                        builder.AppendComment($"🔧 From: Objectives[{i}].Title, HasLocation, UseNpcLocation, NpcId");
                        builder.AppendComment($"🔧 From: Objectives[{i}].NpcId = \"{CodeFormatter.EscapeString(objective.NpcId)}\"");
                        builder.AppendLine($"var npc_{i} = NPC.All.FirstOrDefault(n => n.ID == \"{CodeFormatter.EscapeString(objective.NpcId)}\");");
                        builder.OpenBlock($"if (npc_{i} != null)");
                        builder.AppendLine($"{objectiveVar} = AddEntry(\"{CodeFormatter.EscapeString(objective.Title)}\", npc_{i});");
                        builder.AppendComment("Ensure POI is created and follows NPC location (handles cases where NPC transform wasn't ready during AddEntry)");
                        builder.AppendLine($"{objectiveVar}.SetPOIToNPC(npc_{i});");
                        builder.CloseBlock();
                        builder.OpenBlock("else");
                        builder.AppendLine($"MelonLogger.Warning($\"[Quest] NPC '{CodeFormatter.EscapeString(objective.NpcId)}' not found for quest entry '{CodeFormatter.EscapeString(objective.Title)}'\");");
                        builder.AppendLine($"{objectiveVar} = AddEntry(\"{CodeFormatter.EscapeString(objective.Title)}\");");
                        builder.CloseBlock();
                    }
                    else
                    {
                        builder.AppendComment($"🔧 From: Objectives[{i}].Title, HasLocation, LocationX/Y/Z");
                        builder.AppendLine($"{objectiveVar} = AddEntry(\"{CodeFormatter.EscapeString(objective.Title)}\", {CodeFormatter.FormatVector3(objective)});");
                    }
                }
                else
                {
                    builder.AppendComment($"🔧 From: Objectives[{i}].Title");
                    builder.AppendLine($"{objectiveVar} = AddEntry(\"{CodeFormatter.EscapeString(objective.Title)}\");");
                }

                AppendObjectiveHookCall(builder, quest, i, objectiveVar);

                // Determine if entry should start active or inactive
                bool shouldAutoStart = objective.AutoStart && (objective.StartTriggers?.Any() != true);
                if (shouldAutoStart)
                {
                    builder.AppendComment($"🔧 From: Objectives[{i}].AutoStart = true");
                    builder.AppendLine($"{objectiveVar}.Begin();");
                }
                else if (objective.StartTriggers?.Any() == true)
                {
                    builder.AppendComment($"🔧 From: Objectives[{i}].StartTriggers[]");
                    builder.AppendLine($"{objectiveVar}.SetState(QuestState.Inactive);");
                    builder.AppendComment("Entry will be activated by start trigger");
                }
                else
                {
                    builder.AppendComment($"🔧 From: Objectives[{i}].AutoStart = false");
                    builder.AppendLine($"{objectiveVar}.SetState(QuestState.Inactive);");
                    builder.AppendComment("Entry starts inactive (AutoStart is disabled)");
                }

                builder.AppendComment($"🔧 From: Objectives[{i}].RequiredProgress = {objective.RequiredProgress}");
                builder.AppendLine();
            }
        }

        /// <summary>
        /// Generates the OnLoaded method which creates quest entries after loading from save.
        /// </summary>
        public void GenerateOnLoadedMethod(ICodeBuilder builder, QuestBlueprint quest)
        {
            if (builder == null)
                throw new ArgumentNullException(nameof(builder));
            if (quest == null)
                throw new ArgumentNullException(nameof(quest));

            builder.AppendComment("🔧 Generated from: Quest.Objectives[] (create entries after load)");
            builder.AppendBlockComment(
                "Called after quest data has been loaded from save files.",
                "For loaded quests, this runs BEFORE OnCreated(), so we create entries here.",
                "The game's save system will restore the entry states after we create them.",
                "IMPORTANT: Do NOT call .Begin() or .SetState() here - let the save system restore states.",
                "Note: Do not subscribe to triggers here - that is handled in OnCreated() to avoid duplicate subscriptions."
            );

            builder.OpenBlock("protected override void OnLoaded()");
            builder.AppendLine("base.OnLoaded();");
            builder.AppendLine();

            if (quest.Objectives?.Any() != true)
            {
                builder.AppendComment("No objectives defined, nothing to create");
                if (quest.GenerateHookScaffold)
                {
                    builder.AppendLine("OnAfterLoadedGenerated();");
                }
            }
            else
            {
                // Check if any objectives require NPCs
                bool requiresNPCs = quest.Objectives.Any(obj => obj.HasLocation && obj.UseNpcLocation && !string.IsNullOrWhiteSpace(obj.NpcId));
                
                if (requiresNPCs)
                {
                    builder.AppendComment("Create quest entries if they don't exist yet");
                    builder.AppendComment("This ensures loaded quests have their entries created before OnCreated() runs");
                    builder.AppendComment("Wait for NPCs to spawn before creating entries (NPCs may not be available immediately on load)");
                    builder.OpenBlock("if (QuestEntries.Count == 0)");
                    builder.AppendLine("MelonCoroutines.Start(WaitForNPCsAndCreateEntries());");
                    builder.AppendLine("return;");
                    builder.CloseBlock();
                }
                else
                {
                    builder.AppendComment("Create quest entries if they don't exist yet");
                    builder.AppendComment("This ensures loaded quests have their entries created before OnCreated() runs");
                    builder.OpenBlock("if (QuestEntries.Count == 0)");
                    GenerateObjectiveCreationWithoutState(builder, quest);
                    builder.CloseBlock();
                }

                builder.AppendLine();
                builder.AppendComment("Restore field references so generated triggers and hooks can reach the quest entries");
                GenerateEntryReferenceRestoration(builder, quest);
                if (quest.GenerateHookScaffold)
                {
                    builder.AppendLine("OnAfterLoadedGenerated();");
                }
            }

            builder.CloseBlock();
            builder.AppendLine();
        }

        /// <summary>
        /// Generates a coroutine that waits for NPCs to spawn before creating quest entries.
        /// This is needed because NPCs may not be available immediately when a quest is loaded from save.
        /// </summary>
        public void GenerateWaitForNPCsCoroutine(ICodeBuilder builder, QuestBlueprint quest)
        {
            if (builder == null)
                throw new ArgumentNullException(nameof(builder));
            if (quest == null)
                throw new ArgumentNullException(nameof(quest));

            if (quest.Objectives?.Any() != true)
                return;

            // Collect all unique NPC IDs that are required
            var requiredNpcIds = quest.Objectives
                .Where(obj => obj.HasLocation && obj.UseNpcLocation && !string.IsNullOrWhiteSpace(obj.NpcId))
                .Select(obj => obj.NpcId)
                .Distinct()
                .ToList();

            if (requiredNpcIds.Count == 0)
                return;

            builder.AppendComment("🔧 Generated from: Quest.Objectives[] (wait for NPCs on load)");
            builder.AppendBlockComment(
                "Coroutine that waits for required NPCs to spawn before creating quest entries.",
                "This prevents NPC lookup failures when quests are loaded before NPCs are initialized."
            );
            builder.OpenBlock("private System.Collections.IEnumerator WaitForNPCsAndCreateEntries()");
            
            builder.AppendLine("float timeout = 10f;");
            builder.AppendLine("float waited = 0f;");
            builder.AppendLine();
            builder.AppendComment("Wait for all required NPCs to be available");
            builder.AppendLine("var requiredNPCs = new Dictionary<string, NPC>();");
            builder.OpenBlock("while (waited < timeout)");
            
            // Check each required NPC
            foreach (var npcId in requiredNpcIds)
            {
                builder.AppendLine($"if (!requiredNPCs.ContainsKey(\"{CodeFormatter.EscapeString(npcId)}\"))");
                builder.OpenBlock();
                builder.AppendLine($"var npc = NPC.All.FirstOrDefault(n => n.ID == \"{CodeFormatter.EscapeString(npcId)}\");");
                builder.OpenBlock("if (npc != null)");
                builder.AppendLine($"requiredNPCs[\"{CodeFormatter.EscapeString(npcId)}\"] = npc;");
                builder.CloseBlock();
                builder.CloseBlock();
            }
            
            builder.AppendLine();
            builder.AppendLine($"if (requiredNPCs.Count >= {requiredNpcIds.Count})");
            builder.OpenBlock();
            builder.AppendLine("break; // All NPCs found");
            builder.CloseBlock();
            
            builder.AppendLine();
            builder.AppendLine("waited += Time.deltaTime;");
            builder.AppendLine("yield return null; // wait 1 frame");
            builder.CloseBlock();
            
            builder.AppendLine();
            builder.AppendComment("Log warning if timeout reached and not all NPCs were found");
            builder.AppendLine($"if (requiredNPCs.Count < {requiredNpcIds.Count})");
            builder.OpenBlock();
            builder.AppendLine($"MelonLogger.Warning($\"[Quest] Timeout reached waiting for NPCs. Found {{requiredNPCs.Count}}/{requiredNpcIds.Count} required NPCs. Creating entries anyway.\");");
            builder.CloseBlock();
            builder.AppendLine();
            builder.AppendComment("Create quest entries now that NPCs are available (or timeout reached)");
            builder.AppendLine();
            
            // Generate the actual entry creation code using cached NPCs
            GenerateObjectiveCreationWithoutStateWithCachedNPCs(builder, quest, requiredNpcIds);
            
            builder.CloseBlock();
            builder.AppendLine();
        }

        /// <summary>
        /// Generates objective creation code for OnLoaded WITHOUT setting states, using cached NPCs from dictionary.
        /// Used when NPCs have been waited for and cached in a dictionary.
        /// </summary>
        private void GenerateObjectiveCreationWithoutStateWithCachedNPCs(ICodeBuilder builder, QuestBlueprint quest, List<string> cachedNpcIds)
        {
            var objectiveNames = _entryFieldGenerator.GetAllObjectiveVariableNames(quest);

            for (int i = 0; i < quest.Objectives.Count; i++)
            {
                var objective = quest.Objectives[i];
                var objectiveVar = objectiveNames[i];

                builder.AppendComment($"🔧 Generated from: Quest.Objectives[{i}]");
                builder.AppendComment($"Objective \"{CodeFormatter.EscapeString(objective.Title)}\" ({objective.Name})");

                // Create entry
                // HasLocation automatically implies POI creation
                if (objective.HasLocation)
                {
                    if (objective.UseNpcLocation && !string.IsNullOrWhiteSpace(objective.NpcId))
                    {
                        builder.AppendComment($"🔧 From: Objectives[{i}].Title, HasLocation, UseNpcLocation, NpcId");
                        builder.AppendComment($"🔧 From: Objectives[{i}].NpcId = \"{CodeFormatter.EscapeString(objective.NpcId)}\"");
                        builder.AppendLine($"requiredNPCs.TryGetValue(\"{CodeFormatter.EscapeString(objective.NpcId)}\", out var npc_{i});");
                        builder.OpenBlock($"if (npc_{i} != null)");
                        builder.AppendLine($"{objectiveVar} = AddEntry(\"{CodeFormatter.EscapeString(objective.Title)}\", npc_{i});");
                        builder.AppendComment("Ensure POI is created and follows NPC location (handles cases where NPC transform wasn't ready during AddEntry)");
                        builder.AppendLine($"{objectiveVar}.SetPOIToNPC(npc_{i});");
                        builder.CloseBlock();
                        builder.OpenBlock("else");
                        builder.AppendLine($"MelonLogger.Warning($\"[Quest] NPC '{CodeFormatter.EscapeString(objective.NpcId)}' not found for quest entry '{CodeFormatter.EscapeString(objective.Title)}' (timeout reached)\");");
                        builder.AppendLine($"{objectiveVar} = AddEntry(\"{CodeFormatter.EscapeString(objective.Title)}\");");
                        builder.CloseBlock();
                    }
                    else
                    {
                        builder.AppendComment($"🔧 From: Objectives[{i}].Title, HasLocation, LocationX/Y/Z");
                        builder.AppendLine($"{objectiveVar} = AddEntry(\"{CodeFormatter.EscapeString(objective.Title)}\", {CodeFormatter.FormatVector3(objective)});");
                    }
                }
                else
                {
                    builder.AppendComment($"🔧 From: Objectives[{i}].Title");
                    builder.AppendLine($"{objectiveVar} = AddEntry(\"{CodeFormatter.EscapeString(objective.Title)}\");");
                }

                AppendObjectiveHookCall(builder, quest, i, objectiveVar);

                // DO NOT set state here - let the save system restore it
                builder.AppendComment("State will be restored from save data by S1API");
                builder.AppendLine();
            }

            if (quest.GenerateHookScaffold)
            {
                builder.AppendLine("OnAfterLoadedGenerated();");
            }
        }

        /// <summary>
        /// Generates objective creation code for OnLoaded WITHOUT setting states.
        /// The game's save system will restore entry states after creation.
        /// </summary>
        private void GenerateObjectiveCreationWithoutState(ICodeBuilder builder, QuestBlueprint quest)
        {
            var objectiveNames = _entryFieldGenerator.GetAllObjectiveVariableNames(quest);

            for (int i = 0; i < quest.Objectives.Count; i++)
            {
                var objective = quest.Objectives[i];
                var objectiveVar = objectiveNames[i];

                builder.AppendComment($"🔧 Generated from: Quest.Objectives[{i}]");
                builder.AppendComment($"Objective \"{CodeFormatter.EscapeString(objective.Title)}\" ({objective.Name})");

                // Create entry
                // HasLocation automatically implies POI creation
                if (objective.HasLocation)
                {
                    if (objective.UseNpcLocation && !string.IsNullOrWhiteSpace(objective.NpcId))
                    {
                        builder.AppendComment($"🔧 From: Objectives[{i}].Title, HasLocation, UseNpcLocation, NpcId");
                        builder.AppendComment($"🔧 From: Objectives[{i}].NpcId = \"{CodeFormatter.EscapeString(objective.NpcId)}\"");
                        builder.AppendLine($"var npc_{i} = NPC.All.FirstOrDefault(n => n.ID == \"{CodeFormatter.EscapeString(objective.NpcId)}\");");
                        builder.OpenBlock($"if (npc_{i} != null)");
                        builder.AppendLine($"{objectiveVar} = AddEntry(\"{CodeFormatter.EscapeString(objective.Title)}\", npc_{i});");
                        builder.AppendComment("Ensure POI is created and follows NPC location (handles cases where NPC transform wasn't ready during AddEntry)");
                        builder.AppendLine($"{objectiveVar}.SetPOIToNPC(npc_{i});");
                        builder.CloseBlock();
                        builder.OpenBlock("else");
                        builder.AppendLine($"MelonLogger.Warning($\"[Quest] NPC '{CodeFormatter.EscapeString(objective.NpcId)}' not found for quest entry '{CodeFormatter.EscapeString(objective.Title)}'\");");
                        builder.AppendLine($"{objectiveVar} = AddEntry(\"{CodeFormatter.EscapeString(objective.Title)}\");");
                        builder.CloseBlock();
                    }
                    else
                    {
                        builder.AppendComment($"🔧 From: Objectives[{i}].Title, HasLocation, LocationX/Y/Z");
                        builder.AppendLine($"{objectiveVar} = AddEntry(\"{CodeFormatter.EscapeString(objective.Title)}\", {CodeFormatter.FormatVector3(objective)});");
                    }
                }
                else
                {
                    builder.AppendComment($"🔧 From: Objectives[{i}].Title");
                    builder.AppendLine($"{objectiveVar} = AddEntry(\"{CodeFormatter.EscapeString(objective.Title)}\");");
                }

                // DO NOT set state here - let the save system restore it
                builder.AppendComment("State will be restored from save data by S1API");
                builder.AppendLine();
            }
        }

        /// <summary>
        /// Generates code to restore field references from QuestEntries list.
        /// Assigns QuestEntries[i] to the corresponding field variables.
        /// </summary>
        private void GenerateEntryReferenceRestoration(ICodeBuilder builder, QuestBlueprint quest)
        {
            if (builder == null)
                throw new ArgumentNullException(nameof(builder));
            if (quest == null)
                throw new ArgumentNullException(nameof(quest));

            var objectiveNames = _entryFieldGenerator.GetAllObjectiveVariableNames(quest);

            for (int i = 0; i < objectiveNames.Count; i++)
            {
                var objectiveVar = objectiveNames[i];
                builder.AppendComment($"🔧 Restore reference for objective {i + 1}: {objectiveVar}");
                builder.OpenBlock($"if (QuestEntries.Count > {i})");
                builder.AppendLine($"{objectiveVar} = QuestEntries[{i}];");
                AppendObjectiveHookCall(builder, quest, i, objectiveVar);
                builder.CloseBlock();
                builder.OpenBlock("else");
                builder.AppendLine($"{objectiveVar} = null;");
                builder.AppendLine($"MelonLogger.Warning($\"[Quest] Expected entry index {i} for objective field '{objectiveVar}', but only {{QuestEntries.Count}} entries exist.\");");
                builder.CloseBlock();
            }
        }

        /// <summary>
        /// Generates objective rebuild code for OnLoaded.
        /// </summary>
        private void GenerateObjectiveRebuild(ICodeBuilder builder, QuestBlueprint quest)
        {
            var objectiveNames = _entryFieldGenerator.GetAllObjectiveVariableNames(quest);

            for (int i = 0; i < quest.Objectives.Count; i++)
            {
                var objective = quest.Objectives[i];
                var objectiveVar = objectiveNames[i];

                builder.AppendComment($"🔧 Generated from: Quest.Objectives[{i}]");
                builder.AppendComment($"Rebuild objective \"{CodeFormatter.EscapeString(objective.Title)}\" ({objective.Name})");

                // Create entry
                builder.AppendComment($"🔧 From: Objectives[{i}].Title");
                builder.AppendLine($"{objectiveVar} = AddEntry(\"{CodeFormatter.EscapeString(objective.Title)}\");");

                // Set POI position if objective has a location
                if (objective.HasLocation)
                {
                    builder.AppendComment($"🔧 From: Objectives[{i}].HasLocation, LocationX/Y/Z");
                    builder.AppendLine($"{objectiveVar}.POIPosition = {CodeFormatter.FormatVector3(objective)};");
                }

                // Don't call Begin() here - let the loaded quest state determine entry states
                builder.AppendComment("Entry state will be restored from save data by the base game loader");
                builder.AppendLine();
            }
        }

        /// <summary>
        /// Generates a quest reward method from the rewards collection.
        /// </summary>
        public void GenerateRewardMethod(ICodeBuilder builder, QuestBlueprint quest)
        {
            if (builder == null)
                throw new ArgumentNullException(nameof(builder));
            if (quest == null)
                throw new ArgumentNullException(nameof(quest));

            builder.AppendComment("🔧 Generated from: Quest.QuestRewardsList[]");
            builder.OpenBlock("private void GrantQuestRewards()");

            if (quest.QuestRewardsList == null || quest.QuestRewardsList.Count == 0)
            {
                builder.AppendComment("No rewards configured");
            }
            else
            {
                foreach (var reward in quest.QuestRewardsList)
                {
                    builder.AppendComment($"🔧 Generated from: QuestRewardsList[{quest.QuestRewardsList.IndexOf(reward)}] - {reward.RewardType}");
                    
                    switch (reward.RewardType)
                    {
                        case QuestRewardType.XP:
                            builder.AppendLine($"ConsoleHelper.GiveXp({reward.Amount});");
                            break;
                        case QuestRewardType.Money:
                            builder.AppendLine($"Money.ChangeCashBalance({reward.Amount});");
                            break;
                        case QuestRewardType.Item:
                            if (string.IsNullOrWhiteSpace(reward.ItemId))
                            {
                                builder.AppendComment($"WARNING: Item ID is empty for {reward.RewardType} reward");
                            }
                            else
                            {
                                if (reward.Quantity > 1)
                                {
                                    builder.AppendLine($"ConsoleHelper.AddItemToInventory(\"{CodeFormatter.EscapeString(reward.ItemId)}\", {reward.Quantity});");
                                }
                                else
                                {
                                    builder.AppendLine($"ConsoleHelper.AddItemToInventory(\"{CodeFormatter.EscapeString(reward.ItemId)}\");");
                                }
                            }
                            break;
                    }
                }
            }

            builder.CloseBlock();
            builder.AppendLine();
        }


        /// <summary>
        /// Generates a custom icon loading method.
        /// </summary>
        public void GenerateIconMethod(ICodeBuilder builder, QuestBlueprint quest)
        {
            if (builder == null)
                throw new ArgumentNullException(nameof(builder));
            if (quest == null)
                throw new ArgumentNullException(nameof(quest));

            builder.AppendComment("🔧 Generated from: Quest.CustomIcon, Quest.IconFileName");
            builder.OpenBlock("private Sprite? LoadCustomIcon()");

            if (string.IsNullOrWhiteSpace(quest.IconFileName))
            {
                builder.AppendComment("No icon file specified. Add a resource and select it in the quest settings.");
                builder.AppendLine("return null;");
            }
            else
            {
                // Extract just the filename (in case IconFileName includes a path)
                var fileName = Path.GetFileName(quest.IconFileName);
                // Construct resource name as ModName.Resources.IconName.png
                var resourceName = $"{quest.ModName}.Resources.{fileName}";

                builder.OpenBlock("try");
                builder.AppendLine("var assembly = Assembly.GetExecutingAssembly();");
                builder.AppendLine();
                builder.AppendLine($"using var stream = assembly.GetManifestResourceStream(\"{CodeFormatter.EscapeString(resourceName)}\");");
                builder.OpenBlock("if (stream != null)");
                builder.AppendLine("byte[] data = new byte[stream.Length];");
                builder.AppendLine("stream.Read(data, 0, data.Length);");
                builder.AppendLine("return ImageUtils.LoadImageRaw(data);");
                builder.CloseBlock();
                builder.CloseBlock();

                builder.OpenBlock("catch (Exception ex)");
                builder.AppendLine($"MelonLogger.Msg($\"Failed to load quest icon '{CodeFormatter.EscapeString(quest.IconFileName)}': {{ex.Message}}\");");
                builder.CloseBlock();

                builder.AppendLine();
                builder.AppendLine("return null;");
            }

            builder.CloseBlock();
            builder.AppendLine();
        }

        private void AppendObjectiveHookCall(ICodeBuilder builder, QuestBlueprint quest, int objectiveIndex, string objectiveVar)
        {
            if (!quest.GenerateHookScaffold)
            {
                return;
            }

            builder.OpenBlock($"if ({objectiveVar} != null)");
            builder.AppendLine($"ConfigureGeneratedObjective({objectiveIndex}, {objectiveVar});");
            builder.CloseBlock();
        }
    }
}
