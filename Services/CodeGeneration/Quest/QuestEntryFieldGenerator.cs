using System;
using System.Collections.Generic;
using System.Linq;
using Schedule1ModdingTool.Models;
using Schedule1ModdingTool.Services.CodeGeneration.Abstractions;
using Schedule1ModdingTool.Services.CodeGeneration.Common;

namespace Schedule1ModdingTool.Services.CodeGeneration.Quest
{
    /// <summary>
    /// Generates field declarations for quest entries (objectives).
    /// Maintains consistent naming across OnCreated, OnLoaded, and trigger methods.
    /// </summary>
    public class QuestEntryFieldGenerator
    {
        /// <summary>
        /// Generates field declarations for all quest entry fields.
        /// </summary>
        /// <param name="builder">The code builder to append to.</param>
        /// <param name="quest">The quest blueprint.</param>
        public void Generate(ICodeBuilder builder, QuestBlueprint quest)
        {
            if (builder == null)
                throw new ArgumentNullException(nameof(builder));
            if (quest == null)
                throw new ArgumentNullException(nameof(quest));

            if (quest.Objectives?.Any() != true)
                return;

            builder.AppendComment("🔧 Generated from: Quest.Objectives[] - one field per objective");
            builder.AppendComment("Quest entry fields for objectives");

            var usedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            int index = 0;

            foreach (var objective in quest.Objectives)
            {
                index++;
                var safeVariable = IdentifierSanitizer.EnsureUniqueIdentifier(
                    IdentifierSanitizer.MakeSafeIdentifier(objective.Name, $"objective{index}"),
                    usedNames,
                    index);

                builder.AppendComment($"🔧 From: Objectives[{index - 1}].Name = \"{objective.Name}\"");
                builder.AppendLine($"private QuestEntry {safeVariable};");
            }

            builder.AppendLine();
        }

        /// <summary>
        /// Gets the sanitized variable name for an objective at a given index.
        /// Used by other generators to reference the same variable names.
        /// IMPORTANT: This must use the same logic as Generate() to ensure consistency.
        /// </summary>
        /// <param name="quest">The quest blueprint.</param>
        /// <param name="objectiveIndex">The zero-based index of the objective.</param>
        /// <returns>The variable name for the objective entry.</returns>
        public string GetObjectiveVariableName(QuestBlueprint quest, int objectiveIndex)
        {
            if (quest == null)
                throw new ArgumentNullException(nameof(quest));

            if (quest.Objectives == null || objectiveIndex >= quest.Objectives.Count)
                return $"objective{objectiveIndex + 1}";

            var usedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            int index = 0;
            string result = "";

            foreach (var objective in quest.Objectives)
            {
                index++;
                var safeVariable = IdentifierSanitizer.EnsureUniqueIdentifier(
                    IdentifierSanitizer.MakeSafeIdentifier(objective.Name, $"objective{index}"),
                    usedNames,
                    index);

                if (index - 1 == objectiveIndex)
                {
                    result = safeVariable;
                    break;
                }
            }

            return result;
        }

        /// <summary>
        /// Gets all objective variable names in order.
        /// Useful for iteration scenarios.
        /// </summary>
        /// <param name="quest">The quest blueprint.</param>
        /// <returns>List of variable names for each objective.</returns>
        public List<string> GetAllObjectiveVariableNames(QuestBlueprint quest)
        {
            if (quest == null)
                throw new ArgumentNullException(nameof(quest));

            if (quest.Objectives?.Any() != true)
                return new List<string>();

            var usedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var names = new List<string>();
            int index = 0;

            foreach (var objective in quest.Objectives)
            {
                index++;
                var safeVariable = IdentifierSanitizer.EnsureUniqueIdentifier(
                    IdentifierSanitizer.MakeSafeIdentifier(objective.Name, $"objective{index}"),
                    usedNames,
                    index);

                names.Add(safeVariable);
            }

            return names;
        }
    }
}
