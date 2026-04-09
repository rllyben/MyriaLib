using MyriaLib.Entities.Players;

namespace MyriaLib.Entities.Items
{
    public class MaterialItem : Item
    {
        public override int BuyPrice { get; set; } = 0;
        public override int MaxStackSize { get; set; } = 99;
        public override void Use(Player player) { }
    }
}
