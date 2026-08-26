using System.Text;

namespace Myria.Lib.Core.Utils
{
    /// <summary>
    /// Sanitizes a single untrusted string (a username or character name) for safe use as one
    /// segment of a locally-constructed file name. Server-side validation only enforces a length
    /// cap on these values (MyriaAuthServer's RegisterRequest, MyriaServer's Character.Name) -
    /// nothing stops "..", "/", "\", a drive letter, or glob wildcards from reaching this client.
    /// Strip them here so a value like "..\..\SomeoneElse-Hero" can't make a save-file path
    /// escape Data/users or Data/saves, and so "*" can't turn a Directory.GetFiles prefix filter
    /// into a wildcard that lists every other player's save files.
    /// </summary>
    public static class SafeFileName
    {
        public static string Sanitize(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
                throw new ArgumentException("Name must not be empty.", nameof(raw));

            // Discards any directory/traversal components ("..", "/", "\", a drive letter, ...) -
            // whatever survives is guaranteed to contain no path separator.
            var leaf = Path.GetFileName(raw);

            var invalid = Path.GetInvalidFileNameChars();
            var sb = new StringBuilder(leaf.Length);
            foreach (var c in leaf)
                sb.Append(Array.IndexOf(invalid, c) >= 0 ? '_' : c);

            var result = sb.ToString();
            if (string.IsNullOrWhiteSpace(result) || result is "." or "..")
                throw new ArgumentException($"'{raw}' is not a valid name.", nameof(raw));

            return result;
        }
    }
}
