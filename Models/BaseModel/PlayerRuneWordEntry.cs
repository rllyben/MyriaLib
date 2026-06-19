namespace MyriaLib.Models.BaseModel
{
    /// <summary>
    /// Per-character discovery state for a single runic word.
    /// Saved as part of the character's file.
    /// </summary>
    public class CharacterRuneWordEntry
    {
        /// <summary>References <see cref="RuneWord.Id"/>.</summary>
        public string WordId { get; set; } = "";

        /// <summary>
        /// The player's own translation note — written manually through the dictionary interface.
        /// Shown in brackets alongside or instead of the runic script.
        /// Does not affect game logic; the official word relationships always apply.
        /// </summary>
        public string? CharacterLabel { get; set; }

        /// <summary>
        /// True when the player has learned this word through in-game events (NPC dialogue, lore items, etc.).
        /// When true, the official English name is shown instead of the runic script.
        /// </summary>
        public bool IsOfficiallyLearned { get; set; } = false;
    }
}
