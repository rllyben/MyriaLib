using MyriaLib.Utils;
using MyriaLib.Entities.Items;
using MyriaLib.Systems.Enums;
using MyriaLib.Systems.Events;

namespace MyriaLib.Entities.Players
{
    public class Inventory
    {
        public event EventHandler<ItemReceivedEventArgs>? ItemReceived;
        public event EventHandler<ItemReceivedEventArgs>? ItemRemoved;
        public event EventHandler<ItemReceivedEventArgs>? ItemSold;
        public int Capacity { get; set; } = 49;
        public List<Item> Items { get; set; } = new(); // could become Item class later

        public bool SwapEquipment(string itemId, Player player)
        {
            System.Diagnostics.Debug.WriteLine($"[Inventory] SwapEquipment called for: {itemId}");
            
            var match = InventoryUtils.ResolveInventoryItem(itemId, player);
            System.Diagnostics.Debug.WriteLine($"[Inventory] Resolved item: {match?.Name ?? "NULL"}");
            
            if (match is not EquipmentItem equipment)
            {
                System.Diagnostics.Debug.WriteLine($"[Inventory] Item is not EquipmentItem");
                return false;
            }

            System.Diagnostics.Debug.WriteLine($"[Inventory] Equipment found: {equipment.Name}, SlotType: {equipment.SlotType}");
            
            if (!equipment.IsUsableBy(player))
            {
                System.Diagnostics.Debug.WriteLine($"[Inventory] Equipment not usable by player");
                return false;
            }

            switch (equipment.SlotType)
            {
                case EquipmentType.Weapon:
                    {
                        System.Diagnostics.Debug.WriteLine($"[Inventory] Swapping Weapon slot");
                        
                        if (player.WeaponSlot != null)
                        {
                            System.Diagnostics.Debug.WriteLine($"[Inventory] Weapon slot occupied, swapping out: {player.WeaponSlot.Name}");
                            EquipmentItem we = player.WeaponSlot;
                            player.WeaponSlot = equipment;
                            System.Diagnostics.Debug.WriteLine($"[Inventory] New weapon equipped: {equipment.Name}");
                            
                            RemoveItem(equipment);
                            System.Diagnostics.Debug.WriteLine($"[Inventory] Removed {equipment.Name} from inventory");
                            
                            AddItem(we, player);
                            System.Diagnostics.Debug.WriteLine($"[Inventory] Added unequipped weapon to inventory: {we.Name}");
                            return true;
                        }
                        
                        System.Diagnostics.Debug.WriteLine($"[Inventory] Weapon slot empty, equipping: {equipment.Name}");
                        player.WeaponSlot = equipment;
                        RemoveItem(equipment);
                        System.Diagnostics.Debug.WriteLine($"[Inventory] Weapon equipped successfully");
                        return true;
                    }
                case EquipmentType.Armor:
                    {
                        System.Diagnostics.Debug.WriteLine($"[Inventory] Swapping Armor slot");
                        
                        if (player.ArmorSlot != null)
                        {
                            System.Diagnostics.Debug.WriteLine($"[Inventory] Armor slot occupied, swapping out: {player.ArmorSlot.Name}");
                            EquipmentItem arm = player.ArmorSlot;
                            player.ArmorSlot = equipment;
                            System.Diagnostics.Debug.WriteLine($"[Inventory] New armor equipped: {equipment.Name}");
                            
                            RemoveItem(equipment);
                            System.Diagnostics.Debug.WriteLine($"[Inventory] Removed {equipment.Name} from inventory");
                            
                            AddItem(arm, player);
                            System.Diagnostics.Debug.WriteLine($"[Inventory] Added unequipped armor to inventory: {arm.Name}");
                            return true;
                        }
                        
                        System.Diagnostics.Debug.WriteLine($"[Inventory] Armor slot empty, equipping: {equipment.Name}");
                        player.ArmorSlot = equipment;
                        RemoveItem(equipment);
                        System.Diagnostics.Debug.WriteLine($"[Inventory] Armor equipped successfully");
                        return true;
                    }
                case EquipmentType.Accessory:
                    {
                        System.Diagnostics.Debug.WriteLine($"[Inventory] Swapping Accessory slot");
                        
                        if (player.AccessorySlot != null)
                        {
                            System.Diagnostics.Debug.WriteLine($"[Inventory] Accessory slot occupied, swapping out: {player.AccessorySlot.Name}");
                            EquipmentItem acce = player.AccessorySlot;
                            player.AccessorySlot = equipment;
                            System.Diagnostics.Debug.WriteLine($"[Inventory] New accessory equipped: {equipment.Name}");
                            
                            RemoveItem(equipment);
                            System.Diagnostics.Debug.WriteLine($"[Inventory] Removed {equipment.Name} from inventory");
                            
                            AddItem(acce, player);
                            System.Diagnostics.Debug.WriteLine($"[Inventory] Added unequipped accessory to inventory: {acce.Name}");
                            return true;
                        }
                        
                        System.Diagnostics.Debug.WriteLine($"[Inventory] Accessory slot empty, equipping: {equipment.Name}");
                        player.AccessorySlot = equipment;
                        RemoveItem(equipment);
                        System.Diagnostics.Debug.WriteLine($"[Inventory] Accessory equipped successfully");
                        return true;
                    }
            }
            
            System.Diagnostics.Debug.WriteLine($"[Inventory] SwapEquipment failed - unknown slot type");
            return false;
        }
        public bool UnequipItem(string itemname, Player player)
        {
            System.Diagnostics.Debug.WriteLine($"[Inventory] UnequipItem called for: {itemname}");
            
            var match = InventoryUtils.ResolveInventoryItem(itemname, player);
            System.Diagnostics.Debug.WriteLine($"[Inventory] Resolved item: {match?.Name ?? "NULL"}");
            
            if (match is not EquipmentItem equipment)
            {
                System.Diagnostics.Debug.WriteLine($"[Inventory] Item is not EquipmentItem");
                return false;
            }
            
            if (equipment.IsUsableBy(player))
            {
                System.Diagnostics.Debug.WriteLine($"[Inventory] Equipment IS usable by player (should be false for this check)");
                return false;
            }

            System.Diagnostics.Debug.WriteLine($"[Inventory] Equipment found: {equipment.Name}, SlotType: {equipment.SlotType}");
            
            switch (equipment.SlotType)
            {
                case EquipmentType.Weapon:
                    {
                        System.Diagnostics.Debug.WriteLine($"[Inventory] Unequipping from Weapon slot");
                        
                        if (player.WeaponSlot == null)
                        {
                            System.Diagnostics.Debug.WriteLine($"[Inventory] Weapon slot is empty");
                            return false;
                        }
                        
                        EquipmentItem we = player.WeaponSlot;
                        System.Diagnostics.Debug.WriteLine($"[Inventory] Adding unequipped weapon to inventory: {we.Name}");
                        
                        if (AddItem(we, player))
                        {
                            System.Diagnostics.Debug.WriteLine($"[Inventory] Item added successfully, clearing weapon slot");
                            player.WeaponSlot = null;
                            return true;
                        }
                        
                        System.Diagnostics.Debug.WriteLine($"[Inventory] Failed to add item to inventory (inventory full?)");
                        return false;
                    }
                case EquipmentType.Armor:
                    {
                        System.Diagnostics.Debug.WriteLine($"[Inventory] Unequipping from Armor slot");
                        
                        if (player.ArmorSlot == null)
                        {
                            System.Diagnostics.Debug.WriteLine($"[Inventory] Armor slot is empty");
                            return false;
                        }
                        
                        EquipmentItem arm = player.ArmorSlot;
                        System.Diagnostics.Debug.WriteLine($"[Inventory] Adding unequipped armor to inventory: {arm.Name}");
                        
                        if (AddItem(arm, player))
                        { 
                            System.Diagnostics.Debug.WriteLine($"[Inventory] Item added successfully, clearing armor slot");
                            player.ArmorSlot = null;
                            return true;
                        }
                        
                        System.Diagnostics.Debug.WriteLine($"[Inventory] Failed to add item to inventory (inventory full?)");
                        return false;
                    }
                case EquipmentType.Accessory:
                    {
                        System.Diagnostics.Debug.WriteLine($"[Inventory] Unequipping from Accessory slot");
                        
                        if (player.AccessorySlot == null)
                        {
                            System.Diagnostics.Debug.WriteLine($"[Inventory] Accessory slot is empty");
                            return false;
                        }
                        
                        EquipmentItem accs = player.AccessorySlot;
                        System.Diagnostics.Debug.WriteLine($"[Inventory] Adding unequipped accessory to inventory: {accs.Name}");
                        
                        if (AddItem(accs, player))
                        {
                            System.Diagnostics.Debug.WriteLine($"[Inventory] Item added successfully, clearing accessory slot");
                            player.AccessorySlot = null;
                            return true;
                        }
                        
                        System.Diagnostics.Debug.WriteLine($"[Inventory] Failed to add item to inventory (inventory full?)");
                        return false;
                    }
            }
            
            System.Diagnostics.Debug.WriteLine($"[Inventory] UnequipItem failed - unknown slot type");
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
        public bool AddItem(Item item, Player player, string? source = null)
        {
            int stackSize = item.StackSize;
            foreach (var quest in player.ActiveQuests.Where(q => q.Status == QuestStatus.InProgress))
            {
                foreach (var itemReq in quest.RequiredItems)
                {
                    int owned = player.Inventory.Items.Count(i => i.Id == itemReq.Key);
                    quest.ItemProgress[itemReq.Key] = Math.Min(owned, itemReq.Value);
                }

                bool allKillsDone = quest.RequiredKills.All(rk =>
                    quest.KillProgress.TryGetValue(rk.Key, out int kills) && kills >= rk.Value);
                bool allItemsDone = quest.RequiredItems.All(ri =>
                    quest.ItemProgress.TryGetValue(ri.Key, out int items) && items >= ri.Value);

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
                    {
                        ItemReceived?.Invoke(this, new ItemReceivedEventArgs(item, stackSize, source));
                        return true;
                    }
                        
                }

            }

            // Add remaining as new stack
            if (Items.Count < Capacity)
            {
                Items.Add(item);
                Restack();
                ItemReceived?.Invoke(this, new ItemReceivedEventArgs(item, stackSize, source));
                return true;
            }

            return false; // inventory full
        }
        /// <summary>
        /// removes an item from the inventory
        /// </summary>
        /// <param name="item">item to remove</param>
        /// <returns>if the item was removed</returns>
        public bool RemoveItem(Item item)
        {
            if (!Items.Remove(item)) return false;
            ItemRemoved?.Invoke(this, new ItemReceivedEventArgs(item, item.StackSize));
            return true;
        }

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

            // Snapshot item data before any mutation
            Item soldCopy = item.CloneOne();
            soldCopy.StackSize = quantity;

            // Reduce stack or remove item
            if (player.Money.TryAdd(coinsReceived))
            {
                if (item.StackSize == quantity)
                    Items.Remove(item);
                else
                    item.StackSize -= quantity;

                Restack();
                ItemSold?.Invoke(this, new ItemReceivedEventArgs(soldCopy, quantity, "sell"));
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
