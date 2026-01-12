using MyriaLib.Entities.NPCs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyriaLib.Services
{
    public class NpcService
    {
        private static readonly Dictionary<string, Npc> _npcs = new();

        public static Npc Get(string id) => _npcs[id];

        public static IEnumerable<Npc> Resolve(IEnumerable<string> ids)
            => ids.Where(_npcs.ContainsKey).Select(id => _npcs[id]);
    }

}
