using System.Text.Json;
using System.Text.Json.Serialization;
using MyriaLib.Systems.Enums;

namespace MyriaLib.Entities.Players
{
    public class ClassProfile
    {
        public PlayerClass             Class       { get; set; }
        public Dictionary<string, int> StatGrowth  { get; set; } = new();
        public int                     HpPerLevel   { get; set; }
        public int                     ManaPerLevel { get; set; }

        // ── Registry ─────────────────────────────────────────────────────────────

        private static Dictionary<PlayerClass, ClassProfile> _all = new();

        public static IReadOnlyDictionary<PlayerClass, ClassProfile> All => _all;

        private static readonly JsonSerializerOptions _opts = new()
        {
            PropertyNameCaseInsensitive = true,
            Converters = { new JsonStringEnumConverter() }
        };

        /// <summary>
        /// Loads class profiles from JSON. Replaces any previously loaded data.
        /// Expected format: array of objects matching this class's properties, e.g.:
        /// [{ "class": "Archer", "statGrowth": {"STR":3,"DEX":3,"END":1,"INT":1,"SPR":1},
        ///    "hpPerLevel": 7, "manaPerLevel": 5 }]
        /// </summary>
        public static void Load(string path = "Data/common/classes.json")
        {
            if (!File.Exists(path)) return;
            var profiles = JsonSerializer.Deserialize<List<ClassProfile>>(File.ReadAllText(path), _opts);
            if (profiles == null) return;
            _all = profiles.ToDictionary(p => p.Class);
        }
    }
}
