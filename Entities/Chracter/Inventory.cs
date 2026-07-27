using MyriaLib.Utils;
using MyriaLib.Entities.Items;
using MyriaLib.Services.Manager;
using MyriaLib.Systems;
using MyriaLib.Systems.Enums;
using MyriaLib.Systems.Events;

namespace MyriaLib.Entities.Characters
{
    public class Inventory
    {
        public event EventHandler<ItemReceivedEventArgs>? ItemReceived;
        public event EventHandler<ItemReceivedEventArgs>? ItemRemoved;
        public event EventHandler<ItemReceivedEventArgs>? ItemSold;

        /// <summary>Slot count per inventory page. Defaults to 49 (7×7 UI grid); override via <see cref="MyriaLib.Systems.GameConfig"/>.</summary>
        public static int PageSize { get; set; } = 49;

        /// <summary>Number of unlocked pages. Starts at 1; increased by purchasing inventory expansions.</summary>
        public int Pages { get; set; } = 1;

        /// <summary>Total slot capacity across all unlocked pages.</summary>
        public int Capacity => Pages * PageSize;

        public List<Item> Items { get; set; } = new();

        /// <summary>
        /// Returns a fixed-length array of <see cref="PageSize"/> slots for the given page (0-indexed).
        /// Empty slots are null. Suitable for binding directly to a 7×7 UI grid.
        /// </summary>
        public Item?[] GetPage(int page)
        {
            var result = new Item?[PageSize];
            int start = page * PageSize;
            for (int i = 0; i < PageSize; i++)
            {
                int idx = start + i;
                result[i] = idx < Items.Count ? Items[idx] : null;
            }
            return result;
        }

        /// <summary>
        /// Returns only the non-null items on the given page. Useful for logic that
        /// doesn't need slot positions (console pager, quest tracking, etc.).
        /// </summary>
        public IEnumerable<Item> GetItemsOnPage(int page) =>
            Items.Skip(page * PageSize).Take(PageSize);

        /// <summary>How many pages currently contain at least one item (always ≥ 1).</summary>
        public int UsedPages => Math.Max(1, (int)Math.Ceiling((double)Items.Count / PageSize));

