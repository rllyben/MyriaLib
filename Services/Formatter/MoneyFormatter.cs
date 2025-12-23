using MyriaLib.Entities.Items;
using System.Globalization;

namespace MyriaLib.Services.Formatter
{

    /// <summary>
    /// Parse strings like: "1 CC 2 Pt 3 G 4 S 500 B", "2pt 5g", or plain bronze like "12345".
    /// Units are case-insensitive and can be: CC, Pt, G, S, B. Returns false on failure.
    /// </summary>
    public static class MoneyFormatter
    {
        // Short form like: "1 CC 2 Pt 3 G 4 S 500 B" (skips leading zero groups)
        public static string FormatShort(long cc, long pt, long g, long s, long b, NumberFormatInfo nfi)
        {
            string fmt(long v, string unit) => v == 0 ? string.Empty : $"{v.ToString("N0", nfi)} {unit}";
            // Keep at least Bronze
            var parts = new[] { fmt(cc, "CC"), fmt(pt, "Pt"), fmt(g, "G"), fmt(s, "S"), $"{b.ToString("N0", nfi)} B" };
            // Remove empty leading entries but always keep last (Bronze)
            int start = 0; while (start < parts.Length - 1 && string.IsNullOrEmpty(parts[start])) start++;
            return string.Join(" ", parts, start, parts.Length - start);
        }

        // Compact form keeps the most significant two units, e.g., "1 Pt 23 G" or "450 S"
        public static string FormatCompact(long cc, long pt, long g, long s, long b, NumberFormatInfo nfi)
        {
            (long val, string unit)[] seq =
            {
                (cc, "CC"), (pt, "Pt"), (g, "G"), (s, "S"), (b, "B")
            };
            int first = Array.FindIndex(seq, x => x.val != 0);
            if (first < 0) return $"0 B";
            int second = Math.Min(first + 1, seq.Length - 1);
            if (seq[second].val == 0) return $"{seq[first].val.ToString("N0", nfi)} {seq[first].unit}";
            return $"{seq[first].val.ToString("N0", nfi)} {seq[first].unit} {seq[second].val.ToString("N0", nfi)} {seq[second].unit}";
        }

        public static bool TryParse(string text, out Money money)
        {
            money = new Money(0);
            if (string.IsNullOrWhiteSpace(text)) return false;

            long total = 0;
            var span = text.AsSpan().Trim();
            // Allow plain bronze number
            if (long.TryParse(span, NumberStyles.Integer | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out var plain))
            {
                money = new Money(plain);
                return true;
            }

            // Tokenize on spaces
            var parts = text.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            long sign = 1;
            for (int i = 0; i < parts.Length; i++)
            {
                var p = parts[i];
                if (p == "+" || p == "-") { sign = p == "-" ? -1L : 1L; continue; }
                if (!long.TryParse(p, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value))
                {
                    // maybe "2pt" combined form
                    if (!TrySplitCombined(p, out value, out var unit)) return false;
                    total = checked(total + sign * ToBronze(value, unit));
                    sign = 1; // reset
                    continue;
                }
                if (i + 1 >= parts.Length) { total = checked(total + sign * value); break; }
                var unitTok = parts[++i];
                total = checked(total + sign * ToBronze(value, unitTok));
                sign = 1;
            }

            money = new Money(total);
            return true;

            static bool TrySplitCombined(string token, out long val, out string unit)
            {
                val = 0; unit = "";
                // split numeric prefix + alpha suffix
                int j = 0; while (j < token.Length && char.IsDigit(token[j])) j++;
                if (j == 0 || j == token.Length) return false;
                if (!long.TryParse(token.Substring(0, j), NumberStyles.Integer, CultureInfo.InvariantCulture, out val)) return false;
                unit = token.Substring(j);
                return true;
            }

            static long ToBronze(long value, string unit)
            {
                var u = unit.Trim().ToUpperInvariant();
                return u switch
                {
                    "CC" => checked(value * Money.BRONZE_PER_CRYSTAL),
                    "PT" => checked(value * Money.BRONZE_PER_PLATINUM),
                    "G" => checked(value * Money.BRONZE_PER_GOLD),
                    "S" => checked(value * Money.BRONZE_PER_SILVER),
                    "B" => value,
                    _ => throw new FormatException($"Unknown unit '{unit}'. Use CC, Pt, G, S, B.")
                };

            }

        }

        public static Money Parse(string text)
        {
            if (!TryParse(text, out var m)) throw new FormatException("Invalid money format.");
            return m;
        }

        // Friendly constructors
        public static Money FromBronze(long bronze) => new(bronze);
        public static Money FromSilver(long silver) => new(checked(silver * Money.BRONZE_PER_SILVER));
        public static Money FromGold(long gold) => new(checked(gold * Money.BRONZE_PER_GOLD));
        public static Money FromPlatinum(long platinum) => new(checked(platinum * Money.BRONZE_PER_PLATINUM));
        public static Money FromCrystals(long crystals) => new(checked(crystals * Money.BRONZE_PER_CRYSTAL));
    }

}

