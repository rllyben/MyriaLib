namespace Myria.Lib.Core.Systems.Enums
{
    /// <summary>
    /// Built-in skill targeting identifiers. Stored and compared as strings — like
    /// <see cref="CharacterClass"/>/<see cref="CharacterRace"/>/<see cref="ItemRarity"/>/
    /// <see cref="GatheringType"/>/<see cref="EquipmentType"/> — so mod-added targeting types work
    /// the same way as these built-ins without requiring any code changes. Previously a closed
    /// enum declared in Entities/Skills/Skill.cs. skills.json/SQL always store the current string
    /// form, but character saves can carry a Skill snapshot from before this conversion, storing
    /// Target as the old enum's raw int (e.g. "Target": 0) — handled via FromLegacyInt +
    /// SkillTargetJsonConverter, same pattern as CharacterClass/CharacterRace.
    /// </summary>
    public static class SkillTarget
    {
        public const string SingleEnemy = "SingleEnemy";
        public const string AllEnemies  = "AllEnemies";
        public const string Self        = "Self";
        public const string SingleAlly  = "SingleAlly";
        public const string AllAllies   = "AllAllies";

        public static readonly IReadOnlyList<string> AllBuiltIn = new[]
        {
            SingleEnemy, AllEnemies, Self, SingleAlly, AllAllies
        };

        /// <summary>Maps the old enum integer values to string IDs (for save-file migration).</summary>
        public static readonly IReadOnlyDictionary<int, string> FromLegacyInt =
            new Dictionary<int, string>
            {
                [0] = SingleEnemy,
                [1] = AllEnemies,
                [2] = Self,
                [3] = SingleAlly,
                [4] = AllAllies,
            };
    }
}
