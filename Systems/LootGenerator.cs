using System.Text.Json;
using MyriaLib.Entities.Items;
using MyriaLib.Entities.Monsters;
using MyriaLib.Models;
using MyriaLib.Services.Builder;
using MyriaLib.Systems.Enums;

namespace MyriaLib.Systems
{
    public static class LootGenerator
    {
        private static Dictionary<MonsterType, List<UniqueLootEntry>> _typeLoot = new();

        /// <summary>
        /// Loads type-based loot tables from JSON. Call once at startup.
        /// Expected format: array of { "monsterType": "Beast", "drops": [{ "itemId": "beast_flesh", "dropChance": 0.6 }] }
        /// </summary>
        public static void Load(string path = "Data/common/loot_tables.json")
        {
            if (!File.Exists(path)) return;
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var tables  = JsonSerializer.Deserialize<List<MonsterLootTable>>(File.ReadAllText(path), options);
            if (tables == null) return;

            _typeLoot.Clear();
            foreach (var table in tables)
            {
                if (Enum.TryParse<MonsterType>(table.MonsterType, true, out var type))
                    _typeLoot[type] = table.Drops;
            }
        }

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
            if (!_typeLoot.TryGetValue(monsterType, out var drops)) return loot;

            foreach (var drop in drops)
                TryDrop(loot, drop.ItemId, drop.DropChance);

            return loot;
        }

        private static void TryDrop(List<Item> loot, string itemId, double chance)
        {
            if (Random.Shared.NextDouble() < chance && ItemFactory.TryCreateItem(itemId, out var item))
                loot.Add(item!);
        }
    }
}
