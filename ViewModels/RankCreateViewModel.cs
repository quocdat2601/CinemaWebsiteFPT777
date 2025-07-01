using System.ComponentModel.DataAnnotations;

namespace MovieTheater.ViewModels
{
    public class RankCreateViewModel
    {
        [Required(ErrorMessage = "Rank name is required.")]
        [Display(Name = "Rank Name")]
        public string CurrentRankName { get; set; }

        [Required(ErrorMessage = "Required points is required.")]
        [Display(Name = "Required Points")]
        [Range(0, int.MaxValue, ErrorMessage = "Points must be 0 or greater.")]
        public int RequiredPointsForCurrentRank { get; set; }

        [Range(0, 100, ErrorMessage = "Discount must be between 0 and 100.")]
        [Display(Name = "Discount (%)")]
        public decimal? CurrentDiscountPercentage { get; set; }

        [Range(0, 100, ErrorMessage = "Point earning must be between 0 and 100.")]
        [Display(Name = "Point Earning (%)")]
        public decimal? CurrentPointEarningPercentage { get; set; }

        [Display(Name = "Color Gradient or Color")]
        public string? ColorGradient { get; set; }

        [Display(Name = "Icon Class")]
        public string? IconClass { get; set; }
    }
}