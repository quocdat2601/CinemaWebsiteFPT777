using MovieTheater.ViewModels;
using System.Collections.Generic;

namespace MovieTheater.Service
{
    public interface IRankService
    {
        RankInfoViewModel GetRankInfoForUser(string accountId);
        List<RankInfoViewModel> GetAllRanks();
    }
}