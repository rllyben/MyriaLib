using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyriaLib.Systems.Events
{
    public class ManaChangedEventArgs
    {
        public int OldValue { get; }
        public int NewValue { get; }
        public int Delta => NewValue - OldValue; // negative = spent, positive = restored
        public string? Source { get; }

        public ManaChangedEventArgs(int oldValue, int newValue, string? source = null)
        {
            OldValue = oldValue;
            NewValue = newValue;
            Source = source;
        }

    }

}
