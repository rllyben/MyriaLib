using System.Text.Json;
using MyriaLib.Entities.Players;
using MyriaLib.Services;
using MyriaLib.Services.Builder;
using MyriaLib.Systems;

namespace MyriaLib.Repositories
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

        public Task<Player?> LoadAsync(string username, string characterName)
        {
            var path = SavePath(username, characterName);
            if (!File.Exists(path))
                return Task.FromResult<Player?>(null);

            var player = JsonSerializer.Deserialize<Player>(File.ReadAllText(path), _opts);
            if (player is null) return Task.FromResult<Player?>(null);

            player.CurrentRoom = RoomService.AllRooms.FirstOrDefault(r => r.Id == player.CurrentRoomId);
            player.RecalculateUnusedPoints();
            player.ValidateQuestStatuses();
            SkillFactory.UpdateSkills(player);

            return Task.FromResult<Player?>(player);
        }

        public Task SaveAsync(string username, Player player)
        {
            var path = SavePath(username, player.Name);
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            player.CurrentRoomId = player.CurrentRoom?.Id ?? player.CurrentRoomId;
            File.WriteAllText(path, JsonSerializer.Serialize(player, _opts));
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
