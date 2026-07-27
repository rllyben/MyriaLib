using System.Text.Json.Serialization;
using MyriaLib.Entities.Characters;
using MyriaLib.Systems.Enums;
using MyriaLib.Entities.Maps;

namespace MyriaLib.Entities.Items
{
    public abstract class Item
    {
        public List<string> AllowedClasses { get; set; } = new();
        public string Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public virtual string Rarity { get; set; } = ItemRarity.Common;
        [JsonInclude]
        public int StackSize { get; set; } = 1;
        public virtual int MaxStackSize { get; set; } = 50;
        public bool IsTool { get; set; } = false;
        public GatheringType? ToolType { get; set; } = null;

        public virtual int BuyPrice { get; set; } = 100;
        public virtual int SellValue => (int)(BuyPrice * 0.75);

        /// <summary>
        /// The job this item belongs to for Fame sell-bonus purposes.
        /// Equipment items without an explicit JobId fall back to their UpgradeCategory at runtime.
        /// </summary>
        public virtual string? JobId { get; set; }

        public abstract void Use(Character character); // base method for using an item

        public virtual bool CanStackWith(Item other)
        {
            return other != null && Id == other.Id;
        }

        public Item CloneOne()
        {
            var copy = (Item)MemberwiseClone();
            copy.StackSize = 1;
            return copy;
        }

    }

}
