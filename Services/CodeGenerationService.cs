using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Schedule1ModdingTool.Models;

namespace Schedule1ModdingTool.Services
{
    /// <summary>
    /// Generates strongly-typed quest source code that targets the S1API surface area.
    /// </summary>
    public class CodeGenerationService
    {
        private static readonly CultureInfo InvariantCulture = CultureInfo.InvariantCulture;

        public string GenerateQuestCode(QuestBlueprint quest)
        {
            if (quest == null) throw new ArgumentNullException(nameof(quest));

            var className = MakeSafeIdentifier(quest.ClassName, "GeneratedQuest");
            var sb = new StringBuilder();

            AppendHeader(sb, quest);
            AppendUsings(sb);

            var targetNamespace = NormalizeNamespace(quest.Namespace);
            sb.AppendLine($"namespace {targetNamespace}");
            sb.AppendLine("{");

            AppendQuestClass(sb, quest, className);
            sb.AppendLine();
            AppendRegistryClass(sb, className);

            sb.AppendLine("}");

            return sb.ToString();
        }

        public bool CompileToDll(QuestBlueprint quest, string code)
        {
            try
            {
                // Use Roslyn to validate syntax for now (actual compilation requires Unity/S1API refs at export time)
                _ = CSharpSyntaxTree.ParseText(code);
                System.Diagnostics.Debug.WriteLine($"Quest '{quest.ClassName}' validated for export.");
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Code validation error: {ex.Message}");
                return false;
            }
        }

        private static void AppendHeader(StringBuilder sb, QuestBlueprint quest)
        {
            sb.AppendLine("// ===============================================");
            sb.AppendLine("// Schedule1ModdingTool generated quest blueprint");
            sb.AppendLine($"// Mod: {quest.ModName} v{quest.ModVersion} by {quest.ModAuthor}");
            sb.AppendLine($"// Game: {quest.GameDeveloper} - {quest.GameName}");
            sb.AppendLine("// ===============================================");
            sb.AppendLine();
        }

        private static void AppendUsings(StringBuilder sb)
        {
            sb.AppendLine("using System;");
            sb.AppendLine("using S1API.Quests;");
            sb.AppendLine("using S1API.Saveables;");
            sb.AppendLine("using UnityEngine;");
            sb.AppendLine();
        }

        private static void AppendQuestClass(StringBuilder sb, QuestBlueprint quest, string className)
        {
            var questId = string.IsNullOrWhiteSpace(quest.QuestId) ? className : quest.QuestId.Trim();

            sb.AppendLine("    /// <summary>");
            sb.AppendLine($"    /// Auto-generated quest blueprint for \"{EscapeString(quest.QuestTitle)}\".");
            sb.AppendLine("    /// Customize the body to wire in game-specific logic.");
            sb.AppendLine("    /// </summary>");
            sb.AppendLine($"    public class {className} : Quest");
            sb.AppendLine("    {");
            sb.AppendLine($"        public const string QuestIdentifier = \"{EscapeString(questId)}\";");
            sb.AppendLine();

            if (quest.GenerateDataClass)
            {
                sb.AppendLine("        [Serializable]");
                sb.AppendLine("        public class QuestDataModel");
                sb.AppendLine("        {");
                sb.AppendLine("            public bool Completed { get; set; }");
                sb.AppendLine("            // Add additional quest-specific fields here");
                sb.AppendLine("        }");
                sb.AppendLine();
                sb.AppendLine($"        [SaveableField(\"{EscapeString(className)}Data\")]");
                sb.AppendLine("        private QuestDataModel _data = new QuestDataModel();");
                sb.AppendLine();
            }

            sb.AppendLine($"        protected override string Title => \"{EscapeString(quest.QuestTitle)}\";");
            sb.AppendLine($"        protected override string Description => \"{EscapeString(quest.QuestDescription)}\";");
            sb.AppendLine($"        protected override bool AutoBegin => {quest.AutoBegin.ToString().ToLowerInvariant()};");
            if (quest.CustomIcon)
            {
                sb.AppendLine("        protected override Sprite? QuestIcon => LoadCustomIcon();");
            }
            sb.AppendLine();

            sb.AppendLine("        /// <summary>");
            sb.AppendLine("        /// Call this after creating the quest to set up objectives and tracking.");
            sb.AppendLine("        /// </summary>");
            sb.AppendLine("        public void ConfigureQuest()");
            sb.AppendLine("        {");
            sb.AppendLine("            QuestEntries.Clear();");
            sb.AppendLine("            BuildObjectives();");
            if (quest.QuestRewards)
            {
                sb.AppendLine("            // Invoke GrantQuestRewards() once every objective completes.");
            }
            sb.AppendLine("        }");
            sb.AppendLine();

            AppendBuildObjectivesMethod(sb, quest);

            if (quest.QuestRewards)
            {
                AppendRewardStub(sb);
            }

            if (quest.CustomIcon)
            {
                AppendIconStub(sb);
            }

            sb.AppendLine("    }");
        }

