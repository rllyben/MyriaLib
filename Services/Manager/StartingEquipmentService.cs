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
            {
                if (!ItemFactory.TryCreateItem(id, out var item) || item == null) continue;

                // For equipment items: assign directly to the matching slot so the character
                // starts the game ready to fight. If the slot is already taken (two weapons in
                // the starting kit, for example) the item goes into the inventory bag instead.
                // Bypassing SwapEquipment avoids the inventory-search-then-remove roundtrip
                // which can fail at character creation time before any events are wired.
                if (item is MyriaLib.Entities.Items.EquipmentItem eq && eq.IsUsableBy(character))
                {
                    switch (eq.SlotType)
                    {
                        case MyriaLib.Systems.Enums.EquipmentType.Weapon when character.WeaponSlot == null:
                            character.WeaponSlot = eq;
                            continue;
                        case MyriaLib.Systems.Enums.EquipmentType.Armor when character.ArmorSlot == null:
                            character.ArmorSlot = eq;
                            continue;
                        case MyriaLib.Systems.Enums.EquipmentType.Accessory when character.AccessorySlot == null:
                            character.AccessorySlot = eq;
                            continue;
                    }
                }

                // Non-equipment items or items whose slot is already filled go to the bag.
                character.Inventory.AddItem(item, character);
            }
        }

        private sealed class StartingItemEntry
        {
            public string   ClassId  { get; set; } = "";
            public string[] ItemIds  { get; set; } = [];
        }
    }
}
