using MyriaLib.Entities.Characters;
using MyriaLib.Entities.Items;
using MyriaLib.Systems.Enums;
using Xunit;

namespace MyriaLib.Tests;

public class InventoryTests
{
    [Fact]
    public void UnequipSlot_MovesEquippedItemBackToInventory_AndClearsTheSlot()
    {
        var character = TestHelpers.CreateCharacter();
        var sword = new EquipmentItem { Id = "sword", Name = "Sword", SlotType = EquipmentType.Weapon };
        character.WeaponSlot = sword;

        bool unequipped = character.Inventory.UnequipSlot(EquipmentType.Weapon, character);

        Assert.True(unequipped);
        Assert.Null(character.WeaponSlot);
        Assert.Contains(character.Inventory.Items, i => i.Id == "sword");
    }

    [Fact]
    public void UnequipSlot_EmptySlot_ReturnsFalse()
    {
        var character = TestHelpers.CreateCharacter();
        Assert.False(character.Inventory.UnequipSlot(EquipmentType.Armor, character));
    }

    [Fact]
    public void UnequipSlot_InventoryFull_LeavesItemEquipped()
    {
        int originalPageSize = Inventory.PageSize;
        try
        {
            Inventory.PageSize = 0; // Capacity = 0 -> AddItem always fails
            var character = TestHelpers.CreateCharacter();
            var sword = new EquipmentItem { Id = "sword", Name = "Sword", SlotType = EquipmentType.Weapon };
            character.WeaponSlot = sword;

            bool unequipped = character.Inventory.UnequipSlot(EquipmentType.Weapon, character);

            Assert.False(unequipped);
            Assert.Same(sword, character.WeaponSlot); // still equipped, nothing changed
            Assert.Empty(character.Inventory.Items);
        }
        finally
        {
            Inventory.PageSize = originalPageSize;
        }
    }
    [Fact]
    public void AddItem_MergesFullyIntoExistingStack_WhenSpaceAllows()
    {
        var character = TestHelpers.CreateCharacter();
        var existing = new MaterialItem { Id = "iron_ore", Name = "Iron Ore", StackSize = 90 }; // MaxStackSize = 99
        character.Inventory.Items.Add(existing);

        var incoming = new MaterialItem { Id = "iron_ore", Name = "Iron Ore", StackSize = 5 };
        bool added = character.Inventory.AddItem(incoming, character);

        Assert.True(added);
        Assert.Single(character.Inventory.Items);       // fully merged, no second stack created
        Assert.Equal(95, existing.StackSize);
    }

    [Fact]
    public void AddItem_SplitsAcrossStacks_WhenExistingStackCannotHoldItAll()
    {
        var character = TestHelpers.CreateCharacter();
        var existing = new MaterialItem { Id = "iron_ore", Name = "Iron Ore", StackSize = 90 }; // MaxStackSize = 99
        character.Inventory.Items.Add(existing);

        var incoming = new MaterialItem { Id = "iron_ore", Name = "Iron Ore", StackSize = 20 };
        bool added = character.Inventory.AddItem(incoming, character);

        Assert.True(added);
        Assert.Equal(2, character.Inventory.Items.Count);
        Assert.Equal(99, existing.StackSize);            // filled to max
        Assert.Contains(character.Inventory.Items, i => i.StackSize == 11); // 20 - 9 leftover
    }

    [Fact]
    public void AddItem_RespectsCapacityLimit()
    {
        int originalPageSize = Inventory.PageSize;
        try
        {
            Inventory.PageSize = 2; // small, deterministic capacity for this test
            var character = TestHelpers.CreateCharacter();
            character.Inventory.Pages = 1; // capacity = 2 slots

            Assert.True(character.Inventory.AddItem(new MaterialItem { Id = "a", Name = "A", StackSize = 1 }, character));
            Assert.True(character.Inventory.AddItem(new MaterialItem { Id = "b", Name = "B", StackSize = 1 }, character));

            // Third distinct item can't stack with anything and there's no free slot left.
            bool added = character.Inventory.AddItem(new MaterialItem { Id = "c", Name = "C", StackSize = 1 }, character);

            Assert.False(added);
            Assert.Equal(2, character.Inventory.Items.Count);
        }
        finally
        {
            Inventory.PageSize = originalPageSize; // PageSize is static/shared — never leak this across tests
        }
    }

    [Fact]
    public void RemoveItem_RemovesExactInstance_AndReturnsFalseIfNotPresent()
    {
        var character = TestHelpers.CreateCharacter();
        var item = new MaterialItem { Id = "iron_ore", Name = "Iron Ore", StackSize = 1 };
        character.Inventory.Items.Add(item);

        Assert.True(character.Inventory.RemoveItem(item));
        Assert.Empty(character.Inventory.Items);
        Assert.False(character.Inventory.RemoveItem(item)); // already removed
    }

    [Fact]
    public void SellItem_ReducesStackAndPaysBaseSellValue_WhenNoActiveJobBonusApplies()
    {
        var character = TestHelpers.CreateCharacter();
        var item = new MaterialItem { Id = "iron_ore", Name = "Iron Ore", StackSize = 5 }; // BuyPrice defaults to 100 -> SellValue 75
        character.Inventory.Items.Add(item);

        bool sold = character.Inventory.SellItem("Iron Ore", 3, ref character);

        Assert.True(sold);
        Assert.Equal(2, item.StackSize);
        Assert.Equal(225, character.Money.Balance.BronzeTotal); // 75 * 3
    }

    [Fact]
    public void SellItem_Fails_WhenRequestedQuantityExceedsStack()
    {
        var character = TestHelpers.CreateCharacter();
        var item = new MaterialItem { Id = "iron_ore", Name = "Iron Ore", StackSize = 2 };
        character.Inventory.Items.Add(item);

        bool sold = character.Inventory.SellItem("Iron Ore", 3, ref character);

        Assert.False(sold);
        Assert.Equal(2, item.StackSize);              // unchanged
        Assert.Equal(0, character.Money.Balance.BronzeTotal);
    }
}
