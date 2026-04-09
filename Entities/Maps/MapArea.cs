namespace MyriaLib.Entities.Maps
{
    /// <summary>
    /// Base class for all named map areas (Cave, City, Dungeon, Forest).
    /// An area is a named group of rooms with a designated entry point.
    /// </summary>
    public abstract class MapArea
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public List<int> RoomIds { get; set; } = new();
        public int AnchorRoomId { get; set; } = 0;

        public bool ContainsRoom(Room room) => RoomIds.Contains(room.Id);
    }
}
