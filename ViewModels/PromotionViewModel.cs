using System.ComponentModel.DataAnnotations;

namespace MovieTheater.ViewModels
{
    public class PromotionViewModel
    {
        public int PromotionId { get; set; }

        [Required(ErrorMessage = "Title is required")]
        [StringLength(100, ErrorMessage = "Title cannot be longer than 100 characters")]
        public string Title { get; set; }

        [Required(ErrorMessage = "Detail is required")]
        public string Detail { get; set; }

        [Required(ErrorMessage = "Discount level is required")]
        [Range(1, 100, ErrorMessage = "Discount level must be between 1 and 100")]
        public int DiscountLevel { get; set; }

        [Required(ErrorMessage = "Start time is required")]
        [Display(Name = "Start Time")]
        public DateTime StartTime { get; set; }

        [Required(ErrorMessage = "End time is required")]
        [Display(Name = "End Time")]
        public DateTime EndTime { get; set; }

        [Display(Name = "Promotion Image")]
        public string? Image { get; set; }

        [Display(Name = "Active Status")]
        public bool IsActive { get; set; }

        // Promotion Condition fields
        [Display(Name = "Target Table")]
        public string? TargetField { get; set; }

        [Display(Name = "Target Field Column")]
        public string? TargetFieldColumn { get; set; }

        [Display(Name = "Operator")]
        public string? Operator { get; set; }

        [Display(Name = "Target Value")]
        public string? TargetValue { get; set; }

        // For editing multiple conditions
        public List<PromotionConditionEditViewModel> Conditions { get; set; } = new List<PromotionConditionEditViewModel>();
    }
}