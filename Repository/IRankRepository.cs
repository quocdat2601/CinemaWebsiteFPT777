using MovieTheater.Models;

namespace MovieTheater.Repository
{
    public interface IRankRepository
    {
        Task<List<Rank>> GetAllRanksAsync();
        Task<Rank> GetRankByIdAsync(int rankId);
        Task<Account> GetAccountWithMemberAsync(string accountId);
    }
}
