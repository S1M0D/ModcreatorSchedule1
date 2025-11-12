using System;
using System.Collections.Generic;
using System.Linq;
using Schedule1ModdingTool.Services.CodeGeneration.Abstractions;

namespace Schedule1ModdingTool.Services.CodeGeneration.Common
{
    /// <summary>
    /// Builds using statements with automatic deduplication and sorting.
    /// Provides convenient methods for adding common using directive sets.
    /// </summary>
    public class UsingStatementsBuilder
    {
        private readonly HashSet<string> _namespaces = new HashSet<string>();

        /// <summary>
        /// Adds one or more namespaces to the using statements.
        /// Duplicates are automatically ignored.
        /// </summary>
        /// <param name="namespaces">The namespaces to add.</param>
        /// <returns>This builder for method chaining.</returns>
        public UsingStatementsBuilder Add(params string[] namespaces)
        {
            if (namespaces == null)
                return this;

            foreach (var ns in namespaces)
            {
                if (!string.IsNullOrWhiteSpace(ns))
                {
                    _namespaces.Add(ns.Trim());
                }
            }
            return this;
        }

        /// <summary>
        /// Adds standard using statements for Quest code generation.
        /// Includes S1API.Quests, UnityEngine, MelonLoader, and common System namespaces.
        /// Also includes type aliases for accessing base game quests.
        /// </summary>
        /// <returns>This builder for method chaining.</returns>
        public UsingStatementsBuilder AddQuestUsings()
        {
            return Add(
                "System",
                "System.Collections",
                "System.Collections.Generic",
                "System.Reflection",
                "System.Linq",
                "S1API.Quests",
                "S1API.Quests.Constants",
                "S1API.Saveables",
                "S1API.Internal.Utils",
                "S1API.Entities",
                "S1API.GameTime",
                "S1API.Console",
                "S1API.Money",
                "UnityEngine",
                "MelonLoader"
            );
        }

        /// <summary>
        /// Adds standard using statements for NPC code generation.
        /// Includes S1API.Entities, S1API.Economy, S1API.Products (for DrugType), 
        /// S1API.Map (for Building), S1API.Map.Buildings (for building types like ApartmentBuilding),
        /// S1API.Properties (for Property), UnityEngine, and common System namespaces.
        /// </summary>
        /// <returns>This builder for method chaining.</returns>
        public UsingStatementsBuilder AddNpcUsings()
        {
            return Add(
                "System",
                "S1API.Entities",
                "S1API.Entities.Schedule",
                "S1API.GameTime",
                "S1API.Economy",
                "S1API.Products",
                "S1API.Properties",
                "S1API.Map",
                "S1API.Map.Buildings",
                "UnityEngine"
            );
        }

        /// <summary>
        /// Adds common System namespaces.
        /// </summary>
        /// <returns>This builder for method chaining.</returns>
        public UsingStatementsBuilder AddCommonSystemUsings()
        {
            return Add(
                "System",
                "System.Collections.Generic",
                "System.Linq",
                "System.Text"
            );
        }

        /// <summary>
        /// Removes a namespace from the using statements.
        /// </summary>
        /// <param name="namespace">The namespace to remove.</param>
        /// <returns>This builder for method chaining.</returns>
        public UsingStatementsBuilder Remove(string @namespace)
        {
            if (!string.IsNullOrWhiteSpace(@namespace))
            {
                _namespaces.Remove(@namespace.Trim());
            }
            return this;
        }

        /// <summary>
        /// Clears all using statements.
        /// </summary>
        /// <returns>This builder for method chaining.</returns>
        public UsingStatementsBuilder Clear()
        {
            _namespaces.Clear();
            return this;
        }

        /// <summary>
        /// Generates using statements and appends them to the code builder.
        /// Statements are sorted alphabetically for consistency.
        /// </summary>
        /// <param name="builder">The code builder to append to.</param>
        public void GenerateUsings(ICodeBuilder builder)
        {
            if (builder == null)
                throw new ArgumentNullException(nameof(builder));

            // Sort alphabetically for consistency
            foreach (var ns in _namespaces.OrderBy(n => n))
            {
                builder.AppendLine($"using {ns};");
            }
            builder.AppendLine();
        }

        /// <summary>
        /// Builds the using statements as a single string.
        /// Statements are sorted alphabetically.
        /// </summary>
        /// <returns>The using statements as a formatted string.</returns>
        public string Build()
        {
            var lines = _namespaces
                .OrderBy(n => n)
                .Select(ns => $"using {ns};");

            return string.Join(Environment.NewLine, lines) + Environment.NewLine;
        }

        /// <summary>
        /// Gets the count of unique namespaces.
        /// </summary>
        public int Count => _namespaces.Count;

        /// <summary>
        /// Checks if a namespace is included in the using statements.
        /// </summary>
        /// <param name="namespace">The namespace to check.</param>
        /// <returns>True if the namespace is included.</returns>
        public bool Contains(string @namespace)
        {
            return !string.IsNullOrWhiteSpace(@namespace) &&
                   _namespaces.Contains(@namespace.Trim());
        }
    }
}
