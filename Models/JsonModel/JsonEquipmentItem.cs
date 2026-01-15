using MyriaLib.Entities.Items;
using MyriaLib.Models.BaseModel;
using MyriaLib.Systems.Enums;

namespace MyriaLib.Models.JsonModel
{
    public class JsonEquipmentItem : EquipmentItem
    {
        public JsonEquipmentItem(GameItem def)
        {
            Id = def.Id;
            Name = def.Name;
            Description = def.Description;
            StackSize = def.StackSize;
            MaxStackSize = def.MaxStackSize;
            BuyPrice = def.BuyPrice;
            SlotType = Enum.Parse<EquipmentType>(def.SlotType.ToString());
            AllowedClasses = def.AllowedClasses?
                .Select(c => Enum.Parse<PlayerClass>(c)).ToList() ?? new();

            BaseBonusHP = def.BaseBonusHP;
            BaseBonusMP = def.BaseBonusMP;
            BaseBonusSTR = def.BaseBonusSTR;
            BaseBonusDEX = def.BaseBonusDEX;
            BaseBonusEND = def.BaseBonusEND;
            BaseBonusINT = def.BaseBonusINT;
            BaseBonusSPR = def.BaseBonusSPR;

            BaseBonusATK = def.BaseBonusATK;
            BaseBonusDEF = def.BaseBonusDEF;
            BaseBonusMATK = def.BaseBonusMATK;
            BaseBonusMDEF = def.BaseBonusMDEF;
            BaseBonusAim = def.BaseBonusAim;
            BaseBonusEvasion = def.BaseBonusEvasion;
            BaseBonusCrit = def.BaseBonusCrit;
            BaseBonusBlock = def.BaseBonusBlock;

            // Core stat bonuses
            BonusHP = def.BonusHP;
            BonusMP = def.BonusMP;
            BonusSTR = def.BonusSTR;
            BonusDEX = def.BonusDEX;
            BonusEND = def.BonusEND;
            BonusINT = def.BonusINT;
            BonusSPR = def.BonusSPR;

            // Derived stat bonuses
            BonusATK = def.BonusATK;
            BonusDEF = def.BonusDEF;
            BonusMATK = def.BonusMATK;
            BonusMDEF = def.BonusMDEF;
            BonusAim = def.BonusAim;
            BonusEvasion = def.BonusEvasion;
            BonusCrit = def.BonusCrit;
            BonusBlock = def.BonusBlock;
        }

    }

}
