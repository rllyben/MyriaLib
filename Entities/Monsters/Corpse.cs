using MyriaLib.Entities.Items;

namespace MyriaLib.Entities.Monsters
{
    public class Corpse
    {
        public string Name { get; set; }
        public List<Item> Loot { get; set; } = new();
        public bool IsLooted { get; set; } = false;

        public Corpse(string name, List<Item> loot)
        {
            Name = name;
            Loot = loot;
        }

    }

}
