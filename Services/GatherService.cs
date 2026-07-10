using MyriaLib.Entities.Maps;
using MyriaLib.Entities.Characters;
using MyriaLib.Services.Builder;
using MyriaLib.Services.Manager;
using MyriaLib.Systems;
using MyriaLib.Systems.Enums;

namespace MyriaLib.Services
{
    public static class GatherService
    {
        /// <summary>
        /// Gather directly from a specific <paramref name="spot"/> without touching room charges.
        /// Used by the 3D client when depletion is tracked per visible node rather than per room.
        /// </summary>
        public static GatherResult GatherFromSpot(Character character, GatheringSpot spot)
        {
            if (!character.HasToolFor(spot.Type))
                return GatherResult.NoTool;

            if (!ItemFactory.TryCreateItem(spot.GatheredItemId, out var item) || item == null)
                return GatherResult.Success;

            string jobId = spot.Type switch
            {
                GatheringType.Ore  => "miner",
                GatheringType.Tree => "woodcutter",
                GatheringType.Herb => "herbalist",
                _                  => ""
            };
            if (!string.IsNullOrEmpty(jobId))
            {
                long skillXp = Manager.JobManager.GetOrAdd(character, jobId).SkillXp;
                item.StackSize = JobXpService.ApplyGatherMultiplier(skillXp);
                Manager.JobManager.GrantSkillXp(character, jobId, 10);
            }

            if (!character.Inventory.AddItem(item, character, "gather"))
                return GatherResult.InventoryFull;

            DayCycleManager.AddTicks(GameTick.Gather);
            return GatherResult.Success;
        }

        /// <summary>
        /// Attempts a single gather action in <paramref name="room"/> for <paramref name="character"/>.
        /// Checks tool availability, consumes one gather charge, and adds the item to inventory.
        /// </summary>
        public static GatherResult Gather(Character character, Room room)
        {
            var result = room.TryConsumeGather(out var spot);
            if (result != GatherResult.Success)
                return result;

            // spot is guaranteed non-null here because TryConsumeGather returned Success
            if (!character.HasToolFor(spot!.Type))
            {
                // Undo the consumed gather — no tool means the attempt didn't happen
                room.GathersRemaining++;
                return GatherResult.NoTool;
            }

            if (!ItemFactory.TryCreateItem(spot.GatheredItemId, out var item))
                return GatherResult.Success; // spot has no item defined — treated as empty gather

            // J17: Apply Skill multiplier to gathered stack size
            string jobId = spot.Type switch
            {
                GatheringType.Ore  => "miner",
                GatheringType.Tree => "woodcutter",
                GatheringType.Herb => "herbalist",
                _                  => ""
            };
            if (!string.IsNullOrEmpty(jobId))
            {
                long skillXp = Manager.JobManager.GetOrAdd(character, jobId).SkillXp;
                item.StackSize = JobXpService.ApplyGatherMultiplier(skillXp);
                Manager.JobManager.GrantSkillXp(character, jobId, 10);
            }

            if (!character.Inventory.AddItem(item, character, "gather"))
                return GatherResult.InventoryFull;

            DayCycleManager.AddTicks(GameTick.Gather);
            return GatherResult.Success;
        }
    }
}
