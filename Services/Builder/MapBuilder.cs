using MyriaLib.Entities.Maps;

namespace MyriaLib.Services.Builder
{
    public static class MapBuilder
    {
        public static Dictionary<Room, (int x, int y)> BuildRoomMap(Room startingRoom)
        {
            var positions = new Dictionary<Room, (int x, int y)>();
            var occupied  = new HashSet<(int, int)> { (0, 0) };
            var queue     = new Queue<(Room room, int x, int y)>();
            var visited   = new HashSet<Room>();
            bool inDungeon = startingRoom.IsDungeonRoom;

            var visibleRooms = RoomService.AllRooms.Where(r =>
                inDungeon ? r.IsDungeonRoom : !r.IsDungeonRoom
            );
            queue.Enqueue((startingRoom, 0, 0));
            positions[startingRoom] = (0, 0);

            while (queue.Count > 0)
            {
                var (room, x, y) = queue.Dequeue();
                if (!visited.Add(room)) continue;

                foreach (var exit in room.ExitIds)
                {
                    int dx = 0, dy = 0;
                    switch (exit.Key.ToLower())
                    {
                        case "north": dy = -1; break;
                        case "south": dy =  1; break;
                        case "east":  dx =  1; break;
                        case "west":  dx = -1; break;
                    }

                    var target = visibleRooms.FirstOrDefault(r => r.Id == exit.Value);
                    if (target == null || positions.ContainsKey(target)) continue;

                    // Find first unoccupied cell in the exit direction
                    int extend = 1;
                    (int cx, int cy) candidate = (x + dx * extend, y + dy * extend);
                    while (occupied.Contains(candidate))
                    {
                        extend++;
                        candidate = (x + dx * extend, y + dy * extend);
                    }

                    occupied.Add(candidate);
                    positions[target] = candidate;
                    queue.Enqueue((target, candidate.cx, candidate.cy));
                }
            }

            return positions;
        }

    }

}
