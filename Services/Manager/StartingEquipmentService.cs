using MyriaLib.Entities.Characters;
using MyriaLib.Services.Builder;
using System.Text.Json;

namespace MyriaLib.Services.Manager
{
    public static class StartingEquipmentService
    {
        private static readonly JsonSerializerOptions _opts = new() { PropertyNameCaseInsensitive = true };
        private static Dictionary<string, string[]> _starterItems = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Loads starting-item assignments from JSON. Moddable via Data/common/starting_items.json.
        /// Expected format: [{ "ClassId": "Rogue", "ItemIds": ["starter_dagger", ...] }, ...]
        /// </summary>
        public static void Load(string path = "Data/common/starting_items.json")
        {
            if (!File.Exists(path)) return;
            try
            {
                var entries = JsonSerializer.Deserialize<List<StartingItemEntry>>(
                    File.ReadAllText(path), _opts);
                if (entries == null) return;
                _starterItems = entries
                    .Where(e => !string.IsNullOrWhiteSpace(e.ClassId))
                    .ToDictionary(e => e.ClassId, e => e.ItemIds ?? [], StringComparer.OrdinalIgnoreCase);
            }
            catch { /* file error — skip silently */ }
        }

        public static void GrantStartingEquipment(Character character)
        {
            if (!_starterItems.TryGetValue(character.Class, out var itemIds)) return;
            foreach (var id in itemIds)
                if (ItemFactory.TryCreateItem(id, out var item) && item != null)
                    character.Inventory.AddItem(item, character);
        }

        private sealed class StartingItemEntry
        {
            public string   ClassId  { get; set; } = "";
            public string[] ItemIds  { get; set; } = [];
        }
    }
}
