using MyriaLib.Models;

namespace MyriaLib.Repositories
{
    public interface IUserRepository
    {
        Task<bool> ExistsAsync(string username);
        Task<UserAccount?> GetByUsernameAsync(string username);
        Task SaveAsync(UserAccount user);
    }
}
