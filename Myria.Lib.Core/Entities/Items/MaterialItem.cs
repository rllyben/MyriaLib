using Myria.Lib.Core.Entities.Characters;

namespace Myria.Lib.Core.Entities.Items
{
    public class MaterialItem : Item
    {
        public override void Use(Character character) { }
        public override int MaxStackSize => 99;
    }
}
