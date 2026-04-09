using MyriaLib.Systems.Enums;

namespace MyriaLib.Systems
{
    public class GameStatus
    {
        public int GameDay { get; set; } = 1;
        public TimeSegment TimeOfDay { get; set; } = TimeSegment.Morning;

        /// <summary>Accumulated ticks within the current segment (0 .. TicksPerSegment-1).</summary>
        public int Ticks { get; set; } = 0;

        public DateTime LastSavedAt { get; set; }
    }
}
