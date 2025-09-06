namespace MyriaLib.Models
{
    public class UserAccount
    {
        public const string UserDir = "Data/users/";
        public const string SaveDir = "Data/saves/";
        public string Username { get; set; }
        public string Password { get; set; } // optionally hash later
        public List<string> CharacterNames { get; set; } = new();
    }

}
