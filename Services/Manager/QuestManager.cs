using System.Text.Json;
using MyriaLib.Entities.NPCs;
using MyriaLib.Entities.Characters;
using MyriaLib.Services;
using MyriaLib.Systems.Enums;

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
        public static List<Quest> GetAvailableForCharacter(Character character, int partySize = 1)
        {
            return _allQuests
                .Where(q => character.Level >= q.RequiredLevel && CanAccept(q, character, partySize))
                .ToList();
        }

        private static bool CanAccept(Quest q, Character character, int partySize = 1)
        {
            // Already accepted and in progress
            if (character.ActiveQuests.Any(aq => aq.Id == q.Id))
                return false;

            // Q10: Party requirement
            if (q.RequiresParty && partySize < 2)
                return false;
            if (q.RequiredPartySize > 0 && partySize < q.RequiredPartySize)
                return false;

            // Q12: Class requirement
            if (!string.IsNullOrEmpty(q.RequiredClass)
                && !q.RequiredClass.Equals(character.Class, StringComparison.OrdinalIgnoreCase))
                return false;

            // Q13: Race requirement
            if (!string.IsNullOrEmpty(q.RequiredRace)
                && !q.RequiredRace.Equals(character.Race, StringComparison.OrdinalIgnoreCase))
                return false;

            // Q11: Job aspect level requirements
            if (!string.IsNullOrEmpty(q.RequiredAspectJobId))
            {
                var entry = JobManager.GetOrAdd(character, q.RequiredAspectJobId);
                if (q.RequiredSkillLevel     > 0 && JobXpService.GetLevel(entry.SkillXp)     < q.RequiredSkillLevel)     return false;
                if (q.RequiredKnowledgeLevel > 0 && JobXpService.GetLevel(entry.KnowledgeXp) < q.RequiredKnowledgeLevel) return false;
                if (q.RequiredFameLevel      > 0 && JobXpService.GetLevel(entry.FameXp)      < q.RequiredFameLevel)      return false;
            }

            // J8: Require a specific active job
            if (!string.IsNullOrEmpty(q.RequiredActiveJobId) && character.ActiveJobId != q.RequiredActiveJobId)
                return false;

            // All prerequisite quests must have been completed at least once
            foreach (var prereqId in q.PrerequisiteQuestIds)
            {
                bool done = character.CompletedQuests.Any(cq => cq.Id == prereqId)
                    || (character.RepeatableQuestRecords.TryGetValue(prereqId, out var rec) && rec.TimesCompleted > 0);
                if (!done) return false;
            }

            if (!q.IsRepeatable)
                return !character.CompletedQuests.Any(cq => cq.Id == q.Id);

            // Level cap always enforced on re-accept
            if (q.RepeatMaxLevel > 0 && character.Level > q.RepeatMaxLevel)
                return false;

            character.RepeatableQuestRecords.TryGetValue(q.Id, out var record);

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

        /// <summary>Quests this NPC can give that the player is currently eligible to accept.</summary>
        public static List<Quest> GetAcceptableForNpc(Character character, string npcId, int partySize = 1)
            => _allQuests
                .Where(q => q.GiverNpcId == npcId && character.Level >= q.RequiredLevel && CanAccept(q, character, partySize))
                .ToList();

        /// <summary>Active completed quests the player can return to this NPC.</summary>
        public static List<Quest> GetReturnableForNpc(Character character, string npcId)
            => character.ActiveQuests
                .Where(q => q.Status == QuestStatus.Completed && q.ReturnNpcIdResolved == npcId)
                .ToList();
    }

}
