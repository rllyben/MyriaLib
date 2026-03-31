using MyriaLib.Systems;

namespace MyriaLib.Entities
{
    public class Currency
    {
        public int Bronze { get; private set; }

        public Currency(int bronze = 0)
        {
            Bronze = Math.Max(0, bronze);
        }

        public static readonly int BronzePerSilver = 1_000;
        public static readonly int SilverPerGold = 1_000;
        public static readonly int GoldPerPlatinum = 100;
        public static readonly int PlatinumPerCrystal = 100;

        public int TotalBronze => Bronze;

        public override string ToString()
        {
            Decompose(out int crystals, out int platinum, out int gold, out int silver, out int bronze);
            return $"{crystals}C {platinum}P {gold}G {silver}S {bronze}B";
        }

        /// <summary>
        /// Returns a localized display string, skipping zero denominations.
        /// Unit abbreviations come from currency.unit.* locale keys.
        /// Always shows at least bronze (e.g. "0 B") when the purse is empty.
        /// </summary>
        public string ToDisplayString()
        {
            Decompose(out int crystals, out int platinum, out int gold, out int silver, out int bronze);

            var parts = new List<string>();
            if (crystals > 0) parts.Add($"{crystals} {Localization.T("currency.unit.crystals")}");
            if (platinum > 0) parts.Add($"{platinum} {Localization.T("currency.unit.platinum")}");
            if (gold > 0)     parts.Add($"{gold} {Localization.T("currency.unit.gold")}");
            if (silver > 0)   parts.Add($"{silver} {Localization.T("currency.unit.silver")}");
            if (bronze > 0 || parts.Count == 0)
                              parts.Add($"{bronze} {Localization.T("currency.unit.bronze")}");

            return string.Join(" ", parts);
        }

        private void Decompose(out int crystals, out int platinum, out int gold, out int silver, out int bronze)
        {
            int remaining = Bronze;
            crystals = remaining / (BronzePerSilver * SilverPerGold * GoldPerPlatinum * PlatinumPerCrystal);
            remaining %= BronzePerSilver * SilverPerGold * GoldPerPlatinum * PlatinumPerCrystal;
            platinum = remaining / (BronzePerSilver * SilverPerGold * GoldPerPlatinum);
            remaining %= BronzePerSilver * SilverPerGold * GoldPerPlatinum;
            gold = remaining / (BronzePerSilver * SilverPerGold);
            remaining %= BronzePerSilver * SilverPerGold;
            silver = remaining / BronzePerSilver;
            bronze = remaining % BronzePerSilver;
        }

        public bool CanAfford(int cost) => Bronze >= cost;

        public bool TrySpend(int cost)
        {
            if (CanAfford(cost))
            {
                Bronze -= cost;
                return true;
            }
            return false;
        }

        public void Add(int amount)
        {
            Bronze += Math.Max(0, amount);
        }

    }

}
