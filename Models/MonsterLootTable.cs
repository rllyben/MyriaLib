using MyriaLib.Entities.Items;

namespace MyriaLib.Models
{
    public class MonsterLootTable
    {
        public string MonsterType { get; set; } = "";
        public List<UniqueLootEntry> Drops { get; set; } = new();
    }
}
