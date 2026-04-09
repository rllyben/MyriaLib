namespace MyriaLib.Models
{
    /// <summary>
    /// A single word in the runic language.
    /// The player sees <see cref="RunicScript"/> until the word is learned or self-translated.
    /// Game logic always uses <see cref="Id"/> and <see cref="FamilyId"/> regardless of player knowledge.
    /// </summary>
    public class RuneWord
    {
        public string Id { get; set; } = "";

        /// <summary>The English meaning — used in code and shown once the player has learned the word.</summary>
        public string EnglishName { get; set; } = "";

        /// <summary>
        /// The visual representation shown to the player before they know the word.
        /// Placeholder until digital rune assets are created.
        /// </summary>
        public string RunicScript { get; set; } = "";

        /// <summary>References <see cref="WordFamily.Id"/>. Determines default relationships.</summary>
        public string FamilyId { get; set; } = "";
    }
}
