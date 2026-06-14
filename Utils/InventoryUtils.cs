using MyriaLib.Entities.Items;
using MyriaLib.Entities.Characters;

namespace MyriaLib.Utils
{
    public static class InventoryUtils
    {
        public static Item ResolveInventoryItem(string input, Character character)
        {
            var matches = character.Inventory.Items
                .Where(i => i.Id.StartsWith(input, StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (matches.Count == 0)
                return null;

            if (matches.All(i => i.Name.ToLower() == matches[0].Name.ToLower()))
                return matches[0];
            else
                return null;
        }

    }

}