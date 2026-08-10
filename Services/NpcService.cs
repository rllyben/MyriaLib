using MyriaLib.Entities.Maps;
using MyriaLib.Entities.NPCs;
using MyriaLib.Services.Builder;
using MyriaLib.Services.Regestries;
using MyriaLib.Systems.Mods;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MyriaLib.Services
{
    public static class NpcService
    {
        // Class-specific equipment vendors a general trader fills in for, when missing from the city.
        private static readonly string[] EquipmentShopNpcIds =
        {
            "smith_default", "leathersmith_default", "tailor_default", "artificer_default", "enchanter_default"
        };

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

            var npcs = JsonSerializer.Deserialize<List<Npc>>(json, options) ?? new();
            return LoadNpcs(npcs);
        }

        /// <summary>
        /// Loads NPCs from already-parsed data (e.g. read from a database) and fills the lookup dictionary.
        /// Requires <see cref="Builder.ItemFactory"/> to already be loaded, since item references are resolved here.
        /// </summary>
        public static List<Npc> LoadNpcs(List<Npc> npcs)
        {
            AllNpcs = npcs;
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

        /// <summary>
        /// Item names a shop NPC actually sells. For a general trader (shop_general, no shop_equipment)
        /// this is its own itemNames plus the itemNames of any class-equipment vendor (smith,
        /// leathersmith, tailor, artificer, enchanter) that has no instance in the same city, so classes
        /// without a matching specialist nearby can still buy their gear. Every other NPC is unaffected.
        /// </summary>
        public static List<string> GetEffectiveShopItemNames(Npc npc, Room? currentRoom)
        {
            bool isGeneralTrader = npc.Services.Contains("shop_general") && !npc.Services.Contains("shop_equipment");
            if (!isGeneralTrader || currentRoom == null)
                return npc.ItemNames;

            IEnumerable<string> npcIdsInCity = CityRegistry.GetCityByRoom(currentRoom) is { } city
                ? city.RoomIds.Select(RoomService.GetRoomById).Where(r => r != null).SelectMany(r => r!.Npcs)
                : currentRoom.Npcs;

            var presentIds = new HashSet<string>(npcIdsInCity, StringComparer.OrdinalIgnoreCase);

            var effective = new List<string>(npc.ItemNames);
            foreach (var shopId in EquipmentShopNpcIds)
            {
                if (presentIds.Contains(shopId)) continue;
                if (TryGet(shopId, out var shopNpc) && shopNpc != null)
                    effective.AddRange(shopNpc.ItemNames);
            }
            return effective.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
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
