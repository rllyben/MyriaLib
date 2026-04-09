using MyriaLib.Entities.Monsters;
using MyriaLib.Entities.NPCs;
using MyriaLib.Services.Builder;
using MyriaLib.Systems;

namespace MyriaLib.Services.Formatter
{
    public class QuestFormatter
    {
        public static string BuildProgressText(Quest quest)
        {
            var parts = new List<string>();

            var itemsLine = BuildItemsProgressLine(quest);
            if (!string.IsNullOrWhiteSpace(itemsLine))
                parts.Add(itemsLine);

            var killsLine = BuildKillsProgressLine(quest);
            if (!string.IsNullOrWhiteSpace(killsLine))
                parts.Add(killsLine);

            return parts.Count == 0 ? "No objectives." : string.Join("\n", parts);
        }

        public static string BuildItemsProgressLine(Quest quest)
        {
            if (quest.RequiredItems == null || quest.RequiredItems.Count == 0)
                return string.Empty;

            var segments = quest.RequiredItems.Select(req =>
            {
                var itemId   = req.Key;
                var required = req.Value;

                quest.ItemProgress.TryGetValue(itemId, out var current);
                current = Math.Min(current, required);

                // GetItemName reads directly from the definition cache — no item allocation.
                string? name = ItemFactory.GetItemName(itemId);
                string displayName;
                if (name != null)
                {
                    displayName = name;
                }
                else
                {
                    // Item ID exists in quest data but not in item definitions — data problem.
                    GameLog.Error($"Quest '{quest.Id}': required item '{itemId}' not found in item data.");
                    displayName = itemId; // fall back to raw ID so the UI still shows something
                }

                return $"{displayName} {current}/{required}";
            });

            return "Items: " + string.Join(", ", segments);
        }

        public static string BuildKillsProgressLine(Quest quest)
        {
            if (quest.RequiredKills == null || quest.RequiredKills.Count == 0)
                return string.Empty;

            var segments = quest.RequiredKills.Select(req =>
            {
                var monsterId = req.Key;
                var required  = req.Value;

                quest.KillProgress.TryGetValue(monsterId, out var current);
                current = Math.Min(current, required);

                var monster = MonsterService.GetMonsterById(monsterId);
                if (monster == null)
                {
                    // GetMonsterById already logged the error; just use a fallback display.
                    GameLog.Error($"Quest '{quest.Id}': required kill monster ID {monsterId} not found in monster data.");
                    return $"[#{monsterId}] {current}/{required}";
                }

                return $"{monster.Name} {current}/{required}";
            });

            return "Kills: " + string.Join(", ", segments);
        }

        public static List<string> BuildItemsObjectivesLine(Quest quest)
        {
            var lines = new List<string>();
            foreach (var (itemId, required) in quest.RequiredItems)
            {
                int current = quest.ItemProgress.GetValueOrDefault(itemId, 0);
                lines.Add($"{itemId}: {current}/{required}");
            }
            return lines;
        }

        public static List<string> BuildKillsObjectiveLine(Quest quest)
        {
            var lines = new List<string>();
            foreach (int mobId in quest.RequiredKills.Keys)
            {
                Monster? mob = MonsterService.GetMonsterById(mobId);
                string mobName = mob?.Name ?? $"[#{mobId}]"; // GetMonsterById already logged if null

                int killProgress = quest.KillProgress.GetValueOrDefault(mobId, 0);
                lines.Add($"{mobName}: {killProgress}/{quest.RequiredKills[mobId]}");
            }
            return lines;
        }

        public static List<string> BuildRewardsLine(Quest quest)
        {
            var lines = new List<string>();
            foreach (string item in quest.RewardItems)
                lines.Add(item);

            lines.Add(MoneyFormatter.FromBronze(quest.RewardGold).ToString());
            lines.Add($"{quest.RewardXp}XP");
            return lines;
        }
    }
}
