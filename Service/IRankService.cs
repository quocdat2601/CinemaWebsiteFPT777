using MovieTheater.ViewModels;

namespace MovieTheater.Service
{
    public interface IRankService
    {
        RankInfoViewModel GetRankInfoForUser(string accountId);
    }
} 