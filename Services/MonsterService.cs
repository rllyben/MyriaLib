using System.Text.Json;
using MyriaLib.Entities.Monsters;

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
        public static Monster GetMonsterById(int id)
        {
            return _monsterList.FirstOrDefault(m => m.Id == id);
        }
        public static Monster PickMonsterForFight(List<Monster> monsters, Dictionary<int, float> chances)
        {
            Random rnd = new Random();

            float totalWeight = monsters.Sum(m => chances[m.Id]);
            float roll = rnd.NextSingle();

            float cumulative = 0;
            foreach (Monster monster in monsters)
            {
                cumulative += chances[monster.Id];
                if (roll < cumulative)
                    return monster;
            }
            return monsters[^1];

        }

    }

}


