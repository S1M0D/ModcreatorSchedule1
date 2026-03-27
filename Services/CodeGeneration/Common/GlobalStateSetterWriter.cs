using Schedule1ModdingTool.Models;
using Schedule1ModdingTool.Services.CodeGeneration.Abstractions;

namespace Schedule1ModdingTool.Services.CodeGeneration.Common
{
    /// <summary>
    /// Emits generated calls into Core.SetGeneratedGlobalStateValue for authored saveable setters.
    /// </summary>
    public static class GlobalStateSetterWriter
    {
        public static bool AppendSetterInvocations(
            ICodeBuilder builder,
            IEnumerable<GlobalStateSetterBlueprint> setters,
            string rootNamespace)
        {
            ArgumentNullException.ThrowIfNull(builder);
            ArgumentNullException.ThrowIfNull(setters);

            var wroteAny = false;
            foreach (var setter in setters)
            {
                if (string.IsNullOrWhiteSpace(setter.GlobalStateClassName) ||
                    string.IsNullOrWhiteSpace(setter.FieldSaveKey))
                {
                    continue;
                }

                builder.AppendLine(
                    $"global::{rootNamespace}.Core.SetGeneratedGlobalStateValue(" +
                    $"\"{CodeFormatter.EscapeString(setter.GlobalStateClassName)}\", " +
                    $"\"{CodeFormatter.EscapeString(setter.FieldSaveKey)}\", " +
                    $"\"{CodeFormatter.EscapeString(setter.NewValue)}\", " +
                    $"{setter.RequestSave.ToString().ToLowerInvariant()});");
                wroteAny = true;
            }

            return wroteAny;
        }
    }
}
