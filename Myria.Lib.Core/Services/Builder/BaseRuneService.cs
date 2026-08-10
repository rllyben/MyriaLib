using Myria.Lib.Core.Entities.Characters;
using Myria.Lib.Core.Entities.Skills;
using Myria.Lib.Core.Models;
using Myria.Lib.Core.Models.BaseModel;
using Myria.Lib.Core.Systems;
using Myria.Lib.Core.Systems.Enums;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Myria.Lib.Core.Services.Builder
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

            var runes = JsonSerializer.Deserialize<List<BaseRuneData>>(
                         File.ReadAllText(filePath), _opts) ?? new();
            Load(runes);
        }

        /// <summary>Loads base rune definitions from already-parsed data (e.g. read from a database).</summary>
        public static void Load(List<BaseRuneData> runes)
        {
            _runes = runes;
            _runesById = _runes.ToDictionary(r => r.Id, r => r, StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>Returns a base rune definition by ID, or <c>null</c> if not found.</summary>
        public static BaseRuneData? Get(string id) =>
            _runesById.TryGetValue(id, out var r) ? r : null;

        /// <summary>Returns all loaded base rune definitions.</summary>
        public static IReadOnlyList<BaseRuneData> GetAll() => _runes;

        /// <summary>Returns all base runes available to a specific player class.</summary>
        public static List<BaseRuneData> GetForClass(string playerClass)
        {
            return _runes
                .Where(r => r.Class.Equals(playerClass, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        /// <summary>
        /// Gives a player all base runes for their class as unmodified <see cref="CompositeRune"/> entries
        /// (no added words) and resolves each to a Skill.
        /// Skips runes the player already has.
        /// </summary>
        public static void GrantBaseRunes(Character character)
        {
            var baseRunes = GetForClass(character.Class);
            foreach (var def in baseRunes)
            {
                SeedWordDictionary(character, def);

                if (character.KnownRunes.Any(r => r.BaseRuneId == def.Id && r.AddedWordIds.Count == 0))
                    continue;

                var composite = new CompositeRune { BaseRuneId = def.Id };
                composite.ResolvedSkill = RuneEvaluator.Evaluate(def, Array.Empty<RuneWord>());
                character.KnownRunes.Add(composite);
            }
        }

        /// <summary>
        /// Grants a single base rune by ID to the player if they don't already have it (no added words).
        /// Does nothing if the ID is not found or the player already has that rune.
        /// </summary>
        public static void GrantBaseRune(Character character, string runeId)
        {
            var def = Get(runeId);
            if (def is null) return;

            SeedWordDictionary(character, def);

            if (character.KnownRunes.Any(r => r.BaseRuneId == def.Id && r.AddedWordIds.Count == 0))
                return;

            var composite = new CompositeRune { BaseRuneId = def.Id };
            composite.ResolvedSkill = RuneEvaluator.Evaluate(def, Array.Empty<RuneWord>());
            character.KnownRunes.Add(composite);
        }

        /// <summary>
        /// Re-resolves all <see cref="CompositeRune"/> entries for a player after loading from save.
        /// Call this once after deserializing a player — populates <see cref="CompositeRune.ResolvedSkill"/>.
        /// Also seeds lexica entries for each rune's core word if not yet present.
        /// </summary>
        public static void ResolveRunes(Character character)
        {
            foreach (var composite in character.KnownRunes)
            {
                var def = Get(composite.BaseRuneId);
                if (def == null)
                {
                    GameLog.Error($"Character '{character.Name}': base rune '{composite.BaseRuneId}' not found in data.");
                    continue;
                }

                var addedWords = composite.AddedWordIds
                    .Select(id => RuneWordService.GetWord(id))
                    .Where(w => w != null)
                    .Select(w => w!)
                    .ToList();

                composite.ResolvedSkill = RuneEvaluator.Evaluate(def, addedWords);
                SeedWordDictionary(character, def);
            }
        }

        /// <summary>
        /// Adds the core word of a base rune to the player's lexica with the rune description as the
        /// initial note. No-op if the word is already in the dictionary (existing notes are never overwritten).
        /// </summary>
        private static void SeedWordDictionary(Character character, BaseRuneData def)
        {
            if (string.IsNullOrEmpty(def.CoreWordId)) return;
            if (character.RuneDictionary.Any(e => e.WordId == def.CoreWordId)) return;
            character.RuneDictionary.Add(new CharacterRuneWordEntry
            {
                WordId     = def.CoreWordId,
                CharacterLabel = def.Description
            });
        }
    }
}
