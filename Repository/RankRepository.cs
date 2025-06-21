using MovieTheater.Models;
using System.Collections.Generic;
using System.Linq;

namespace MovieTheater.Repository
{
    public class RankRepository : IRankRepository
    {
        private readonly MovieTheaterContext _context;

        public RankRepository(MovieTheaterContext context)
        {
            _context = context;
        }

        public IEnumerable<Rank> GetAllRanks()
        {
            return _context.Ranks.OrderBy(r => r.RequiredPoints).ToList();
        }

        public Rank GetRankById(int rankId)
        {
            return _context.Ranks.Find(rankId);
        }
    }
} 