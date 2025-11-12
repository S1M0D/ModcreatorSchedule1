using System;
using System.Text;
using System.Text.RegularExpressions;
using Schedule1ModdingTool.Models;

namespace Schedule1ModdingTool.Utils
{
    /// <summary>
    /// Helper methods for validating and normalizing NPC IDs, Quest IDs, and ClassNames
    /// </summary>
    public static class ValidationHelpers
    {
        // Regex pattern for valid NPC/Quest ID: lowercase, alphanumeric, underscores, no leading/trailing underscores, no consecutive underscores
        private static readonly Regex NpcIdPattern = new Regex(@"^[a-z][a-z0-9_]*[a-z0-9]$|^[a-z]$", RegexOptions.Compiled);
        
        // Regex pattern for valid ClassName: PascalCase, alphanumeric, no spaces
        private static readonly Regex ClassNamePattern = new Regex(@"^[A-Z][a-zA-Z0-9]*$", RegexOptions.Compiled);

        /// <summary>
        /// Validates an NPC ID format (lowercase with underscores)
        /// </summary>
        /// <param name="id">The ID to validate</param>
        /// <returns>True if valid, false otherwise</returns>
        public static bool IsValidNpcId(string? id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return false;

            // Check for consecutive underscores
            if (id.Contains("__"))
                return false;

            // Check regex pattern
            return NpcIdPattern.IsMatch(id);
        }

        /// <summary>
        /// Validates a Quest ID format (same as NPC ID)
        /// </summary>
        /// <param name="id">The ID to validate</param>
        /// <returns>True if valid, false otherwise</returns>
        public static bool IsValidQuestId(string? id)
        {
            return IsValidNpcId(id);
        }

        /// <summary>
        /// Validates a ClassName format (PascalCase, no spaces)
        /// </summary>
        /// <param name="name">The name to validate</param>
        /// <returns>True if valid, false otherwise</returns>
        public static bool IsValidClassName(string? name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return false;

            return ClassNamePattern.IsMatch(name);
        }

        /// <summary>
        /// Normalizes an input string to a valid NPC ID format
        /// Converts to lowercase, replaces spaces with underscores, removes invalid characters
        /// </summary>
        /// <param name="input">The input string to normalize</param>
        /// <returns>Normalized NPC ID</returns>
        public static string NormalizeNpcId(string? input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return string.Empty;

            var sb = new StringBuilder();
            bool lastWasUnderscore = false;

            foreach (var ch in input)
            {
                if (char.IsLetterOrDigit(ch))
                {
                    sb.Append(char.ToLowerInvariant(ch));
                    lastWasUnderscore = false;
                }
                else if (ch == ' ' || ch == '-' || ch == '_')
                {
                    // Replace spaces and hyphens with underscores, but don't add consecutive underscores
                    if (!lastWasUnderscore && sb.Length > 0)
                    {
                        sb.Append('_');
                        lastWasUnderscore = true;
                    }
                }
                // Ignore other characters
            }

            var result = sb.ToString();

            // Remove leading/trailing underscores
            result = result.Trim('_');

            // Ensure it starts with a letter
            if (result.Length > 0 && char.IsDigit(result[0]))
            {
                result = "npc_" + result;
            }

            return result;
        }

        /// <summary>
        /// Normalizes an input string to a valid ClassName format (PascalCase)
        /// Removes spaces, ensures first letter is uppercase
        /// </summary>
        /// <param name="input">The input string to normalize</param>
        /// <returns>Normalized ClassName</returns>
        public static string NormalizeClassName(string? input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return string.Empty;

            var sb = new StringBuilder();
            bool nextShouldBeUpper = true;

            foreach (var ch in input)
            {
                if (char.IsLetterOrDigit(ch))
                {
                    if (nextShouldBeUpper)
                    {
                        sb.Append(char.ToUpperInvariant(ch));
                        nextShouldBeUpper = false;
                    }
                    else
                    {
                        sb.Append(ch);
                    }
                }
                else if (ch == ' ' || ch == '-' || ch == '_')
                {
                    // Spaces/hyphens/underscores indicate word boundary - next char should be uppercase
                    nextShouldBeUpper = true;
                }
                // Ignore other characters
            }

            var result = sb.ToString();

            // Ensure it starts with a letter (not a digit)
            if (result.Length > 0 && char.IsDigit(result[0]))
            {
                result = "Class" + result;
            }

            // If empty or doesn't start with uppercase letter, ensure it does
            if (result.Length > 0 && char.IsLower(result[0]))
            {
                result = char.ToUpperInvariant(result[0]) + result.Substring(1);
            }

            return result;
        }

