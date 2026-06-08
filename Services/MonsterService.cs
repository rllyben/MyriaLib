using System.Text.Json;
using MyriaLib.Entities.Monsters;
using MyriaLib.Systems;

namespace MyriaLib.Services
{
    public static class MonsterService
    {
        private static readonly string _defaultPath = "Data/common/monsters.json";
        private static List<Monster> _monsterList = new List<Monster>();
        public static List<Monster> LoadMonsters(string path = "")
        {
            string filePath = string.IsNullOrEmpty(path) ? _defaultPath : path;
            if (!File.Exists(filePath))
                return new List<Monster>();

            string json = File.ReadAllText(filePath);
            _monsterList = JsonSerializer.Deserialize<List<Monster>>(json) ?? new();
            return _monsterList;
        }

        public static void SaveMonsters(List<Monster> monsters)
        {
            string json = JsonSerializer.Serialize(monsters, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_defaultPath, json);
        }
        public static Monster? GetMonsterById(int id)
        {
            var monster = _monsterList.FirstOrDefault(m => m.Id == id);
            if (monster == null)
                GameLog.Error($"Monster ID {id} not found in loaded data.");
            return monster;
        }
        public static Monster PickMonsterForFight(List<Monster> monsters, Dictionary<int, float> chances)
        {
            var eligible = monsters.Where(m => chances.ContainsKey(m.Id)).ToList();
            if (eligible.Count == 0)
                return monsters[Random.Shared.Next(monsters.Count)];

            float totalWeight = eligible.Sum(m => chances[m.Id]);
            float roll = Random.Shared.NextSingle() * totalWeight;

            float cumulative = 0;
            foreach (Monster monster in eligible)
            {
                cumulative += chances[monster.Id];
                if (roll < cumulative)
                    return monster;
            }
            return eligible[^1];
        }

        /// <summary>
        /// Picks up to <paramref name="max"/> monsters for a group fight.
        /// Dungeon rooms use their spawned <paramref name="currentMonsters"/> instances (capped at max).
        /// Overworld rooms always fill up to max slots by weighted-random selection from templates.
        /// </summary>
        public static List<Monster> PickMonstersForGroupFight(
            List<Monster> templates,
            Dictionary<int, float> chances,
            List<Monster> currentMonsters,
            bool isDungeonRoom,
            int max = 5)
        {
            if (isDungeonRoom && currentMonsters.Count > 0)
                return currentMonsters.Take(max).ToList();

            var result = new List<Monster>();
            for (int i = 0; i < max; i++)
                result.Add(PickMonsterForFight(templates, chances).Clone());
            return result;
        }

    }

}


