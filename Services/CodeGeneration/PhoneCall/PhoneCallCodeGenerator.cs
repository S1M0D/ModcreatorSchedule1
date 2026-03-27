using Schedule1ModdingTool.Models;
using Schedule1ModdingTool.Services;
using Schedule1ModdingTool.Services.CodeGeneration.Abstractions;
using Schedule1ModdingTool.Services.CodeGeneration.Builders;
using Schedule1ModdingTool.Services.CodeGeneration.Common;

namespace Schedule1ModdingTool.Services.CodeGeneration.PhoneCall
{
    /// <summary>
    /// Generates S1API phone call definitions from editor-authored blueprints.
    /// </summary>
    public class PhoneCallCodeGenerator : ICodeGenerator<PhoneCallBlueprint>
    {
        public string GenerateCode(PhoneCallBlueprint phoneCall)
        {
            ArgumentNullException.ThrowIfNull(phoneCall);

            var builder = new CodeBuilder();
            var className = IdentifierSanitizer.MakeSafeIdentifier(phoneCall.ClassName, "GeneratedPhoneCall");
            var targetNamespace = NamespaceNormalizer.NormalizeForPhoneCall(phoneCall.Namespace);
            var rootNamespace = GetRootNamespace(targetNamespace);
            var usingsBuilder = new UsingStatementsBuilder();
            usingsBuilder.AddPhoneCallUsings();

            builder.AppendComment("Auto-generated S1API phone call definition.");
            usingsBuilder.GenerateUsings(builder);
            builder.OpenBlock($"namespace {targetNamespace}");
            GeneratePhoneCallClass(builder, phoneCall, className, rootNamespace);
            builder.CloseBlock();
            return builder.Build();
        }

        public CodeGenerationValidationResult Validate(PhoneCallBlueprint blueprint)
        {
            var result = new CodeGenerationValidationResult { IsValid = true };
            if (blueprint == null)
            {
                result.IsValid = false;
                result.Errors.Add("Phone call blueprint cannot be null.");
                return result;
            }

            if (string.IsNullOrWhiteSpace(blueprint.ClassName))
                result.Warnings.Add("Class name is empty, defaulting to 'GeneratedPhoneCall'.");
            if (string.IsNullOrWhiteSpace(blueprint.CallId))
                result.Errors.Add("Call ID is required.");
            if (string.IsNullOrWhiteSpace(blueprint.CallTitle))
                result.Errors.Add("Call title is required.");
            if (blueprint.UsesCustomCaller && string.IsNullOrWhiteSpace(blueprint.CallerName))
                result.Errors.Add("Caller Name is required when using a custom caller.");
            if (blueprint.UsesNpcCaller && string.IsNullOrWhiteSpace(blueprint.CallerNpcId))
                result.Errors.Add("NPC Caller ID is required when using an NPC caller.");
            if (blueprint.Stages.Count == 0)
                result.Errors.Add("At least one stage is required. S1API skips calls with zero stages.");

            for (var stageIndex = 0; stageIndex < blueprint.Stages.Count; stageIndex++)
            {
                var stage = blueprint.Stages[stageIndex];
                if (string.IsNullOrWhiteSpace(stage.Text))
                {
                    result.Errors.Add($"Stage {stageIndex + 1} text is required.");
                }

                ValidateTriggers(stage.StartTriggers, $"Stage {stageIndex + 1} start trigger", result);
                ValidateTriggers(stage.DoneTriggers, $"Stage {stageIndex + 1} done trigger", result);
            }

            result.IsValid = result.Errors.Count == 0;
            return result;
        }

        private static void ValidateTriggers(
            IEnumerable<PhoneCallSystemTriggerBlueprint> triggers,
            string labelPrefix,
            CodeGenerationValidationResult result)
        {
            var triggerIndex = 0;
            foreach (var trigger in triggers)
            {
                triggerIndex++;
                foreach (var variableSetter in trigger.VariableSetters)
                {
                    if (string.IsNullOrWhiteSpace(variableSetter.VariableName))
                    {
                        result.Errors.Add($"{labelPrefix} {triggerIndex}: variable setter requires a variable name.");
                    }
                }

                foreach (var setter in trigger.GlobalStateSetters)
                {
                    if (string.IsNullOrWhiteSpace(setter.GlobalStateClassName) || string.IsNullOrWhiteSpace(setter.FieldSaveKey))
                    {
                        result.Errors.Add($"{labelPrefix} {triggerIndex}: global save setter requires a target save field.");
                    }
                }

                foreach (var questSetter in trigger.QuestSetters)
                {
                    if (string.IsNullOrWhiteSpace(questSetter.QuestId))
                    {
                        result.Errors.Add($"{labelPrefix} {triggerIndex}: quest setter requires a quest reference.");
                    }

                    if (!questSetter.SetQuestAction && !questSetter.SetQuestEntryState)
                    {
                        result.Errors.Add($"{labelPrefix} {triggerIndex}: quest setter must set a quest action, an entry state, or both.");
                    }
                }
            }
        }

