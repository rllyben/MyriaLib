using System.Text.Json;
using MyriaLib.Models;
using MyriaLib.Systems.Enums;

namespace MyriaLib.Services.Regestries
{
    /// <summary>
    /// Loads item rarity definitions from JSON.
    /// Expected format: [{ "id": "Common", "displayName": "Common", "order": 0 }, ...]
    /// </summary>
    public static class ItemRarityRegistry
    {
        private static readonly JsonSerializerOptions _opts = new() { PropertyNameCaseInsensitive = true };
        private static List<EnumDefinition> _definitions = new();

        public static void Load(string path = "Data/common/item_rarities.json")
        {
            if (!File.Exists(path)) return;
            _definitions = JsonSerializer.Deserialize<List<EnumDefinition>>(File.ReadAllText(path), _opts) ?? new();
        }

        public static IReadOnlyList<EnumDefinition> All => _definitions;

        public static string GetDisplayName(ItemRarity rarity)
            => _definitions.FirstOrDefault(d => d.Id.Equals(rarity.ToString(), StringComparison.OrdinalIgnoreCase))
                           ?.DisplayName ?? rarity.ToString();

        public static int GetOrder(ItemRarity rarity)
            => _definitions.FirstOrDefault(d => d.Id.Equals(rarity.ToString(), StringComparison.OrdinalIgnoreCase))
                           ?.Order ?? (int)rarity;
    }
}
