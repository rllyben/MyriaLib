using System.Text.Json.Serialization;

namespace Myria.Lib.Core.Models
{
    public class UserAccount
    {
        public const string UserDir = "Data/users/";
        public const string SaveDir = "Data/saves/";
        public string Username { get; set; }
        // In-memory only (used to re-authenticate a dropped hub connection - see
        // Myria.Console's Game.TryConnectHubAsync). [JsonIgnore] so UserAccoundService.SaveUser
        // never writes this to the local Data/users/*.json file - nothing reads it back after
        // login, so persisting it would only be unnecessary offline-cracking surface for
        // anyone with access to that file.
        [JsonIgnore]
        public string Password { get; set; }
        public List<string> CharacterNames { get; set; } = new();
    }

}
