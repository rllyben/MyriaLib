using MyriaLib.Entities.Maps;
using MyriaLib.Entities.Monsters;
using MyriaLib.Entities.Players;
using MyriaLib.Systems.Enums;
using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MyriaLib.Services
{
    public static class RoomService
    {
        public static List<Room> AllRooms { get; private set; } = new List<Room>();
        private static readonly string _defaultPath = "Data/common/rooms.json";

        /// <summary>
        /// Loads all rooms from the given path (default: Data/common/rooms.json).
        /// </summary>
        /// <returns>Room dictionary keyed by room ID.</returns>
        public static Dictionary<int, Room> LoadRooms(string path = "")
        {
            string filePath = string.IsNullOrEmpty(path) ? _defaultPath : path;
            string test = Path.Combine(Directory.GetCurrentDirectory(), filePath);
            Debug.WriteLine(Directory.GetCurrentDirectory());
            Debug.WriteLine(filePath);
            Debug.WriteLine(test);

            if (!File.Exists(filePath))
                return new();

            string json = File.ReadAllText(filePath);
            
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
            };

            AllRooms = JsonSerializer.Deserialize<List<Room>>(json, options) ?? new();

            // Create lookup map
            var roomMap = AllRooms.ToDictionary(r => r.Id, r => r);

            // Resolve exits
            foreach (var room in roomMap.Values)
            {
                foreach (var (direction, targetId) in room.ExitIds)
                {
                    if (roomMap.TryGetValue(targetId, out var targetRoom))
                        room.Exits[direction] = targetRoom;
                }
            }

            return roomMap;
        }
        /// <summary>
        /// gives back a room when given its id
        /// </summary>
        /// <param name="id">room id</param>
        /// <returns>room with that id</returns>
        public static Room GetRoomById(int id)
        {
            return AllRooms.FirstOrDefault(r => r.Id == id);
        }
        /// <summary>
        /// connects monsters to their saved rooms
        /// </summary>
        public static void ConnectMonsterRooms(List<Monster> monster, List<Room> rooms)
        {
            foreach (Monster mob in monster)
            {
                foreach (Room room in rooms)
                {
                    if (room.EncounterableMonsters.Keys.Contains(mob.Id))
                        room.Monsters.Add(mob);
                }

            }

        }
        public static bool CanEnterRoom(Room room, Player player)
        {
            if (room.IsDungeonRoom && room.CurrentMonsters.Count > 0)
                return false;

            switch (room.RequirementType)
            {
                case RoomRequirementType.None:
                    return true;
                case RoomRequirementType.Level:
                    return player.Level >= room.AccessLevel;
                case RoomRequirementType.Quest:
                    return !string.IsNullOrEmpty(room.RequiredQuestId)
                        && player.CompletedQuests.Any(q => q.Id == room.RequiredQuestId);
                case RoomRequirementType.Party:
                    // TODO: enforce party check once multiplayer is implemented.
                    // In singleplayer there are no parties, so this gate is a no-op for now.
                    return true;
            }
            return true;
        }

    }

}