using System.Text.Json;
using Myria.Lib.Core.Entities.Characters;
using Myria.Lib.Core.Services;
using Myria.Lib.Core.Services.Builder;
using Myria.Lib.Core.Systems;

namespace Myria.Lib.Core.Repositories
{
    public class JsonCharacterRepository : ICharacterRepository
    {
        private static readonly JsonSerializerOptions _opts = new()
        {
            PropertyNameCaseInsensitive = true,
            WriteIndented = true,
            Converters = { new ItemConverter() }
        };

        private static string SavePath(string username, string characterName) =>
            Path.Combine("Data", "saves", $"{username}-{characterName}.json");

        public Task<List<string>> GetNamesAsync(string username)
        {
            var dir = Path.Combine("Data", "saves");
            if (!Directory.Exists(dir))
                return Task.FromResult(new List<string>());

            var prefix = $"{username}-";
            var names = Directory.GetFiles(dir, $"{username}-*.json")
                .Select(f => Path.GetFileNameWithoutExtension(f)[prefix.Length..])
                .ToList();

            return Task.FromResult(names);
        }

        public Task<Character?> LoadAsync(string username, string characterName)
        {
            var path = SavePath(username, characterName);
            if (!File.Exists(path))
                return Task.FromResult<Character?>(null);

            var character = JsonSerializer.Deserialize<Character>(File.ReadAllText(path), _opts);
            if (character is null) return Task.FromResult<Character?>(null);

            character.Inventory.Items.RemoveAll(i => i == null);
            character.CurrentRoom = RoomService.AllRooms.FirstOrDefault(r => r.Id == character.CurrentRoomId);
            character.RecalculateUnusedPoints();
            character.ValidateQuestStatuses();
            SkillFactory.UpdateSkills(character);

            return Task.FromResult<Character?>(character);
        }

        public Task SaveAsync(string username, Character character)
        {
            var path = SavePath(username, character.Name);
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            character.CurrentRoomId = character.CurrentRoom?.Id ?? character.CurrentRoomId;
            File.WriteAllText(path, JsonSerializer.Serialize(character, _opts));
            return Task.CompletedTask;
        }

        public Task DeleteAsync(string username, string characterName)
        {
            var path = SavePath(username, characterName);
            if (File.Exists(path)) File.Delete(path);
            return Task.CompletedTask;
        }
    }
}
