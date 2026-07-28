namespace MyriaLib.Systems.Enums
{
    /// <summary>
    /// Built-in skill targeting identifiers. Stored and compared as strings — like
    /// <see cref="CharacterClass"/>/<see cref="CharacterRace"/>/<see cref="ItemRarity"/>/
    /// <see cref="GatheringType"/>/<see cref="EquipmentType"/> — so mod-added targeting types work
    /// the same way as these built-ins without requiring any code changes. Previously a closed
    /// enum declared in Entities/Skills/Skill.cs; already stored as a plain string in skills.json
    /// and SQL (DbSkill.Target), converted via Enum.Parse at factory time — same situation as
    /// GatheringType, no legacy-int migration needed. Moved here to sit alongside the other
    /// string-constant enums.
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
    }
}
