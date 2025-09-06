﻿using System.Text.Json;
using MyriaLib.Entities.Items;
using MyriaLib.Entities.Players;
using MyriaLib.Models.BaseModel;
using MyriaLib.Models.JsonModel;
using MyriaLib.Systems.Enums;

namespace MyriaLib.Services.Builder
{
    public static class ItemFactory
    {
        private static Dictionary<string, GameItem> _itemDefs;

        public static void LoadItems(string path = "Data/items.json")
        {
            string json = File.ReadAllText(path);
            var list = JsonSerializer.Deserialize<List<GameItem>>(json);
            _itemDefs = list.ToDictionary(i => i.Id, i => i);
        }

        public static Item CreateItem(string id, int stackSize = 1)
        {
            if (!_itemDefs.TryGetValue(id, out var def))
                throw new Exception($"Item with ID '{id}' not found.");

            SetItemBonuses(def);

            Item item = def.Type switch
            {
                "consumable" => new JsonConsumableItem(def),
                "equipment" => new JsonEquipmentItem(def),
                "material" => new JsonMaterialItem(def),
                _ => throw new Exception($"Unsupported item type: {def.Type}")
            };

            // ✅ Set Rarity here
            if (Enum.TryParse<ItemRarity>(def.Rarity, true, out var parsed))
                item.Rarity = parsed;
            else
                item.Rarity = ItemRarity.Common; // fallback

            if (stackSize == 1)
                item.StackSize = def.StackSize > 0 ? def.StackSize : 1;
            else
                item.StackSize = stackSize;
            return item;
        }
        public static bool TryCreateItem(string id, out Item item)
        {
            if (_itemDefs.TryGetValue(id, out var def))
            {
                item = CreateItem(id);
                return true;
            }
            item = null!;
            return false;
        }
        public static List<Item> GetAllItemsFor(Player player)
        {
            return _itemDefs.Values
                .Where(def => def.Type == "equipment"
                    && (def.AllowedClasses.Count == 0 || def.AllowedClasses.Contains(player.Class.ToString())))
                .Select(def => CreateItem(def.Id))
                .ToList();
        }
        public static List<Item> GetSmithItemsFor(Player player)
        {
            return _itemDefs.Values
                .Where(def => def.Type == "equipment" && def.AllowedClasses.Contains(player.Class.ToString()) && def.Rarity == "Common")
                .Select(def => CreateItem(def.Id))
                .ToList();
        }
        private static void SetItemBonuses(GameItem item)
        {
            item.BonusCrit = item.BaseBonusCrit;
            item.BonusBlock = item.BaseBonusBlock;
            item.BonusSTR = item.BaseBonusSTR;
            item.BonusDEX = item.BaseBonusDEX;
            item.BonusEND = item.BaseBonusEND;
            item.BonusINT = item.BaseBonusINT;
            item.BonusSPR = item.BaseBonusSPR;
            item.BonusHP = item.BaseBonusHP;
            item.BonusMP = item.BaseBonusMP;
            item.BonusAim = item.BaseBonusAim;
            item.BonusEvasion = item.BaseBonusEvasion;
            item.BonusATK = item.BaseBonusATK;
            item.BonusDEF = item.BaseBonusDEF;
            item.BonusMATK = item.BaseBonusMATK;
            item.BonusMDEF = item.BaseBonusMDEF;
        }

    }

}
