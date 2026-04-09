using System.Text.Json;
using MyriaLib.Entities.Items;
using MyriaLib.Entities.Players;
using MyriaLib.Models.BaseModel;
using MyriaLib.Systems;
using MyriaLib.Systems.Enums;

namespace MyriaLib.Services.Builder
{
    public static class ItemFactory
    {
        private static Dictionary<string, GameItem> _itemDefs = new();

        public static void LoadItems(string path = "Data/common/items.json")
        {
            string json = File.ReadAllText(path);
            var list = JsonSerializer.Deserialize<List<GameItem>>(json);
            _itemDefs = list.ToDictionary(i => i.Id, i => i);
        }

        /// <summary>
        /// Returns the display name for an item ID without allocating an item instance.
        /// Returns <c>null</c> if the ID is not found (no error logged — use for soft checks).
        /// </summary>
        public static string? GetItemName(string id) =>
            _itemDefs.TryGetValue(id, out var def) ? def.Name : null;

        /// <summary>
        /// Creates an item by ID. Returns <c>null</c> and logs an error if the ID is not found.
        /// Use <see cref="TryCreateItem"/> for silent existence checks.
        /// </summary>
        public static Item? CreateItem(string id, int stackSize = 1)
        {
            if (!_itemDefs.TryGetValue(id, out var def))
            {
                GameLog.Error($"Item '{id}' not found in loaded item data.");
                return null;
            }

            Item item = def.Type switch
            {
                "consumable"           => BuildConsumable(def),
                "equipment"            => BuildEquipment(def),
                "material"             => BuildMaterial(def),
                "inventory_expansion"  => BuildInventoryExpansion(def),
                _ => throw new Exception($"Unsupported item type: {def.Type}")
            };

            if (Enum.TryParse<ItemRarity>(def.Rarity, true, out var rarity))
                item.Rarity = rarity;

            item.StackSize = stackSize > 1 ? stackSize : (def.StackSize > 0 ? def.StackSize : 1);
            return item;
        }

        /// <summary>
        /// Tries to create an item by ID. Returns <c>false</c> silently if not found.
        /// Use this when absence is an acceptable outcome (e.g. optional loot).
        /// </summary>
        public static bool TryCreateItem(string id, out Item? item)
        {
            if (!_itemDefs.TryGetValue(id, out var def))
            {
                item = null;
                return false;
            }
            item = CreateItem(id); // def exists, so CreateItem will not return null
            return item != null;
        }

        public static List<Item> GetAllItemsFor(Player player)
        {
            return _itemDefs.Values
                .Where(def => def.Type == "equipment"
                    && (def.AllowedClasses.Count == 0 || def.AllowedClasses.Contains(player.Class.ToString())))
                .Select(def => CreateItem(def.Id)!)   // def.Id is from the dictionary, always found
                .ToList();
        }

        public static List<Item> GetSmithItemsFor(Player player)
        {
            return _itemDefs.Values
                .Where(def => def.Type == "equipment"
                    && def.AllowedClasses.Contains(player.Class.ToString())
                    && def.Rarity == "Common")
                .Select(def => CreateItem(def.Id)!)   // def.Id is from the dictionary, always found
                .ToList();
        }

        private static ConsumableItem BuildConsumable(GameItem def) => new()
        {
            Id          = def.Id,
            Name        = def.Name,
            Description = def.Description,
            BuyPrice    = def.BuyPrice,
            MaxStackSize = def.MaxStackSize,
            HealAmount  = def.HealAmount,
            ManaRestore = def.ManaRestore,
            AllowedClasses = ParseClasses(def.AllowedClasses),
        };

        private static MaterialItem BuildMaterial(GameItem def) => new()
        {
            Id          = def.Id,
            Name        = def.Name,
            Description = def.Description,
            BuyPrice    = def.BuyPrice,
            MaxStackSize = def.MaxStackSize,
            ToolType    = def.ToolType,
            AllowedClasses = ParseClasses(def.AllowedClasses),
        };

        private static EquipmentItem BuildEquipment(GameItem def)
        {
            var baseStats = new EquipmentBonuses
            {
                HP      = def.BaseBonusHP,
                MP      = def.BaseBonusMP,
                STR     = def.BaseBonusSTR,
                DEX     = def.BaseBonusDEX,
                END     = def.BaseBonusEND,
                INT     = def.BaseBonusINT,
                SPR     = def.BaseBonusSPR,
                ATK     = def.BaseBonusATK,
                DEF     = def.BaseBonusDEF,
                MATK    = def.BaseBonusMATK,
                MDEF    = def.BaseBonusMDEF,
                Aim     = def.BaseBonusAim,
                Evasion = def.BaseBonusEvasion,
                Crit    = def.BaseBonusCrit,
                Block   = def.BaseBonusBlock,
            };

            return new EquipmentItem
            {
                Id          = def.Id,
                Name        = def.Name,
                Description = def.Description,
                BuyPrice    = def.BuyPrice,
                SlotType    = def.SlotType,
                ToolType    = def.ToolType,
                AllowedClasses = ParseClasses(def.AllowedClasses),
                BaseStats   = baseStats,
                Bonuses     = baseStats,    // starts equal to base; scales on upgrade
            };
        }

        private static InventoryExpansion BuildInventoryExpansion(GameItem def) => new()
        {
            Id          = def.Id,
            Name        = def.Name,
            Description = def.Description,
            BuyPrice    = def.BuyPrice,
            AllowedClasses = ParseClasses(def.AllowedClasses),
        };

        private static List<PlayerClass> ParseClasses(List<string> classes) =>
            classes?.Select(c => Enum.Parse<PlayerClass>(c)).ToList() ?? new();
    }
}
