using Schedule1ModdingTool.Models;
using Schedule1ModdingTool.Services.CodeGeneration.Abstractions;
using Schedule1ModdingTool.Services.CodeGeneration.Builders;
using Schedule1ModdingTool.Services.CodeGeneration.Common;

namespace Schedule1ModdingTool.Services.CodeGeneration.GlobalState
{
    /// <summary>
    /// Generates S1API saveable classes from authored global state blueprints.
    /// </summary>
    public class GlobalStateCodeGenerator : ICodeGenerator<GlobalStateBlueprint>
    {
        public string GenerateCode(GlobalStateBlueprint globalState)
        {
            ArgumentNullException.ThrowIfNull(globalState);

            var builder = new CodeBuilder();
            var className = IdentifierSanitizer.MakeSafeIdentifier(globalState.ClassName, "GeneratedGlobalState");
            var targetNamespace = NamespaceNormalizer.Normalize(globalState.Namespace, "Schedule1Mods.Saveables");
            var usingsBuilder = new UsingStatementsBuilder();
            usingsBuilder.Add(
                "System",
                "System.Collections.Generic",
                "S1API.Internal.Abstraction",
                "S1API.Saveables");

            builder.AppendComment("Auto-generated S1API saveable definition.");
            usingsBuilder.GenerateUsings(builder);
            builder.OpenBlock($"namespace {targetNamespace}");
            GenerateSaveableClass(builder, globalState, className);
            builder.CloseBlock();
            return builder.Build();
        }

        public CodeGenerationValidationResult Validate(GlobalStateBlueprint blueprint)
        {
            var result = new CodeGenerationValidationResult { IsValid = true };

            if (blueprint == null)
            {
                result.IsValid = false;
                result.Errors.Add("Global state blueprint cannot be null.");
                return result;
            }

            if (string.IsNullOrWhiteSpace(blueprint.ClassName))
            {
                result.Errors.Add("Class name is required.");
            }

            if (string.IsNullOrWhiteSpace(blueprint.Namespace))
            {
                result.Errors.Add("Namespace is required.");
            }

            if (blueprint.Fields.Count == 0)
            {
                result.Warnings.Add("No save fields are defined. The generated saveable will not persist any data.");
            }

            var fieldNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var saveKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (var index = 0; index < blueprint.Fields.Count; index++)
            {
                var field = blueprint.Fields[index];
                var label = $"Field {index + 1}";

                if (string.IsNullOrWhiteSpace(field.FieldName))
                {
                    result.Errors.Add($"{label}: field name is required.");
                    continue;
                }

                if (!fieldNames.Add(field.FieldName.Trim()))
                {
                    result.Errors.Add($"{label}: duplicate field name '{field.FieldName}'.");
                }

                var saveKey = field.ResolvedSaveKey?.Trim();
                if (string.IsNullOrWhiteSpace(saveKey))
                {
                    result.Errors.Add($"{label}: save key resolves to an empty value.");
                }
                else if (!saveKeys.Add(saveKey))
                {
                    result.Errors.Add($"{label}: duplicate save key '{saveKey}'.");
                }
            }

            result.IsValid = result.Errors.Count == 0;
            return result;
        }

        private static void GenerateSaveableClass(ICodeBuilder builder, GlobalStateBlueprint globalState, string className)
        {
            builder.AppendBlockComment(
                $"Builds the generated saveable \"{CodeFormatter.EscapeString(globalState.DisplayName)}\".",
                "S1API auto-discovers Saveable subclasses, so this class does not require manual registration in Core."
            );
            builder.OpenBlock($"public partial class {className} : Saveable");

            builder.AppendLine($"public static {className}? Instance {{ get; private set; }}");
            builder.AppendLine();
            builder.AppendLine($"public {className}()");
            builder.OpenBlock();
            builder.AppendLine("Instance = this;");
            builder.CloseBlock();
            builder.AppendLine();

            builder.AppendLine($"public override SaveableLoadOrder LoadOrder => SaveableLoadOrder.{globalState.LoadOrder};");
            builder.AppendLine("public static bool RequestGeneratedSave() => RequestGameSave();");
            builder.AppendLine();

            AppendGeneratedFields(builder, globalState);
            AppendGeneratedHelpers(builder);

            if (globalState.GenerateHookScaffold)
            {
                AppendGeneratedHooks(builder);
            }

            builder.CloseBlock();
        }