        private static void GeneratePhoneCallClass(
            ICodeBuilder builder,
            PhoneCallBlueprint phoneCall,
            string className,
            string rootNamespace)
        {
            builder.AppendBlockComment(
                $"Builds the generated phone call \"{CodeFormatter.EscapeString(phoneCall.DisplayName)}\".",
                "The constructor fully configures the call so Core can queue it directly through CallManager."
            );
            builder.OpenBlock($"public partial class {className} : PhoneCallDefinition");
            builder.AppendLine($"public const string GeneratedCallId = \"{CodeFormatter.EscapeString(phoneCall.CallId)}\";");
            builder.AppendLine();
            builder.AppendLine(GetConstructorSignature(phoneCall, className));
            builder.OpenBlock();
            builder.AppendLine("BuildGeneratedCall();");
            builder.AppendLine("ConfigureGeneratedCall();");
            builder.CloseBlock();
            builder.AppendLine();
            builder.OpenBlock("private void BuildGeneratedCall()");
            if (phoneCall.Stages.Count == 0)
            {
                builder.AppendComment("No stages were authored. Validation should catch this before export.");
            }

            for (var stageIndex = 0; stageIndex < phoneCall.Stages.Count; stageIndex++)
            {
                AppendStage(builder, phoneCall.Stages[stageIndex], stageIndex, rootNamespace);
            }
            builder.CloseBlock();
            builder.AppendLine();

            if (phoneCall.UsesNpcCaller)
            {
                AppendNpcLookupHelper(builder, phoneCall);
                builder.AppendLine();
            }
            else
            {
                AppendCustomCallerIconHelper(builder, phoneCall);
                builder.AppendLine();
            }

            AppendPartialHooks(builder);
            builder.CloseBlock();
        }

        private static string GetConstructorSignature(PhoneCallBlueprint phoneCall, string className)
        {
            if (phoneCall.UsesNpcCaller)
            {
                return $"public {className}() : base(ResolveCallerNpc())";
            }

            return $"public {className}() : base(\"{CodeFormatter.EscapeString(phoneCall.CallerName)}\", LoadCallerIcon())";
        }

        private static void AppendStage(
            ICodeBuilder builder,
            PhoneCallStageBlueprint stage,
            int stageIndex,
            string rootNamespace)
        {
            var stageKey = $"Stage{stageIndex + 1}";
            var stageVariable = $"stage{stageIndex + 1}";
            builder.AppendLine($"var {stageVariable} = AddStage(\"{CodeFormatter.EscapeString(stage.Text)}\");");
            AppendTriggerGroup(builder, stage.StartTriggers, stageVariable, stageIndex, isDoneTrigger: false, rootNamespace);
            AppendTriggerGroup(builder, stage.DoneTriggers, stageVariable, stageIndex, isDoneTrigger: true, rootNamespace);
            builder.AppendLine($"OnGeneratedStageBuilt(\"{CodeFormatter.EscapeString(stageKey)}\", {stageVariable});");
            builder.AppendLine();
        }

        private static void AppendTriggerGroup(
            ICodeBuilder builder,
            IEnumerable<PhoneCallSystemTriggerBlueprint> triggers,
            string stageVariable,
            int stageIndex,
            bool isDoneTrigger,
            string rootNamespace)
        {
            var triggerType = isDoneTrigger ? "Done" : "Start";
            var systemTriggerType = isDoneTrigger ? "DoneTrigger" : "StartTrigger";
            var triggerIndex = 0;
            foreach (var trigger in triggers)
            {
                triggerIndex++;
                var triggerVariable = $"trigger_{stageIndex + 1}_{triggerType.ToLowerInvariant()}_{triggerIndex}";
                builder.AppendLine($"var {triggerVariable} = {stageVariable}.AddSystemTrigger(SystemTriggerType.{systemTriggerType});");
                AppendVariableSetters(builder, trigger.VariableSetters, triggerVariable);
                AppendGlobalStateSetters(builder, trigger.GlobalStateSetters, triggerVariable, rootNamespace);
                AppendQuestSetters(builder, trigger.QuestSetters, triggerVariable, stageIndex, triggerIndex, rootNamespace);
                builder.AppendLine($"OnGeneratedTriggerBuilt(\"Stage{stageIndex + 1}\", \"{triggerType}\", {triggerIndex}, {triggerVariable});");
            }
        }

