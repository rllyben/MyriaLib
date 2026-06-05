using System.Text.Json;
using MyriaLib.Entities.Items;
using MyriaLib.Entities.Jobs;
using MyriaLib.Entities.Players;

namespace MyriaLib.Services.Manager
{
    public static class JobManager
    {
        private static List<Job> _allJobs = new();

        public static void LoadJobs(string path = "Data/common/jobs.json")
        {
            if (!File.Exists(path)) return;
            _allJobs = JsonSerializer.Deserialize<List<Job>>(File.ReadAllText(path)) ?? new();
        }

        public static Job? GetById(string id)
            => _allJobs.FirstOrDefault(j => j.Id == id);

        public static IReadOnlyList<Job> GetAll() => _allJobs;

        public static TimeSpan JobChangeCooldown         { get; set; } = TimeSpan.FromDays(7);
        public static long     FameTickPerDay            { get; set; } = 5;
        public static long     SkillDecayCap             { get; set; } = 50;
        public static long     FameDecayDivisor          { get; set; } = 200;
        public static double   ActiveJobBonusFraction    { get; set; } = 0.5;

        // ── XP grants ────────────────────────────────────────────────────────────

        /// <summary>
        /// Skill XP — earned by performing the job's main task (crafting/gathering).
        /// Grants a 50 % bonus when this is the player's active job.
        /// Also stamps <see cref="PlayerJob.LastSkillUsedDay"/> so Skill decay (J12) is suppressed today.
        /// </summary>
        public static void GrantSkillXp(Player player, string jobId, long amount)
        {
            if (amount <= 0) return;
            if (player.ActiveJobId == jobId)
                amount += (long)(amount * ActiveJobBonusFraction);
            var entry = GetOrAdd(player, jobId);
            entry.SkillXp += amount;
            entry.LastSkillUsedDay = DayCycleManager.GameDay; // J12: mark skill as used today
        }

        /// <summary>Knowledge XP — earned from job-master NPC quests.</summary>
        public static void GrantKnowledgeXp(Player player, string jobId, long amount)
        {
            if (amount <= 0) return;
            GetOrAdd(player, jobId).KnowledgeXp += amount;
        }

        /// <summary>
        /// Fame XP — earned only when this is the player's active job,
        /// by completing job-related tasks while working it.
        /// </summary>
        public static void GrantFameXp(Player player, string jobId, long amount)
        {
            if (amount <= 0 || player.ActiveJobId != jobId) return;
            GetOrAdd(player, jobId).FameXp += amount;
        }

        // ── Active job ───────────────────────────────────────────────────────────

        /// <summary>
        /// Switches the player's active job.
        /// Clearing the job (null) is always free.
        /// Switching to a new non-null job requires <see cref="JobChangeCooldown"/> since the
        /// last switch; returns false and leaves the job unchanged if the cooldown has not elapsed.
        /// </summary>
        public static bool SetActiveJob(Player player, string? jobId)
        {
            if (jobId == player.ActiveJobId) return true;

            if (jobId != null)
            {
                if (!CanChangeJob(player)) return false;
                player.LastJobChanged = DateTime.UtcNow;
            }

            player.ActiveJobId = jobId;
            return true;
        }

        /// <summary>Returns true when the player is allowed to switch to a new job.</summary>
        public static bool CanChangeJob(Player player)
            => GetCooldownRemaining(player) <= TimeSpan.Zero;

        /// <summary>How long until the player may switch jobs again; zero or negative means ready.</summary>
        public static TimeSpan GetCooldownRemaining(Player player)
            => player.LastJobChanged == DateTime.MinValue
                ? TimeSpan.Zero
                : player.LastJobChanged + JobChangeCooldown - DateTime.UtcNow;

        // ── Selling ──────────────────────────────────────────────────────────────

        /// <summary>
        /// Returns the sell value of <paramref name="item"/> for <paramref name="player"/>,
        /// applying the Fame bonus when the player is actively working the item's job.
        /// Equipment items without an explicit JobId fall back to their UpgradeCategory.
        /// </summary>
        public static int GetSellValue(Item item, Player player)
        {
            int baseSell = item.SellValue;

            string? jobId = item.JobId
                ?? (item is EquipmentItem eq ? eq.UpgradeCategory : null);

            if (jobId == null || jobId != player.ActiveJobId)
                return baseSell;

            var entry = GetOrAdd(player, jobId);
            double multiplier = JobXpService.GetFameMultiplierFromXp(entry.FameXp);
            return (int)(baseSell * multiplier);
        }