        /// <summary>
        /// Gets a user-friendly error message for an invalid NPC/Quest ID
        /// </summary>
        public static string GetNpcIdErrorMessage(string? id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return "NPC ID cannot be empty";

            if (id.Contains("__"))
                return "NPC ID cannot contain consecutive underscores";

            if (id.StartsWith("_") || id.EndsWith("_"))
                return "NPC ID cannot start or end with an underscore";

            if (id != id.ToLowerInvariant())
                return "NPC ID must be lowercase";

            if (!NpcIdPattern.IsMatch(id))
                return "NPC ID must contain only lowercase letters, numbers, and underscores";

            return "Invalid NPC ID format";
        }

        /// <summary>
        /// Gets a user-friendly error message for an invalid ClassName
        /// </summary>
        public static string GetClassNameErrorMessage(string? name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return "Class name cannot be empty";

            if (name.Contains(" "))
                return "Class name cannot contain spaces";

            if (char.IsDigit(name[0]))
                return "Class name cannot start with a number";

            if (char.IsLower(name[0]))
                return "Class name must start with an uppercase letter (PascalCase)";

            if (!ClassNamePattern.IsMatch(name))
                return "Class name must be PascalCase (e.g., 'BobbyCooley')";

            return "Invalid class name format";
        }

        /// <summary>
        /// Validates a default value string for a given field type.
        /// Empty strings are always valid (will use type default).
        /// </summary>
        /// <param name="defaultValue">The default value string to validate</param>
        /// <param name="fieldType">The field type to validate against</param>
        /// <returns>True if valid, false otherwise</returns>
        public static bool IsValidDefaultValue(string? defaultValue, DataClassFieldType? fieldType)
        {
            // Empty is always valid (will use type default)
            if (string.IsNullOrWhiteSpace(defaultValue))
                return true;

            if (!fieldType.HasValue)
                return true; // Can't validate without type

            return fieldType.Value switch
            {
                DataClassFieldType.Bool => bool.TryParse(defaultValue.Trim(), out _),
                DataClassFieldType.Int => int.TryParse(defaultValue.Trim(), out _),
                DataClassFieldType.Float => float.TryParse(defaultValue.Trim(), out _),
                DataClassFieldType.String => true, // Any string is valid (will be escaped in code generation)
                DataClassFieldType.ListString => true, // Comma-separated or newline-separated values are valid
                _ => true
            };
        }

        /// <summary>
        /// Gets a user-friendly error message for an invalid default value.
        /// </summary>
        public static string GetDefaultValueErrorMessage(string? defaultValue, DataClassFieldType? fieldType)
        {
            if (string.IsNullOrWhiteSpace(defaultValue))
                return string.Empty;

            if (!fieldType.HasValue)
                return "Invalid default value";

            return fieldType.Value switch
            {
                DataClassFieldType.Bool => "Default value must be 'true' or 'false'",
                DataClassFieldType.Int => "Default value must be a whole number (e.g., '100', '0', '-5')",
                DataClassFieldType.Float => "Default value must be a decimal number (e.g., '1.5', '0.0', '-3.14')",
                DataClassFieldType.String => string.Empty, // Any string is valid
                DataClassFieldType.ListString => string.Empty, // Any string is valid (will be parsed as comma/newline-separated)
                _ => "Invalid default value format"
            };
        }
    }
}

