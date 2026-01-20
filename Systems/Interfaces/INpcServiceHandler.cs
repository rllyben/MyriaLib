using MyriaLib.Entities.Maps;
using MyriaLib.Entities.NPCs;
using MyriaLib.Entities.Players;

namespace MyriaLib.Systems.Interfaces
{
    public interface INpcServiceHandler
    {
        string ServiceId { get; }
        bool CanUse(Player player, Npc npc, Room room);
        NpcActionResult Execute(Player player, Npc npc, Room room, object? args = null);
    }

}
