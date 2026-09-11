using System.Text.Json.Serialization;

namespace Myria.Lib.Core.Models
{
    public class UserAccount
    {
        public const string UserDir = "Data/users/";
        public const string SaveDir = "Data/saves/";
        /// <summary>Per-account character cap, shared by every enforcement point (server
        /// <c>CharactersController.Save</c>, singleplayer's local creation check, and the
        /// character-selection UI, which only has 5 fixed slots in its current design and has
        /// no way to show or select a 6th character even if one existed).</summary>
        public const int MaxCharacters = 5;
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
