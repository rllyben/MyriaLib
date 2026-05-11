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
        public string? UpgradeCategory { get; set; }

        /// <summary>
        /// Quality multiplier baked in at craft/upgrade time from the blacksmith's skill level.
        /// Applied on top of the upgrade scaling so higher-skill work produces permanently stronger items.
        /// Default 1.0 for items that were not player-crafted (shops, loot).
        /// </summary>
        public float CraftQuality { get; set; } = 1f;

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
            Bonuses = BaseStats.Scale(multiplier * CraftQuality);
        }
    }
}