        public bool SwapEquipment(string itemId, Character character)
        {
            System.Diagnostics.Debug.WriteLine($"[Inventory] SwapEquipment called for: {itemId}");
            
            var match = InventoryUtils.ResolveInventoryItem(itemId, character);
            System.Diagnostics.Debug.WriteLine($"[Inventory] Resolved item: {match?.Name ?? "NULL"}");
            
            if (match is not EquipmentItem equipment)
            {
                System.Diagnostics.Debug.WriteLine($"[Inventory] Item is not EquipmentItem");
                return false;
            }

            System.Diagnostics.Debug.WriteLine($"[Inventory] Equipment found: {equipment.Name}, SlotType: {equipment.SlotType}");
            
            if (!equipment.IsUsableBy(character))
            {
                System.Diagnostics.Debug.WriteLine($"[Inventory] Equipment not usable by player");
                return false;
            }

            switch (equipment.SlotType)
            {
                case EquipmentType.Weapon:
                    {
                        System.Diagnostics.Debug.WriteLine($"[Inventory] Swapping Weapon slot");
                        
                        if (character.WeaponSlot != null)
                        {
                            System.Diagnostics.Debug.WriteLine($"[Inventory] Weapon slot occupied, swapping out: {character.WeaponSlot.Name}");
                            EquipmentItem we = character.WeaponSlot;
                            character.WeaponSlot = equipment;
                            System.Diagnostics.Debug.WriteLine($"[Inventory] New weapon equipped: {equipment.Name}");
                            
                            RemoveItem(equipment);
                            System.Diagnostics.Debug.WriteLine($"[Inventory] Removed {equipment.Name} from inventory");
                            
                            AddItem(we, character);
                            System.Diagnostics.Debug.WriteLine($"[Inventory] Added unequipped weapon to inventory: {we.Name}");
                            return true;
                        }
                        
                        System.Diagnostics.Debug.WriteLine($"[Inventory] Weapon slot empty, equipping: {equipment.Name}");
                        character.WeaponSlot = equipment;
                        RemoveItem(equipment);
                        System.Diagnostics.Debug.WriteLine($"[Inventory] Weapon equipped successfully");
                        return true;
                    }
                case EquipmentType.Armor:
                    {
                        System.Diagnostics.Debug.WriteLine($"[Inventory] Swapping Armor slot");
                        
                        if (character.ArmorSlot != null)
                        {
                            System.Diagnostics.Debug.WriteLine($"[Inventory] Armor slot occupied, swapping out: {character.ArmorSlot.Name}");
                            EquipmentItem arm = character.ArmorSlot;
                            character.ArmorSlot = equipment;
                            System.Diagnostics.Debug.WriteLine($"[Inventory] New armor equipped: {equipment.Name}");
                            
                            RemoveItem(equipment);
                            System.Diagnostics.Debug.WriteLine($"[Inventory] Removed {equipment.Name} from inventory");
                            
                            AddItem(arm, character);
                            System.Diagnostics.Debug.WriteLine($"[Inventory] Added unequipped armor to inventory: {arm.Name}");
                            return true;
                        }
                        
                        System.Diagnostics.Debug.WriteLine($"[Inventory] Armor slot empty, equipping: {equipment.Name}");
                        character.ArmorSlot = equipment;
                        RemoveItem(equipment);
                        System.Diagnostics.Debug.WriteLine($"[Inventory] Armor equipped successfully");
                        return true;
                    }
                case EquipmentType.Accessory:
                    {
                        System.Diagnostics.Debug.WriteLine($"[Inventory] Swapping Accessory slot");
                        
                        if (character.AccessorySlot != null)
                        {
                            System.Diagnostics.Debug.WriteLine($"[Inventory] Accessory slot occupied, swapping out: {character.AccessorySlot.Name}");
                            EquipmentItem acce = character.AccessorySlot;
                            character.AccessorySlot = equipment;
                            System.Diagnostics.Debug.WriteLine($"[Inventory] New accessory equipped: {equipment.Name}");
                            
                            RemoveItem(equipment);
                            System.Diagnostics.Debug.WriteLine($"[Inventory] Removed {equipment.Name} from inventory");
                            
                            AddItem(acce, character);
                            System.Diagnostics.Debug.WriteLine($"[Inventory] Added unequipped accessory to inventory: {acce.Name}");
                            return true;
                        }
                        
                        System.Diagnostics.Debug.WriteLine($"[Inventory] Accessory slot empty, equipping: {equipment.Name}");
                        character.AccessorySlot = equipment;
                        RemoveItem(equipment);
                        System.Diagnostics.Debug.WriteLine($"[Inventory] Accessory equipped successfully");
                        return true;
                    }
            }
            
            System.Diagnostics.Debug.WriteLine($"[Inventory] SwapEquipment failed - unknown slot type");
            return false;
        }
        public bool UnequipItem(string itemname, Character character)
        {
            System.Diagnostics.Debug.WriteLine($"[Inventory] UnequipItem called for: {itemname}");
            
            var match = InventoryUtils.ResolveInventoryItem(itemname, character);
            System.Diagnostics.Debug.WriteLine($"[Inventory] Resolved item: {match?.Name ?? "NULL"}");
            
            if (match is not EquipmentItem equipment)
            {
                System.Diagnostics.Debug.WriteLine($"[Inventory] Item is not EquipmentItem");
                return false;
            }
            
            if (equipment.IsUsableBy(character))
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
                        
                        if (character.WeaponSlot == null)
                        {
                            System.Diagnostics.Debug.WriteLine($"[Inventory] Weapon slot is empty");
                            return false;
                        }
                        
                        EquipmentItem we = character.WeaponSlot;
                        System.Diagnostics.Debug.WriteLine($"[Inventory] Adding unequipped weapon to inventory: {we.Name}");
                        
                        if (AddItem(we, character))
                        {
                            System.Diagnostics.Debug.WriteLine($"[Inventory] Item added successfully, clearing weapon slot");
                            character.WeaponSlot = null;
                            return true;
                        }
                        
                        System.Diagnostics.Debug.WriteLine($"[Inventory] Failed to add item to inventory (inventory full?)");
                        return false;
                    }
                case EquipmentType.Armor:
                    {
                        System.Diagnostics.Debug.WriteLine($"[Inventory] Unequipping from Armor slot");
                        
                        if (character.ArmorSlot == null)
                        {
                            System.Diagnostics.Debug.WriteLine($"[Inventory] Armor slot is empty");
                            return false;
                        }
                        
                        EquipmentItem arm = character.ArmorSlot;
                        System.Diagnostics.Debug.WriteLine($"[Inventory] Adding unequipped armor to inventory: {arm.Name}");
                        
                        if (AddItem(arm, character))
                        { 
                            System.Diagnostics.Debug.WriteLine($"[Inventory] Item added successfully, clearing armor slot");
                            character.ArmorSlot = null;
                            return true;
                        }
                        
                        System.Diagnostics.Debug.WriteLine($"[Inventory] Failed to add item to inventory (inventory full?)");
                        return false;
                    }
                case EquipmentType.Accessory:
                    {
                        System.Diagnostics.Debug.WriteLine($"[Inventory] Unequipping from Accessory slot");
                        
                        if (character.AccessorySlot == null)
                        {
                            System.Diagnostics.Debug.WriteLine($"[Inventory] Accessory slot is empty");
                            return false;
                        }
                        
                        EquipmentItem accs = character.AccessorySlot;
                        System.Diagnostics.Debug.WriteLine($"[Inventory] Adding unequipped accessory to inventory: {accs.Name}");
                        
                        if (AddItem(accs, character))
                        {
                            System.Diagnostics.Debug.WriteLine($"[Inventory] Item added successfully, clearing accessory slot");
                            character.AccessorySlot = null;
                            return true;
                        }
                        
                        System.Diagnostics.Debug.WriteLine($"[Inventory] Failed to add item to inventory (inventory full?)");
                        return false;
                    }
            }
            
