namespace MyriaLib.Systems.Enums
{
    public enum GatherResult
    {
        Success,
        NoSpots,       // room has no gathering spots
        Depleted,      // all gathers used up for this game-day
        NoTool,        // player lacks the required tool
        InventoryFull,
    }
}
