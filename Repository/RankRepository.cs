using Microsoft.EntityFrameworkCore;
using MovieTheater.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MovieTheater.Repository
{
    public class RankRepository : IRankRepository
    {
        private readonly MovieTheaterContext _context;

        public RankRepository(MovieTheaterContext context)
        {
            _context = context;
        }

        public async Task<List<Rank>> GetAllRanksAsync()
        {
            return await _context.Ranks.AsNoTracking().ToListAsync();
        }

        public async Task<Rank> GetRankByIdAsync(int rankId)
        {
            return await _context.Ranks.FindAsync(rankId);
        }

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