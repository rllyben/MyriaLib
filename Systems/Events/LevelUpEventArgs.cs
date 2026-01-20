using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyriaLib.Systems.Events
{
    public class LevelUpEventArgs : EventArgs
    {
        public int OldLevel { get; }
        public int NewLevel { get; }

        public LevelUpEventArgs(int oldLevel, int newLevel)
        {
            OldLevel = oldLevel;
            NewLevel = newLevel;
        }

    }

}
