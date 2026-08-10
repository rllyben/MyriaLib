using System.Text.Json;
using System.Text.Json.Serialization;

namespace Myria.Lib.Core.Systems.Enums
{
    /// <summary>
    /// Reads Character.Class as either the new string ID, a raw legacy enum int (e.g. "Class": 0),
    /// or a legacy enum int stored as a string (e.g. "Class": "0") - via CharacterClass.FromLegacyInt -
    /// so save files written before the enum-to-string-constant conversion keep loading.
    /// Always writes the string form.
    /// </summary>
    public class CharacterClassJsonConverter : JsonConverter<string>
    {
        public override string Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Number)
                return CharacterClass.FromLegacyInt.GetValueOrDefault(reader.GetInt32(), CharacterClass.Fighter);

            var value = reader.GetString();
            if (value != null && int.TryParse(value, out var legacyInt))
                return CharacterClass.FromLegacyInt.GetValueOrDefault(legacyInt, CharacterClass.Fighter);

            return value ?? CharacterClass.Fighter;
        }

        public override void Write(Utf8JsonWriter writer, string value, JsonSerializerOptions options)
            => writer.WriteStringValue(value);
    }
}
