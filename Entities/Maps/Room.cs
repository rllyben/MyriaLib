using MyriaLib.Entities.Monsters;
using MyriaLib.Systems.Enums;

namespace MyriaLib.Entities.Maps
{
    public class Room
    {
        public int Id { get; set; } = 0;
        public int DungonId { get; set; } = -1;
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public bool HasMonsters { get; set; }
        public RoomRequirementType RequirementType { get; set; } = RoomRequirementType.None;
        public int AccessLevel { get; set; } = 0;
        public string RequiredQuestId { get; set; } = null!;
        public bool RequiresParty { get; set; } = false;
        public bool IsCity { get; set; } = false; 
        public bool IsCaveRoom { get; set; } = false;
        public string? CaveId { get; set; } = null;
        public bool IsDungeonRoom { get; set; } = false;
        public bool IsBossRoom { get; set; } = false;
        public bool IsCleared { get; set; } = false; // unlocked once monsters defeated
        public int DailyGatherLimit { get; set; } = 0;
        public int GathersRemaining { get; set; } = 0;
        public List<GatheringSpot> GatheringSpots { get; set; } = new();
        public List<string> Npcs { get; set; } = new(); // e.g. ["Healer", "Smith"]
        public Dictionary<string, int> ExitIds { get; set; } = new(); // "north": "2"
        public Dictionary<string, Room> Exits { get; set; } = new();     // populated after loading
        public List<Room> DungonList { get; set; } = new();
        public Dictionary<int, float> EncounterableMonsters { get; set; } = new();
        public List<Monster> Monsters { get; set; } = new List<Monster>(); 
        public List<Monster> CurrentMonsters { get; set; } = new();
        public List<Corpse> Corpses { get; set; } = new();

        public Room(string name, string description)
        {
            Name = name;
            Description = description;
        }

        public void ConnectRoom(string direction, Room room)
        {
            Exits[direction] = room;
        }
        /// <summary>
        /// rolles the dayly gather limit for the room
        /// </summary>
        public void RollGatherLimit()
        {
            var rng = new Random();
            DailyGatherLimit = rng.Next(1, 6); // 1 to 5
            GathersRemaining = DailyGatherLimit;
        }

    }

}
