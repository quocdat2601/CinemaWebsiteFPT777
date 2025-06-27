using MovieTheater.Models;

namespace MovieTheater.Repository
{
    public interface IRankRepository
    {
        // [HttpGet]
        // /// Get all ranks from the database
        // /// url: /Rank/GetAllRanksAsync
        Task<List<Rank>> GetAllRanksAsync();
        // [HttpGet]
        // /// Get a rank by its ID
        // /// url: /Rank/GetRankByIdAsync
        Task<Rank> GetRankByIdAsync(int rankId);
        // [HttpGet]
        // /// Get account with member and rank info
        // /// url: /Rank/GetAccountWithMemberAsync
        Task<Account> GetAccountWithMemberAsync(string accountId);
    }
}
