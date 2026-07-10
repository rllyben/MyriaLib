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

        // Compatibility aliases — old API used "Added" suffix for bonus points
        public int StrengthAdded     { get => StrengthBonus;     set => StrengthBonus     = value; }
        public int DexterityAdded    { get => DexterityBonus;    set => DexterityBonus    = value; }
        public int EnduranceAdded    { get => EnduranceBonus;    set => EnduranceBonus    = value; }
        public int IntelligenceAdded { get => IntelligenceBonus; set => IntelligenceBonus = value; }
        public int SpiritAdded       { get => SpiritBonus;       set => SpiritBonus       = value; }

        // Returns the stat-only contribution to a derived stat (gear excluded).
        public int GetAddedStatBonus(DerivedStatType type) => type switch
        {
            DerivedStatType.MaxHealth        => EnduranceBonus * 10,
            DerivedStatType.MaxMana          => SpiritBonus * 5,
            DerivedStatType.PhysicalAttack   => StrengthBonus * 2 + EnduranceBonus,
            DerivedStatType.PhysicalDefense  => EnduranceBonus * 2 + StrengthBonus,
            DerivedStatType.MagicAttack      => IntelligenceBonus * 2 + SpiritBonus,
            DerivedStatType.MagicDefense     => SpiritBonus * 2 + IntelligenceBonus,
            DerivedStatType.HitChance        => DexterityBonus,
            DerivedStatType.DodgeChance      => (int)(DexterityBonus * 0.95f),
            _                                => 0,
        };

        public Stats Clone() => (Stats)MemberwiseClone();
    }
}
