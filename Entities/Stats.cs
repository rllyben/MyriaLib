namespace MyriaLib.Entities
{
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

        public int MaxHealth => BaseHealth + TotalEndurance * 5; // Reduced effect from END
        public int MaxMana => BaseMana + TotalSpirit * 5; // Spirit no longer affects this

        public int PhysicalAttack => (int)((TotalStrength * 2 + TotalEndurance) * (1 + TotalStrength * 0.005));
        public int PhysicalDefense => (int)((TotalEndurance * 2 + TotalStrength) * (1 + TotalEndurance * 0.005));

        public int MagicAttack => (int)((TotalIntelligence * 2 + TotalSpirit) * (1 + TotalIntelligence * 0.005));
        public int MagicDefense => (int)((TotalSpirit * 2 + TotalIntelligence) * (1 + TotalSpirit * 0.005));
        public int HitChance => (int)(TotalDexterity * (1 + TotalDexterity * 0.005));
        public int DodgeChance => (int)(TotalDexterity * 0.85f * (1 + TotalDexterity * 0.005));
        public float GearBlockBonus { get; set; } = 0f; // To be set via gear

        public Stats Clone()
        {
            return (Stats)this.MemberwiseClone();
        }
    }

}