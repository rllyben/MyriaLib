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
            List<Item> loot = new List<Item>();
            switch (monsterType)
            {
                case MonsterType.Spirit:
                    if (_rand.NextDouble() < 0.7)
                    {
                        Item drop = ItemFactory.CreateItem("spirit_dust");
                        drop.StackSize++;
                        loot.Add(drop);
                    }  
                    break;
                case MonsterType.Shadow:
                    if (_rand.NextDouble() < 0.7)
                    {
                        Item drop = ItemFactory.CreateItem("shadow_remnant");
                        drop.StackSize++;
                        loot.Add(drop);
                    }
                    break;
                case MonsterType.Elemental:
                    if (_rand.NextDouble() < 0.3)
                    {
                        Item drop = ItemFactory.CreateItem("earth_essence");
                        drop.StackSize++;
                        loot.Add(drop);
                    }
                    if (_rand.NextDouble() < 0.3)
                    {
                        Item drop = ItemFactory.CreateItem("stone_fragment");
                        drop.StackSize++;
                        loot.Add(drop);
                    }
                    if (_rand.NextDouble() < 0.3)
                    {
                        Item drop = ItemFactory.CreateItem("fire_ash");
                        drop.StackSize++;
                        loot.Add(drop);
                    }
                    if (_rand.NextDouble() < 0.3)
                    {
                        Item drop = ItemFactory.CreateItem("wind_whisper");
                        drop.StackSize++;
                        loot.Add(drop);
                    }
                    if (_rand.NextDouble() < 0.3)
                    {
                        Item drop = ItemFactory.CreateItem("water_bead");
                        drop.StackSize++;
                        loot.Add(drop);
                    }
                    break;
                case MonsterType.Beast:
                    if (_rand.NextDouble() < 0.6)
                    {
                        Item drop = ItemFactory.CreateItem("beast_flesh");
                        drop.StackSize++;
                        loot.Add(drop);
                    }
                    if (_rand.NextDouble() < 0.4)
                    {
                        Item drop = ItemFactory.CreateItem("feral_leather");
                        drop.StackSize++;
                        loot.Add(drop);
                    }
                    if (_rand.NextDouble() < 0.2)
                    {
                        Item drop = ItemFactory.CreateItem("beast_fang");
                        drop.StackSize++;
                        loot.Add(drop);
                    }
                    break;
                case MonsterType.Humanoid:
                    if (_rand.NextDouble() < 0.7)
                    {
                        Item drop = ItemFactory.CreateItem("beast_flesh");
                        drop.StackSize++;
                        loot.Add(drop);
                    }
                    break;

            }
            return loot;
        }

    }

}
