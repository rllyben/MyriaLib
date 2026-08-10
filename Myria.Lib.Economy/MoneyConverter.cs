using System.Text.Json;
using System.Text.Json.Serialization;
using Myria.Lib.Core.Entities.Characters;

namespace Myria.Lib.Core.Systems
{
    public class MoneyConverter : JsonConverter<Money>
    {
        public override Money Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            using var doc = JsonDocument.ParseValue(ref reader);
            var root = doc.RootElement;

            long bronzeTotal = root.TryGetProperty("BronzeTotal", out var prop)
                ? prop.GetInt64()
                : 0;

            return new Money(bronzeTotal);
        }

        public override void Write(Utf8JsonWriter writer, Money value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            writer.WriteNumber("BronzeTotal", value.BronzeTotal);
            writer.WriteEndObject();
        }
    }
}
