namespace MyriaLib.Entities.NPCs
{
    public class RepeatRecord
    {
        public int TimesCompleted { get; set; } = 0;
        public int CompletionsToday { get; set; } = 0;
        public DateTime? LastCompletionDate { get; set; } = null;
    }
}
