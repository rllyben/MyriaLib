using MyriaLib.Entities.Items;
using MyriaLib.Entities.NPCs;
using MyriaLib.Entities.Characters;

namespace MyriaLib.Services
{
    public static class NpcInteractionService
    {
        public static NpcActionResult Execute(Character character, Npc npc, string serviceId, Item? item = null, int amount = 1)
        {
            switch (serviceId)
            {
                case "heal":
                    return npc.HealingAction(character);

                case "buy_items":
                    if (item == null) return NpcActionResult.Fail("npc.action.item.null");
                    return npc.BuyItem(character, item, amount);

                case "sell_items":
                    if (item == null) return NpcActionResult.Fail("npc.action.item.null");
                    return npc.SellItem(character, item, amount);

                case "upgrade":
                    if (item == null) return NpcActionResult.Fail("npc.action.item.null");
                    return npc.UpgradeItem(character, item);

                case "craft":
                    if (item == null) return NpcActionResult.Fail("npc.action.item.null");
                    return npc.CraftItem(character, item);

                case "shop_equipment":
                    // UI-driven: open shop window in WPF
                    return NpcActionResult.Ok("npc.action.open.shop_equipment");

                case "shop_general":
                    return NpcActionResult.Ok("npc.action.open.shop_general");

                case "talk":
                    return NpcActionResult.Ok("npc.action.talk.default");

                default:
                    return NpcActionResult.Fail("npc.action.unknown", serviceId);
            }

        }

    }

}
