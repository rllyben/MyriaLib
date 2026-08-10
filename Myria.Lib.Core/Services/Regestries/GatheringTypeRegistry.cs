using System.Text.Json;
using Myria.Lib.Core.Models;
using Myria.Lib.Core.Systems.Enums;

namespace Myria.Lib.Core.Services.Regestries
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

        public static string GetDisplayName(string type)
            => _definitions.FirstOrDefault(d => d.Id.Equals(type, StringComparison.OrdinalIgnoreCase))
                           ?.DisplayName ?? type;
    }
}
