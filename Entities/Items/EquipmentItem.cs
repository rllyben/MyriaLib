using MyriaLib.Entities.Players;
using MyriaLib.Systems.Enums;

namespace MyriaLib.Entities.Items
{
    public class EquipmentItem : Item
    {
        public EquipmentType SlotType { get; set; }
        public override int BuyPrice { get; set; } = 300;

        /// <summary>Base bonus values as defined in item data (before upgrades).</summary>
        public EquipmentBonuses BaseStats { get; set; }

        /// <summary>Current bonus values (scales with upgrade level).</summary>
        public EquipmentBonuses Bonuses { get; set; }

        public int UpgradeLevel { get; set; } = 0;

        // Convenience properties — delegate to Bonuses struct so existing UI code keeps working
        public int BonusHP      => Bonuses.HP;
        public int BonusMP      => Bonuses.MP;
        public int BonusSTR     => Bonuses.STR;
        public int BonusDEX     => Bonuses.DEX;
        public int BonusEND     => Bonuses.END;
        public int BonusINT     => Bonuses.INT;
        public int BonusSPR     => Bonuses.SPR;
        public int BonusATK     => Bonuses.ATK;
        public int BonusDEF     => Bonuses.DEF;
        public int BonusMATK    => Bonuses.MATK;
        public int BonusMDEF    => Bonuses.MDEF;
        public int BonusAim     => Bonuses.Aim;
        public int BonusEvasion => Bonuses.Evasion;
        public float BonusCrit  => Bonuses.Crit;
        public float BonusBlock => Bonuses.Block;

        public bool IsUsableBy(Player player) => AllowedClasses.Contains(player.Class);

        public override void Use(Player player) { }

        /// <summary>
        /// Applies one upgrade level, scaling Bonuses from BaseStats.
        /// Called both from the UI upgrade flow and from save-file reload (ItemConverter).
        /// </summary>
        public bool TryUpgrade(Player player)
        {
            if (UpgradeLevel >= 9) return false;

            string requiredItemId = "upgrade_stone";
            var material = player.Inventory.Items.FirstOrDefault(i => i.Id == requiredItemId);
            if (material == null) return false;

            if (material.StackSize < 2)
                player.Inventory.RemoveItem(material);
            else
                material.StackSize--;

            TryUpgrade_Internal();
            return true;
        }

        /// <summary>
        /// Applies the next upgrade level without consuming materials.
        /// Used by <see cref="TryUpgrade"/> and by save-file deserialization.
        /// </summary>
        public void TryUpgrade_Internal()
        {
            UpgradeLevel++;
            float multiplier = UpgradeLevel < 4  ? 1 + (UpgradeLevel * 0.1f)
                             : UpgradeLevel < 7  ? 1 + (UpgradeLevel * 0.3f)
                                                 : 1 + (UpgradeLevel * 0.7f);
            Bonuses = BaseStats.Scale(multiplier);
        }
    }
}
