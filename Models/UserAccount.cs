namespace MyriaLib.Models
{
    public class UserAccount
    {
        public const string UserDir = "Data/users/";
        public const string SaveDir = "Data/saves/";
        public string Username { get; set; }
        /// <summary>Stored as "salt_base64:hash_base64" (PBKDF2-SHA512). Never plain-text.</summary>
        public string Password { get; set; }
        public List<string> CharacterNames { get; set; } = new();
    }

}
