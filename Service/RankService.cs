using MovieTheater.Models;
using MovieTheater.Repository;
using MovieTheater.ViewModels;
using System.Collections.Generic;
using System.Linq;

namespace MovieTheater.Service
{
    public class RankService : IRankService
    {
        private readonly IAccountRepository _accountRepository;
        private readonly MovieTheaterContext _context;

        public RankService(IAccountRepository accountRepository, MovieTheaterContext context)
        {
            _accountRepository = accountRepository;
            _context = context;
        }

        public List<RankInfoViewModel> GetAllRanks()
        {
            return _context.Ranks.OrderBy(r => r.RequiredPoints)
                .Select(r => new RankInfoViewModel
                {
                    CurrentRankId = r.RankId,
                    CurrentRankName = r.RankName,
                    RequiredPointsForCurrentRank = r.RequiredPoints ?? 0,
                    CurrentDiscountPercentage = r.DiscountPercentage ?? 0,
                    CurrentPointEarningPercentage = r.PointEarningPercentage ?? 0,
                    ColorGradient = r.ColorGradient ?? "linear-gradient(135deg, #4e54c8 0%, #6c63ff 50%, #8f94fb 100%)",
                    IconClass = r.IconClass ?? "fa-crown"
                }).ToList();
        }

        public RankInfoViewModel GetRankInfoForUser(string accountId)
        {
            var account = _accountRepository.GetById(accountId);
            if (account == null || !account.RankId.HasValue) return null;

            var currentScore = account.Members.FirstOrDefault()?.Score ?? 0;
            var allRanks = _context.Ranks.OrderBy(r => r.RequiredPoints).ToList();
            var currentRank = allRanks.FirstOrDefault(r => r.RankId == account.RankId.Value);
            if (currentRank == null) return null;

            var nextRank = allRanks.FirstOrDefault(r => r.RequiredPoints > currentRank.RequiredPoints);

            var pointsForCurrentRank = currentRank.RequiredPoints ?? 0;
            var pointsForNextRank = nextRank?.RequiredPoints ?? pointsForCurrentRank;
            var totalPointsNeededForNextRank = pointsForNextRank - pointsForCurrentRank;
            var pointsProgressed = currentScore - pointsForCurrentRank;

            double progressPercentage = 0;
            if (totalPointsNeededForNextRank > 0)
            {
                progressPercentage = ((double)pointsProgressed / totalPointsNeededForNextRank) * 100;
            }
            else if (currentScore >= pointsForCurrentRank)
            {
                progressPercentage = 100;
            }

            return new RankInfoViewModel
            {
                CurrentRankId = currentRank.RankId,
                CurrentRankName = currentRank.RankName,
                CurrentDiscountPercentage = currentRank.DiscountPercentage ?? 0,
                CurrentPointEarningPercentage = currentRank.PointEarningPercentage ?? 0,
                CurrentScore = currentScore,
                RequiredPointsForCurrentRank = pointsForCurrentRank,
                RequiredPointsForNextRank = pointsForNextRank,
                PointsToNextRank = nextRank != null ? pointsForNextRank - currentScore : 0,
                ProgressToNextRank = progressPercentage,
                HasNextRank = nextRank != null,
                NextRankName = nextRank?.RankName,
                NextRankDiscountPercentage = nextRank?.DiscountPercentage ?? 0,
                NextRankPointEarningPercentage = nextRank?.PointEarningPercentage ?? 0,
                ColorGradient = currentRank.ColorGradient ?? "linear-gradient(135deg, #4e54c8 0%, #6c63ff 50%, #8f94fb 100%)",
                IconClass = currentRank.IconClass ?? "fa-crown"
            };
        }
    }
}