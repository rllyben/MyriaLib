using MyriaLib.Models;
using MyriaLib.Systems;
using MyriaLib.Systems.Enums;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MyriaLib.Services
{
    /// <summary>
    /// Loads and caches the runic language data (words, families, pair relations).
    /// Provides the relationship lookup used by <c>RuneEvaluator</c>.
    /// Call <see cref="Load"/> once at app start before any rune evaluation.
    /// </summary>
    public static class RuneWordService
    {
        private static readonly string _wordsPath    = "Data/common/rune_words.json";
        private static readonly string _familiesPath = "Data/common/rune_families.json";
        private static readonly string _pairsPath    = "Data/common/rune_word_pairs.json";

        private static Dictionary<string, RuneWord>        _words    = new(StringComparer.OrdinalIgnoreCase);
        private static Dictionary<string, WordFamily>      _families = new(StringComparer.OrdinalIgnoreCase);
        private static Dictionary<string, WordPairRelation> _pairs   = new(StringComparer.OrdinalIgnoreCase);

        public static IReadOnlyDictionary<string, RuneWord>   AllWords    => _words;
        public static IReadOnlyDictionary<string, WordFamily> AllFamilies => _families;

        private static readonly JsonSerializerOptions _opts = new()
        {
            PropertyNameCaseInsensitive = true,
            Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
        };

        public static void Load()
        {
            _words    = LoadWords();
            _families = LoadFamilies();
            _pairs    = LoadPairs();
        }

        /// <summary>Returns a word by ID, or <c>null</c> if not found.</summary>
        public static RuneWord? GetWord(string id) =>
            _words.TryGetValue(id, out var w) ? w : null;

        // ── Relationship resolution ───────────────────────────────────────────────

        /// <summary>
        /// Resolves the relationship between two runic words.
        /// Priority: explicit word-pair entry → family-level default → Neutral.
        /// Word order does not matter.
        /// </summary>
        public static WordRelationship GetRelationship(string wordIdA, string wordIdB)
        {
            // 1. Explicit pair override (order-independent key)
            string pairKey = MakePairKey(wordIdA, wordIdB);
            if (_pairs.TryGetValue(pairKey, out var pair))
                return pair.Relationship;

            // 2. Family-level default
            if (_words.TryGetValue(wordIdA, out var wA) && _words.TryGetValue(wordIdB, out var wB))
            {
                if (wA.FamilyId == wB.FamilyId)
                    return WordRelationship.Support; // same family always supports

                if (_families.TryGetValue(wA.FamilyId, out var fA)
                    && fA.FamilyRelations.TryGetValue(wB.FamilyId, out var rel))
                    return rel;

                // Check the reverse direction too (family relations are typically symmetric in JSON,
                // but this makes sure we don't miss an asymmetric authoring mistake)
                if (_families.TryGetValue(wB.FamilyId, out var fB)
                    && fB.FamilyRelations.TryGetValue(wA.FamilyId, out var relRev))
                    return relRev;
            }

            // 3. Default fallback
            return WordRelationship.Neutral;
        }

        /// <summary>
        /// Returns the Transform result rune ID for a specific word pair, or <c>null</c> if this pair
        /// is not a Transform combination.
        /// </summary>
        public static string? GetTransformResult(string wordIdA, string wordIdB)
        {
            string pairKey = MakePairKey(wordIdA, wordIdB);
            if (_pairs.TryGetValue(pairKey, out var pair)
                && pair.Relationship == WordRelationship.Transform)
                return pair.TransformResultRuneId;
            return null;
        }

        // ── Private helpers ───────────────────────────────────────────────────────

        /// <summary>Canonical order-independent key for a word pair.</summary>
        private static string MakePairKey(string a, string b)
        {
            // Always put the lexicographically smaller id first so (A,B) == (B,A)
            return string.CompareOrdinal(a, b) <= 0 ? $"{a}:{b}" : $"{b}:{a}";
        }

        private static Dictionary<string, RuneWord> LoadWords()
        {
            if (!File.Exists(_wordsPath))
            {
                GameLog.Error($"Rune words file not found at '{_wordsPath}'.");
                return new(StringComparer.OrdinalIgnoreCase);
            }
            var list = JsonSerializer.Deserialize<List<RuneWord>>(File.ReadAllText(_wordsPath), _opts)
                       ?? new();
            return list.ToDictionary(w => w.Id, w => w, StringComparer.OrdinalIgnoreCase);
        }

        private static Dictionary<string, WordFamily> LoadFamilies()
        {
            if (!File.Exists(_familiesPath))
            {
                GameLog.Error($"Rune families file not found at '{_familiesPath}'.");
                return new(StringComparer.OrdinalIgnoreCase);
            }
            var list = JsonSerializer.Deserialize<List<WordFamily>>(File.ReadAllText(_familiesPath), _opts)
                       ?? new();
            return list.ToDictionary(f => f.Id, f => f, StringComparer.OrdinalIgnoreCase);
        }

        private static Dictionary<string, WordPairRelation> LoadPairs()
        {
            if (!File.Exists(_pairsPath))
            {
                // Not an error — an empty pairs file just means family rules cover everything.
                return new(StringComparer.OrdinalIgnoreCase);
            }
            var list = JsonSerializer.Deserialize<List<WordPairRelation>>(File.ReadAllText(_pairsPath), _opts)
                       ?? new();
            return list.ToDictionary(
                p => MakePairKey(p.WordIdA, p.WordIdB),
                p => p,
                StringComparer.OrdinalIgnoreCase);
        }
    }
}