        private static void AppendVariableSetters(
            ICodeBuilder builder,
            IEnumerable<PhoneCallVariableSetterBlueprint> setters,
            string triggerVariable)
        {
            foreach (var setter in setters)
            {
                builder.AppendLine(
                    $"{triggerVariable}.AddVariableSetter(" +
                    $"EvaluationType.{setter.Evaluation}, " +
                    $"\"{CodeFormatter.EscapeString(setter.VariableName)}\", " +
                    $"\"{CodeFormatter.EscapeString(setter.NewValue)}\");");
            }
        }

        private static void AppendGlobalStateSetters(
            ICodeBuilder builder,
            IEnumerable<GlobalStateSetterBlueprint> setters,
            string triggerVariable,
            string rootNamespace)
        {
            foreach (var setter in setters)
            {
                if (string.IsNullOrWhiteSpace(setter.GlobalStateClassName) || string.IsNullOrWhiteSpace(setter.FieldSaveKey))
                {
                    continue;
                }

                var evaluationEvent = setter.Evaluation == PhoneCallTriggerEvaluationOption.PassOnFalse
                    ? "OnEvaluateFalse"
                    : "OnEvaluateTrue";

                builder.AppendLine(
                    $"{triggerVariable}.{evaluationEvent} += () => " +
                    $"global::{rootNamespace}.Core.SetGeneratedGlobalStateValue(" +
                    $"\"{CodeFormatter.EscapeString(setter.GlobalStateClassName)}\", " +
                    $"\"{CodeFormatter.EscapeString(setter.FieldSaveKey)}\", " +
                    $"\"{CodeFormatter.EscapeString(setter.NewValue)}\", " +
                    $"{setter.RequestSave.ToString().ToLowerInvariant()});");
            }
        }

        private static void AppendQuestSetters(
            ICodeBuilder builder,
            IEnumerable<PhoneCallQuestSetterBlueprint> setters,
            string triggerVariable,
            int stageIndex,
            int triggerIndex,
            string rootNamespace)
        {
            var setterIndex = 0;
            foreach (var setter in setters)
            {
                setterIndex++;
                var questVariable = $"quest_{stageIndex + 1}_{triggerIndex}_{setterIndex}";
                AppendQuestLookup(builder, setter, questVariable, rootNamespace);
                builder.OpenBlock($"if ({questVariable} != null)");
                builder.AppendLine(BuildQuestSetterInvocation(triggerVariable, questVariable, setter));
                builder.CloseBlock();
            }
        }

        private static void AppendQuestLookup(
            ICodeBuilder builder,
            PhoneCallQuestSetterBlueprint setter,
            string questVariable,
            string rootNamespace)
        {
            if (setter.UsesBaseGameQuest && BaseGameQuestCatalogService.TryResolve(setter.QuestId, out var baseGameQuest))
            {
                builder.AppendLine($"var {questVariable} = QuestManager.Get<{baseGameQuest.IdentifierName}>();");
                return;
            }

            var escapedQuestId = CodeFormatter.EscapeString(setter.QuestId);
            builder.AppendLine(
                $"var {questVariable} = global::{rootNamespace}.Core.GetRegisteredQuest(\"{escapedQuestId}\") " +
                $"?? QuestManager.GetQuestByGuid(\"{escapedQuestId}\") " +
                $"?? QuestManager.GetQuestByName(\"{escapedQuestId}\");");
        }

        private static string BuildQuestSetterInvocation(
            string triggerVariable,
            string questVariable,
            PhoneCallQuestSetterBlueprint setter)
        {
            var arguments = new List<string>
            {
                $"EvaluationType.{setter.Evaluation}",
                questVariable
            };

            if (setter.SetQuestAction)
            {
                arguments.Add($"questAction: QuestAction.{setter.QuestAction}");
            }

            if (setter.SetQuestEntryState)
            {
                arguments.Add($"questEntryState: Tuple.Create({setter.QuestEntryIndex}, QuestState.{setter.QuestEntryState})");
            }

            return $"{triggerVariable}.AddQuestSetter({string.Join(", ", arguments)});";
        }

