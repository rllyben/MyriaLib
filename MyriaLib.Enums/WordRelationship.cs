namespace MyriaLib.Systems.Enums
{
    /// <summary>
    /// The semantic relationship between two runic words (or between two word families).
    /// Determines how combining words affects the rune's power and cost.
    /// </summary>
    public enum WordRelationship
    {
        /// <summary>Words reinforce each other — rune power increases.</summary>
        Support,

        /// <summary>Words oppose each other — rune raw strength increases at the cost of instability.</summary>
        Contradiction,

        /// <summary>Words are unrelated — no power change, but the rune costs more MP.</summary>
        Neutral,

        /// <summary>
        /// The combination changes the fundamental meaning of the rune, unlocking a new derived rune.
        /// The new rune is added to the player's collection without consuming the original.
        /// </summary>
        Transform
    }
}
