using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyriaLib.Systems.Events
{
    public class HealthChangedEventArgs
    {
        public int OldValue { get; }
        public int NewValue { get; }
        public int Delta => NewValue - OldValue; // negative = damage, positive = heal
        public string? Source { get; }           // "combat", "skill", "potion" etc.

        public HealthChangedEventArgs(int oldValue, int newValue, string? source = null)
        {
            OldValue = oldValue;
            NewValue = newValue;
            Source = source;
        }

    }

}
