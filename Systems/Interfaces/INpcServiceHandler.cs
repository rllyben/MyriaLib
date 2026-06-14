using MyriaLib.Entities.Maps;
using MyriaLib.Entities.NPCs;
using MyriaLib.Entities.Characters;

namespace MyriaLib.Systems.Interfaces
{
    public interface INpcServiceHandler
    {
        string ServiceId { get; }
        bool CanUse(Character character, Npc npc, Room room);
        NpcActionResult Execute(Character character, Npc npc, Room room, object? args = null);
    }

}
