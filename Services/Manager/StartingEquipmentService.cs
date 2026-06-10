using MyriaLib.Entities.Players;
using MyriaLib.Services.Builder;
using MyriaLib.Systems.Enums;

namespace MyriaLib.Services.Manager
{
    public static class StartingEquipmentService
    {
        private static readonly Dictionary<PlayerClass, string[]> _starterItems = new()
        {
            [PlayerClass.Archer]        = ["leather_bow",       "archer_garb",    "focus_band"],
            [PlayerClass.Hunter]        = ["starter_crossbow",  "hunter_coat",    "tracker_band"],
            [PlayerClass.Knight]        = ["steel_sword",       "starter_armor",  "iron_band"],
            [PlayerClass.Fighter]       = ["starter_gauntlets", "padded_armor",   "fighter_token"],
            [PlayerClass.Barbarian]     = ["woodcutter_axe",    "fur_vest",       "power_charm"],
            [PlayerClass.Cleric]        = ["starter_mace",      "prayer_robes",   "faith_charm"],
            [PlayerClass.Rogue]         = ["starter_dagger",    "rogue_cloak",    "crit_band"],
            [PlayerClass.ElementalMage] = ["flamecaster_staff", "storm_robes",    "crystal_focus_band"],
            [PlayerClass.ArcanMage]     = ["mages_tome",        "mage_robe",      "mana_ring"],
            [PlayerClass.Druid]         = ["starter_branch",    "nature_robe",    "life_leaf"],
            [PlayerClass.SoulsKnight]   = ["souls_blade",       "bone_plate",     "soul_shard"],
            [PlayerClass.RunicMage]     = ["runic_focus",       "runic_vestment", "runic_sigil"],
        };

        public static void GrantStartingEquipment(Player player)
        {
            if (!_starterItems.TryGetValue(player.Class, out var itemIds))
                return;

            foreach (var id in itemIds)
                if (ItemFactory.TryCreateItem(id, out var item) && item != null)
                    player.Inventory.AddItem(item, player);
        }
    }
}
