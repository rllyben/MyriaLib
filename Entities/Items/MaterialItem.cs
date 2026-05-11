using MyriaLib.Entities.Players;

namespace MyriaLib.Entities.Items
{
    public class MaterialItem : Item
    {
        public override void Use(Player player) { }
        public override int MaxStackSize => 99;
    }
}
