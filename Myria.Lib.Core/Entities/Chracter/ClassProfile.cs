using System.Text.Json;
using Myria.Lib.Core.Systems.Enums;

namespace Myria.Lib.Core.Entities.Characters
{
    public class ClassProfile
    {
        public string             Class       { get; set; } = "";
        /// <summary>
        /// Class group name, e.g. "Physical", "RangerRogue", "Mage", "DivineHybrid".
        /// Used for XP-transfer bonuses when switching within the same group.
        /// Mod-added classes may define any group string.
        /// </summary>
        public string             Group       { get; set; } = "Physical";
        public Dictionary<string, int> StatGrowth  { get; set; } = new();
        public int                HpPerLevel   { get; set; }
        public int                ManaPerLevel { get; set; }

        // ── Registry ─────────────────────────────────────────────────────────────

        private static Dictionary<string, ClassProfile> _all = new(StringComparer.OrdinalIgnoreCase);

        public static IReadOnlyDictionary<string, ClassProfile> All => _all;

        private static readonly JsonSerializerOptions _opts = new()
        {
            PropertyNameCaseInsensitive = true,
        };

        /// <summary>
        /// Loads class profiles from JSON. Replaces any previously loaded data.
        /// The JSON format is an array of ClassProfile objects, e.g.:
        /// [{ "class": "Archer", "group": "RangerRogue", "statGrowth": {...},
        ///    "hpPerLevel": 7, "manaPerLevel": 5 }]
        /// Mods can extend this file to add new moddable classes.
        /// </summary>
        public static void Load(string path = "Data/common/classes.json")
        {
            if (!File.Exists(path)) return;
            var profiles = JsonSerializer.Deserialize<List<ClassProfile>>(File.ReadAllText(path), _opts);
            if (profiles == null) return;
            Load(profiles);
        }

        /// <summary>Loads class profiles from already-parsed data (e.g. read from a database). Replaces any previously loaded data.</summary>
        public static void Load(List<ClassProfile> profiles)
        {
            _all = profiles
                .Where(p => !string.IsNullOrWhiteSpace(p.Class))
                .ToDictionary(p => p.Class, StringComparer.OrdinalIgnoreCase);
        }
    }
}
