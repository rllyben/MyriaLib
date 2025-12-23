using MyriaLib.Entities.Players;
using MyriaLib.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace MyriaLib.Services
{
    public class UserAccoundService
    {
        public static UserAccount CurrentUser { get; set; }
        public static Player CurrentCharacter { get; set; }
        public static void SaveUser()
        {
            string path = $"Data/users/{CurrentUser.Username}.json";
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);

            var options = new JsonSerializerOptions { WriteIndented = true };
            File.WriteAllText(path, JsonSerializer.Serialize(CurrentUser, options));
        }
    }

}
