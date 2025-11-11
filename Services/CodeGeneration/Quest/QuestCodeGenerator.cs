using System;
using System.Collections.Generic;
using System.Linq;
using Schedule1ModdingTool.Models;
using Schedule1ModdingTool.Services.CodeGeneration.Abstractions;
using Schedule1ModdingTool.Services.CodeGeneration.Builders;
using Schedule1ModdingTool.Services.CodeGeneration.Common;
using Schedule1ModdingTool.Services.CodeGeneration.Triggers;

namespace Schedule1ModdingTool.Services.CodeGeneration.Quest
{
    /// <summary>
    /// Main orchestrator for generating complete quest source code.
    /// Composes multiple specialized generators to build the final output.
    /// </summary>
    public class QuestCodeGenerator : ICodeGenerator<QuestBlueprint>
    {
        private readonly QuestHeaderGenerator _headerGenerator;
        private readonly QuestEntryFieldGenerator _entryFieldGenerator;
        private readonly QuestMethodGenerator _methodGenerator;
        private readonly TriggerHandlerCollector _triggerCollector;
        private readonly TriggerSubscriptionGenerator _triggerSubscriptionGenerator;

        public QuestCodeGenerator()
        {
            _headerGenerator = new QuestHeaderGenerator();
            _entryFieldGenerator = new QuestEntryFieldGenerator();
            _methodGenerator = new QuestMethodGenerator(_entryFieldGenerator);
            _triggerCollector = new TriggerHandlerCollector();
            _triggerSubscriptionGenerator = new TriggerSubscriptionGenerator(_entryFieldGenerator);
        }

        /// <summary>
        /// Generates complete C# source code for a quest blueprint.
        /// </summary>
        public string GenerateCode(QuestBlueprint quest)
        {
            if (quest == null)
                throw new ArgumentNullException(nameof(quest));

            var builder = new CodeBuilder();
            var className = IdentifierSanitizer.MakeSafeIdentifier(quest.ClassName, "GeneratedQuest");
            var targetNamespace = NamespaceNormalizer.Normalize(quest.Namespace);

            // File header
            _headerGenerator.Generate(builder, quest);

            // Using statements
            var usingsBuilder = new UsingStatementsBuilder();
            usingsBuilder.AddQuestUsings();
            usingsBuilder.GenerateUsings(builder);

            // Namespace
            builder.OpenBlock($"namespace {targetNamespace}");

            // Quest class
            GenerateQuestClass(builder, quest, className);

            builder.CloseBlock(); // namespace

            return builder.Build();
        }

        /// <summary>
        /// Generates the quest class definition with all members.
        /// </summary>
        private void GenerateQuestClass(ICodeBuilder builder, QuestBlueprint quest, string className)
        {
            var questId = string.IsNullOrWhiteSpace(quest.QuestId) ? className : quest.QuestId.Trim();

            // Class XML comment
            builder.AppendComment("🔧 Generated from: Quest.QuestTitle");
            builder.AppendBlockComment(
                $"Auto-generated quest blueprint for \"{CodeFormatter.EscapeString(quest.QuestTitle)}\".",
                "Customize the body to wire in game-specific logic."
            );

            builder.OpenBlock($"public class {className} : Quest");

            // Quest identifier constant
            builder.AppendComment("🔧 Generated from: Quest.QuestId (or Quest.ClassName if QuestId is empty)");
            builder.AppendLine($"public const string QuestIdentifier = \"{CodeFormatter.EscapeString(questId)}\";");
            builder.AppendLine();

            // Data class if needed
            if (quest.GenerateDataClass)
            {
                GenerateDataClass(builder, className);
            }

            // Quest properties
            GenerateQuestProperties(builder, quest);

            // Quest entry fields
            _entryFieldGenerator.Generate(builder, quest);

            // OnCreated method
            _methodGenerator.GenerateOnCreatedMethod(builder, quest);

            // OnLoaded method
            _methodGenerator.GenerateOnLoadedMethod(builder, quest);

            // Reward method if needed
            if (quest.QuestRewards)
            {
                _methodGenerator.GenerateRewardMethod(builder);
            }

            // Icon loading method if needed
            if (quest.CustomIcon)
            {
                _methodGenerator.GenerateIconMethod(builder, quest);
            }

            // Trigger handler fields
            var handlerInfos = _triggerCollector.CollectHandlers(quest);
            GenerateTriggerHandlerFields(builder, handlerInfos);

            // SubscribeToTriggers method
            _triggerSubscriptionGenerator.Generate(builder, quest, className, handlerInfos);

            builder.CloseBlock(); // class
        }

        /// <summary>
        /// Generates the serializable data class for quest state.
        /// </summary>
        private void GenerateDataClass(ICodeBuilder builder, string className)
        {
            builder.AppendLine("[Serializable]");
            builder.OpenBlock("public class QuestDataModel");
            builder.AppendLine("public bool Completed { get; set; }");
            builder.AppendComment("Add additional quest-specific fields here");
            builder.CloseBlock();
            builder.AppendLine();
            builder.AppendLine($"[SaveableField(\"{CodeFormatter.EscapeString(className)}Data\")]");
            builder.AppendLine("private QuestDataModel _data = new QuestDataModel();");
            builder.AppendLine();
        }

