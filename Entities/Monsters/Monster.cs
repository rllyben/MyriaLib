﻿using MyriaLib.Systems.Enums;
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
            CurrentHealth = stats.MaxHealth;
            Exp = exp;
        }

        public void ResetHealth()
        {
            CurrentHealth = MaxHealth;
        }

    }

}
