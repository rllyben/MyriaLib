namespace MyriaLib.Entities.Maps
{
    public class Forest
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public List<int> RoomIds { get; set; } = new();
        public bool ContainsRoom(Room room) => RoomIds.Contains(room.Id);
    }
}
