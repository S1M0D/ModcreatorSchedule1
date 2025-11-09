using System;
using Schedule1ModdingTool.Models;
using Newtonsoft.Json;

namespace Schedule1ModdingTool.Services
{
    /// <summary>
    /// Utility class for serializing NpcAppearanceSettings to JSON for transmission to the Connector mod.
    /// </summary>
    public static class AppearanceConverter
    {
        /// <summary>
        /// Serializes NpcAppearanceSettings to JSON string for transmission.
        /// </summary>
        public static string SerializeToJson(NpcAppearanceSettings appearance)
        {
            if (appearance == null)
                throw new ArgumentNullException(nameof(appearance));

            return JsonConvert.SerializeObject(appearance, Formatting.None);
        }
    }
}

