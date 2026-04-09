using MyriaLib.Entities.Maps;

namespace MyriaLib.Services.Regestries
{
    public static class DungeonRegistry
    {
        private static readonly MapAreaRegistry<Dungeon> _registry = new();

        public static void Load(string path = "Data/common/dungeons.json") => _registry.Load(path);
        public static Dungeon? GetDungeonByRoom(Room room) => _registry.GetByRoom(room);
        public static Dungeon? GetDungeonById(string id) => _registry.GetById(id);
        public static Dungeon? GetDungeonByName(string name) => _registry.GetByName(name);
        public static IReadOnlyCollection<Dungeon> GetAll() => _registry.GetAll();
    }
}
