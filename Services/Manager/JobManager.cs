using System.Text.Json;
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

        // ── XP grants ────────────────────────────────────────────────────────────

        /// <summary>
        /// Skill XP — earned by performing the job's main task (crafting/gathering)
        /// regardless of whether this is the player's active job.
        /// </summary>
        public static void GrantSkillXp(Player player, string jobId, long amount)
        {
            if (amount <= 0) return;
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

        /// <summary>Sets the player's active job. Pass null to stop working.</summary>
        public static void SetActiveJob(Player player, string? jobId)
            => player.ActiveJobId = jobId;

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
