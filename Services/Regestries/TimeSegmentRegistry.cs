using System.Text.Json;
using MyriaLib.Models;
using MyriaLib.Systems.Enums;

namespace MyriaLib.Services.Regestries
{
    /// <summary>
    /// Loads time segment definitions from JSON.
    /// Expected format: [{ "id": "Morning", "displayName": "Morning", "order": 0 }, ...]
    /// </summary>
    public static class TimeSegmentRegistry
    {
        private static readonly JsonSerializerOptions _opts = new() { PropertyNameCaseInsensitive = true };
        private static List<EnumDefinition> _definitions = new();

        public static void Load(string path = "Data/common/time_segments.json")
        {
            if (!File.Exists(path)) return;
            _definitions = JsonSerializer.Deserialize<List<EnumDefinition>>(File.ReadAllText(path), _opts) ?? new();
        }

        public static IReadOnlyList<EnumDefinition> All => _definitions;

        public static string GetDisplayName(TimeSegment segment)
            => _definitions.FirstOrDefault(d => d.Id.Equals(segment.ToString(), StringComparison.OrdinalIgnoreCase))
                           ?.DisplayName ?? segment.ToString();
    }
}