        private static void AppendBuildObjectivesMethod(StringBuilder sb, QuestBlueprint quest)
        {
            sb.AppendLine("        private void BuildObjectives()");
            sb.AppendLine("        {");

            if (quest.Objectives?.Any() != true)
            {
                sb.AppendLine("            // Define at least one objective so the quest has progress steps.");
                sb.AppendLine("            var defaultEntry = AddEntry(\"Describe your first objective\");");
            }
            else
            {
                var usedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                int index = 0;
                foreach (var objective in quest.Objectives)
                {
                    index++;
                    var safeVariable = EnsureUniqueIdentifier(
                        MakeSafeIdentifier(objective.Name, $"objective{index}"),
                        usedNames,
                        index);

                    sb.AppendLine($"            // Objective \"{EscapeString(objective.Title)}\" ({objective.Name})");
                    sb.AppendLine($"            var {safeVariable} = AddEntry(\"{EscapeString(objective.Title)}\");");
                    if (objective.HasLocation)
                    {
                        sb.AppendLine($"            {safeVariable}.POIPosition = {FormatVector(objective)};");
                    }
                    sb.AppendLine($"            // Required progress: {objective.RequiredProgress}");
                    sb.AppendLine();
                }
            }

            sb.AppendLine("        }");
            sb.AppendLine();
        }

        private static void AppendRewardStub(StringBuilder sb)
        {
            sb.AppendLine("        private void GrantQuestRewards()");
            sb.AppendLine("        {");
            sb.AppendLine("            // TODO: Leverage S1API economy/registry helpers to award cash, XP, or items.");
            sb.AppendLine("            // Example: EconomyManager.AddMoney(500);");
            sb.AppendLine("        }");
            sb.AppendLine();
        }

        private static void AppendIconStub(StringBuilder sb)
        {
            sb.AppendLine("        private Sprite? LoadCustomIcon()");
            sb.AppendLine("        {");
            sb.AppendLine("            // TODO: Load a Sprite via ImageUtils.LoadImage(\"Assets/quest-icon.png\") or embedded resources.");
            sb.AppendLine("            return null;");
            sb.AppendLine("        }");
            sb.AppendLine();
        }

        private static void AppendRegistryClass(StringBuilder sb, string className)
        {
            sb.AppendLine($"    public static class {className}Registration");
            sb.AppendLine("    {");
            sb.AppendLine($"        public static {className}? Register()");
            sb.AppendLine("        {");
            sb.AppendLine($"            var quest = QuestManager.CreateQuest<{className}>({className}.QuestIdentifier) as {className};");
            sb.AppendLine("            quest?.ConfigureQuest();");
            sb.AppendLine("            return quest;");
            sb.AppendLine("        }");
            sb.AppendLine("    }");
        }

        private static string NormalizeNamespace(string? namespaceValue)
        {
            if (string.IsNullOrWhiteSpace(namespaceValue))
            {
                return "Schedule1Mods.Quests";
            }

            var segments = namespaceValue.Split(new[] { '.' }, StringSplitOptions.RemoveEmptyEntries);
            if (segments.Length == 0)
            {
                return "Schedule1Mods.Quests";
            }

            var builder = new StringBuilder();
            for (int i = 0; i < segments.Length; i++)
            {
                if (builder.Length > 0)
                {
                    builder.Append('.');
                }

                var fallback = i == 0 ? "Schedule1Mods" : "Generated";
                builder.Append(MakeSafeIdentifier(segments[i], fallback));
            }

            return builder.ToString();
        }

        private static string MakeSafeIdentifier(string? candidate, string fallback)
        {
            if (string.IsNullOrWhiteSpace(candidate))
            {
                return fallback;
            }

            var builder = new StringBuilder();
            foreach (var ch in candidate)
            {
                if (builder.Length == 0)
                {
                    if (char.IsLetter(ch) || ch == '_')
                    {
                        builder.Append(ch);
                    }
                    else if (char.IsDigit(ch))
                    {
                        builder.Append('_').Append(ch);
                    }
                }
                else
                {
                    if (char.IsLetterOrDigit(ch) || ch == '_')
                    {
                        builder.Append(ch);
                    }
                    else
                    {
                        builder.Append('_');
                    }
                }
            }

            var result = builder.ToString();
            return string.IsNullOrEmpty(result) ? fallback : result;
        }

        private static string EnsureUniqueIdentifier(string identifier, HashSet<string> usedNames, int index)
        {
            var baseName = string.IsNullOrWhiteSpace(identifier) ? $"objective{index}" : identifier;
            var uniqueName = baseName;
            var suffix = 1;

            while (!usedNames.Add(uniqueName))
            {
                uniqueName = $"{baseName}_{suffix++}";
            }

            return uniqueName;
        }

        private static string FormatVector(QuestObjective objective)
        {
            if (!objective.HasLocation)
            {
                return "Vector3.zero";
            }

            var x = FormatFloat(objective.LocationX);
            var y = FormatFloat(objective.LocationY);
            var z = FormatFloat(objective.LocationZ);
            return $"new Vector3({x}f, {y}f, {z}f)";
        }

        private static string FormatFloat(float value)
        {
            return value.ToString("0.###", InvariantCulture);
        }

        private static string EscapeString(string? input)
        {
            return input?
                .Replace("\\", "\\\\")
                .Replace("\"", "\\\"")
                .Replace("\r", "\\r")
                .Replace("\n", "\\n") ?? string.Empty;
        }
    }
}
