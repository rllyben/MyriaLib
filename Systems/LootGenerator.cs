using MyriaLib.Entities.Monsters;
using MyriaLib.Entities.Items;
using MyriaLib.Systems.Enums;
using MyriaLib.Services.Builder;

namespace MyriaLib.Systems
{
    public static class LootGenerator
    {
        private static Random _rand = new();

        public static List<Item> GetLootFor(Monster monster)
        {
            var loot = new List<Item>();

            if (monster.LootTable.Count > 0)
            {
                loot.AddRange(monster.LootTable); // optional override
                return loot;
            }
            else
            {
                loot.AddRange(GetTypeBasedLoot(monster.Type)); // your existing elemental/beast/spirit logic
            }

            foreach (var entry in monster.UniqueLootTable)
            {
                double rnd = _rand.NextDouble();
                if (rnd <= entry.DropChance)
                {
                    if (ItemFactory.TryCreateItem(entry.ItemId, out var item))
                        loot.Add(item);
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
                    if (_rand.NextDouble() < 0.7) loot.Add(ItemFactory.CreateItem("spirit_dust"));
                    break;
                case MonsterType.Shadow:
                    if (_rand.NextDouble() < 0.7) loot.Add(ItemFactory.CreateItem("shadow_remnant"));
                    break;
                case MonsterType.Elemental:
                    if (_rand.NextDouble() < 0.3) loot.Add(ItemFactory.CreateItem("earth_essence")); // use actual elemental info later
                    if (_rand.NextDouble() < 0.3) loot.Add(ItemFactory.CreateItem("stone_fragment"));
                    if (_rand.NextDouble() < 0.3) loot.Add(ItemFactory.CreateItem("fire_ash"));
                    if (_rand.NextDouble() < 0.3) loot.Add(ItemFactory.CreateItem("wind_whisper"));
                    if (_rand.NextDouble() < 0.3) loot.Add(ItemFactory.CreateItem("water_bead"));
                    break;
                case MonsterType.Beast:
                    if (_rand.NextDouble() < 0.6) loot.Add(ItemFactory.CreateItem("beast_flesh"));
                    if (_rand.NextDouble() < 0.4) loot.Add(ItemFactory.CreateItem("feral_leather"));
                    if (_rand.NextDouble() < 0.2) loot.Add(ItemFactory.CreateItem("beast_fang"));
                    break;
                case MonsterType.Humanoid:
                    if (_rand.NextDouble() < 0.7) loot.Add(ItemFactory.CreateItem("beast_flesh"));
                    break;
                    // more types here...
            }
            return loot;
        }

    }

}
