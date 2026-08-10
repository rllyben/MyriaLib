using Myria.Lib.Core.Entities.Maps;
using Myria.Lib.Core.Entities.Characters;
using Myria.Lib.Core.Services.Builder;
using Myria.Lib.Core.Services.Manager;
using Myria.Lib.Core.Systems;
using Myria.Lib.Core.Systems.Enums;

namespace Myria.Lib.Core.Services
{
    /// <summary>
    /// Outcome of a single <see cref="GatherService.Gather"/> attempt. ItemId/Amount/JobId/
    /// XpGranted are only meaningful when Result is <see cref="GatherResult.Success"/>.
    /// </summary>
    public readonly record struct GatherOutcome(
        GatherResult Result,
        string? ItemId,
        int Amount,
        string? JobId,
        long XpGranted);

    public static class GatherService
    {
        /// <summary>
        /// Attempts a single gather action in <paramref name="room"/> for <paramref name="character"/>.
        /// Tries each of the room's gathering spots in order, skipping any the character lacks the
        /// required tool for, and uses the first workable one. Consumes one gather charge (taking
        /// the character's job-knowledge bonus into account) and adds the item to inventory.
        /// This is the single implementation both single-player and multiplayer hosts should call —
        /// previously each had its own independent, hand-duplicated copy of this exact logic.
        /// </summary>
        public static GatherOutcome Gather(Character character, Room room)
        {
            if (room.GatheringSpots.Count == 0)
                return new GatherOutcome(GatherResult.NoSpots, null, 0, null, 0);

            int bonus = JobManager.GetGatherKnowledgeBonus(character);
            if (room.GathersRemaining + bonus <= 0)
                return new GatherOutcome(GatherResult.Depleted, null, 0, null, 0);

            foreach (var spot in room.GatheringSpots)
            {
                if (!string.IsNullOrEmpty(spot.RequiredToolId))
                {
                    bool hasTool = character.Inventory.Items.Any(i => i.Id == spot.RequiredToolId)
                                || character.WeaponSlot?.Id == spot.RequiredToolId;
                    if (!hasTool) continue;
                }

                if (!ItemFactory.TryCreateItem(spot.GatheredItemId, out var item))
                    continue;

                string jobId = spot.Type switch
                {
                    GatheringType.Ore  => "miner",
                    GatheringType.Tree => "woodcutter",
                    GatheringType.Herb => "herbalist",
                    _                  => ""
                };

                long xpGranted = 0;
                if (!string.IsNullOrEmpty(jobId))
                {
                    item.StackSize = JobManager.GetGatherAmount(character, jobId);
                    xpGranted = 10;
                }
                if (item.StackSize <= 0)
                    item.StackSize = 1;

                // Capture before AddItem — it decrements StackSize during stack-merging.
                int gatheredAmount = item.StackSize;

                if (!character.Inventory.AddItem(item, character, "gather"))
                    return new GatherOutcome(GatherResult.InventoryFull, null, 0, null, 0);

                room.GathersRemaining--;
                if (!string.IsNullOrEmpty(jobId))
                    JobManager.GrantSkillXp(character, jobId, xpGranted);

                DayCycleManager.AddTicks(GameTick.Gather);
                return new GatherOutcome(GatherResult.Success, item.Id, gatheredAmount, jobId, xpGranted);
            }

            return new GatherOutcome(GatherResult.NoTool, null, 0, null, 0);
        }
    }
}
