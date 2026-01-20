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
        public static bool InitializeGame()
        {
            bool success = false;
            try
            { 
                Game = GameStatusService.Load();
                ItemFactory.LoadItems();
                monster = MonsterService.LoadMonsters();
                NpcService.LoadNpcs();
                success = LoadRooms();
                RoomService.ConnectMonsterRooms(monster, RoomService.AllRooms);
                NpcService.ConnectNpcRooms(NpcService.AllNpcs, RoomService.AllRooms);
                DayCycleManager.Initialize();
                DungeonRegistry.Load();
                CaveRegistry.Load();
                CityRegistry.Load();
                ForestRegistry.Load();
                QuestManager.LoadQuests();
                SkillFactory.LoadSkills();
            }
            catch (Exception ex)
            {
                success = false;
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
                return false;
            }
            return true;
        }

    }

}