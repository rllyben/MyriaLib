using Myria.Lib.Core.Entities.Characters;
using Myria.Lib.Core.Models;
using Myria.Lib.Core.Utils;
using System.Text.Json;

namespace Myria.Lib.Core.Services
{
    public static class UserAccountService
    {
        public static UserAccount CurrentUser { get; set; }
        public static Character CurrentCharacter { get; set; }
        public static void SaveUser()
        {
            string path = $"Data/users/{SafeFileName.Sanitize(CurrentUser.Username)}.json";
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);

            var options = new JsonSerializerOptions { WriteIndented = true };
            File.WriteAllText(path, JsonSerializer.Serialize(CurrentUser, options));
        }
    }

}
