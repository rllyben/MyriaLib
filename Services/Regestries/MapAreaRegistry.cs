using System.Text.Json;
using MyriaLib.Entities.Maps;

namespace MyriaLib.Services.Regestries
{
    /// <summary>
    /// Generic registry for any <see cref="MapArea"/> subtype.
    /// Loads from JSON, indexes by Id, and supports lookup by room, id, or name.
    /// </summary>
    public class MapAreaRegistry<T> where T : MapArea
    {
        private Dictionary<string, T> _items = new();

        public void Load(string path)
        {
            var json = File.ReadAllText(path);
            var list = JsonSerializer.Deserialize<List<T>>(json) ?? new();
            _items = list.ToDictionary(x => x.Id);
        }

        public T? GetByRoom(Room room) =>
            _items.Values.FirstOrDefault(x => x.ContainsRoom(room));

        public T? GetById(string id) =>
            _items.TryGetValue(id, out var x) ? x : null;

        public T? GetByName(string name) =>
            _items.Values.FirstOrDefault(x => x.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

        public IReadOnlyCollection<T> GetAll() => _items.Values.ToList();
    }
}
