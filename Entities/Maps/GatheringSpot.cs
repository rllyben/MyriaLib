using MyriaLib.Systems.Enums;

namespace MyriaLib.Entities.Maps
{
    public class GatheringSpot
    {
        public string Id { get; set; }           // e.g., "iron_vein"
        public string Name { get; set; }         // e.g., "Iron Vein"
        public string Description { get; set; }
        public GatheringType Type { get; set; }
        public string GatheredItemId { get; set; } // e.g., "iron_ore"
    }

}