        /// <summary>
        /// Returns the amount the <paramref name="seller"/> receives from a player-to-player sale
        /// where the buyer pays <paramref name="agreedPrice"/>.
        /// The seller's active-job Fame level is applied on top of the agreed price;
        /// the buyer is never charged more than <paramref name="agreedPrice"/>.
        /// Returns <paramref name="agreedPrice"/> unchanged when the seller has no active job.
        /// </summary>
        public static int GetPlayerSellReceipt(Player seller, int agreedPrice)
        {
            if (agreedPrice <= 0 || seller.ActiveJobId == null) return agreedPrice;
            var entry = GetOrAdd(seller, seller.ActiveJobId);
            double multiplier = JobXpService.GetFameMultiplierFromXp(entry.FameXp);
            return (int)(agreedPrice * multiplier);
        }

        // ── Gather ───────────────────────────────────────────────────────────────

        /// <summary>
        /// Extra daily gather charges for a player, based on their highest gathering-job knowledge level.
        /// </summary>
        public static int GetGatherKnowledgeBonus(Player player)
        {
            int maxLevel = new[] { "miner", "woodcutter", "herbalist" }
                .Select(id => JobXpService.GetLevel(GetOrAdd(player, id).KnowledgeXp))
                .Max();
            return JobXpService.GetGatherLimitBonus(maxLevel);
        }

        /// <summary>
        /// Items gathered per action for the given job, derived from knowledge level.
        /// Uses the same tier thresholds as the daily gather limit bonus (1 base + 0/1/2/3/4 bonus).
        /// </summary>
        public static int GetGatherAmount(Player player, string jobId)
        {
            int skillLevel = JobXpService.GetLevel(GetOrAdd(player, jobId).SkillXp);
            return 1 + JobXpService.GetGatherLimitBonus(skillLevel);
        }

        // ── Daily ticks (J11–J14) ────────────────────────────────────────────────

        /// <summary>
        /// Applies all per-day job progression changes for the current player.
        /// Call once per game-day (subscribe to <see cref="DayCycleManager.DayAdvanced"/>).
        /// </summary>
        public static void ApplyDailyTicks(Player player, int gameDay)
        {
            foreach (var job in player.Jobs)
            {
                bool isActive = job.JobId == player.ActiveJobId;

                // J11: Passive daily Fame tick — active job only, once per game day
                if (isActive && job.LastFameTickDay < gameDay)
                {
                    job.FameXp += FameTickPerDay;
                    job.LastFameTickDay = gameDay;
                }

                // J12: Skill decay — small loss each day the skill was not used
                if (job.LastSkillUsedDay < gameDay && job.SkillXp > 0)
                {
                    int  level = JobXpService.GetLevel(job.SkillXp);
                    long floor = JobXpService.TotalXpToReach(level);
                    long decay = Math.Min(job.SkillXp - floor, SkillDecayCap);
                    job.SkillXp -= decay;
                }

                // J13: Knowledge decay — progress toward next level resets to 0 daily
                // (levels already earned are preserved; only the partial-level progress drains)
                if (job.KnowledgeXp > 0)
                {
                    int  level = JobXpService.GetLevel(job.KnowledgeXp);
                    job.KnowledgeXp = JobXpService.TotalXpToReach(level);
                }

                // J14: Fame decay — XP (and levels) decay when this is not the active job
                if (!isActive && job.FameXp > 0)
                {
                    long decay = Math.Max(1L, job.FameXp / FameDecayDivisor);
                    job.FameXp = Math.Max(0L, job.FameXp - decay);
                }
            }
        }

        // ── Helpers ──────────────────────────────────────────────────────────────

        /// <summary>Returns the player's progress record for a job, creating one if needed.</summary>
        public static PlayerJob GetOrAdd(Player player, string jobId)
        {
            var entry = player.Jobs.FirstOrDefault(j => j.JobId == jobId);
            if (entry != null) return entry;
            entry = new PlayerJob { JobId = jobId };
            player.Jobs.Add(entry);
            return entry;
        }
    }
}
