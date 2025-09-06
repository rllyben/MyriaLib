using System.Text.Json;
using MyriaLib.Entities.Maps;

namespace MyriaLib.Services.Regestries
{
    public static class CityRegistry
    {
        private static Dictionary<string, City> _cities= new();
        /// <summary>
        /// loads all dungons
        /// </summary>
        /// <param name="path">path to an dungon file, default is Data/dungeons.json</param>
        public static void Load(string path = "Data/Maps/cities.json")
        {
            var json = File.ReadAllText(path);
            var list = JsonSerializer.Deserialize<List<City>>(json);
            _cities = list.ToDictionary(d => d.Id);
        }
        /// <summary>
        /// retruns the dungon a room belongs to
        /// </summary>
        /// <param name="room">a room of the dungeon</param>
        /// <returns>the found dungon or null</returns>
        public static City? GetCityByRoom(Room room)
        {
            return _cities.Values.FirstOrDefault(d => d.ContainsRoom(room));
        }
        /// <summary>
        /// Gets the dungeon by its id
        /// </summary>
        /// <param name="id">the dungon id of the wanted dungeon</param>
        /// <returns>the found dungeon or null</returns>
        public static City? GetCityById(string id) =>
            _cities.TryGetValue(id, out var d) ? d : null;
        public static City? GetCityByName(string name) =>
            _cities.Values.FirstOrDefault(c => c.Name.ToLower() == name);
    }
}
