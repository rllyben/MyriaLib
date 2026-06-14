using MyriaLib.Entities.Characters;

namespace MyriaLib.Entities.Items
{
    public class MaterialItem : Item
    {
        public override void Use(Character character) { }
        public override int MaxStackSize => 99;
    }
}