            System.Diagnostics.Debug.WriteLine($"[Inventory] UnequipItem failed - unknown slot type");
            return false;
        }

        /// <summary>
        /// Unequips whatever is in <paramref name="slotType"/> and returns it to
        /// <paramref name="character"/>'s inventory. Returns false (no state changed) if the slot
        /// is already empty or the inventory has no room for the returned item.
        /// <para>
        /// This is the single implementation single-player (EquipmentViewModel.ExecuteUnequip) and
        /// multiplayer (GameHub.UnequipItem) both now call — previously each had its own identical
        /// by-slot-type logic hand-duplicated independently. Not to be confused with the older
        /// <see cref="UnequipItem(string, Character)"/> overload above, which resolves by item name
        /// against the loose inventory list — that can never actually match an equipped item (it
        /// isn't in that list) and is unrelated to this method.
        /// </para>
        /// </summary>
        public bool UnequipSlot(string slotType, Character character)
        {
            EquipmentItem? item = slotType switch
            {
                EquipmentType.Weapon    => character.WeaponSlot,
                EquipmentType.Armor     => character.ArmorSlot,
                EquipmentType.Accessory => character.AccessorySlot,
                _                       => null
            };
            if (item == null) return false;

            if (!AddItem(item, character, "unequip"))
                return false;

            switch (slotType)
            {
                case EquipmentType.Weapon:    character.WeaponSlot    = null; break;
                case EquipmentType.Armor:     character.ArmorSlot     = null; break;
                case EquipmentType.Accessory: character.AccessorySlot = null; break;
            }
            return true;
        }

        public bool UseItem(string itemname, Character character)
        {
            var item = InventoryUtils.ResolveInventoryItem(itemname, character);
            if (item == null || item is not ConsumableItem consumable) 
                return false;

            consumable.Use(character);
            if (item.StackSize > 1)
            {
                item.StackSize--;
                Restack();
                ItemRemoved?.Invoke(this, new ItemReceivedEventArgs(item.CloneOne(), 1));
            }
            else
            {
                RemoveItem(item);
            }

            return true;
        }
        /// <summary>
        /// tries to add item to the inventory
        /// </summary>
        /// <param name="item">item to add</param>
        /// <param name="player">player character</param>
        /// <returns>if the item was added</returns>
        public bool AddItem(Item item, Character character, string? source = null)
        {
            int stackSize = item.StackSize;

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
                        GameEvents.FireItemReceived(character, item, stackSize);
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
                GameEvents.FireItemReceived(character, item, stackSize);
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
        public bool SellItem(string name, int quantity, ref Character character)
        {
            var item = Items.FirstOrDefault(i => i.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
            if (item == null || quantity <= 0)
                return false;

            if (item.StackSize < quantity)
                return false;

            int coinsReceived = JobManager.GetSellValue(item, character) * quantity;

            // Snapshot item data before any mutation
            Item soldCopy = item.CloneOne();
            soldCopy.StackSize = quantity;

            // Reduce stack or remove item
            if (character.Money.TryAdd(coinsReceived))
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
