using MyriaLib.Entities.Players;

namespace MyriaLib.Entities.Items
{
    public class MaterialItem : Item
    {
        public override int BuyPrice => 0; // or define some vendor value
        public override void Use(Player player) { }

        public override int MaxStackSize => 99; // or whatever default
    }
}
