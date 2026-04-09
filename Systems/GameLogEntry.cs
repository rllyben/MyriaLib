namespace MyriaLib.Systems
{
    public class GameLogEntry
    {
        public string Message { get; }
        public bool IsError { get; }
        public DateTime Timestamp { get; }

        public GameLogEntry(string message, bool isError)
        {
            Message = message;
            IsError = isError;
            Timestamp = DateTime.Now;
        }
    }
}
