using Myria.Lib.Core.Entities.Items;

namespace Myria.Lib.Core.Models
{
    public class MonsterLootTable
    {
        public string MonsterType { get; set; } = "";
        public List<UniqueLootEntry> Drops { get; set; } = new();
    }
}
