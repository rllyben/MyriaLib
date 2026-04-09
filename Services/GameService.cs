using MyriaLib.Entities.Maps;
using MyriaLib.Entities.Monsters;
using MyriaLib.Entities.Players;
using MyriaLib.Services.Builder;
using MyriaLib.Services.Manager;
using MyriaLib.Services.Regestries;
using MyriaLib.Systems;
using MyriaLib.Services;

namespace MyriaLib.Services
{
    public static class GameService
    {
        /// <summary>Shared game state (day, time, ticks). Loaded from disk by InitializeGame.</summary>
        public static GameStatus Game { get; private set; } = new();

        /// <summary>Room lookup by ID. Populated by InitializeGame.</summary>
        public static IReadOnlyDictionary<int, Room> Rooms => _rooms;
        private static Dictionary<int, Room> _rooms = new();
        private static List<Monster> _monsters = new();

        // ── Events ───────────────────────────────────────────────────────────────

        /// <summary>
        /// Fires when a player session becomes active (after <see cref="StartSession"/> is called).
        /// Apps subscribe here to perform per-session setup — for example:
        /// <list type="bullet">
        ///   <item>WPF / desktop: call <see cref="DayCycleManager.StartInactivityTimer"/></item>
        ///   <item>Unity: configure <see cref="GameTick"/> values and start a custom timer</item>
        ///   <item>Console: no action needed — ticks advance on player actions only</item>
        /// </list>
        /// On a future server, call <see cref="StartSession"/> once per connecting player.
        /// </summary>
        public static event Action<Player>? SessionStarted;

        // ── Initialization (shared world data — call once at app / server start) ─

        /// <summary>Loads all shared game data. Safe to call before any player logs in.</summary>
        public static bool InitializeGame() => InitializeGame(null);

        /// <summary>
        /// Loads all shared game data and initialises game systems.
        /// <para>
        /// This is the "server start" phase. It does not start per-session features such as
        /// the inactivity timer — call <see cref="StartSession"/> after loading a character.
        /// </para>
        /// </summary>
        public static bool InitializeGame(IProgress<string>? progress)
        {
            void Report(string step) => progress?.Report(step);

            Game = GameStatusService.Load();
            Report("game_status");

            ItemFactory.LoadItems();
            Report("items");

            _monsters = MonsterService.LoadMonsters();
            Report("monsters");

            NpcService.LoadNpcs();
            Report("npcs");

            _rooms = RoomService.LoadRooms();
            if (_rooms.Count == 0)
                throw new Exception("Failed to load rooms — check rooms.json for syntax errors.");
            Report("rooms");

            RoomService.ConnectMonsterRooms(_monsters, RoomService.AllRooms);
            NpcService.ConnectNpcRooms(NpcService.AllNpcs, RoomService.AllRooms);
            Report("connections");

            // Restores saved day/time/tick state and rolls daily gather limits.
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

            // ── New skill systems (WPF / Unity only — console ignores these) ──────
            RuneWordService.Load();
            BaseRuneService.Load();
            BaseSkillLoader.Load();
            FusionRecipeService.Load();
            Report("skill_systems");

            return true;
        }

        // ── Session start (per player / per client connection) ───────────────────

        /// <summary>
        /// Call this once a player character has been loaded and is ready to play.
        /// Fires <see cref="SessionStarted"/> so apps and systems can perform
        /// per-session setup (inactivity timer, UI bindings, etc.).
        /// </summary>
        public static void StartSession(Player player)
        {
            SessionStarted?.Invoke(player);
        }
    }
}
