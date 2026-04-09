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

            string id = idProp.GetString()!;

            int stackSize = root.TryGetProperty("StackSize", out var stackProp)
                ? stackProp.GetInt32()
                : 1;

            var item = ItemFactory.CreateItem(id, stackSize);
            if (item == null)
                throw new JsonException($"Save data references item '{id}' which no longer exists in item definitions.");

            // Re-apply upgrade level so upgraded gear isn't reset on load
            if (item is EquipmentItem equipment
                && root.TryGetProperty("UpgradeLevel", out var upgradeProp))
            {
                int savedLevel = upgradeProp.GetInt32();
                for (int i = 0; i < savedLevel; i++)
                    equipment.TryUpgrade_Internal();
            }

            return item;
        }

        public override void Write(Utf8JsonWriter writer, Item value, JsonSerializerOptions options)
        {
            JsonSerializer.Serialize(writer, (object)value, value.GetType(), options);
        }
    }
}
