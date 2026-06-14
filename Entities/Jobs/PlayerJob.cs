namespace MyriaLib.Entities.Jobs
{
    /// <summary>Tracks a character's progress across the three aspects of a single job.</summary>
    public class CharacterJob
    {
        public string JobId       { get; set; } = "";
        public long   SkillXp     { get; set; }
        public long   KnowledgeXp { get; set; }
        public long   FameXp      { get; set; }

        // ── Daily-tick tracking ───────────────────────────────────────────────────
        /// <summary>Game day on which the last passive Fame tick was applied (J11).</summary>
        public int LastFameTickDay  { get; set; } = -1;
        /// <summary>Game day on which the player last used this job's Skill (J12).</summary>
        public int LastSkillUsedDay { get; set; } = -1;
    }
}
