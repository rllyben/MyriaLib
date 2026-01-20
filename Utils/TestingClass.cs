using MyriaLib.Entities.NPCs;
using MyriaLib.Models.BaseModel;
using MyriaLib.Services.Builder;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MyriaLib.Utils
{
    public class TestingClass
    {
        private static string path = "C:/Users/Benn/Documents/";
        public static void TestSaveNpc()
        {
            Npc npc = new Npc();
            npc.Id = "_test";
            npc.NameKey = "name";
            npc.DescriptionKey = "description";
            npc.Type = Systems.Enums.NpcType.Healer;
            npc.Services = new() { "hi" };

            var json = JsonSerializer.Serialize(npc);
            File.WriteAllText(path + "test.json", json);
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

            //var list = JsonSerializer.Deserialize<List<Npc>>(json, options) ?? new();
           // AllNpcs = list.ToList();

            return AllNpcs;
        }
        public static void UpdateItems()
        {
            foreach (GameItem item in ItemFactory._itemDefs.Values)
            {
                item.Name = "item." + item.Id;
            }
            JsonSerializerOptions options = new JsonSerializerOptions
            {
                WriteIndented = true
            };
            var json = JsonSerializer.Serialize(ItemFactory._itemDefs.Values, options);
            File.WriteAllText(path + "items.json", json);
        }

    }

}
