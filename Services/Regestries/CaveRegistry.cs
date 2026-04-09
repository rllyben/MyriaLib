using MyriaLib.Entities.Maps;

namespace MyriaLib.Services.Regestries
{
    public static class CaveRegistry
    {
        private static readonly MapAreaRegistry<Cave> _registry = new();

        public static void Load(string path = "Data/common/caves.json") => _registry.Load(path);
        public static Cave? GetCaveByRoom(Room room) => _registry.GetByRoom(room);
        public static Cave? GetCaveById(string id) => _registry.GetById(id);
        public static Cave? GetCaveByName(string name) => _registry.GetByName(name);
        public static IReadOnlyCollection<Cave> GetAll() => _registry.GetAll();
    }
}
