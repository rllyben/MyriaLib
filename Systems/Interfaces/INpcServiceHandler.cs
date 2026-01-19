using MyriaLib.Entities.Maps;
using MyriaLib.Entities.NPCs;
using MyriaLib.Entities.Players;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyriaLib.Systems.Interfaces
{
    public interface INpcServiceHandler
    {
        string ServiceId { get; }
        bool CanUse(Player player, Npc npc, Room room);
        NpcActionResult Execute(Player player, Npc npc, Room room, object? args = null);
    }

}
