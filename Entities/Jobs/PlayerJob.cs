namespace MyriaLib.Entities.Jobs
{
    /// <summary>Tracks a player's progress across the three aspects of a single job.</summary>
    public class PlayerJob
    {
        public string JobId       { get; set; } = "";
        public long   SkillXp     { get; set; }
        public long   KnowledgeXp { get; set; }
        public long   FameXp      { get; set; }
    }
}
