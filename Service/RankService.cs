using MovieTheater.Models;
using MovieTheater.Repository;
using MovieTheater.ViewModels;
using System.Collections.Generic;
using System.Linq;
using System;
using System.Threading.Tasks;

namespace MovieTheater.Service
{
    /// Service: Handles rank-related business logic
    public class RankService : IRankService
    {
        private readonly IAccountRepository _accountRepository;
        private readonly MovieTheaterContext _context;
        private readonly IMemberRepository _memberRepository;
        private readonly IRankRepository _rankRepository;

        public RankService(IAccountRepository accountRepository, MovieTheaterContext context, IMemberRepository memberRepository, IRankRepository rankRepository)
        {
            _accountRepository = accountRepository;
            _context = context;
            _memberRepository = memberRepository;
            _rankRepository = rankRepository;
        }

        // [HttpGet]
        // /// Get all rank tiers and their info
        // /// url: /Rank/GetAllRanks
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

        // [HttpGet]
        // /// Get rank info for a specific user (sync wrapper)
        // /// url: /Rank/GetRankInfoForUser
        public RankInfoViewModel GetRankInfoForUser(string accountId)
        {
            return GetRankInfoForUserAsync(accountId).GetAwaiter().GetResult();
        }

        // [HttpGet]
        // /// Get rank info for a specific user (async)
        // /// url: /Rank/GetRankInfoForUserAsync
        public async Task<RankInfoViewModel> GetRankInfoForUserAsync(string accountId)
        {
            var user = _accountRepository.GetById(accountId);
            if (user == null) return null;

            var member = _memberRepository.GetByAccountId(user.AccountId);
            if (member == null || user.RankId == null) return null;

            // Always sort allRanks by RequiredPoints ascending for correct next-rank logic
            var allRanks = (await _rankRepository.GetAllRanksAsync())
                .OrderBy(r => r.RequiredPoints ?? 0)
                .ToList();
            var currentRank = allRanks.FirstOrDefault(r => r.RankId == user.RankId);
            if (currentRank == null) return null;

            var requiredPointsForCurrentRank = currentRank.RequiredPoints ?? 0;
            // Find the next rank by RequiredPoints, not by RankId
            var nextRank = allRanks.FirstOrDefault(r => (r.RequiredPoints ?? 0) > requiredPointsForCurrentRank);

            var currentScore = member.Score ?? 0;
            var totalPoints = member.TotalPoints;
            var requiredForCurrent = currentRank.RequiredPoints ?? 0;
            var requiredForNext = nextRank?.RequiredPoints;

            var pointsToNextRank = requiredForNext.HasValue ? requiredForNext.Value - totalPoints : 0;
            
            double progressToNextRank = 0.0;
            if (requiredForNext.HasValue)
            {
                var pointsInThisRank = requiredForNext.Value - requiredForCurrent;
                if (pointsInThisRank > 0)
                {
                    var progressInRank = totalPoints - requiredForCurrent;
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
                CurrentPointEarningPercentage = currentRank.PointEarningPercentage ?? 0m,
                CurrentScore = currentScore,
                TotalPoints = totalPoints,
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
                    PointEarningPercentage = r.PointEarningPercentage ?? 0m,
                    IsCurrentRank = r.RankId == currentRank.RankId,
                    IsAchieved = totalPoints >= (r.RequiredPoints ?? 0),
                    ColorGradient = r.ColorGradient,
                    IconClass = r.IconClass
                }).ToList()
            };

            return rankModel;
        }

        // [HttpGet]
        // /// Get rank by id
        public RankInfoViewModel GetById(int id)
        {
            var rank = _context.Ranks.FirstOrDefault(r => r.RankId == id);
            if (rank == null) return null;
            return new RankInfoViewModel
            {
                CurrentRankId = rank.RankId,
                CurrentRankName = rank.RankName,
                RequiredPointsForCurrentRank = rank.RequiredPoints ?? 0,
                CurrentDiscountPercentage = rank.DiscountPercentage ?? 0,
                CurrentPointEarningPercentage = rank.PointEarningPercentage ?? 0,
                ColorGradient = rank.ColorGradient ?? "linear-gradient(135deg, #4e54c8 0%, #6c63ff 50%, #8f94fb 100%)",
                IconClass = rank.IconClass ?? "fa-crown"
            };
        }

        public bool Create(RankInfoViewModel model)
        {
            if (model == null) return false;
            // Check for duplicate RequiredPoints before adding
            if (_context.Ranks.Any(r => r.RequiredPoints == model.RequiredPointsForCurrentRank))
            {
                // Duplicate found, do not add
                return false;
            }
            var rank = new Rank
            {
                RankName = model.CurrentRankName,
                RequiredPoints = model.RequiredPointsForCurrentRank,
                DiscountPercentage = model.CurrentDiscountPercentage,
                PointEarningPercentage = model.CurrentPointEarningPercentage,
                ColorGradient = model.ColorGradient,
                IconClass = model.IconClass
            };
            _context.Ranks.Add(rank);
            _context.SaveChanges();
            return true;
        }

        public bool Update(RankInfoViewModel model)
        {
            var rank = _context.Ranks.FirstOrDefault(r => r.RankId == model.CurrentRankId);
            if (rank == null) return false;
            // Prevent duplicate RequiredPoints (excluding self)
            if (_context.Ranks.Any(r => r.RequiredPoints == model.RequiredPointsForCurrentRank && r.RankId != model.CurrentRankId))
            {
                // Duplicate found, do not update
                return false;
            }
            rank.RankName = model.CurrentRankName;
            rank.RequiredPoints = model.RequiredPointsForCurrentRank;
            rank.DiscountPercentage = model.CurrentDiscountPercentage;
            rank.PointEarningPercentage = model.CurrentPointEarningPercentage;
            rank.ColorGradient = model.ColorGradient;
            rank.IconClass = model.IconClass;
            _context.SaveChanges();
            return true;
        }

        public bool Delete(int id)
        {
            var rank = _context.Ranks.FirstOrDefault(r => r.RankId == id);
            if (rank == null) return false;
            _context.Ranks.Remove(rank);
            _context.SaveChanges();
            return true;
        }
    }
}