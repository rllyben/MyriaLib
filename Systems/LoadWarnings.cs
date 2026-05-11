namespace MyriaLib.Systems
{
    /// <summary>
    /// Collects item IDs that were skipped during character deserialization because
    /// they no longer exist in the current item definitions.
    /// Thread-static so parallel loads don't interfere.
    /// </summary>
    public static class LoadWarnings
    {
        [ThreadStatic]
        private static List<string>? _tracked;

        internal static void Track(string itemId)
        {
            if (_tracked == null) _tracked = new List<string>();
            _tracked.Add(itemId);
        }

        /// <summary>
        /// Returns all item IDs tracked since the last Consume() call and clears the tracker.
        /// Call this immediately after a deserialization operation.
        /// </summary>
        public static IReadOnlyList<string> Consume()
        {
            var result = _tracked != null
                ? (IReadOnlyList<string>)_tracked.AsReadOnly()
                : Array.Empty<string>();
            _tracked = null;
            return result;
        }
    }
}
