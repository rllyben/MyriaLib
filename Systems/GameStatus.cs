namespace MyriaLib.Systems
{
    [Serializable]
    public class GameStatus
    {
        public DateTime LastUpdateTime { get; set; }
        public Dictionary<string, DateTime> RoomGatheringStatus { get; set; } = new();
    }

}
