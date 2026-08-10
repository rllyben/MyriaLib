using System.Text.Json.Serialization;
using Myria.Lib.Core.Systems.Enums;

namespace Myria.Lib.Core.Systems
{
    public class GameStatus
    {
        public int GameDay { get; set; } = 1;

        [JsonConverter(typeof(TimeSegmentJsonConverter))]
        public string TimeOfDay { get; set; } = TimeSegment.Morning;

        /// <summary>Accumulated ticks within the current segment (0 .. TicksPerSegment-1).</summary>
        public int Ticks { get; set; } = 0;

        public DateTime LastSavedAt { get; set; }
    }
}
