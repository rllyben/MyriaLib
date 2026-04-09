using MyriaLib.Entities.Players;
using MyriaLib.Systems.Enums;

namespace MyriaLib.Entities.Items
{
    public class EquipmentItem : Item
    {
        public EquipmentType SlotType { get; set; }
        public override int BuyPrice { get; set; } = 300;
        public int UpgradeLevel { get; set; } = 0;

        // BaseStats are the item's definition values — never modified after creation
        public EquipmentBonuses BaseStats { get; set; }
        // Bonuses are the current effective values — scaled up by TryUpgrade
        public EquipmentBonuses Bonuses { get; set; }

        public bool IsUsableBy(Player player) => AllowedClasses.Contains(player.Class);

        public override void Use(Player player) { }

        public bool TryUpgrade(Player player)
        {
            if (UpgradeLevel >= 9)
                return false;

            var material = player.Inventory.Items.FirstOrDefault(i => i.Id == "upgrade_stone");
            if (material == null)
                return false;

            if (material.StackSize < 2)
                player.Inventory.RemoveItem(material);
            else
                material.StackSize--;

            TryUpgrade_Internal();
            return true;
        }

        /// <summary>
        /// Applies one upgrade level without consuming any materials.
        /// Used when restoring upgrade state from a save file.
        /// </summary>
        internal void TryUpgrade_Internal()
        {
            if (UpgradeLevel >= 9) return;

            UpgradeLevel++;

            float multiplier = UpgradeLevel < 4 ? 1 + (UpgradeLevel * 0.1f)
                             : UpgradeLevel < 7 ? 1 + (UpgradeLevel * 0.3f)
                             :                    1 + (UpgradeLevel * 0.7f);

            Bonuses = BaseStats.Scale(multiplier);
        }
    }
}
