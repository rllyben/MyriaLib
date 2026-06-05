using System.Text.Json;
using System.Text.Json.Serialization;
using MyriaLib.Systems.Enums;

namespace MyriaLib.Entities.Players
{
    public class RaceProfile
    {
        public PlayerRace                Race           { get; set; }
        public Dictionary<string, int>   BaseStatBonus  { get; set; } = new();
        public int                       BaseHpBonus    { get; set; }
        public int                       BaseManaBonus  { get; set; }
        public Dictionary<string, int>   StatGrowth     { get; set; } = new();
        public int                       HpPerLevel     { get; set; }
        public int                       ManaPerLevel   { get; set; }
        public HashSet<PlayerClass>      ForbiddenClasses { get; set; } = new();

        // ── Registry ─────────────────────────────────────────────────────────────

        private static Dictionary<PlayerRace, RaceProfile> _all = new();

        public static IReadOnlyDictionary<PlayerRace, RaceProfile> All => _all;

        private static readonly JsonSerializerOptions _opts = new()
        {
            PropertyNameCaseInsensitive = true,
            Converters = { new JsonStringEnumConverter() }
        };

        /// <summary>
        /// Loads race profiles from JSON. Replaces any previously loaded data.
        /// Expected format: array of objects matching this class's properties, e.g.:
        /// [{ "race": "Myralu", "baseStatBonus": {"STR":3,...}, "baseHpBonus": 30,
        ///    "baseManaBonus": 20, "statGrowth": {"STR":1,...}, "hpPerLevel": 6,
        ///    "manaPerLevel": 5, "forbiddenClasses": ["RunicMage"] }]
        /// </summary>
        public static void Load(string path = "Data/common/races.json")
        {
            if (!File.Exists(path)) return;
            var profiles = JsonSerializer.Deserialize<List<RaceProfile>>(File.ReadAllText(path), _opts);
            if (profiles == null) return;
            _all = profiles.ToDictionary(p => p.Race);
        }
    }
}
