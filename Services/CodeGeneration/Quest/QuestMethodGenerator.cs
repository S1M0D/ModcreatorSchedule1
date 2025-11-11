using System;
using System.IO;
using System.Linq;
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
                "Called when the quest is created. Sets up objectives, POI positions, and activates entries.",
                "Only creates entries if they don't already exist (e.g., from save data)."
            );

            builder.OpenBlock("protected override void OnCreated()");
            builder.AppendLine("base.OnCreated();");
            builder.AppendLine();

            builder.AppendComment("Skip entry creation if quest was loaded from save data");
            builder.OpenBlock("if (QuestEntries.Count > 0)");
            builder.AppendComment("Quest entries already exist (loaded from save), just subscribe to triggers");
            builder.AppendLine("SubscribeToTriggers();");
            builder.AppendLine("return;");
            builder.CloseBlock();
            builder.AppendLine();

            if (quest.Objectives?.Any() != true)
            {
                builder.AppendComment("Define at least one objective so the quest has progress steps.");
                builder.AppendLine("var defaultEntry = AddEntry(\"Describe your first objective\");");
                builder.AppendLine("defaultEntry.Begin();");
            }
            else
            {
                GenerateObjectiveInitialization(builder, quest);
            }

            builder.AppendLine();
            builder.AppendLine("SubscribeToTriggers();");
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
                if (objective.HasLocation && objective.CreatePOI)
                {
                    builder.AppendComment($"🔧 From: Objectives[{i}].Title, HasLocation, LocationX/Y/Z");
                    builder.AppendLine($"{objectiveVar} = AddEntry(\"{CodeFormatter.EscapeString(objective.Title)}\", {CodeFormatter.FormatVector3(objective)});");
                }
                else
                {
                    builder.AppendComment($"🔧 From: Objectives[{i}].Title");
                    builder.AppendLine($"{objectiveVar} = AddEntry(\"{CodeFormatter.EscapeString(objective.Title)}\");");
                }

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
        /// Generates the OnLoaded method which rebuilds quest entries after loading from save.
        /// </summary>
        public void GenerateOnLoadedMethod(ICodeBuilder builder, QuestBlueprint quest)
        {
            if (builder == null)
                throw new ArgumentNullException(nameof(builder));
            if (quest == null)
                throw new ArgumentNullException(nameof(quest));

            builder.AppendComment("🔧 Generated from: Quest.Objectives[] (rebuild logic for save/load)");
            builder.AppendBlockComment(
                "Called after quest data has been loaded from save files.",
                "Rebuilds quest entries if they were cleared during load."
            );

            builder.OpenBlock("protected override void OnLoaded()");
            builder.AppendLine("base.OnLoaded();");
            builder.AppendLine();

            if (quest.Objectives?.Any() != true)
            {
                builder.AppendComment("No objectives defined, nothing to rebuild");
            }
            else
            {
                builder.AppendComment("Rebuild entries if they were cleared during load");
                builder.OpenBlock("if (QuestEntries.Count == 0)");

                GenerateObjectiveRebuild(builder, quest);

                builder.AppendComment("Re-subscribe to triggers after rebuilding entries");
                builder.AppendLine("SubscribeToTriggers();");
                builder.CloseBlock();
            }

            builder.CloseBlock();
            builder.AppendLine();
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
        /// Generates a quest reward method stub.
        /// </summary>
        public void GenerateRewardMethod(ICodeBuilder builder)
        {
            if (builder == null)
                throw new ArgumentNullException(nameof(builder));

            builder.OpenBlock("private void GrantQuestRewards()");
            builder.AppendComment("TODO: Leverage S1API economy/registry helpers to award cash, XP, or items.");
            builder.AppendComment("Example: EconomyManager.AddMoney(500);");
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
    }
}
