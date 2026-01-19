using MyriaLib.Entities.Maps;
using MyriaLib.Entities.NPCs;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MyriaLib.Services
{
    public class NpcService
    {
        private static readonly Dictionary<string, Npc> _npcs = new(StringComparer.OrdinalIgnoreCase);
        private static readonly string _filePath = "Data/common/npcs.json";

        public static List<Npc> AllNpcs { get; private set; } = new();

        public static Npc Get(string id) => _npcs[id];

        public static bool TryGet(string id, out Npc npc)
            => _npcs.TryGetValue(id, out npc);

        public static IEnumerable<Npc> Resolve(IEnumerable<string> ids)
            => ids.Where(id => _npcs.ContainsKey(id)).Select(id => _npcs[id]);

        /// <summary>
        /// Loads all NPCs from Data/common/npcs.json and fills the lookup dictionary.
        /// Call once at program start.
        /// </summary>
        public static List<Npc> LoadNpcs()
        {
            if (!File.Exists(_filePath))
            {
                AllNpcs = new();
                _npcs.Clear();
                return AllNpcs;
            }

            string json = File.ReadAllText(_filePath);

            JsonSerializerOptions options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
            };

            AllNpcs = JsonSerializer.Deserialize<List<Npc>>(json, options) ?? new();

            _npcs.Clear();
            foreach (Npc npc in AllNpcs)
            {
                if (!string.IsNullOrWhiteSpace(npc.Id))
                    _npcs[npc.Id] = npc;
            }

            return AllNpcs;
        }
        public static void ConnectNpcRooms(List<Npc> npcs, List<Room> rooms)
        {
            foreach (Room room in rooms)
            {
                foreach (string npcid in room.Npcs)
                {
                    room.NpcRefs.Add(npcs.Where(n => n.Id == npcid).FirstOrDefault());
                }

            }

        }

    }

}
