using System.Text.Json;
using MyriaLib.Entities;
using MyriaLib.Entities.Players;
using MyriaLib.Models;
using MyriaLib.Systems;
using MyriaLib.Systems.Enums;

namespace MyriaLib.Services.Manager
{
    public class LoginManager
    {
        public static UserAccount UserAccount { get; private set; }
        public static bool Register(string username, string password)
        {
            string path = Path.Combine("Data/users", $"{username.ToLower()}.json");
            if (File.Exists(path))
            {
                return false;
            }

            var account = new UserAccount
            {
                Username = username,
                Password = password
            };

            if (!Path.Exists(path))
                Directory.CreateDirectory("Data/users");

            var json = JsonSerializer.Serialize(account, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(path, json);

            return true;
        }
        public static bool Login(string username, string password)
        {
            string path = Path.Combine("Data/users", $"{username.ToLower()}.json");

            if (!File.Exists(path))
            {
                return false;
            }

            var json = File.ReadAllText(path);
            UserAccoundService.CurrentUser = JsonSerializer.Deserialize<UserAccount>(json);

            if (UserAccoundService.CurrentUser!.Password != password)
            {
                return false;
            }

            return true;
        }

    }

}



