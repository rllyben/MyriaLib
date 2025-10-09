using System.Globalization;
using MyriaLib.Services.Formatter;

namespace MyriaLib.Entities.Items
{/// <summary>
 /// Immutable money amount stored in base units (Bronze). Supports your denomination ladder:
 /// Bronze (base) → Silver (1,000 Bronze) → Gold (1,000 Silver) → Platinum (100 Gold) → Coin Crystals (100 Platinum).
 /// </summary>
    public readonly struct Money : IComparable<Money>, IEquatable<Money>, IFormattable
    {
        // Conversion factors
        public const long BRONZE_PER_SILVER = 1_000;
        public const long SILVER_PER_GOLD = 1_000;
        public const long GOLD_PER_PLATINUM = 100;
        public const long PLATINUM_PER_CRYSTAL = 100;


        public const long BRONZE_PER_GOLD = BRONZE_PER_SILVER * SILVER_PER_GOLD; // 1,000,000
        public const long BRONZE_PER_PLATINUM = BRONZE_PER_GOLD * GOLD_PER_PLATINUM; // 100,000,000
        public const long BRONZE_PER_CRYSTAL = BRONZE_PER_PLATINUM * PLATINUM_PER_CRYSTAL; // 10,000,000,000


        /// <summary>Total amount in Bronze (can be negative).</summary>
        public long BronzeTotal { get; }


        public Money(long bronzeTotal) => BronzeTotal = bronzeTotal;


        public static Money FromComponents(long crystals, long platinum, long gold, long silver, long bronze)
        {
            checked
            {
                var total = bronze
                + silver * BRONZE_PER_SILVER
                + gold * BRONZE_PER_GOLD
                + platinum * BRONZE_PER_PLATINUM
                + crystals * BRONZE_PER_CRYSTAL;
                return new Money(total);
            }
        }


        // Decompose to components (non-negative remainders, sign carried on the highest non-zero unit)
        public void Decompose(out long crystals, out long platinum, out long gold, out long silver, out long bronze)
        {
            long sign = BronzeTotal < 0 ? -1 : 1;
            long abs = Math.Abs(BronzeTotal);


            crystals = abs / BRONZE_PER_CRYSTAL; abs %= BRONZE_PER_CRYSTAL;
            platinum = abs / BRONZE_PER_PLATINUM; abs %= BRONZE_PER_PLATINUM;
            gold = abs / BRONZE_PER_GOLD; abs %= BRONZE_PER_GOLD;
            silver = abs / BRONZE_PER_SILVER; abs %= BRONZE_PER_SILVER;
            bronze = abs;


            crystals *= sign; if (crystals != 0) return;
            platinum *= sign; if (platinum != 0) return;
            gold *= sign; if (gold != 0) return;
            silver *= sign; if (silver != 0) return;
            bronze *= sign;
        }


        // Arithmetic
        public static Money operator +(Money a, Money b) => new(a.BronzeTotal + b.BronzeTotal);
        public static Money operator -(Money a, Money b) => new(a.BronzeTotal - b.BronzeTotal);
        public static Money operator -(Money a) => new(-a.BronzeTotal);
        public static bool operator >(Money a, Money b) => a.BronzeTotal > b.BronzeTotal;
        public static bool operator <(Money a, Money b) => a.BronzeTotal < b.BronzeTotal;
        public static bool operator >=(Money a, Money b) => a.BronzeTotal >= b.BronzeTotal;
        public static bool operator <=(Money a, Money b) => a.BronzeTotal <= b.BronzeTotal;
        public static bool operator ==(Money a, Money b) => a.BronzeTotal == b.BronzeTotal;
        public static bool operator !=(Money a, Money b) => a.BronzeTotal != b.BronzeTotal;


        public int CompareTo(Money other) => BronzeTotal.CompareTo(other.BronzeTotal);
        public bool Equals(Money other) => BronzeTotal == other.BronzeTotal;
        public override bool Equals(object? obj) => obj is Money m && Equals(m);
        public override int GetHashCode() => BronzeTotal.GetHashCode();

        public override string ToString()
        {
            Decompose(out var cc, out var pt, out var g, out var s, out var b);

            return $"{cc} Coin Crystals {pt} Platinum {g} Gold {s} Silver {b} Bronze";

        }

        // Formatting
        /// <summary>
        /// Format using: "S" (short) => "{CC} CC {Pt} Pt {G} G {S} S {B} B" (skips leading zeros smartly),
        /// "L" (long) => full words, "C" (compact) => most significant 2 units, "B" => raw bronze with unit.
        /// Culture controls digit grouping only.
        /// </summary>
        public string ToString(string? format, IFormatProvider? formatProvider)
        {
            var f = string.IsNullOrWhiteSpace(format) ? "S" : format!.Trim().ToUpperInvariant();
            var nfi = NumberFormatInfo.GetInstance(formatProvider);
            Decompose(out var cc, out var pt, out var g, out var s, out var b);

            string fmt(long v) => v.ToString("N0", nfi);


            return f switch
            {
                "L" => $"{fmt(cc)} Coin Crystals {fmt(pt)} Platinum {fmt(g)} Gold {fmt(s)} Silver {fmt(b)} Bronze",
                "C" => MoneyFormatter.FormatCompact(cc, pt, g, s, b, nfi),
                "B" => $"{fmt(BronzeTotal)} Bronze",
                _ => MoneyFormatter.FormatShort(cc, pt, g, s, b, nfi)
            };

        }

    }

}
