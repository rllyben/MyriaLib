using MyriaLib.Entities.Items;
using MyriaLib.Models.BaseModel;

namespace MyriaLib.Models.JsonModel
{
    public class JsonMaterialItem : MaterialItem
    {
        public JsonMaterialItem(GameItem def)
        {
            Id = def.Id;
            Name = def.Name;
            Description = def.Description;
            StackSize = def.StackSize;
            _buyPrice = def.BuyPrice;
            _maxStack = def.MaxStackSize;
        }

        private readonly int _buyPrice;
        private readonly int _maxStack;

        public override int BuyPrice => _buyPrice;
        public override int MaxStackSize => _maxStack;
    }

}
