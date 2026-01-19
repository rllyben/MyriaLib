using MyriaLib.Entities.NPCs;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace MyriaLib.Utils
{
    public class TestingClass
    {
        private static string path = "C:/Users/Benn/Documents/test.json";
        public static void TestSaveNpc()
        {
            Npc npc = new Npc();
            npc.Id = "_test";
            npc.NameKey = "name";
            npc.DescriptionKey = "description";
            npc.Type = Systems.Enums.NpcType.Healer;
            npc.Services = new() { "hi" };

            var json = JsonSerializer.Serialize(npc);
            File.WriteAllText(path, json);
            LoadNpcs();
        }

        public static List<Npc> AllNpcs { get; private set; } = new();

        /// <summary>
        /// Loads all NPCs from Data/common/npcs.json and fills the lookup dictionary.
        /// Call once at program start.
        /// </summary>
        public static List<Npc> LoadNpcs()
        {
            if (!File.Exists(path))
            {
                AllNpcs = new();
                return AllNpcs;
            }

            string json = File.ReadAllText(path);

            JsonSerializerOptions options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
            };

            AllNpcs = JsonSerializer.Deserialize<List<Npc>>(json, options) ?? new();

            return AllNpcs;
        }

    }

}
