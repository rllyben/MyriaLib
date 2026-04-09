using MyriaLib.Entities.Maps;

namespace MyriaLib.Services.Regestries
{
    public static class CityRegistry
    {
        private static readonly MapAreaRegistry<City> _registry = new();

        public static void Load(string path = "Data/common/cities.json") => _registry.Load(path);
        public static City? GetCityByRoom(Room room) => _registry.GetByRoom(room);
        public static City? GetCityById(string id) => _registry.GetById(id);
        public static City? GetCityByName(string name) => _registry.GetByName(name);
        public static IReadOnlyCollection<City> GetAll() => _registry.GetAll();
    }
}
