using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyriaLib.Systems
{
    public sealed class CombatLogEntry
    {
        public string Key { get; }          // localization key OR plain text
        public object[] Args { get; }       // format args
        public CombatLogEntry(string key, params object[] args)
        {
            Key = key; Args = args;
        }

    }

}
