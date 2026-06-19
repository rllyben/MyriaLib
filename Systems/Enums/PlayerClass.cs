namespace MyriaLib.Systems.Enums
{
    /// <summary>
    /// Built-in character class identifiers. Stored and compared as strings so that
    /// mod-added classes (defined in Data/common/classes.json) work the same way as
    /// these built-ins without requiring any code changes.
    /// </summary>
    public static class CharacterClass
    {
        public const string Archer        = "Archer";
        public const string Hunter        = "Hunter";
        public const string Knight        = "Knight";
        public const string Fighter       = "Fighter";
        public const string Barbarian     = "Barbarian";
        public const string Cleric        = "Cleric";
        public const string Rogue         = "Rogue";
        public const string ElementalMage = "ElementalMage";
        public const string ArcanMage     = "ArcanMage";
        public const string Druid         = "Druid";
        public const string SoulsKnight   = "SoulsKnight";
        public const string RunicMage     = "RunicMage";

        /// <summary>All built-in class IDs. Mod-added classes appear in ClassProfile.All.Keys.</summary>
        public static readonly IReadOnlyList<string> AllBuiltIn = new[]
        {
            Archer, Hunter, Knight, Fighter, Barbarian, Cleric,
            Rogue, ElementalMage, ArcanMage, Druid, SoulsKnight, RunicMage
        };

        /// <summary>Maps the old enum integer values to string IDs (for save-file migration).</summary>
        public static readonly IReadOnlyDictionary<int, string> FromLegacyInt =
            new Dictionary<int, string>
            {
                [0]  = Archer,
                [1]  = Hunter,
                [2]  = Knight,
                [3]  = Fighter,
                [4]  = Barbarian,
                [5]  = Cleric,
                [6]  = Rogue,
                [7]  = ElementalMage,
                [8]  = ArcanMage,
                [9]  = Druid,
                [10] = SoulsKnight,
                [11] = RunicMage,
            };
    }
}
