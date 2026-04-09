using MyriaLib.Entities.Maps;

namespace MyriaLib.Services.Regestries
{
    public static class ForestRegistry
    {
        private static readonly MapAreaRegistry<Forest> _registry = new();

        public static void Load(string path = "Data/common/forests.json") => _registry.Load(path);
        public static Forest? GetForestByRoom(Room room) => _registry.GetByRoom(room);
        public static Forest? GetForestById(string id) => _registry.GetById(id);
        public static Forest? GetForestByName(string name) => _registry.GetByName(name);
        public static IReadOnlyCollection<Forest> GetAll() => _registry.GetAll();
    }
}
