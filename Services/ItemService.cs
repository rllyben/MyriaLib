using MyriaLib.Entities.Items;
using MyriaLib.Entities.Characters;

namespace MyriaLib.Services
{
    public static class ItemService
    {
        /// <summary>
        /// Finds an item by name across the character's inventory and all equipped slots.
        /// Returns null if not found.
        /// </summary>
        public static Item? GetItemByNameFromCharacter(string name, Character character)
        {
            var item = character.Inventory.Items.FirstOrDefault(i =>
                i.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

            if (item != null) return item;
            if (character.WeaponSlot?.Name.Equals(name, StringComparison.OrdinalIgnoreCase) == true)
                return character.WeaponSlot;
            if (character.ArmorSlot?.Name.Equals(name, StringComparison.OrdinalIgnoreCase) == true)
                return character.ArmorSlot;
            if (character.AccessorySlot?.Name.Equals(name, StringComparison.OrdinalIgnoreCase) == true)
                return character.AccessorySlot;

            return null;
        }

        public static Item? GetItemByNameFromInventory(string name, Inventory inventory)
        {
            return inventory.Items.FirstOrDefault(i =>
                i.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        }
    }
}
