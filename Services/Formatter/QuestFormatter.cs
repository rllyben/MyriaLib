using MyriaLib.Entities.Items;
using MyriaLib.Entities.Monsters;
using MyriaLib.Entities.NPCs;
using MyriaLib.Services.Builder;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

            // If a quest has neither (rare, but possible), show something friendly.
            return parts.Count == 0 ? "No objectives." : string.Join("\n", parts);
        }

        public static string BuildItemsProgressLine(Quest quest)
        {
            if (quest.RequiredItems == null || quest.RequiredItems.Count == 0)
                return string.Empty;

            // Format: Items: itemA 2/5, itemB 1/3
            var segments = quest.RequiredItems.Select(req =>
            {
                var itemId = req.Key;
                var required = req.Value;

                quest.ItemProgress.TryGetValue(itemId, out var current);
                current = Math.Min(current, required);

                return $"{ItemFactory.CreateItem(itemId).Name} {current}/{required}";
            });

            return "Items: " + string.Join(", ", segments);
        }

        public static string BuildKillsProgressLine(Quest quest)
        {
            if (quest.RequiredKills == null || quest.RequiredKills.Count == 0)
                return string.Empty;

            // Format: Kills: 7 0/1, 3 4/10
            // (If you later have a MonsterService that maps id->name, swap the id for a name here.)
            var segments = quest.RequiredKills.Select(req =>
            {
                var monsterId = req.Key;
                var required = req.Value;

                quest.KillProgress.TryGetValue(monsterId, out var current);
                current = Math.Min(current, required);

                return $"{MonsterService.GetMonsterById(monsterId).Name} {current}/{required}";
            });

            return "Kills: " + string.Join(", ", segments);
        }
        public static List<string> BuildItemsObjectivesLine(Quest quest)
        {
            List<string> lines = new List<string>();
            foreach (string item in quest.RequiredItems.Keys)
            {
                lines.Add($"{item}: {quest.ItemProgress[item]}/{quest.RequiredItems[item]}");
            }
            return lines;
        }
        public static List<string> BuildKillsObjectiveLine(Quest quest)
        {
            List<string> lines = new List<string>();
            foreach(int mobId in quest.RequiredKills.Keys)
            {
                Monster mob = MonsterService.GetMonsterById(mobId);
                lines.Add($"{mob.Name}: {quest.KillProgress[mobId]}/{quest.RequiredKills[mobId]}");
            }
            return lines;
        }
        public static List<string> BuildRewardsLine(Quest quest)
        {
            List<string> lines = new();
            foreach(string item in quest.RewardItems)
            {
                lines.Add($"{item}");
            }

            string money = MoneyFormatter.FromBronze(quest.RewardGold).ToString();
            lines.Add(money);

            string xp = quest.RewardXp.ToString() + "XP";
            lines.Add(xp);

            return lines;
        }

    }

}