        /// <summary>
        /// Generates quest property overrides (Title, Description, AutoBegin, etc.).
        /// </summary>
        private void GenerateQuestProperties(ICodeBuilder builder, QuestBlueprint quest)
        {
            builder.AppendComment("🔧 Generated from: Quest.QuestTitle");
            builder.AppendLine($"protected override string Title => \"{CodeFormatter.EscapeString(quest.QuestTitle)}\";");
            builder.AppendComment("🔧 Generated from: Quest.QuestDescription");
            builder.AppendLine($"protected override string Description => \"{CodeFormatter.EscapeString(quest.QuestDescription)}\";");
            builder.AppendComment("🔧 Generated from: Quest.AutoBegin");
            builder.AppendLine($"protected override bool AutoBegin => {quest.AutoBegin.ToString().ToLowerInvariant()};");

            if (quest.CustomIcon)
            {
                builder.AppendComment("🔧 Generated from: Quest.CustomIcon");
                builder.AppendLine("protected override Sprite? QuestIcon => LoadCustomIcon();");
            }

            builder.AppendLine();
            
            // Generate quest initialization code - override CreateInternal to set properties before base initialization
            if (!quest.TrackOnBegin || !quest.AutoCompleteOnAllEntriesComplete)
            {
                builder.AppendComment("🔧 Generated from: Quest.TrackOnBegin, Quest.AutoCompleteOnAllEntriesComplete");
                builder.AppendBlockComment(
                    "Quest initialization - sets tracking and auto-complete behavior",
                    "Called during quest construction, before base initialization"
                );
                builder.OpenBlock("internal override void CreateInternal()");

                // Set properties before calling base
                if (!quest.TrackOnBegin)
                {
                    builder.AppendComment("🔧 Generated from: Quest.TrackOnBegin = false");
                    builder.AppendLine("S1Quest.TrackOnBegin = false;");
                }

                if (!quest.AutoCompleteOnAllEntriesComplete)
                {
                    builder.AppendComment("🔧 Generated from: Quest.AutoCompleteOnAllEntriesComplete = false");
                    builder.AppendLine("S1Quest.AutoCompleteOnAllEntriesComplete = false;");
                }
                
                builder.AppendLine();
                builder.AppendLine("base.CreateInternal();");
                builder.CloseBlock();
                builder.AppendLine();
            }
        }

        /// <summary>
        /// Generates field declarations for trigger handlers.
        /// </summary>
        private void GenerateTriggerHandlerFields(ICodeBuilder builder, List<TriggerHandlerInfo> handlerInfos)
        {
            if (handlerInfos.Count == 0)
                return;

            builder.AppendComment("🔧 Generated from: Quest.Objectives[].StartTriggers, Quest.Objectives[].FinishTriggers, Quest.StartTriggers, Quest.FinishTriggers");
            builder.AppendComment("Trigger event handlers");
            foreach (var handlerInfo in handlerInfos)
            {
                var actionType = GetActionTypeForTrigger(handlerInfo.Trigger?.TargetAction);
                builder.AppendLine($"private {actionType} {handlerInfo.FieldName};");
            }
            builder.AppendLine();
        }

        /// <summary>
        /// Gets the Action type string for a trigger action based on its parameters.
        /// Returns "Action" for no parameters, or parameterized Action types like "Action&lt;float&gt;" or "Action&lt;UnlockType, bool&gt;".
        /// </summary>
        private string GetActionTypeForTrigger(string? targetAction)
        {
            if (string.IsNullOrWhiteSpace(targetAction))
                return "Action";

            // NPCRelationship.OnChanged -> Action<float>
            if (targetAction == "NPCRelationship.OnChanged")
                return "Action<float>";

            // NPCRelationship.OnUnlocked -> Action<UnlockType, bool>
            if (targetAction == "NPCRelationship.OnUnlocked")
                return "Action<NPCRelationship.UnlockType, bool>";

            // NPCCustomer.OnContractAssigned -> Action<float, int, int, int>
            if (targetAction == "NPCCustomer.OnContractAssigned")
                return "Action<float, int, int, int>";

            // TimeManager.OnSleepEnd -> Action<int>
            if (targetAction == "TimeManager.OnSleepEnd")
                return "Action<int>";

            // Player static events -> Action<Player>
            if (targetAction == "Player.PlayerSpawned" || targetAction == "Player.LocalPlayerSpawned" || targetAction == "Player.PlayerDespawned")
                return "Action<Player>";

            // Default: no parameters
            return "Action";
        }

        /// <summary>
        /// Validates whether the blueprint can be successfully generated.
        /// </summary>
        public CodeGenerationValidationResult Validate(QuestBlueprint blueprint)
        {
            var result = new CodeGenerationValidationResult { IsValid = true };

            if (blueprint == null)
            {
                result.IsValid = false;
                result.Errors.Add("Blueprint cannot be null");
                return result;
            }

            if (string.IsNullOrWhiteSpace(blueprint.ClassName))
            {
                result.Warnings.Add("Class name is empty, will use default 'GeneratedQuest'");
            }

            if (string.IsNullOrWhiteSpace(blueprint.QuestTitle))
            {
                result.Warnings.Add("Quest title is empty");
            }

            if (blueprint.Objectives == null || !blueprint.Objectives.Any())
            {
                result.Warnings.Add("Quest has no objectives");
            }

            return result;
        }
    }
}
