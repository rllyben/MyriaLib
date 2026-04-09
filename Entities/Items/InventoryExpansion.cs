using MyriaLib.Entities.Players;

namespace MyriaLib.Entities.Items
{
    /// <summary>
    /// A purchasable item that permanently unlocks one additional inventory page (7×7 slots).
    /// It is never added to the player's inventory — the NPC purchase flow applies it directly
    /// via <see cref="Use"/> and discards it.
    /// </summary>
    public class InventoryExpansion : Item
    {
        public override int MaxStackSize { get; set; } = 1;

        public override void Use(Player player)
        {
            player.Inventory.Pages++;
        }
    }
}
