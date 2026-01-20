using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyriaLib.Systems.Events
{
    public class XpGainedEventArgs : EventArgs
    {
        public long Amount { get; }
        public long TotalXp { get; }
        public long XpToNextLevel { get; }

        public XpGainedEventArgs(long amount, long totalXp, long xpToNextLevel)
        {
            Amount = amount;
            TotalXp = totalXp;
            XpToNextLevel = xpToNextLevel;
        }

    }
    
}
