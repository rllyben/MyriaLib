using System.Text.Json;
using MyriaLib.Models;
using MyriaLib.Systems.Enums;

namespace MyriaLib.Services.Regestries
{
    /// <summary>
    /// Loads gathering type definitions from JSON.
    /// Expected format: [{ "id": "Ore", "displayName": "Ore", "order": 0 }, ...]
    /// </summary>
    public static class GatheringTypeRegistry
    {
        private static readonly JsonSerializerOptions _opts = new() { PropertyNameCaseInsensitive = true };
        private static List<EnumDefinition> _definitions = new();

        public static void Load(string path = "Data/common/gathering_types.json")
        {
            if (!File.Exists(path)) return;
            _definitions = JsonSerializer.Deserialize<List<EnumDefinition>>(File.ReadAllText(path), _opts) ?? new();
        }

        public static IReadOnlyList<EnumDefinition> All => _definitions;

        public static string GetDisplayName(GatheringType type)
            => _definitions.FirstOrDefault(d => d.Id.Equals(type.ToString(), StringComparison.OrdinalIgnoreCase))
                           ?.DisplayName ?? type.ToString();
    }
}
