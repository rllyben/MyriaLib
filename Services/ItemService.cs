using MyriaLib.Entities.Items;
using MyriaLib.Entities.Players;

namespace MyriaLib.Services
{
    public static class ItemService
    {
        /// <summary>
        /// Finds an item by name across the player's inventory and all equipped slots.
        /// Returns null if not found.
        /// </summary>
        public static Item? GetItemByNameFromPlayer(string name, Player player)
        {
            var item = player.Inventory.Items.FirstOrDefault(i =>
                i.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

            if (item != null) return item;
            if (player.WeaponSlot?.Name.Equals(name, StringComparison.OrdinalIgnoreCase) == true)
                return player.WeaponSlot;
            if (player.ArmorSlot?.Name.Equals(name, StringComparison.OrdinalIgnoreCase) == true)
                return player.ArmorSlot;
            if (player.AccessorySlot?.Name.Equals(name, StringComparison.OrdinalIgnoreCase) == true)
                return player.AccessorySlot;

            return null;
        }

        public static Item? GetItemByNameFromInventory(string name, Inventory inventory)
        {
            return inventory.Items.FirstOrDefault(i =>
                i.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        }
    }
}
