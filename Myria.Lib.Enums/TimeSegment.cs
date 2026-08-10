namespace Myria.Lib.Core.Systems.Enums
{
    /// <summary>
    /// Built-in time-of-day segment identifiers. Stored and compared as strings — like
    /// <see cref="CharacterClass"/>/<see cref="CharacterRace"/>/<see cref="ItemRarity"/>/
    /// <see cref="GatheringType"/>/<see cref="EquipmentType"/> — so mod-added segments (defined in
    /// Data/common/time_segments.json) work the same way as these built-ins without requiring any
    /// code changes. Previously a closed enum; unlike GatheringType (already string end-to-end),
    /// existing gameStatus.json save files store TimeOfDay as a raw JSON int (e.g. 1) — handled via
    /// <see cref="FromLegacyInt"/> and <see cref="TimeSegmentJsonConverter"/> so old saves keep loading.
    /// </summary>
    public static class TimeSegment
    {
        public const string Morning = "Morning";
        public const string Midday  = "Midday";
        public const string Evening = "Evening";
        public const string Night   = "Night";

        /// <summary>
        /// All built-in segment IDs, in their original enum declaration order — the cycle
        /// <see cref="Myria.Lib.Core.Services.Manager.DayCycleManager"/> advances through, and the
        /// default sort order fallback (see TimeSegmentRegistry.GetOrder) when no
        /// time_segments.json entry overrides it.
        /// </summary>
        public static readonly IReadOnlyList<string> AllBuiltIn = new[]
        {
            Morning, Midday, Evening, Night
        };

        /// <summary>Maps the old enum's ordinal values to the new string IDs, for save files
        /// written before this conversion (gameStatus.json stores TimeOfDay as a raw int).</summary>
        public static readonly IReadOnlyDictionary<int, string> FromLegacyInt = new Dictionary<int, string>
        {
            [0] = Morning,
            [1] = Midday,
            [2] = Evening,
            [3] = Night,
        };
    }
}
