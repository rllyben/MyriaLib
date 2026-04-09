using MyriaLib.Systems.Enums;
using MyriaLib.Entities.Items;

namespace MyriaLib.Entities.Monsters
{
    public class Monster : CombatEntity
    {
        public int Id { get; set; }
        public string Description { get; set; }
        public MonsterType Type { get; set; }
        public long Exp { get; set; }
        public int MinLoot { get; set; } = 25;
        public int MaxLoot { get; set; } = 75;
        public List<Item> LootTable { get; set; } = new(); // optional overrides
        public List<UniqueLootEntry> UniqueLootTable { get; set; } = new();
        public bool DropsCorpse =>
            Type != MonsterType.Spirit && Type != MonsterType.Shadow;

        public Monster(int id, string name, Stats stats, string description, long exp)
        {
            Id = id;
            Name = name;
            Stats = stats;
            Description = description;
            CurrentHealth = MaxHealth;
            Exp = exp;
        }

        public void ResetHealth()
        {
            CurrentHealth = MaxHealth;
        }

        /// <summary>
        /// Returns a fresh instance with the same definition but independent health.
        /// Use this when spawning dungeon monsters so the shared template is not mutated.
        /// </summary>
        public Monster Clone() => new Monster(Id, Name, Stats.Clone(), Description, Exp)
        {
            Type             = Type,
            MinLoot          = MinLoot,
            MaxLoot          = MaxLoot,
            LootTable        = new List<Item>(LootTable),
            UniqueLootTable  = new List<UniqueLootEntry>(UniqueLootTable),
        };
    }

}
