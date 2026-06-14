using MyriaLib.Entities.Characters;
using MyriaLib.Services.Builder;
using MyriaLib.Systems.Enums;

namespace MyriaLib.Services.Manager
{
    public static class StartingEquipmentService
    {
        private static readonly Dictionary<CharacterClass, string[]> _starterItems = new()
        {
            [CharacterClass.Archer]        = ["leather_bow",       "archer_garb",    "focus_band"],
            [CharacterClass.Hunter]        = ["starter_crossbow",  "hunter_coat",    "tracker_band"],
            [CharacterClass.Knight]        = ["steel_sword",       "starter_armor",  "iron_band"],
            [CharacterClass.Fighter]       = ["starter_gauntlets", "padded_armor",   "fighter_token"],
            [CharacterClass.Barbarian]     = ["woodcutter_axe",    "fur_vest",       "power_charm"],
            [CharacterClass.Cleric]        = ["starter_mace",      "prayer_robes",   "faith_charm"],
            [CharacterClass.Rogue]         = ["starter_dagger",    "rogue_cloak",    "crit_band"],
            [CharacterClass.ElementalMage] = ["flamecaster_staff", "storm_robes",    "crystal_focus_band"],
            [CharacterClass.ArcanMage]     = ["mages_tome",        "mage_robe",      "mana_ring"],
            [CharacterClass.Druid]         = ["starter_branch",    "nature_robe",    "life_leaf"],
            [CharacterClass.SoulsKnight]   = ["souls_blade",       "bone_plate",     "soul_shard"],
            [CharacterClass.RunicMage]     = ["runic_focus",       "runic_vestment", "runic_sigil"],
        };

        public static void GrantStartingEquipment(Character character)
        {
            if (!_starterItems.TryGetValue(character.Class, out var itemIds))
                return;

            foreach (var id in itemIds)
                if (ItemFactory.TryCreateItem(id, out var item) && item != null)
                    character.Inventory.AddItem(item, character);
        }
    }
}
