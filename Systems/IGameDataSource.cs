using MyriaLib.Entities.Characters;
using MyriaLib.Entities.Effects;
using MyriaLib.Entities.Jobs;
using MyriaLib.Entities.Maps;
using MyriaLib.Entities.Monsters;
using MyriaLib.Entities.NPCs;
using MyriaLib.Models;
using MyriaLib.Models.BaseModel;
using MyriaLib.Services;

namespace MyriaLib.Systems
{
    /// <summary>
    /// Supplies all static game content that <see cref="MyriaLib.Services.GameService.InitializeGame(IGameDataSource?, IProgress{string}?, bool)"/>
    /// needs, as plain in-memory data — the same shapes each loader already deserializes JSON into.
    /// <para>
    /// Implementations decide where the data actually comes from (a database, a remote call, etc.).
    /// MyriaLib itself has no persistence-specific knowledge — it only ever sees these plain types.
    /// </para>
    /// </summary>
    public interface IGameDataSource
    {
        List<RaceProfile> GetRaces();
        List<ClassProfile> GetClasses();
        List<MonsterLootTable> GetLootTables();
        List<GameItem> GetItems();
        List<Monster> GetMonsters();
        List<Npc> GetNpcs();
        List<Room> GetRooms();
        Dictionary<string, CraftingRecipe[]> GetRecipes();
        List<Job> GetJobs();
        List<Quest> GetQuests();
        List<SkillData> GetSkills();
        List<EffectDefinition> GetEffects();
        Dictionary<string, string[]> GetStartingItems();
        List<City> GetCities();
        List<Cave> GetCaves();
        List<Dungeon> GetDungeons();
        List<Forest> GetForests();
        (List<RuneWord> Words, List<WordFamily> Families, List<WordPairRelation> Pairs) GetRuneWords();
        List<BaseRuneData> GetBaseRunes();
        List<BaseSkillData> GetBaseSkills();
        List<FusionRecipe> GetFusionRecipes();
        List<SkillCombinationRecipe> GetSkillCombinations();
    }
}
