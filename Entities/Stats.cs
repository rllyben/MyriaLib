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
        // Base stats (from class/level)
        public int Strength { get; set; } = 10;
        public int Dexterity { get; set; } = 10;
        public int Endurance { get; set; } = 10;
        public int Intelligence { get; set; } = 10;
        public int Spirit { get; set; } = 10;

        // Points spent by player
        public int StrengthAdded { get; set; } = 0;
        public int DexterityAdded { get; set; } = 0;
        public int EnduranceAdded { get; set; } = 0;
        public int IntelligenceAdded { get; set; } = 0;
        public int SpiritAdded { get; set; } = 0;

        public int UnusedPoints { get; set; } = 0;

        // Total stats for calculation
        public int TotalStrength => Strength + StrengthAdded;
        public int TotalDexterity => Dexterity + DexterityAdded;
        public int TotalEndurance => Endurance + EnduranceAdded;
        public int TotalIntelligence => Intelligence + IntelligenceAdded;
        public int TotalSpirit => Spirit + SpiritAdded;

        // Flat HP and Mana stats can be modified by class/gear/level
        public int BaseHealth { get; set; } = 30;
        public int BaseMana { get; set; } = 30;

        public int MaxHealth => BaseHealth + TotalEndurance * 5;
        public int MaxMana => BaseMana + TotalSpirit * 5;

        public int PhysicalAttack => (int)((TotalStrength * 2 + TotalEndurance) * (1 + TotalStrength * 0.005));
        public int PhysicalDefense => (int)((TotalEndurance * 2 + TotalStrength) * (1 + TotalEndurance * 0.005));

        public int MagicAttack => (int)((TotalIntelligence * 2 + TotalSpirit) * (1 + TotalIntelligence * 0.005));
        public int MagicDefense => (int)((TotalSpirit * 2 + TotalIntelligence) * (1 + TotalSpirit * 0.005));
        public int HitChance => (int)(TotalDexterity * (1 + TotalDexterity * 0.005));
        public int DodgeChance => (int)(TotalDexterity * 0.85f * (1 + TotalDexterity * 0.005));
        public float GearBlockBonus { get; set; } = 0f;

        public int GetAddedStatBonus(StatType statType)
        {
            return statType switch
            {
                StatType.Strength => StrengthAdded,
                StatType.Dexterity => DexterityAdded,
                StatType.Endurance => EnduranceAdded,
                StatType.Intelligence => IntelligenceAdded,
                StatType.Spirit => SpiritAdded,
                _ => 0
            };

        }

        public int GetAddedStatBonus(DerivedStatType derivedStat)
        {
            // Clone current stats (with bonuses)
            var withBonus = this;

            // Clone and remove all added stats
            var withoutBonus = this.Clone();
            withoutBonus.StrengthAdded = 0;
            withoutBonus.DexterityAdded = 0;
            withoutBonus.EnduranceAdded = 0;
            withoutBonus.IntelligenceAdded = 0;
            withoutBonus.SpiritAdded = 0;

            int GetValue(Stats s) => derivedStat switch
            {
                DerivedStatType.PhysicalAttack => s.PhysicalAttack,
                DerivedStatType.PhysicalDefense => s.PhysicalDefense,
                DerivedStatType.MagicAttack => s.MagicAttack,
                DerivedStatType.MagicDefense => s.MagicDefense,
                DerivedStatType.MaxHealth => s.MaxHealth,
                DerivedStatType.MaxMana => s.MaxMana,
                DerivedStatType.HitChance => s.HitChance,
                DerivedStatType.DodgeChance => s.DodgeChance,
                _ => 0
            };

            return GetValue(withBonus) - GetValue(withoutBonus);
        }
        public Stats Clone()
        {
            return (Stats)this.MemberwiseClone();
        }

    }

}