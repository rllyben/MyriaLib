using System.Text.Json;
using MyriaLib.Entities.NPCs;
using MyriaLib.Entities.Players;

namespace MyriaLib.Services.Manager
{
    public static class QuestManager
    {
        private static List<Quest> _allQuests = new();

        public static void LoadQuests(string path = "Data/common/quests.json")
        {
            var json = File.ReadAllText(path);
            _allQuests = JsonSerializer.Deserialize<List<Quest>>(json)!;
        }
        public static List<Quest> GetAvailableForPlayer(Player player)
        {
            return _allQuests
                .Where(q => player.Level >= q.RequiredLevel && CanAccept(q, player))
                .ToList();
        }

        private static bool CanAccept(Quest q, Player player)
        {
            // Already accepted and in progress
            if (player.ActiveQuests.Any(aq => aq.Id == q.Id))
                return false;

            // All prerequisite quests must have been completed at least once
            foreach (var prereqId in q.PrerequisiteQuestIds)
            {
                bool done = player.CompletedQuests.Any(cq => cq.Id == prereqId)
                    || (player.RepeatableQuestRecords.TryGetValue(prereqId, out var rec) && rec.TimesCompleted > 0);
                if (!done) return false;
            }

            if (!q.IsRepeatable)
                return !player.CompletedQuests.Any(cq => cq.Id == q.Id);

            // Level cap always enforced on re-accept
            if (q.RepeatMaxLevel > 0 && player.Level > q.RepeatMaxLevel)
                return false;

            player.RepeatableQuestRecords.TryGetValue(q.Id, out var record);

            // Total completion limit
            if (q.RepeatTotalLimit > 0 && record != null && record.TimesCompleted >= q.RepeatTotalLimit)
                return false;

            // Daily limit
            if (q.RepeatDailyLimit > 0 && record != null &&
                record.LastCompletionDate?.Date == DateTime.Today &&
                record.CompletionsToday >= q.RepeatDailyLimit)
                return false;

            return true;
        }

        public static Quest? GetQuestById(string id) =>
            _allQuests.FirstOrDefault(q => q.Id == id);
    }

}
