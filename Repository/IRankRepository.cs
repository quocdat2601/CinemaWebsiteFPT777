using MovieTheater.Models;
using System.Collections.Generic;

namespace MovieTheater.Repository
{
    public interface IRankRepository
    {
        IEnumerable<Rank> GetAllRanks();
        Rank GetRankById(int rankId);
    }
} 