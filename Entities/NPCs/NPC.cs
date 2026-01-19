using MyriaLib.Systems;
using MyriaLib.Systems.Enums;

namespace MyriaLib.Entities.NPCs
{
    public class Npc
    {
        public string Id { get; set; } = "";
        public string NameKey { get; set; } = "";        // e.g. "game.npc.healer.name"
        public string DescriptionKey { get; set; } = ""; // e.g. "game.npc.healer.desc"

        public NpcType Type { get; set; }                // Healer, Shop, QuestGiver, etc.
        public List<string> Services { get; set; } = new(); // optional: "heal", "shop", "quests"
        public override string ToString()
        {
            return Localization.T(NameKey);
        }

    }

}
