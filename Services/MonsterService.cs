﻿using System.Text.Json;
using MyriaLib.Entities.Monsters;

namespace MyriaLib.Services
{
    public static class MonsterService
    {
        private static readonly string _filePath = "Data/monsters.json";

        public static List<Monster> LoadMonsters()
        {
            if (!File.Exists(_filePath))
                return new List<Monster>();

            string json = File.ReadAllText(_filePath);
            return JsonSerializer.Deserialize<List<Monster>>(json) ?? new();
        }

        public static void SaveMonsters(List<Monster> monsters)
        {
            string json = JsonSerializer.Serialize(monsters, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_filePath, json);
        }

    }

}


