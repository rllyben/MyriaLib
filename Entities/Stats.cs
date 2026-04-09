namespace MyriaLib.Entities
{
    public enum StatType
    {
        Strength,
        Dexterity,
        Endurance,
        Intelligence,
        Spirit
    }
    public enum DerivedStatType
    {
        PhysicalAttack,
        PhysicalDefense,
        MagicAttack,
        MagicDefense,
        MaxHealth,
        MaxMana,
        HitChance,
        DodgeChance
    }
    public class Stats
    {
        // Base stats from class/level progression
        public int Strength { get; set; } = 10;
        public int Dexterity { get; set; } = 10;
        public int Endurance { get; set; } = 10;
        public int Intelligence { get; set; } = 10;
        public int Spirit { get; set; } = 10;

        // Stat points invested by the player
        public int StrengthBonus { get; set; } = 0;
        public int DexterityBonus { get; set; } = 0;
        public int EnduranceBonus { get; set; } = 0;
        public int IntelligenceBonus { get; set; } = 0;
        public int SpiritBonus { get; set; } = 0;

        public int UnusedPoints { get; set; } = 0;

        // Base health/mana — modified by class and level
        public int BaseHealth { get; set; } = 30;
        public int BaseMana { get; set; } = 30;

        // Combined stats (class base + player investment, gear excluded)
        public int TotalStrength => Strength + StrengthBonus;
        public int TotalDexterity => Dexterity + DexterityBonus;
        public int TotalEndurance => Endurance + EnduranceBonus;
        public int TotalIntelligence => Intelligence + IntelligenceBonus;
        public int TotalSpirit => Spirit + SpiritBonus;

        public Stats Clone() => (Stats)MemberwiseClone();
    }
}
