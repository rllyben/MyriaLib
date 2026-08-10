using System.Text.Json;

namespace Myria.Lib.Core.Entities.Characters
{
    public class RaceProfile
    {
        public string                Race           { get; set; } = "";
        public Dictionary<string, int>  BaseStatBonus  { get; set; } = new();
        public int                   BaseHpBonus    { get; set; }
        public int                   BaseManaBonus  { get; set; }
        public Dictionary<string, int>  StatGrowth     { get; set; } = new();
        public int                   HpPerLevel     { get; set; }
        public int                   ManaPerLevel   { get; set; }
        /// <summary>Class IDs that cannot be chosen by characters of this race.</summary>
        public HashSet<string>       ForbiddenClasses { get; set; } = new(StringComparer.OrdinalIgnoreCase);

        // ── Registry ─────────────────────────────────────────────────────────────

        private static Dictionary<string, RaceProfile> _all = new(StringComparer.OrdinalIgnoreCase);

        public static IReadOnlyDictionary<string, RaceProfile> All => _all;

        private static readonly JsonSerializerOptions _opts = new()
        {
            PropertyNameCaseInsensitive = true,
        };

        /// <summary>
        /// Loads race profiles from JSON. Replaces any previously loaded data.
        /// Mods can extend this file to add new moddable races.
        /// </summary>
        public static void Load(string path = "Data/common/races.json")
        {
            if (!File.Exists(path)) return;
            var profiles = JsonSerializer.Deserialize<List<RaceProfile>>(File.ReadAllText(path), _opts);
            if (profiles == null) return;
            Load(profiles);
        }

        /// <summary>Loads race profiles from already-parsed data (e.g. read from a database). Replaces any previously loaded data.</summary>
        public static void Load(List<RaceProfile> profiles)
        {
            _all = profiles
                .Where(p => !string.IsNullOrWhiteSpace(p.Race))
                .ToDictionary(p => p.Race, StringComparer.OrdinalIgnoreCase);
        }
    }
}
