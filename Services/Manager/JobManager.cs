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

        public static readonly TimeSpan JobChangeCooldown = TimeSpan.FromDays(7);

        // ── XP grants ────────────────────────────────────────────────────────────

        /// <summary>
        /// Skill XP — earned by performing the job's main task (crafting/gathering).
        /// Grants a 50 % bonus when this is the player's active job.
        /// </summary>
        public static void GrantSkillXp(Player player, string jobId, long amount)
        {
            if (amount <= 0) return;
            if (player.ActiveJobId == jobId)
                amount += amount / 2;   // +50 % active-job bonus
            GetOrAdd(player, jobId).SkillXp += amount;
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
