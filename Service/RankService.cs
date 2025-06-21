using MovieTheater.Models;
using MovieTheater.Repository;
using MovieTheater.ViewModels;
using System;
using System.Linq;

namespace MovieTheater.Service
{
    public class RankService : IRankService
    {
        private readonly IAccountRepository _accountRepository;
        private readonly IMemberRepository _memberRepository;
        private readonly IRankRepository _rankRepository;

        public RankService(IAccountRepository accountRepository, IMemberRepository memberRepository, IRankRepository rankRepository)
        {
            _accountRepository = accountRepository;
            _memberRepository = memberRepository;
            _rankRepository = rankRepository;
        }

        public RankInfoViewModel GetRankInfoForUser(string accountId)
        {
            var user = _accountRepository.GetById(accountId);
            if (user == null) return null;

            var member = _memberRepository.GetByAccountId(user.AccountId);
            if (member == null || user.RankId == null) return null;

            var allRanks = _rankRepository.GetAllRanks().ToList();
            var currentRank = allRanks.FirstOrDefault(r => r.RankId == user.RankId);
            if (currentRank == null) return null;

            var requiredPointsForCurrentRank = currentRank.RequiredPoints ?? 0;
            var nextRank = allRanks.FirstOrDefault(r => (r.RequiredPoints ?? 0) > requiredPointsForCurrentRank);

            var currentScore = member.Score ?? 0;
            var requiredForCurrent = currentRank.RequiredPoints ?? 0;
            var requiredForNext = nextRank?.RequiredPoints;

            var pointsToNextRank = requiredForNext.HasValue ? requiredForNext.Value - currentScore : 0;
            
            double progressToNextRank = 0.0;
            if (requiredForNext.HasValue)
            {
                var pointsInThisRank = requiredForNext.Value - requiredForCurrent;
                if (pointsInThisRank > 0)
                {
                    var progressInRank = currentScore - requiredForCurrent;
                    progressToNextRank = (double)progressInRank * 100 / pointsInThisRank;
                }
            }
            else
            {
                progressToNextRank = 100; // Max rank
            }

            var rankModel = new RankInfoViewModel
            {
                CurrentRankId = currentRank.RankId,
                CurrentRankName = currentRank.RankName,
                CurrentDiscountPercentage = currentRank.DiscountPercentage ?? 0m,
                CurrentPointEarningPercentage = currentRank.PointEarningPercentage,
                CurrentScore = currentScore,
                RequiredPointsForCurrentRank = requiredForCurrent,
                RequiredPointsForNextRank = requiredForNext ?? requiredForCurrent,
                PointsToNextRank = pointsToNextRank > 0 ? pointsToNextRank : 0,
                ProgressToNextRank = Math.Max(0, Math.Min(100, progressToNextRank)),
                HasNextRank = nextRank != null,
                NextRankName = nextRank?.RankName,
                NextRankDiscountPercentage = nextRank?.DiscountPercentage ?? 0m,
                NextRankPointEarningPercentage = nextRank?.PointEarningPercentage ?? 0m,
                AllRanks = allRanks.Select(r => new RankDisplayInfo
                {
                    RankId = r.RankId,
                    RankName = r.RankName,
                    RequiredPoints = r.RequiredPoints ?? 0,
                    DiscountPercentage = r.DiscountPercentage ?? 0m,
                    PointEarningPercentage = r.PointEarningPercentage,
                    IsCurrentRank = r.RankId == currentRank.RankId,
                    IsAchieved = currentScore >= (r.RequiredPoints ?? 0)
                }).ToList()
            };

            return rankModel;
        }
    }
} 