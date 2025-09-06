using MyriaLib.Entities.Players;
using MyriaLib.Entities.Maps;
using MyriaLib.Entities.Monsters;
using MyriaLib.Services.Builder;
using MyriaLib.Services.Manager;
using MyriaLib.Services.Regestries;
using MyriaLib.Systems;

namespace MyriaLib.Services
{
    public class GameService
    {
        public static Dictionary<int, Room> rooms = new();
        private static List<Monster> monster = new();
        public static GameStatus Game = new GameStatus();
        /// <summary>
        /// Loads all game data
        /// </summary>
        /// <returns>if all loadings where successful</returns>
        public static bool InitializeGame(Player player)
        {
            bool success = false;
            try
            { 
                Game = GameStatusService.Load();
                monster = MonsterService.LoadMonsters();
                success = LoadRooms();
                ConnectMonsterRooms();
                DungeonRegistry.Load();
                CaveRegistry.Load();
                CityRegistry.Load();
                ForestRegistry.Load();
                ItemFactory.LoadItems();
                QuestManager.LoadQuests();
                SkillFactory.LoadSkills();
                SettingsService.Load();
                Localization.Load(SettingsService.Current.Language);
                NotifyUser("Day cycle");
                DayCycleManager.Initialize(player);
                DayCycleManager.StartBackgroundLoop(player);
                Console.WriteLine();
            }
            catch (Exception ex)
            {
                success = false;
                Console.WriteLine("Exited with error: ", ex.ToString());
            }

            return success;
        }
        /// <summary>
        /// loads all rooms from RoomService
        /// </summary>
        /// <returns>returns if it was successful</returns>
        private static bool LoadRooms()
        {
            rooms = RoomService.LoadRooms();
            if (rooms.Count == 0)
            {
                Console.WriteLine("No rooms found! Exiting...");
                return false;
            }
            return true;
        }
        /// <summary>
        /// connects monsters to their saved rooms
        /// </summary>
        private static void ConnectMonsterRooms()
        {
            foreach (Monster mob in monster)
            {
                var selectedRooms = rooms.Values.Where(r => r.HasMonsters && r.EncounterableMonsters.ContainsKey(mob.Id));
                foreach (Room room in selectedRooms)
                {
                    room.Monsters.Add(mob);
                }

            }

        }
        /// <summary>
        /// prints an user notification what is loaded currently
        /// </summary>
        /// <param name="status">the info whats loaded</param>
        private static void NotifyUser(string status)
        {
            Console.WriteLine($"Loading {status} ...");
        }

    }

}