using System.Text.Json;
using System.Text.Json.Serialization;

namespace MyriaLib.Systems.Enums
{
    /// <summary>
    /// Reads Character.Race as either the new string ID, a raw legacy enum int (e.g. "Race": 0),
    /// or a legacy enum int stored as a string (e.g. "Race": "0") - via CharacterRace.FromLegacyInt -
    /// so save files written before the enum-to-string-constant conversion keep loading.
    /// Always writes the string form.
    /// </summary>
    public class CharacterRaceJsonConverter : JsonConverter<string>
    {
        public override string Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Number)
                return CharacterRace.FromLegacyInt.GetValueOrDefault(reader.GetInt32(), CharacterRace.Myralu);

            var value = reader.GetString();
            if (value != null && int.TryParse(value, out var legacyInt))
                return CharacterRace.FromLegacyInt.GetValueOrDefault(legacyInt, CharacterRace.Myralu);

            return value ?? CharacterRace.Myralu;
        }

        public override void Write(Utf8JsonWriter writer, string value, JsonSerializerOptions options)
            => writer.WriteStringValue(value);
    }
}
