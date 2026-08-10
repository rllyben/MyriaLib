namespace Myria.Lib.Core.Entities.NPCs
{
    /// <summary>
    /// A single line in a quest dialog sequence.
    /// Speaker values: "npc" (quest giver), "player", "npc:&lt;id&gt;" (specific NPC), or a literal display name.
    /// </summary>
    public class DialogLine
    {
        public string Speaker { get; set; } = "";
        public string Text    { get; set; } = "";
    }
}
