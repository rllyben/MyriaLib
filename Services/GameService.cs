using MyriaLib.Entities.Maps;
using MyriaLib.Entities.Monsters;
using MyriaLib.Entities.Players;
using MyriaLib.Services.Builder;
using MyriaLib.Services.Manager;
using MyriaLib.Services.Regestries;
using MyriaLib.Systems;
using MyriaLib.Systems.Mods;
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

            RaceProfile.Load(ModLoader.ResolvePath("Data/common/races.json"));
            ClassProfile.Load(ModLoader.ResolvePath("Data/common/classes.json"));
            LootGenerator.Load(ModLoader.ResolvePath("Data/common/loot_tables.json"));
            Report("profiles");

            ItemFactory.LoadItems(ModLoader.ResolvePath("Data/common/items.json"));
            Report("items");

            _monsters = MonsterService.LoadMonsters(ModLoader.ResolvePath("Data/common/monsters.json"));
            Report("monsters");

            NpcService.LoadNpcs(ModLoader.ResolvePath("Data/common/npcs.json"));
            Report("npcs");

            _rooms = RoomService.LoadRooms(ModLoader.ResolvePath("Data/common/rooms.json"));
            if (_rooms.Count == 0)
                throw new Exception("Failed to load rooms — check rooms.json for syntax errors.");
            Report("rooms");

            RoomService.ConnectMonsterRooms(_monsters, RoomService.AllRooms);
            NpcService.ConnectNpcRooms(NpcService.AllNpcs, RoomService.AllRooms);
            Report("connections");

            DayCycleManager.Initialize();
            Report("day_cycle");

            CraftingService.LoadRecipes(ModLoader.ResolvePath("Data/common/recipes.json"));
            Report("recipes");

            JobManager.LoadJobs(ModLoader.ResolvePath("Data/common/jobs.json"));
            Report("jobs");

            QuestManager.LoadQuests(ModLoader.ResolvePath("Data/common/quests.json"));
            Report("quests");

            SkillFactory.LoadSkills(ModLoader.ResolvePath("Data/common/skills.json"));
            Report("skills");

            DungeonRegistry.Load(ModLoader.ResolvePath("Data/common/dungeons.json"));
            CaveRegistry.Load(ModLoader.ResolvePath("Data/common/caves.json"));
            CityRegistry.Load(ModLoader.ResolvePath("Data/common/cities.json"));
            ForestRegistry.Load(ModLoader.ResolvePath("Data/common/forests.json"));
            Report("registries");

            RuneWordService.Load(
                ModLoader.ResolvePath("Data/common/rune_words.json"),
                ModLoader.ResolvePath("Data/common/rune_families.json"),
                ModLoader.ResolvePath("Data/common/rune_word_pairs.json"));
            BaseRuneService.Load(ModLoader.ResolvePath("Data/common/base_runes.json"));
            BaseSkillLoader.Load(ModLoader.ResolvePath("Data/common/base_skills.json"));
            FusionRecipeService.Load(ModLoader.ResolvePath("Data/common/fusion_recipes.json"));
            SkillCombinationService.Load(ModLoader.ResolvePath("Data/common/skill_combinations.json"));
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
