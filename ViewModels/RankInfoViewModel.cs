using MovieTheater.Models;
using System.Collections.Generic;

namespace MovieTheater.ViewModels
{
    public class RankInfoViewModel
    {
        public int CurrentRankId { get; set; }
        public string CurrentRankName { get; set; }
        public decimal CurrentDiscountPercentage { get; set; }
        public decimal CurrentPointEarningPercentage { get; set; }
        
        public int CurrentScore { get; set; }
        public int TotalPoints { get; set; } // total lifetime points, not reset on redemption
        public int RequiredPointsForCurrentRank { get; set; }
        public int RequiredPointsForNextRank { get; set; }
        public int PointsToNextRank { get; set; }
        public double ProgressToNextRank { get; set; } // Percentage (0-100)
        
        public bool HasNextRank { get; set; }
        public string NextRankName { get; set; }
        public decimal NextRankDiscountPercentage { get; set; }
        public decimal NextRankPointEarningPercentage { get; set; }
        
        public string ColorGradient { get; set; }
        public string IconClass { get; set; }

        // All ranks in system
        public List<RankDisplayInfo> AllRanks { get; set; } = new List<RankDisplayInfo>();

    }

    public class RankDisplayInfo
    {
        public int RankId { get; set; }
        public string RankName { get; set; }
        public int RequiredPoints { get; set; }
        public decimal DiscountPercentage { get; set; }
        public decimal PointEarningPercentage { get; set; }
        public bool IsCurrentRank { get; set; }
        public bool IsAchieved { get; set; }
        public string ColorGradient { get; set; }
        public string IconClass { get; set; }
    }
} 