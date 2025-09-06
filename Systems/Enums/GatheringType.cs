using System.Text.Json.Serialization;

namespace MyriaLib.Systems.Enums
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum GatheringType
    {
        Ore,
        Tree,
        Herb
    }

}
