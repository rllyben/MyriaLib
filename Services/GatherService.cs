using MyriaLib.Entities.Maps;
using MyriaLib.Entities.Players;
using MyriaLib.Services.Builder;
using MyriaLib.Services.Manager;
using MyriaLib.Systems;
using MyriaLib.Systems.Enums;

namespace MyriaLib.Services
{
    public static class GatherService
    {
        /// <summary>
        /// Attempts a single gather action in <paramref name="room"/> for <paramref name="player"/>.
        /// Checks tool availability, consumes one gather charge, and adds the item to inventory.
        /// </summary>
        public static GatherResult Gather(Player player, Room room)
        {
            var result = room.TryConsumeGather(out var spot);
            if (result != GatherResult.Success)
                return result;

            // spot is guaranteed non-null here because TryConsumeGather returned Success
            if (!player.HasToolFor(spot!.Type))
            {
                // Undo the consumed gather — no tool means the attempt didn't happen
                room.GathersRemaining++;
                return GatherResult.NoTool;
            }

            if (!ItemFactory.TryCreateItem(spot.GatheredItemId, out var item))
                return GatherResult.Success; // spot has no item defined — treated as empty gather

            if (!player.Inventory.AddItem(item, player, "gather"))
                return GatherResult.InventoryFull;

            DayCycleManager.AddTicks(GameTick.Gather);
            return GatherResult.Success;
        }
    }
}
