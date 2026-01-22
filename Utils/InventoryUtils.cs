using MyriaLib.Entities.Items;
using MyriaLib.Entities.Players;

namespace MyriaLib.Utils
{
    public static class InventoryUtils
    {
        public static Item ResolveInventoryItem(string input, Player player)
        {
            var matches = player.Inventory.Items
                .Where(i => i.Id.StartsWith(input, StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (matches.All(i => i.Name.ToLower() == matches[0].Name.ToLower()))
                return matches[0];
            else
                return null;
        }

    }

}