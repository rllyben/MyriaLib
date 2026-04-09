using MyriaLib.Entities.Players;
using MyriaLib.Entities.Skills;
using MyriaLib.Models;
using MyriaLib.Systems;
using MyriaLib.Systems.Enums;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MyriaLib.Services.Builder
{
    /// <summary>
    /// Loads base rune definitions from JSON and resolves them into player rune collections.
    /// Call <see cref="Load"/> once at app start after <c>RuneWordService.Load</c>.
    /// </summary>
    public static class BaseRuneService
    {
        private static readonly string _path = "Data/common/base_runes.json";
        private static List<BaseRuneData> _runes = new();
        private static Dictionary<string, BaseRuneData> _runesById = new(StringComparer.OrdinalIgnoreCase);

        private static readonly JsonSerializerOptions _opts = new()
        {
            PropertyNameCaseInsensitive = true,
            Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
        };

        public static void Load(string path = "")
        {
            string filePath = string.IsNullOrEmpty(path) ? _path : path;

            if (!File.Exists(filePath))
            {
                GameLog.Error($"Base runes file not found at '{filePath}'.");
                _runes    = new();
                _runesById = new(StringComparer.OrdinalIgnoreCase);
                return;
            }

            _runes = JsonSerializer.Deserialize<List<BaseRuneData>>(
                         File.ReadAllText(filePath), _opts) ?? new();

            _runesById = _runes.ToDictionary(r => r.Id, r => r, StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>Returns a base rune definition by ID, or <c>null</c> if not found.</summary>
        public static BaseRuneData? Get(string id) =>
            _runesById.TryGetValue(id, out var r) ? r : null;

        /// <summary>Returns all base runes available to a specific player class.</summary>
        public static List<BaseRuneData> GetForClass(PlayerClass playerClass)
        {
            string className = playerClass.ToString();
            return _runes
                .Where(r => r.Class.Equals(className, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        /// <summary>
        /// Gives a player all base runes for their class as unmodified <see cref="CompositeRune"/> entries
        /// (no added words) and resolves each to a Skill.
        /// Skips runes the player already has.
        /// </summary>
        public static void GrantBaseRunes(Player player)
        {
            var baseRunes = GetForClass(player.Class);
            foreach (var def in baseRunes)
            {
                if (player.KnownRunes.Any(r => r.BaseRuneId == def.Id && r.AddedWordIds.Count == 0))
                    continue; // already has this base rune

                var composite = new CompositeRune { BaseRuneId = def.Id };
                composite.ResolvedSkill = RuneEvaluator.Evaluate(def, Array.Empty<RuneWord>());
                player.KnownRunes.Add(composite);
            }
        }

        /// <summary>
        /// Re-resolves all <see cref="CompositeRune"/> entries for a player after loading from save.
        /// Call this once after deserializing a player — populates <see cref="CompositeRune.ResolvedSkill"/>.
        /// </summary>
        public static void ResolveRunes(Player player)
        {
            foreach (var composite in player.KnownRunes)
            {
                var def = Get(composite.BaseRuneId);
                if (def == null)
                {
                    GameLog.Error($"Player '{player.Name}': base rune '{composite.BaseRuneId}' not found in data.");
                    continue;
                }

                var addedWords = composite.AddedWordIds
                    .Select(id => RuneWordService.GetWord(id))
                    .Where(w => w != null)
                    .Select(w => w!)
                    .ToList();

                composite.ResolvedSkill = RuneEvaluator.Evaluate(def, addedWords);
            }
        }
    }
}
