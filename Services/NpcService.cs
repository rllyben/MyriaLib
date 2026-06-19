using MyriaLib.Entities.Maps;
using MyriaLib.Entities.NPCs;
using MyriaLib.Services.Builder;
using MyriaLib.Systems.Mods;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MyriaLib.Services
{
    public static class NpcService
    {
        private static readonly Dictionary<string, Npc> _npcs = new(StringComparer.OrdinalIgnoreCase);
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
        public static List<Npc> LoadNpcs(string path = "Data/common/npcs.json")
        {
            if (!File.Exists(path))
            {
                AllNpcs = new();
                _npcs.Clear();
                return AllNpcs;
            }

            string json = File.ReadAllText(path);

            JsonSerializerOptions options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
            };

            AllNpcs = JsonSerializer.Deserialize<List<Npc>>(json, options) ?? new();
            ResolveItemRefs(AllNpcs);

            _npcs.Clear();
            foreach (Npc npc in AllNpcs)
            {
                if (!string.IsNullOrWhiteSpace(npc.Id))
                    _npcs[npc.Id] = npc;
            }

            return AllNpcs;
        }

        private static void ResolveItemRefs(List<Npc> npcs)
        {
            foreach (var npc in npcs)
            {
                npc.ItemRefs.Clear();
                foreach (var itemId in npc.ItemNames)
                {
                    var item = ItemFactory.CreateItem(itemId);
                    if (item != null)
                        npc.ItemRefs.Add(item);
                }
            }
        }

        public static void ConnectNpcRooms(List<Npc> npcs, List<Room> rooms)
        {
            foreach (Room room in rooms)
            {
                foreach (string npcid in room.Npcs)
                {
                    var npc = npcs.FirstOrDefault(n =>
                        string.Equals(n.Id, npcid, StringComparison.OrdinalIgnoreCase));

                    if (npc != null)
                        room.NpcRefs.Add(npc);
                }
            }
        }
    }
}
