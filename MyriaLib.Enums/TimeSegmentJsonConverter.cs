using System.Text.Json;
using System.Text.Json.Serialization;

namespace MyriaLib.Systems.Enums
{
    /// <summary>
    /// Reads GameStatus.TimeOfDay as either the new string ID or the old enum's raw int
    /// (via TimeSegment.FromLegacyInt), so gameStatus.json save files written before the
    /// TimeSegment enum-to-string-constant conversion keep loading. Always writes the string form.
    /// </summary>
    public class TimeSegmentJsonConverter : JsonConverter<string>
    {
        public override string Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Number)
                return TimeSegment.FromLegacyInt.GetValueOrDefault(reader.GetInt32(), TimeSegment.Morning);

            return reader.GetString() ?? TimeSegment.Morning;
        }

        public override void Write(Utf8JsonWriter writer, string value, JsonSerializerOptions options)
            => writer.WriteStringValue(value);
    }
}
