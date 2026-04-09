using System.Security.Cryptography;
using System.Text.Json;
using MyriaLib.Models;

namespace MyriaLib.Services.Manager
{
    public static class LoginManager
    {
        private const int SaltSize    = 16;   // bytes
        private const int HashSize    = 32;   // bytes (256-bit)
        private const int Iterations  = 200_000;
        private static readonly HashAlgorithmName _hashAlg = HashAlgorithmName.SHA512;

        public static bool Register(string username, string password)
        {
            string path = Path.Combine("Data/users", $"{username.ToLower()}.json");
            if (File.Exists(path))
                return false;

            var account = new UserAccount
            {
                Username = username,
                Password = HashPassword(password)
            };

            Directory.CreateDirectory(Path.GetDirectoryName(path)!);

            var json = JsonSerializer.Serialize(account, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(path, json);

            return true;
        }

        public static bool Login(string username, string password)
        {
            string path = Path.Combine("Data/users", $"{username.ToLower()}.json");
            if (!File.Exists(path))
                return false;

            var json = File.ReadAllText(path);
            UserAccoundService.CurrentUser = JsonSerializer.Deserialize<UserAccount>(json);

            return VerifyPassword(password, UserAccoundService.CurrentUser!.Password);
        }

        // ── Helpers ──────────────────────────────────────────────────────────────

        private static string HashPassword(string password)
        {
            byte[] salt = RandomNumberGenerator.GetBytes(SaltSize);
            byte[] hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, _hashAlg, HashSize);
            return $"{Convert.ToBase64String(salt)}:{Convert.ToBase64String(hash)}";
        }

        private static bool VerifyPassword(string password, string stored)
        {
            var parts = stored.Split(':');
            if (parts.Length != 2)
                return false;

            byte[] salt = Convert.FromBase64String(parts[0]);
            byte[] expectedHash = Convert.FromBase64String(parts[1]);
            byte[] actualHash = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, _hashAlg, HashSize);

            return CryptographicOperations.FixedTimeEquals(actualHash, expectedHash);
        }
    }
}



