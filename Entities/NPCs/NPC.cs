using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MyriaLib.Systems.Enums;

namespace MyriaLib.Entities.NPCs
{
    public class Npc
    {
        public string Id { get; init; } = "";
        public string NameKey { get; init; } = "";        // e.g. "game.npc.healer.name"
        public string DescriptionKey { get; init; } = ""; // e.g. "game.npc.healer.desc"

        public NpcType Type { get; init; }                // Healer, Shop, QuestGiver, etc.
        public List<string> Services { get; init; } = new(); // optional: "heal", "shop", "quests"
    }

}
