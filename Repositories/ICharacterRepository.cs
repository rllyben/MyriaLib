using MyriaLib.Entities.Characters;

namespace MyriaLib.Repositories
{
    public interface ICharacterRepository
    {
        Task<List<string>> GetNamesAsync(string username);
        Task<Character?> LoadAsync(string username, string characterName);
        Task SaveAsync(string username, Character character);
        Task DeleteAsync(string username, string characterName);
    }
}
