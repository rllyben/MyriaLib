using MyriaLib.Entities.Players;
using MyriaLib.Services.Builder;
using MyriaLib.Systems.Enums;

namespace MyriaLib.Entities.NPCs
{
    public class Quest
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }

        public string GiverNpc { get; set; }
        public int RequiredLevel { get; set; } = 1;
        public QuestStatus Status { get; set; } = QuestStatus.NotStarted;

        public List<string> PrerequisiteQuestIds { get; set; } = new(); // quest IDs that must be completed first

        // Repeatable config (defined in JSON, read-only after load)
        public bool IsRepeatable { get; set; } = false;
        public int RepeatMaxLevel { get; set; } = 0;    // 0 = no cap; always enforced on re-accept
        public int RepeatDailyLimit { get; set; } = 0;  // 0 = no daily limit
        public int RepeatTotalLimit { get; set; } = 0;  // 0 = no total limit
        public Dictionary<int, int> RequiredKills { get; set; } = new(); // monsterId => amount
        public Dictionary<int, int> KillProgress { get; set; } = new();  // monsterId => current count
        public Dictionary<string, int> RequiredItems { get; set; } = new();   // itemId => amount
        public Dictionary<string, int> ItemProgress { get; set; } = new();    // itemId => how many the player has

        public int RewardXp { get; set; }
        public int RewardGold { get; set; }
        public List<string> RewardItems { get; set; } = new();

        /// <summary>Returns a fresh copy of the quest template ready to be accepted.</summary>
        public Quest Clone() => new Quest
        {
            Id = Id,
            Name = Name,
            Description = Description,
            GiverNpc = GiverNpc,
            RequiredLevel = RequiredLevel,
            Status = QuestStatus.NotStarted,
            RequiredKills = new Dictionary<int, int>(RequiredKills),
            KillProgress = new Dictionary<int, int>(),
            RequiredItems = new Dictionary<string, int>(RequiredItems),
            ItemProgress = new Dictionary<string, int>(),
            RewardXp = RewardXp,
            RewardGold = RewardGold,
            RewardItems = new List<string>(RewardItems),
            PrerequisiteQuestIds = new List<string>(PrerequisiteQuestIds),
            IsRepeatable = IsRepeatable,
            RepeatMaxLevel = RepeatMaxLevel,
            RepeatDailyLimit = RepeatDailyLimit,
            RepeatTotalLimit = RepeatTotalLimit
        };

        public void GrantRewards(Player player)
        {
            player.GainXp(RewardXp);

            if (RewardGold > 0)
            {
                player.Money.TryAdd(RewardGold);
            }

            foreach (var itemId in RewardItems)
            {
                if (ItemFactory.TryCreateItem(itemId, out var item))
                {
                    player.Inventory.AddItem(item, player);
                }

            }

        }

    }

}
