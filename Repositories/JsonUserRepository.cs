using System.Security.Cryptography;
using System.Text.Json;
using MyriaLib.Models;

namespace MyriaLib.Repositories
{
    public class JsonUserRepository : IUserRepository
    {
        private const int SaltSize   = 16;
        private const int HashSize   = 32;
        private const int Iterations = 200_000;
        private static readonly HashAlgorithmName HashAlg = HashAlgorithmName.SHA512;

        private static string UserPath(string username) =>
            Path.Combine("Data", "users", $"{username.ToLower()}.json");

        public Task<bool> ExistsAsync(string username) =>
            Task.FromResult(File.Exists(UserPath(username)));

        public Task<UserAccount?> GetByUsernameAsync(string username)
        {
            var path = UserPath(username);
            if (!File.Exists(path))
                return Task.FromResult<UserAccount?>(null);

            var json = File.ReadAllText(path);
            return Task.FromResult(JsonSerializer.Deserialize<UserAccount>(json));
        }

        public Task SaveAsync(UserAccount user)
        {
            var path = UserPath(user.Username);
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, JsonSerializer.Serialize(user, new JsonSerializerOptions { WriteIndented = true }));
            return Task.CompletedTask;
        }

        public static string HashPassword(string password)
        {
            byte[] salt = RandomNumberGenerator.GetBytes(SaltSize);
            byte[] hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlg, HashSize);
            return $"{Convert.ToBase64String(salt)}:{Convert.ToBase64String(hash)}";
        }

        public static bool VerifyPassword(string password, string stored)
        {
            var parts = stored.Split(':');
            if (parts.Length != 2) return false;
            byte[] salt         = Convert.FromBase64String(parts[0]);
            byte[] expectedHash = Convert.FromBase64String(parts[1]);
            byte[] actualHash   = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlg, HashSize);
            return CryptographicOperations.FixedTimeEquals(actualHash, expectedHash);
        }
    }
}
