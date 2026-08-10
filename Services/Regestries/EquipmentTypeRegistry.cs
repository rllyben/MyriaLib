using System.Text.Json;
using MyriaLib.Models;
using MyriaLib.Systems.Enums;

namespace MyriaLib.Services.Regestries
{
    /// <summary>
    /// Loads equipment slot definitions from JSON.
    /// Expected format: [{ "id": "Weapon", "displayName": "Weapon", "order": 0 }, ...]
    /// </summary>
    public static class EquipmentTypeRegistry
    {
        private static readonly JsonSerializerOptions _opts = new() { PropertyNameCaseInsensitive = true };
        private static List<EnumDefinition> _definitions = new();

        public static void Load(string path = "Data/common/equipment_slots.json")
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
