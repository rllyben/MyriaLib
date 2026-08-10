using System.Text.Json;
using System.Text.Json.Serialization;

namespace Myria.Lib.Core.Systems.Enums
{
    /// <summary>
    /// Reads Skill.Target as either the new string ID or a legacy enum int/numeric string
    /// (via SkillTarget.FromLegacyInt), so character saves holding a Skill snapshot from before
    /// the enum-to-string-constant conversion keep loading. Always writes the string form.
    /// </summary>
    public class SkillTargetJsonConverter : JsonConverter<string>
    {
        public override string Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Number)
                return SkillTarget.FromLegacyInt.GetValueOrDefault(reader.GetInt32(), SkillTarget.SingleEnemy);

            var value = reader.GetString();
            if (value != null && int.TryParse(value, out var legacyInt))
                return SkillTarget.FromLegacyInt.GetValueOrDefault(legacyInt, SkillTarget.SingleEnemy);

            return value ?? SkillTarget.SingleEnemy;
        }

        public override void Write(Utf8JsonWriter writer, string value, JsonSerializerOptions options)
            => writer.WriteStringValue(value);
    }
}
