using System.Text.Json.Serialization;

namespace Myria.Lib.Core.Entities
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
        // Canonical stat IDs ("STR"/"DEX"/"END"/"INT"/"SPR") — same convention as
        // ClassManager.GetClassBonusForStat, RaceProfile/ClassProfile's StatGrowth dictionaries,
        // and Skill.StatToScaleFrom. Not itself serialized: Strength/StrengthBonus/etc. below
        // remain the only JSON wire format (existing save files have "Strength"/"StrengthBonus"
        // as keys inside the nested "Stats" object, no "BaseValues"/"BonusValues" key), so their
        // setters are what actually populate these dictionaries on load.
        private Dictionary<string, int> _baseValues = new(StringComparer.OrdinalIgnoreCase)
        {
            ["STR"] = 10, ["DEX"] = 10, ["END"] = 10, ["INT"] = 10, ["SPR"] = 10,
        };
        private Dictionary<string, int> _bonusValues = new(StringComparer.OrdinalIgnoreCase)
        {
            ["STR"] = 0, ["DEX"] = 0, ["END"] = 0, ["INT"] = 0, ["SPR"] = 0,
        };

        [JsonIgnore] public IReadOnlyDictionary<string, int> BaseValues => _baseValues;
        [JsonIgnore] public IReadOnlyDictionary<string, int> BonusValues => _bonusValues;

        public int GetBase(string statId) => _baseValues.GetValueOrDefault(statId);
        public void SetBase(string statId, int value) => _baseValues[statId] = value;
        public int GetBonus(string statId) => _bonusValues.GetValueOrDefault(statId);
        public void SetBonus(string statId, int value) => _bonusValues[statId] = value;
        public int GetTotal(string statId) => GetBase(statId) + GetBonus(statId);

        // Base stats from class/level progression
        public int Strength     { get => GetBase("STR"); set => SetBase("STR", value); }
        public int Dexterity    { get => GetBase("DEX"); set => SetBase("DEX", value); }
        public int Endurance    { get => GetBase("END"); set => SetBase("END", value); }
        public int Intelligence { get => GetBase("INT"); set => SetBase("INT", value); }
        public int Spirit       { get => GetBase("SPR"); set => SetBase("SPR", value); }

        // Stat points invested by the player
        public int StrengthBonus     { get => GetBonus("STR"); set => SetBonus("STR", value); }
        public int DexterityBonus    { get => GetBonus("DEX"); set => SetBonus("DEX", value); }
        public int EnduranceBonus    { get => GetBonus("END"); set => SetBonus("END", value); }
        public int IntelligenceBonus { get => GetBonus("INT"); set => SetBonus("INT", value); }
        public int SpiritBonus       { get => GetBonus("SPR"); set => SetBonus("SPR", value); }

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

        public Stats Clone()
        {
            var clone = (Stats)MemberwiseClone();
            // MemberwiseClone is shallow — without this, the clone and the original would share
            // the same two dictionary instances, so mutating one would mutate the other.
            clone._baseValues = new Dictionary<string, int>(_baseValues, StringComparer.OrdinalIgnoreCase);
            clone._bonusValues = new Dictionary<string, int>(_bonusValues, StringComparer.OrdinalIgnoreCase);
            return clone;
        }
    }
}
