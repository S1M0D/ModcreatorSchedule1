namespace Schedule1ModdingTool.Services
{
    /// <summary>
    /// Canonical base-game quest identifiers exposed by S1API.Quests.Identifiers.
    /// Stores the current identifier type name plus legacy IDs used by older tool versions.
    /// </summary>
    public static class BaseGameQuestCatalogService
    {
        private static readonly IReadOnlyList<BaseGameQuestDefinition> _quests = new[]
        {
            new BaseGameQuestDefinition("Botanists", "Botanists", "Quest_Botanists"),
            new BaseGameQuestDefinition("Chemists", "Chemists", "Quest_Chemists"),
            new BaseGameQuestDefinition("CleanCash", "Clean Cash", "Quest_CleanCash"),
            new BaseGameQuestDefinition("Cleaners", "Cleaners", "Quest_Cleaners"),
            new BaseGameQuestDefinition("DealForCartel", "Deal for the Benzies Family", "Quest_DealForCartel"),
            new BaseGameQuestDefinition("DefeatCartel", "Finishing the Job", "Quest_DefeatCartel"),
            new BaseGameQuestDefinition("DodgyDealing", "Dodgy Dealing"),
            new BaseGameQuestDefinition("GearingUp", "Gearing Up", "Quest_GearingUp"),
            new BaseGameQuestDefinition("GettingStarted", "Getting Started", "Quest_GettingStarted"),
            new BaseGameQuestDefinition("KeepingItFresh", "Keeping it Fresh"),
            new BaseGameQuestDefinition("MakingTheRounds", "Making the Rounds"),
            new BaseGameQuestDefinition("MixingMania", "Mixing Mania"),
            new BaseGameQuestDefinition("MoneyManagement", "Money Management"),
            new BaseGameQuestDefinition("MovingUp", "Moving Up", "Quest_MovingUp"),
            new BaseGameQuestDefinition("NeedingTheGreen", "Needin' the Green", "Quest_NeedingTheGreen"),
            new BaseGameQuestDefinition("OnTheGrind", "On the Grind", "Quest_OnTheGrind"),
            new BaseGameQuestDefinition("Packagers", "Handlers", "Quest_Packagers"),
            new BaseGameQuestDefinition("Packin", "Packin'"),
            new BaseGameQuestDefinition("UnfavourableAgreements", "Unfavourable Agreements", "Quest_UnfavourableAgreements"),
            new BaseGameQuestDefinition("Warehouse", "Wretched Hive of Scum and Villainy", "Quest_Warehouse"),
            new BaseGameQuestDefinition("WelcomeToHylandPoint", "Welcome to Hyland Point", "Quest_WelcomeToHylandPoint"),
            new BaseGameQuestDefinition("WeNeedToCook", "We Need To Cook", "Quest_WeNeedToCook")
        };

        private static readonly Dictionary<string, BaseGameQuestDefinition> _lookup = BuildLookup();

        public static IReadOnlyList<BaseGameQuestDefinition> GetQuests() => _quests;

        public static bool TryResolve(string? questIdOrName, out BaseGameQuestDefinition definition)
        {
            definition = null!;

            if (string.IsNullOrWhiteSpace(questIdOrName))
                return false;

            if (_lookup.TryGetValue(questIdOrName.Trim(), out var resolved))
            {
                definition = resolved;
                return true;
            }

            return false;
        }

        private static Dictionary<string, BaseGameQuestDefinition> BuildLookup()
        {
            var lookup = new Dictionary<string, BaseGameQuestDefinition>(StringComparer.OrdinalIgnoreCase);

            foreach (var quest in _quests)
            {
                lookup[quest.IdentifierName] = quest;

                foreach (var legacyId in quest.LegacyIds)
                {
                    lookup[legacyId] = quest;
                }
            }

            return lookup;
        }
    }

    /// <summary>
    /// Base-game quest identifier metadata used by the editor and code generators.
    /// </summary>
    public sealed class BaseGameQuestDefinition
    {
        public BaseGameQuestDefinition(string identifierName, string displayName, params string[] legacyIds)
        {
            IdentifierName = identifierName;
            DisplayName = displayName;
            LegacyIds = legacyIds ?? Array.Empty<string>();
        }

        public string IdentifierName { get; }

        public string DisplayName { get; }

        public IReadOnlyList<string> LegacyIds { get; }
    }
}
