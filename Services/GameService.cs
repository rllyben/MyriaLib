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
        public static bool InitializeGame() => InitializeGame(null);

        public static bool InitializeGame(IProgress<string>? progress)
        {
            void Report(string step) => progress?.Report(step);

            Game = GameStatusService.Load();
            Report("game_status");

            ItemFactory.LoadItems();
            Report("items");

            monster = MonsterService.LoadMonsters();
            Report("monsters");

            NpcService.LoadNpcs();
            Report("npcs");

            if (!LoadRooms())
                throw new Exception("Failed to load rooms — check rooms.json for syntax errors.");
            Report("rooms");

            RoomService.ConnectMonsterRooms(monster, RoomService.AllRooms);
            NpcService.ConnectNpcRooms(NpcService.AllNpcs, RoomService.AllRooms);
            Report("connections");

            DayCycleManager.Initialize();
            Report("day_cycle");

            QuestManager.LoadQuests();
            Report("quests");

            SkillFactory.LoadSkills();
            Report("skills");

            DungeonRegistry.Load();
            CaveRegistry.Load();
            CityRegistry.Load();
            ForestRegistry.Load();
            Report("registries");

            return true;
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