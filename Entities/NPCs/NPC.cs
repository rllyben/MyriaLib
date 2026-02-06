using MyriaLib.Entities.Items;
using MyriaLib.Entities.Players;
using MyriaLib.Services;
using MyriaLib.Services.Builder;
using MyriaLib.Systems;
using MyriaLib.Systems.Enums;

namespace MyriaLib.Entities.NPCs
{
    public class Npc
    {
        public string Id { get; set; } = "";
        public string NameKey { get; set; } = "";        // e.g. "game.npc.healer.name"
        public string DescriptionKey { get; set; } = ""; // e.g. "game.npc.healer.desc"

        public NpcType Type { get; set; }                // Healer, Shop, QuestGiver, etc.
        public List<string> Services { get; set; } = new(); // optional: "heal", "shop", "quests"
        public List<string> ItemNames { get; set; } = new();
        public List<Item> ItemRefs { get; set; } = new();
        public override string ToString()
        {
            return Localization.T(NameKey);
        }
        // --- Healing (Healer) ---
        public NpcActionResult HealingAction(Player player)
        {
            // Use your new HP/MP event-friendly methods
            player.Heal(int.MaxValue, ToString());
            player.RestoreMana(int.MaxValue, ToString());

            return NpcActionResult.Ok("npc.action.heal.ok");
        }
        // --- Buy (Smith/Trader/Healer potions etc.) ---
        public NpcActionResult BuyItem(Player player, Item item, int amount = 1)
        {
            if (amount <= 0) return NpcActionResult.Fail("npc.action.amount.invalid");
            if (item == null) return NpcActionResult.Fail("npc.action.item.null");

            int totalCost = item.BuyPrice * amount;

            // placeholder API, adjust if your MoneyBag differs
            if (!player.Money.TrySpend(totalCost))
                return NpcActionResult.Fail("npc.action.buy.notEnoughMoney", totalCost);

            // Create the correct amount as stack(s)
            // If stackable, we can just create one stack with StackSize=amount
            var toAdd = ItemFactory.CreateItem(item.Id, amount);

            // Inventory.AddItem signature in your projects has been used as AddItem(item, player)
            // If yours differs, adjust here.
            bool ok = player.Inventory.AddItem(toAdd, player);
            if (!ok)
            {
                player.Money.TryAdd(totalCost);
                return NpcActionResult.Fail("npc.action.buy.inventoryFull");
            }

            return NpcActionResult.Ok("npc.action.buy.ok", amount, item.Id);
        }

        // --- Sell (Trader/Healer) ---
        public NpcActionResult SellItem(Player player, Item item, int amount = 1)
        {
            if (amount <= 0) return NpcActionResult.Fail("npc.action.amount.invalid");
            if (item == null) return NpcActionResult.Fail("npc.action.item.null");

            // Find matching inventory item (by Id)
            var invItem = player.Inventory.Items.FirstOrDefault(i => i.Id == item.Id);
            if (invItem == null)
                return NpcActionResult.Fail("npc.action.sell.notOwned", item.Id);

            if (invItem.StackSize < amount)
                return NpcActionResult.Fail("npc.action.sell.notEnoughAmount", amount);

            int totalGain = invItem.SellValue * amount;

            // reduce stack / remove item
            if (invItem.StackSize == amount)
                player.Inventory.RemoveItem(invItem);
            else
                invItem.StackSize -= amount;

            player.Money.TryAdd(totalGain); // placeholder API

            return NpcActionResult.Ok("npc.action.sell.ok", amount, item.Id, totalGain);
        }
        // --- Upgrade (Smith) ---
        public NpcActionResult UpgradeItem(Player player, Item item)
        {
            if (item is not EquipmentItem eq)
                return NpcActionResult.Fail("npc.action.upgrade.notEquipment");

            // You have TryUpgrade in console version on EquipmentItem :contentReference[oaicite:6]{index=6}
            // If MyriaLib EquipmentItem also has TryUpgrade(player), use it:
            if (eq.TryUpgrade(player))
                return NpcActionResult.Ok("npc.action.upgrade.ok", eq.Id, eq.UpgradeLevel);

            return NpcActionResult.Fail("npc.action.upgrade.fail", eq.Id);
        }
        // --- Craft (Smith) ---
        public NpcActionResult CraftItem(Player player, Item item)
        {
            // Crafting rules differ per game. The console version crafts upgrade_stone from iron_ore :contentReference[oaicite:7]{index=7}.
            // Keep this as a simple hook for now:
            return NpcActionResult.Fail("npc.action.craft.notImplemented");
        }

    }

}
