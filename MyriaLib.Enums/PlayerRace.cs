namespace MyriaLib.Systems.Enums
{
    /// <summary>
    /// Built-in character race identifiers. String-based like <see cref="CharacterClass"/>
    /// so that mod-added races work without code changes.
    /// </summary>
    public static class CharacterRace
    {
        public const string Rotuka = "Rotuka";
        public const string Iymva  = "Iymva";
        public const string Myralu = "Myralu";
        public const string Zalu   = "Zalu";
        public const string Gavon  = "Gavon";
        public const string Gamato = "Gamato";
        public const string Amato  = "Amato";

        /// <summary>All built-in race IDs.</summary>
        public static readonly IReadOnlyList<string> AllBuiltIn = new[]
        {
            Rotuka, Iymva, Myralu, Zalu, Gavon, Gamato, Amato
        };

        /// <summary>Maps the old enum integer values to string IDs (for save-file migration).</summary>
        public static readonly IReadOnlyDictionary<int, string> FromLegacyInt =
            new Dictionary<int, string>
            {
                [0] = Rotuka,
                [1] = Iymva,
                [2] = Myralu,
                [3] = Zalu,
                [4] = Gavon,
                [5] = Gamato,
                [6] = Amato,
            };
    }
}
