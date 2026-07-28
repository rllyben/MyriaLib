namespace MyriaLib.Systems
{
    /// <summary>
    /// Default tick costs for player actions.
    /// All values are settable so individual apps can tune the pace or disable
    /// library-side auto-ticking for actions they manage themselves
    /// (e.g. Unity can set <see cref="RoomTraversal"/> to 0 and drive ticks from
    /// real-time movement distance instead).
    /// </summary>
    public static class GameTick
    {
        /// <summary>Moving from one room to an adjacent one. Set to 0 in Unity if using real-time movement.</summary>
        public static int RoomTraversal { get; set; } = 2;

        /// <summary>Completing an NPC interaction (heal, buy, quest hand-in, etc.).</summary>
        public static int NpcInteraction { get; set; } = 5;

        /// <summary>Winning a combat encounter.</summary>
        public static int CombatVictory { get; set; } = 8;

        /// <summary>A single successful gather action.</summary>
        public static int Gather { get; set; } = 6;

        /// <summary>Resting (skips a full segment — equals TicksPerSegment by default).</summary>
        public static int Rest { get; set; } = 50;
    }
}
