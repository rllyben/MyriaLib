using System.Text.Json;
using MyriaLib.Entities.Monsters;
using MyriaLib.Systems;

namespace MyriaLib.Services
{
    public static class MonsterService
    {
        private static readonly string _filePath = "Data/common/monsters.json";
        private static List<Monster> _monsterList = new List<Monster>();
        public static List<Monster> LoadMonsters()
        {
            if (!File.Exists(_filePath))
                return new List<Monster>();

            string json = File.ReadAllText(_filePath);
            _monsterList = JsonSerializer.Deserialize<List<Monster>>(json) ?? new();
            return _monsterList;
        }

        public static void SaveMonsters(List<Monster> monsters)
        {
            string json = JsonSerializer.Serialize(monsters, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_filePath, json);
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
            // Only consider monsters that have a defined encounter weight
            var eligible = monsters.Where(m => chances.ContainsKey(m.Id)).ToList();
            if (eligible.Count == 0)
                return monsters[0];

            Random rnd = new Random();
            float totalWeight = eligible.Sum(m => chances[m.Id]);
            float roll = rnd.NextSingle() * totalWeight;

            float cumulative = 0;
            foreach (Monster monster in eligible)
            {
                cumulative += chances[monster.Id];
                if (roll < cumulative)
                    return monster;
            }
            return eligible[^1];
        }

    }

}


