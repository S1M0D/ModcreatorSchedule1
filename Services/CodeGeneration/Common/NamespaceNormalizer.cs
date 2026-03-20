using System.Text;

namespace Schedule1ModdingTool.Services.CodeGeneration.Common
{
    /// <summary>
    /// Normalizes namespace strings to valid C# namespaces.
    /// Ensures namespace segments are valid identifiers.
    /// </summary>
    public static class NamespaceNormalizer
    {
        private const string DefaultNamespace = "Schedule1Mods.Quests";
        private const string DefaultNpcNamespace = "Schedule1Mods.NPCs";
        private const string DefaultItemNamespace = "Schedule1Mods.Items";

        /// <summary>
        /// Normalizes a namespace string to a valid C# namespace.
        /// Splits on dots, sanitizes each segment, and rebuilds the namespace.
        /// </summary>
        /// <param name="namespaceValue">The namespace to normalize.</param>
        /// <returns>A valid namespace string.</returns>
        public static string Normalize(string? namespaceValue)
        {
            return Normalize(namespaceValue, DefaultNamespace);
        }

        /// <summary>
        /// Normalizes a namespace string to a valid C# namespace with a custom default.
        /// Splits on dots, sanitizes each segment, and rebuilds the namespace.
        /// </summary>
        /// <param name="namespaceValue">The namespace to normalize.</param>
        /// <param name="defaultNamespace">The default namespace to use if normalization fails.</param>
        /// <returns>A valid namespace string.</returns>
        public static string Normalize(string? namespaceValue, string defaultNamespace)
        {
            if (string.IsNullOrWhiteSpace(namespaceValue))
            {
                return defaultNamespace;
            }

            var segments = namespaceValue.Split(new[] { '.' }, StringSplitOptions.RemoveEmptyEntries);
            if (segments.Length == 0)
            {
                return defaultNamespace;
            }

            var builder = new StringBuilder();
            for (int i = 0; i < segments.Length; i++)
            {
                if (builder.Length > 0)
                {
                    builder.Append('.');
                }

                // Use different fallbacks for first vs. subsequent segments
                var fallback = i == 0 ? "Schedule1Mods" : "Generated";
                var sanitized = IdentifierSanitizer.MakeSafeIdentifier(segments[i], fallback);
                builder.Append(sanitized);
            }

            return builder.ToString();
        }

        /// <summary>
        /// Normalizes a namespace for NPC code generation.
        /// Uses a default NPC-specific namespace if input is invalid.
        /// </summary>
        /// <param name="namespaceValue">The namespace to normalize.</param>
        /// <returns>A valid namespace string for NPC code.</returns>
        public static string NormalizeForNpc(string? namespaceValue)
        {
            return Normalize(namespaceValue, DefaultNpcNamespace);
        }

        /// <summary>
        /// Normalizes a namespace for item code generation.
        /// Uses a default item-specific namespace if input is invalid.
        /// </summary>
        public static string NormalizeForItem(string? namespaceValue)
        {
            return Normalize(namespaceValue, DefaultItemNamespace);
        }

        /// <summary>
        /// Validates whether a namespace string is valid according to C# rules.
        /// </summary>
        /// <param name="namespaceValue">The namespace to validate.</param>
        /// <returns>True if the namespace is valid, false otherwise.</returns>
        public static bool IsValidNamespace(string? namespaceValue)
        {
            if (string.IsNullOrWhiteSpace(namespaceValue))
                return false;

            var segments = namespaceValue.Split('.');
            if (segments.Length == 0)
                return false;

            // Each segment must be a valid identifier
            return segments.All(segment => IdentifierSanitizer.IsValidIdentifier(segment));
        }

        /// <summary>
        /// Gets the root namespace from a fully-qualified namespace.
        /// For example, "MyMod.Quests.Custom" returns "MyMod".
        /// </summary>
        /// <param name="namespaceValue">The namespace to extract from.</param>
        /// <returns>The root namespace segment, or null if invalid.</returns>
        public static string? GetRootNamespace(string? namespaceValue)
        {
            if (string.IsNullOrWhiteSpace(namespaceValue))
                return null;

            var firstDot = namespaceValue.IndexOf('.');
            return firstDot >= 0
                ? namespaceValue.Substring(0, firstDot)
                : namespaceValue;
        }

        /// <summary>
        /// Joins namespace segments into a qualified namespace string.
        /// </summary>
        /// <param name="segments">The namespace segments to join.</param>
        /// <returns>A dot-separated namespace string.</returns>
        public static string Join(params string[] segments)
        {
            if (segments == null || segments.Length == 0)
                return DefaultNamespace;

            var sanitized = segments
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Select((s, i) =>
                {
                    var fallback = i == 0 ? "Schedule1Mods" : "Generated";
                    return IdentifierSanitizer.MakeSafeIdentifier(s, fallback);
                });

            var result = string.Join(".", sanitized);
            return string.IsNullOrEmpty(result) ? DefaultNamespace : result;
        }
    }
}
