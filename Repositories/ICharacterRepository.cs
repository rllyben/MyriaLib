using MyriaLib.Entities.Players;

namespace MyriaLib.Repositories
{
    public interface ICharacterRepository
    {
        Task<List<string>> GetNamesAsync(string username);
        Task<Player?> LoadAsync(string username, string characterName);
        Task SaveAsync(string username, Player player);
        Task DeleteAsync(string username, string characterName);
    }
}
