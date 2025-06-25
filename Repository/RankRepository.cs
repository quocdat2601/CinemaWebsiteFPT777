using Microsoft.EntityFrameworkCore;
using MovieTheater.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MovieTheater.Repository
{
    // /// Repository: Handles data access for rank features
    public class RankRepository : IRankRepository
    {
        private readonly MovieTheaterContext _context;

        public RankRepository(MovieTheaterContext context)
        {
            _context = context;
        }

        // [HttpGet]
        // /// Get all ranks from the database
        // /// url: /Rank/GetAllRanksAsync
        public async Task<List<Rank>> GetAllRanksAsync()
        {
            return await _context.Ranks.AsNoTracking().ToListAsync();
        }

        // [HttpGet]
        // /// Get a rank by its ID
        // /// url: /Rank/GetRankByIdAsync
        public async Task<Rank> GetRankByIdAsync(int rankId)
        {
            return await _context.Ranks.FindAsync(rankId);
        }

        // [HttpGet]
        // /// Get account with member and rank info
        // /// url: /Rank/GetAccountWithMemberAsync
        public async Task<Account> GetAccountWithMemberAsync(string accountId)
        {
            return await _context.Accounts
                                 .AsNoTracking()
                                 .Include(a => a.Members)
                                 .Include(a => a.Rank)
                                 .FirstOrDefaultAsync(a => a.AccountId == accountId);
        }
    }
}