        private static void AppendGeneratedFields(ICodeBuilder builder, GlobalStateBlueprint globalState)
        {
            if (globalState.Fields.Count == 0)
            {
                builder.AppendComment("Add save fields in the editor to persist global mod state.");
                builder.AppendLine();
                return;
            }

            for (var index = 0; index < globalState.Fields.Count; index++)
            {
                var field = globalState.Fields[index];
                var propertyName = IdentifierSanitizer.MakeSafeIdentifier(field.FieldName, $"Field{index + 1}");
                var backingFieldName = "_" + char.ToLowerInvariant(propertyName[0]) + propertyName[1..];
                var typeName = GetCSharpTypeName(field.FieldType);
                var defaultValue = GetDefaultValueString(field.FieldType, field.DefaultValue);
                var saveKey = string.IsNullOrWhiteSpace(field.ResolvedSaveKey)
                    ? propertyName
                    : field.ResolvedSaveKey.Trim();

                if (!string.IsNullOrWhiteSpace(field.Comment))
                {
                    builder.AppendLine("/// <summary>");
                    builder.AppendLine($"/// {CodeFormatter.EscapeString(field.Comment)}");
                    builder.AppendLine("/// </summary>");
                }

                builder.AppendLine($"[SaveableField(\"{CodeFormatter.EscapeString(saveKey)}\")]");
                builder.AppendLine($"private {typeName} {backingFieldName} = {defaultValue};");
                builder.AppendLine($"public {typeName} {propertyName}");
                builder.OpenBlock();
                builder.AppendLine($"get => {backingFieldName};");
                if (field.FieldType == DataClassFieldType.ListString)
                {
                    builder.AppendLine($"set => {backingFieldName} = value ?? new List<string>();");
                }
                else
                {
                    builder.AppendLine($"set => {backingFieldName} = value;");
                }
                builder.CloseBlock();
                builder.AppendLine();

                builder.AppendLine($"public void Set{propertyName}({typeName} value, bool requestSave = false)");
                builder.OpenBlock();
                if (field.FieldType == DataClassFieldType.ListString)
                {
                    builder.AppendLine($"{backingFieldName} = value ?? new List<string>();");
                }
                else
                {
                    builder.AppendLine($"{backingFieldName} = value;");
                }
                builder.AppendLine("if (requestSave)");
                builder.AppendLine("    RequestGameSave();");
                builder.CloseBlock();
                builder.AppendLine();
            }
        }

        private static void AppendGeneratedHelpers(ICodeBuilder builder)
        {
            builder.AppendComment("Convenience wrapper for mods that want an instance method instead of the static Saveable helper.");
            builder.AppendLine("public bool RequestSaveNow() => RequestGameSave();");
            builder.AppendLine();
        }

        private static void AppendGeneratedHooks(ICodeBuilder builder)
        {
            builder.AppendComment("Optional hook surface for advanced saveable logic.");
            builder.AppendLine("partial void OnAfterLoadedGenerated();");
            builder.AppendLine("partial void OnAfterSavedGenerated();");
            builder.AppendLine();

            builder.OpenBlock("protected override void OnLoaded()");
            builder.AppendLine("base.OnLoaded();");
            builder.AppendLine("OnAfterLoadedGenerated();");
            builder.CloseBlock();
            builder.AppendLine();

            builder.OpenBlock("protected override void OnSaved()");
            builder.AppendLine("base.OnSaved();");
            builder.AppendLine("OnAfterSavedGenerated();");
            builder.CloseBlock();
            builder.AppendLine();
        }

        private static string GetCSharpTypeName(DataClassFieldType fieldType)
        {
            return fieldType switch
            {
                DataClassFieldType.Bool => "bool",
                DataClassFieldType.Int => "int",
                DataClassFieldType.Float => "float",
                DataClassFieldType.String => "string",
                DataClassFieldType.ListString => "List<string>",
                _ => "object"
            };
        }

        private static string GetDefaultValueString(DataClassFieldType fieldType, string defaultValue)
        {
            if (string.IsNullOrWhiteSpace(defaultValue))
            {
                return fieldType switch
                {
                    DataClassFieldType.Bool => "false",
                    DataClassFieldType.Int => "0",
                    DataClassFieldType.Float => "0f",
                    DataClassFieldType.String => "string.Empty",
                    DataClassFieldType.ListString => "new List<string>()",
                    _ => "null"
                };
            }

            return fieldType switch
            {
                DataClassFieldType.Bool => bool.TryParse(defaultValue, out _) ? defaultValue.ToLowerInvariant() : "false",
                DataClassFieldType.Int => int.TryParse(defaultValue, out _) ? defaultValue : "0",
                DataClassFieldType.Float => float.TryParse(defaultValue, out _) ? $"{defaultValue}f" : "0f",
                DataClassFieldType.String => $"\"{CodeFormatter.EscapeString(defaultValue)}\"",
                DataClassFieldType.ListString => GetListStringDefaultValue(defaultValue),
                _ => "null"
            };
        }

        private static string GetListStringDefaultValue(string defaultValue)
        {
            if (string.IsNullOrWhiteSpace(defaultValue))
            {
                return "new List<string>()";
            }

            var items = defaultValue
                .Split(new[] { ',', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(item => item.Trim())
                .Where(item => !string.IsNullOrWhiteSpace(item))
                .ToList();

            if (items.Count == 0)
            {
                return "new List<string>()";
            }

            var itemStrings = items.Select(item => $"\"{CodeFormatter.EscapeString(item)}\"");
            return $"new List<string> {{ {string.Join(", ", itemStrings)} }}";
        }
    }
}
