using MyriaLib.Entities.Characters;
using MyriaLib.Services;
using MyriaLib.Services.Manager;
using MyriaLib.Services.Regestries;

namespace MyriaLib.Systems
{
    /// <summary>
    /// Central configuration entry point. Call these methods once at startup before any gameplay begins.
    /// </summary>
    public static class GameConfig
    {
        /// <summary>
        /// Sets the currency conversion ratios that define the denomination ladder.
        /// Default: Bronze 1,000 → Silver 1,000 → Gold 100 → Platinum 100 → Crystal.
        /// </summary>
        public static void SetCurrencyRatios(
            long bronzePerSilver,
            long silverPerGold,
            long goldPerPlatinum,
            long platinumPerCrystal)
        {
            Money.BRONZE_PER_SILVER    = bronzePerSilver;
            Money.SILVER_PER_GOLD      = silverPerGold;
            Money.GOLD_PER_PLATINUM    = goldPerPlatinum;
            Money.PLATINUM_PER_CRYSTAL = platinumPerCrystal;
        }

        /// <summary>
        /// Sets the number of item slots per inventory page. Default: 49 (7×7 grid).
        /// </summary>
        public static void SetInventoryPageSize(int pageSize)
        {
            Inventory.PageSize = pageSize;
        }

        /// <summary>
        /// Configures class-level progression. Default: max level 50, XP cost base 5,000 per level.
        /// XP to advance from level n = n × xpCostBase.
        /// </summary>
        public static void SetClassProgression(int maxLevel, long xpCostBase)
        {
            ClassXpService.MaxLevel   = maxLevel;
            ClassXpService.XpCostBase = xpCostBase;
        }

        /// <summary>
        /// Configures job aspect (Skill / Knowledge / Fame) progression.
        /// Default: max level 100, XP cost base 1,000, fame max bonus +150%, skill max bonus +200%.
        /// Fame multiplier scales from 1.0 to (1.0 + maxFameBonus); skill multiplier from 1.0 to (1.0 + maxSkillBonus).
        /// </summary>
        public static void SetJobProgression(
            int maxLevel,
            long xpCostBase,
            double maxFameBonus  = 1.5,
            double maxSkillBonus = 2.0)
        {
            JobXpService.MaxLevel      = maxLevel;
            JobXpService.XpCostBase    = xpCostBase;
            JobXpService.MaxFameBonus  = maxFameBonus;
            JobXpService.MaxSkillBonus = maxSkillBonus;
        }

        /// <summary>
        /// Overrides the knowledge-level thresholds that grant extra daily gather charges.
        /// Pass entries sorted ascending by level. Default: (10,+1), (30,+2), (60,+3), (100,+4).
        /// </summary>
        public static void SetGatherBonusThresholds((int Level, int Bonus)[] thresholds)
        {
            JobXpService.GatherBonusThresholds = thresholds;
        }

        /// <summary>
        /// Overrides the knowledge-level gates that control maximum equipment upgrade levels.
        /// Pass entries sorted ascending by level. Default base: 2. Default gates: (10,4),(30,6),(60,8),(100,10).
        /// </summary>
        public static void SetUpgradeGates(int defaultMax, (int Level, int MaxUpgrade)[] gates)
        {
            JobXpService.DefaultMaxUpgrade = defaultMax;
            JobXpService.UpgradeGates      = gates;
        }

        /// <summary>
        /// Overrides the level breakpoints for combat skill-bar and fusion-skill slot counts
        /// (both share this curve). Pass entries sorted ascending by level. Default: base 1 slot,
        /// +1 at levels 3, 9, 18, 27, 36, 45, 54, 63, 72.
        /// </summary>
        public static void SetSkillSlotBreakpoints((int Level, int Slots)[] breakpoints)
        {
            Character.SkillSlotBreakpoints = breakpoints;
        }

        /// <summary>Sets the cooldown between class changes. Default: 7 days.</summary>
        public static void SetClassCooldown(TimeSpan cooldown)
        {
            ClassManager.ClassChangeCooldown = cooldown;
        }

        /// <summary>Sets the daily inactive-class XP penalty. Default: 500 XP/day.</summary>
        public static void SetClassPenaltyPerDay(long xpPerDay)
        {
            ClassManager.PenaltyPerDay = xpPerDay;
        }

        /// <summary>Sets the cooldown between job changes. Default: 7 days.</summary>
        public static void SetJobCooldown(TimeSpan cooldown)
        {
            JobManager.JobChangeCooldown = cooldown;
        }

        /// <summary>
        /// Configures the per-day job mechanics.
        /// Defaults: fame tick +5 XP, skill decay cap 50 XP, fame decay divisor 200 (~0.5%/day), active job bonus 50%.
        /// </summary>
        public static void SetJobDailyMechanics(
            long fameTickPerDay,
            long skillDecayCap,
            long fameDecayDivisor,
            double activeJobBonusFraction)
        {
            JobManager.FameTickPerDay         = fameTickPerDay;
            JobManager.SkillDecayCap          = skillDecayCap;
            JobManager.FameDecayDivisor       = fameDecayDivisor;
            JobManager.ActiveJobBonusFraction = activeJobBonusFraction;
        }

        // ── Enum / profile loaders ────────────────────────────────────────────────

        /// <summary>Loads race profiles (stat bonuses, growth, forbidden classes) from JSON.</summary>
        public static void LoadRaces(string path = "Data/common/races.json")
            => RaceProfile.Load(path);

        /// <summary>Loads class profiles (stat growth, HP/mana per level) from JSON.</summary>
        public static void LoadClasses(string path = "Data/common/classes.json")
            => ClassProfile.Load(path);

        /// <summary>Loads equipment slot definitions (id, displayName, order) from JSON.</summary>
        public static void LoadEquipmentSlots(string path = "Data/common/equipment_slots.json")
            => EquipmentTypeRegistry.Load(path);

        /// <summary>Loads item rarity definitions (id, displayName, order) from JSON.</summary>
        public static void LoadItemRarities(string path = "Data/common/item_rarities.json")
            => ItemRarityRegistry.Load(path);

        /// <summary>Loads time segment definitions (id, displayName, order) from JSON.</summary>
        public static void LoadTimeSegments(string path = "Data/common/time_segments.json")
            => TimeSegmentRegistry.Load(path);

        /// <summary>Loads gathering type definitions (id, displayName, order) from JSON.</summary>
        public static void LoadGatheringTypes(string path = "Data/common/gathering_types.json")
            => GatheringTypeRegistry.Load(path);
    }
}
