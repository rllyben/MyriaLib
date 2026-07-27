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

        public static string GetDisplayName(string rarity)
            => _definitions.FirstOrDefault(d => d.Id.Equals(rarity, StringComparison.OrdinalIgnoreCase))
                           ?.DisplayName ?? rarity;

        /// <summary>
        /// Sort order for a rarity. Falls back to its position in
        /// <see cref="ItemRarity.AllBuiltIn"/> (matching the original enum's ordinal) for built-in
        /// rarities not overridden by item_rarities.json, or 0 for an unrecognized mod-added one.
        /// </summary>
        public static int GetOrder(string rarity)
        {
            var match = _definitions.FirstOrDefault(d => d.Id.Equals(rarity, StringComparison.OrdinalIgnoreCase));
            if (match != null) return match.Order;

            int index = ItemRarity.AllBuiltIn.ToList().IndexOf(rarity);
            return index >= 0 ? index : 0;
        }
    }
}
