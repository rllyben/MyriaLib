using MyriaLib.Entities.Items;
using MyriaLib.Models.BaseModel;

namespace MyriaLib.Models.JsonModel
{
    public class JsonConsumableItem : ConsumableItem
    {
        public JsonConsumableItem(GameItem def)
        {
            Id = def.Id;
            Name = def.Name;
            Description = def.Description;
            HealAmount = def.HealAmount;
            ManaRestore = def.ManaRestore;
            StackSize = def.StackSize;
            _buyPrice = def.BuyPrice;
            _maxStack = def.MaxStackSize;
        }

        private int _buyPrice, _maxStack;
        public override int BuyPrice => _buyPrice;
        public override int MaxStackSize => _maxStack;
    }

}
