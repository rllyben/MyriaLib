using MyriaLib.Entities.Players;
using MyriaLib.Entities.Skills;
using MyriaLib.Models;
using MyriaLib.Models.BaseModel;
using MyriaLib.Services.Builder;
using MyriaLib.Systems;

namespace MyriaLib.Services.Manager
{
    public enum AddWordResult
    {
        Success,
        /// <summary>The word ID was not found in the loaded word data.</summary>
        WordNotFound,
        /// <summary>The word is already on this rune.</summary>
        AlreadyPresent,
        /// <summary>The base rune definition could not be resolved.</summary>
        RuneNotFound,
    }

    /// <summary>
    /// Player-facing coordinator for the runic magic system.
    /// Handles adding words to runes, re-evaluation, transform discovery,
    /// and the player's translation dictionary.
    /// </summary>
    public static class RuneManager
    {
        // ── Adding words to runes ─────────────────────────────────────────────────

        /// <summary>
        /// Adds a runic word to an existing rune in the player's collection.
        /// Re-evaluates the rune and checks for Transform combinations.
        /// Any newly unlocked runes are added to <see cref="Player.KnownRunes"/> and returned in
        /// <paramref name="newRunes"/> (empty list if none).
        /// </summary>
        public static AddWordResult AddWord(
            Player player,
            CompositeRune rune,
            string wordId,
            out List<CompositeRune> newRunes)
        {
            newRunes = new List<CompositeRune>();

            var word = RuneWordService.GetWord(wordId);
            if (word == null)
                return AddWordResult.WordNotFound;

            if (rune.AddedWordIds.Contains(wordId))
                return AddWordResult.AlreadyPresent;

            var baseDef = BaseRuneService.Get(rune.BaseRuneId);
            if (baseDef == null)
                return AddWordResult.RuneNotFound;

            // Add the word
            rune.AddedWordIds.Add(wordId);

            // Re-evaluate
            var addedWords = ResolveWords(rune.AddedWordIds);
            rune.ResolvedSkill = RuneEvaluator.Evaluate(baseDef, addedWords);

            // Check for transforms — any new rune IDs the player doesn't already have
            var allWordIds = new List<string>();
            if (!string.IsNullOrEmpty(baseDef.CoreWordId))
                allWordIds.Add(baseDef.CoreWordId);
            allWordIds.AddRange(rune.AddedWordIds);

            foreach (string newRuneId in RuneEvaluator.FindTransforms(allWordIds))
            {
                if (player.KnownRunes.Any(r => r.BaseRuneId == newRuneId && r.AddedWordIds.Count == 0))
                    continue; // already unlocked

                var newRuneDef = BaseRuneService.Get(newRuneId);
                if (newRuneDef == null)
                {
                    GameLog.Error($"Transform result rune '{newRuneId}' not found in base rune data.");
                    continue;
                }

                var newComposite = new CompositeRune { BaseRuneId = newRuneId };
                newComposite.ResolvedSkill = RuneEvaluator.Evaluate(newRuneDef, Array.Empty<RuneWord>());
                player.KnownRunes.Add(newComposite);
                newRunes.Add(newComposite);
            }

            EnsureWordInDictionary(player, wordId);
            return AddWordResult.Success;
        }

        /// <summary>
        /// Removes a word from a rune and re-evaluates.
        /// Returns false if the word was not on the rune.
        /// </summary>
        public static bool RemoveWord(Player player, CompositeRune rune, string wordId)
        {
            if (!rune.AddedWordIds.Remove(wordId))
                return false;

            var baseDef = BaseRuneService.Get(rune.BaseRuneId);
            if (baseDef == null) return true; // removed from list at least

            rune.ResolvedSkill = RuneEvaluator.Evaluate(baseDef, ResolveWords(rune.AddedWordIds));
            return true;
        }

        // ── Player dictionary ─────────────────────────────────────────────────────

        /// <summary>
        /// Sets the player's personal label for a runic word (their own translation guess).
        /// Creates the dictionary entry if it doesn't exist yet.
        /// </summary>
        public static void SetPlayerLabel(Player player, string wordId, string label)
        {
            var entry = GetOrCreateEntry(player, wordId);
            entry.PlayerLabel = string.IsNullOrWhiteSpace(label) ? null : label.Trim();
        }

        /// <summary>
        /// Marks a word as officially learned (e.g. taught by an NPC or found in a lore item).
        /// The official English name will be shown to the player from this point on.
        /// </summary>
        public static void LearnWord(Player player, string wordId)
        {
            EnsureWordInDictionary(player, wordId);
            GetOrCreateEntry(player, wordId).IsOfficiallyLearned = true;
        }

        /// <summary>
        /// Returns the display string for a runic word as the player currently sees it.
        /// Priority: officially learned → player label (in brackets) → runic script.
        /// </summary>
        public static string GetDisplayName(Player player, RuneWord word)
        {
            var entry = player.RuneDictionary.FirstOrDefault(e => e.WordId == word.Id);

            if (entry?.IsOfficiallyLearned == true)
                return word.EnglishName;

            if (!string.IsNullOrWhiteSpace(entry?.PlayerLabel))
                return $"[{entry.PlayerLabel}]";

            return string.IsNullOrWhiteSpace(word.RunicScript) ? "???" : word.RunicScript;
        }

        // ── Private helpers ───────────────────────────────────────────────────────

        private static List<RuneWord> ResolveWords(IEnumerable<string> ids) =>
            ids.Select(id => RuneWordService.GetWord(id))
               .Where(w => w != null)
               .Select(w => w!)
               .ToList();

        private static void EnsureWordInDictionary(Player player, string wordId)
        {
            if (!player.RuneDictionary.Any(e => e.WordId == wordId))
                player.RuneDictionary.Add(new PlayerRuneWordEntry { WordId = wordId });
        }

        private static PlayerRuneWordEntry GetOrCreateEntry(Player player, string wordId)
        {
            var entry = player.RuneDictionary.FirstOrDefault(e => e.WordId == wordId);
            if (entry == null)
            {
                entry = new PlayerRuneWordEntry { WordId = wordId };
                player.RuneDictionary.Add(entry);
            }
            return entry;
        }
    }
}
