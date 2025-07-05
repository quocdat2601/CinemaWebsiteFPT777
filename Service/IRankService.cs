using MovieTheater.ViewModels;

namespace MovieTheater.Service
{
    public interface IRankService
    {
        // [HttpGet]
        // /// Get rank info for a specific user
        // /// url: /Rank/GetRankInfoForUser
        RankInfoViewModel GetRankInfoForUser(string accountId);
        // [HttpGet]
        // /// Get all rank tiers and their info
        // /// url: /Rank/GetAllRanks
        List<RankInfoViewModel> GetAllRanks();
        RankInfoViewModel GetById(int id);
        bool Create(RankInfoViewModel model);
        bool Update(RankInfoViewModel model);
        bool Delete(int id);
    }
}