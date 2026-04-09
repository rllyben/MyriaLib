using MyriaLib.Entities.Items;
using MyriaLib.Entities.Monsters;
using MyriaLib.Services.Builder;
using MyriaLib.Systems.Enums;

namespace MyriaLib.Systems
{
    public static class LootGenerator
    {
        // ── Item IDs ─────────────────────────────────────────────────────────────
        private const string SpiritDust     = "spirit_dust";
        private const string ShadowRemnant  = "shadow_remnant";
        private const string EarthEssence   = "earth_essence";
        private const string StoneFragment  = "stone_fragment";
        private const string FireAsh        = "fire_ash";
        private const string WindWhisper    = "wind_whisper";
        private const string WaterBead      = "water_bead";
        private const string BeastFlesh     = "beast_flesh";
        private const string FeralLeather   = "feral_leather";
        private const string BeastFang      = "beast_fang";

        // ─────────────────────────────────────────────────────────────────────────

        public static List<Item> GetLootFor(Monster monster)
        {
            var loot = new List<Item>();

            if (monster.LootTable.Count > 0)
            {
                loot.AddRange(monster.LootTable);
                return loot;
            }

            loot.AddRange(GetTypeBasedLoot(monster.Type));

            foreach (var entry in monster.UniqueLootTable)
            {
                if (Random.Shared.NextDouble() <= entry.DropChance)
                {
                    if (ItemFactory.TryCreateItem(entry.ItemId, out var item))
                        loot.Add(item!);
                }
            }

            return loot;
        }

        private static List<Item> GetTypeBasedLoot(MonsterType monsterType)
        {
            var loot = new List<Item>();

            switch (monsterType)
            {
                case MonsterType.Spirit:
                    TryDrop(loot, SpiritDust, 0.7);
                    break;

                case MonsterType.Shadow:
                    TryDrop(loot, ShadowRemnant, 0.7);
                    break;

                case MonsterType.Elemental:
                    TryDrop(loot, EarthEssence,  0.3);
                    TryDrop(loot, StoneFragment,  0.3);
                    TryDrop(loot, FireAsh,        0.3);
                    TryDrop(loot, WindWhisper,    0.3);
                    TryDrop(loot, WaterBead,      0.3);
                    break;

                case MonsterType.Beast:
                    TryDrop(loot, BeastFlesh,    0.6);
                    TryDrop(loot, FeralLeather,  0.4);
                    TryDrop(loot, BeastFang,     0.2);
                    break;

                case MonsterType.Humanoid:
                    // Placeholder until a proper humanoid drop item exists.
                    TryDrop(loot, BeastFlesh, 0.7);
                    break;
            }

            return loot;
        }

        /// <summary>Rolls a drop chance and adds the item to <paramref name="loot"/> if successful.</summary>
        private static void TryDrop(List<Item> loot, string itemId, double chance)
        {
            if (Random.Shared.NextDouble() < chance && ItemFactory.TryCreateItem(itemId, out var item))
                loot.Add(item!);
        }
    }
}
