using Myria.Lib.Core.Entities.Maps;
using Myria.Lib.Core.Entities.NPCs;
using Myria.Lib.Core.Entities.Characters;

namespace Myria.Lib.Core.Systems.Interfaces
{
    public interface INpcServiceHandler
    {
        string ServiceId { get; }
        bool CanUse(Character character, Npc npc, Room room);
        NpcActionResult Execute(Character character, Npc npc, Room room, object? args = null);
    }

}
