namespace MyriaLib.Systems
{
    /// <summary>
    /// Application-wide event log. Records errors and informational messages that are
    /// not part of combat. Subscribe to <see cref="EntryAdded"/> to display entries in the UI.
    /// </summary>
    public static class GameLog
    {
        private const int MaxHistory = 20;
        private static readonly Queue<GameLogEntry> _history = new();
        private static readonly object _lock = new();

        /// <summary>
        /// Fires on any thread when a new entry is added.
        /// UI subscribers must dispatch to the UI thread themselves.
        /// </summary>
        public static event Action<GameLogEntry>? EntryAdded;

        /// <summary>The most recent entries (up to <see cref="MaxHistory"/>), oldest first.</summary>
        public static IReadOnlyList<GameLogEntry> RecentEntries
        {
            get { lock (_lock) { return _history.ToArray(); } }
        }

        /// <summary>Logs an error. Shown in red in the room log panel.</summary>
        public static void Error(string message) =>
            Add(new GameLogEntry(message, isError: true));

        /// <summary>Logs an informational message.</summary>
        public static void Info(string message) =>
            Add(new GameLogEntry(message, isError: false));

        private static void Add(GameLogEntry entry)
        {
            lock (_lock)
            {
                _history.Enqueue(entry);
                if (_history.Count > MaxHistory)
                    _history.Dequeue();
            }
            EntryAdded?.Invoke(entry);
        }
    }
}
