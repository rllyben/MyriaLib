using MyriaLib.Entities.Items;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyriaLib.Systems.Events
{
    public class ItemReceivedEventArgs : EventArgs
    {
        public Item Item { get; }
        public int Amount { get; }
        public string? Source { get; }  // "combat", "quest", "shop", etc. optional

        public ItemReceivedEventArgs(Item item, int amount, string? source = null)
        {
            Item = item ?? throw new ArgumentNullException(nameof(item));
            Amount = amount;
            Source = source;
        }

    }

}