        private static void AppendNpcLookupHelper(ICodeBuilder builder, PhoneCallBlueprint phoneCall)
        {
            builder.AppendComment("Resolves the authored NPC caller at runtime. Base-game and custom NPCs are both searched through NPC.All.");
            builder.OpenBlock("private static NPC? ResolveCallerNpc()");
            builder.AppendLine($"var npc = NPC.All.FirstOrDefault(candidate => string.Equals(candidate.ID, \"{CodeFormatter.EscapeString(phoneCall.CallerNpcId)}\", StringComparison.OrdinalIgnoreCase));");
            builder.OpenBlock("if (npc == null)");
            builder.AppendLine($"MelonLogger.Warning(\"Generated phone call caller NPC '{CodeFormatter.EscapeString(phoneCall.CallerNpcId)}' was not found. The call will fall back to 'Unknown Caller'.\");");
            builder.CloseBlock();
            builder.AppendLine("return npc;");
            builder.CloseBlock();
        }

        private static void AppendCustomCallerIconHelper(ICodeBuilder builder, PhoneCallBlueprint phoneCall)
        {
            builder.AppendComment("Loads an optional embedded caller icon resource for custom callers.");
            builder.OpenBlock("private static Sprite? LoadCallerIcon()");
            if (string.IsNullOrWhiteSpace(phoneCall.CallerIconResourcePath))
            {
                builder.AppendLine("return null;");
                builder.CloseBlock();
                return;
            }

            builder.OpenBlock("try");
            builder.AppendLine($"var resourceName = ResolveEmbeddedResourceName(\"{CodeFormatter.EscapeString(phoneCall.CallerIconResourcePath)}\");");
            builder.OpenBlock("if (resourceName == null)");
            builder.AppendLine("return null;");
            builder.CloseBlock();
            builder.AppendLine("var assembly = Assembly.GetExecutingAssembly();");
            builder.AppendLine("using var stream = assembly.GetManifestResourceStream(resourceName);");
            builder.OpenBlock("if (stream == null)");
            builder.AppendLine("return null;");
            builder.CloseBlock();
            builder.AppendLine("byte[] data = new byte[stream.Length];");
            builder.AppendLine("stream.Read(data, 0, data.Length);");
            builder.AppendLine("return ImageUtils.LoadImageRaw(data);");
            builder.CloseBlock();
            builder.OpenBlock("catch (Exception ex)");
            builder.AppendLine($"MelonLogger.Warning($\"Failed to load phone call caller icon '{CodeFormatter.EscapeString(phoneCall.CallerIconResourcePath)}': {{ex.Message}}\");");
            builder.AppendLine("return null;");
            builder.CloseBlock();
            builder.CloseBlock();
            builder.AppendLine();
            builder.OpenBlock("private static string? ResolveEmbeddedResourceName(string relativePath)");
            builder.AppendLine("if (string.IsNullOrWhiteSpace(relativePath))");
            builder.AppendLine("    return null;");
            builder.AppendLine();
            builder.AppendLine("var normalized = relativePath.Replace('\\\\', '.').Replace('/', '.');");
            builder.AppendLine("var assembly = Assembly.GetExecutingAssembly();");
            builder.AppendLine("return assembly.GetManifestResourceNames().FirstOrDefault(name =>");
            builder.AppendLine("    string.Equals(name, normalized, StringComparison.OrdinalIgnoreCase) ||");
            builder.AppendLine("    name.EndsWith('.' + normalized, StringComparison.OrdinalIgnoreCase));");
            builder.CloseBlock();
        }

        private static void AppendPartialHooks(ICodeBuilder builder)
        {
            builder.AppendComment("Optional hook surface for advanced custom logic.");
            builder.AppendLine("partial void ConfigureGeneratedCall();");
            builder.AppendLine("partial void OnGeneratedStageBuilt(string stageKey, CallStageEntry stage);");
            builder.AppendLine("partial void OnGeneratedTriggerBuilt(string stageKey, string triggerPhase, int triggerIndex, SystemTriggerEntry trigger);");
        }

        private static string GetRootNamespace(string normalizedNamespace)
        {
            const string phoneCallSuffix = ".PhoneCalls";
            return normalizedNamespace.EndsWith(phoneCallSuffix, StringComparison.Ordinal)
                ? normalizedNamespace[..^phoneCallSuffix.Length]
                : normalizedNamespace;
        }
    }
}
