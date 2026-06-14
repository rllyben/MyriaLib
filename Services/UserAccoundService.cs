using MyriaLib.Entities.Characters;
using MyriaLib.Models;
using System.Text.Json;

namespace MyriaLib.Services
{
    public static class UserAccoundService
    {
        public static UserAccount CurrentUser { get; set; }
        public static Character CurrentCharacter { get; set; }
        public static void SaveUser()
        {
            string path = $"Data/users/{CurrentUser.Username}.json";
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);

            var options = new JsonSerializerOptions { WriteIndented = true };
            File.WriteAllText(path, JsonSerializer.Serialize(CurrentUser, options));
        }
    }

}
