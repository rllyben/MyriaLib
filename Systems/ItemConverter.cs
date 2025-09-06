using System.Text.Json;
using System.Text.Json.Serialization;
using MyriaLib.Entities.Items;
using MyriaLib.Services.Builder;

namespace MyriaLib.Systems
{
    public class ItemConverter : JsonConverter<Item>
    {
        public override Item Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            using var doc = JsonDocument.ParseValue(ref reader);
            var root = doc.RootElement;

            if (!root.TryGetProperty("Id", out var idProp))
                throw new JsonException("Missing 'Id' field in item.");
            int stackSize = 1;
            if (doc.RootElement.TryGetProperty("StackSize", out var stackSizeProp))
                stackSize = stackSizeProp.GetInt32(); // ✅ apply saved value

            string id = idProp.GetString();

            return ItemFactory.CreateItem(id, stackSize);
        }

        public override void Write(Utf8JsonWriter writer, Item value, JsonSerializerOptions options)
        {
            JsonSerializer.Serialize(writer, (object)value, value.GetType(), options);
        }

    }

}
