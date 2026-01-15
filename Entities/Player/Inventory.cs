using ConsoleWorldRPG.Utils;
using MyriaLib.Entities.Items;
using MyriaLib.Systems.Enums;

namespace MyriaLib.Entities.Players
{
    public class Inventory
    {
        public int Capacity { get; set; } = 20;
        public List<Item> Items { get; set; } = new(); // could become Item class later

        public bool SwapEquipment(string itemname, Player player)
        {
            var match = InventoryUtils.ResolveInventoryItem(itemname, player);
            if (match is not EquipmentItem equipment)
                return false;
            if (!equipment.IsUsableBy(player))
                return false;
            switch (equipment.SlotType)
            {
                case EquipmentType.Weapon:
                    {
                        if (player.WeaponSlot != null)
                        {
                            EquipmentItem we = player.WeaponSlot;
                            player.WeaponSlot = equipment;
                            RemoveItem(equipment);
                            AddItem(we, player);
                            return true;
                        }
                        player.WeaponSlot = equipment;
                        RemoveItem(equipment);
                        return true;
                    }
                case EquipmentType.Armor:
                    {
                        if (player.ArmorSlot != null)
                        {
                            EquipmentItem arm = player.ArmorSlot;
                            player.ArmorSlot = equipment;
                            RemoveItem(equipment);
                            AddItem(arm, player);
                            return true;
                        }
                        player.ArmorSlot = equipment;
                        RemoveItem(equipment);
                        return true;
                    }
                case EquipmentType.Accessory:
                    {
                        if (player.AccessorySlot != null)
                        {
                            EquipmentItem acce = player.AccessorySlot;
                            player.AccessorySlot = equipment;
                            RemoveItem(equipment);
                            AddItem(acce, player);
                            return true;
                        }
                        player.AccessorySlot = equipment;
                        RemoveItem(equipment);
                        return true;
                    }


            }
            return false;
        }
        public bool UnequipItem(string itemname, Player player)
        {
            var match = InventoryUtils.ResolveInventoryItem(itemname, player);
            if (match is not EquipmentItem equipment)
                return false;
            if (equipment.IsUsableBy(player))
                return false;
            switch (equipment.SlotType)
            {
                case EquipmentType.Weapon:
                    {
                        if (player.WeaponSlot == null)
                            return false;
                        EquipmentItem we = player.WeaponSlot;
                        if (AddItem(we, player))
                        {
                            player.WeaponSlot = null;
                            return true;
                        }
                        return false;
                    }
                case EquipmentType.Armor:
                    {
                        if (player.ArmorSlot == null)
                            return false;
                        EquipmentItem arm = player.ArmorSlot;
                        if (AddItem(arm, player))
                        { 
                            player.ArmorSlot = null;
                            return true;
                        }
                        return false;
                    }
                case EquipmentType.Accessory:
                    {
                        if (player.AccessorySlot == null)
                            return false;
                        EquipmentItem accs = player.AccessorySlot;
                        if (AddItem(accs, player))
                        {
                            player.AccessorySlot = null;
                            return true;
                        }
                        return false;
                    }


            }
            return false;
        }
        public bool UseItem(string itemname, Player player)
        {
            var item = InventoryUtils.ResolveInventoryItem(itemname, player);
            if (item == null || item is not ConsumableItem consumable) 
                return false;

            consumable.Use(player);
            RemoveItem(item);
            return true;
        }
        /// <summary>
        /// tries to add item to the inventory
        /// </summary>
        /// <param name="item">item to add</param>
        /// <param name="player">player character</param>
        /// <returns>if the item was added</returns>
        public bool AddItem(Item item, Player player)
        {
            foreach (var quest in player.ActiveQuests.Where(q => q.Status == QuestStatus.InProgress))
            {
                foreach (var itemReq in quest.RequiredItems)
                {
                    int owned = player.Inventory.Items.Count(i => i.Id == itemReq.Key);
                    quest.ItemProgress[itemReq.Key] = Math.Min(owned, itemReq.Value);
                }

                bool allKillsDone = quest.KillProgress.All(kp => kp.Value >= quest.RequiredKills[kp.Key]);
                bool allItemsDone = quest.ItemProgress.All(ip => ip.Value >= quest.RequiredItems[ip.Key]);

                if (allKillsDone && allItemsDone)
                {
                    quest.Status = QuestStatus.Completed;
                }

            }

            // First try to stack
            foreach (var existing in Items)
            {
                if (existing.CanStackWith(item) && existing.StackSize < existing.MaxStackSize)
                {
                    int space = existing.MaxStackSize - existing.StackSize;
                    int toAdd = Math.Min(space, item.StackSize);

                    existing.StackSize += toAdd;
                    item.StackSize -= toAdd;

                    Restack();
                    
                    if (item.StackSize == 0)
                        return true;
                        
                }

            }

            // Add remaining as new stack
            if (Items.Count < Capacity)
            {
                Items.Add(item);
                Restack();
                return true;
            }

            return false; // inventory full
        }
        /// <summary>
        /// removes an item from the inventory
        /// </summary>
        /// <param name="item">item to remove</param>
        /// <returns>if the item was removed</returns>
        public bool RemoveItem(Item item) => Items.Remove(item);

        /// <summary>
        /// tries to sell an item
        /// </summary>
        /// <param name="name">item name</param>
        /// <param name="quantity">amount to be sold</param>
        /// <param name="player">player character</param>
        /// <returns>if the item was successfully sold</returns>
        public bool SellItem(string name, int quantity, ref Player player)
        {
            var item = Items.FirstOrDefault(i => i.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
            if (item == null || quantity <= 0)
                return false;

            if (item.StackSize < quantity)
                return false;

            int coinsReceived = item.SellValue * quantity;

            // Reduce stack or remove item
            if (player.Money.TryAdd(coinsReceived))
            {
                if (item.StackSize == quantity)
                {
                    Items.Remove(item);
                }
                else
                {
                    item.StackSize -= quantity;
                }
                Restack();
                return true;
            }
            else
                return false;
        }
        public void Restack()
        {
            var grouped = Items
                .Where(i => i.MaxStackSize > 1)
                .GroupBy(i => i.Id)
                .ToList();

            foreach (var group in grouped)
            {
                var sorted = group.OrderByDescending(i => i.StackSize).ToList();
                var primary = sorted[0];

                for (int i = 1; i < sorted.Count; i++)
                {
                    var donor = sorted[i];
                    int space = primary.MaxStackSize - primary.StackSize;
                    int move = Math.Min(space, donor.StackSize);

                    primary.StackSize += move;
                    donor.StackSize -= move;

                    if (donor.StackSize <= 0)
                        Items.Remove(donor);
                }

            }

        }

    }

}
