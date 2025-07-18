using System.ComponentModel.DataAnnotations;

namespace MovieTheater.ViewModels
{
    public class PromotionConditionEditViewModel
    {
        public int ConditionId { get; set; }
        
        [Display(Name = "Target Entity")]
        public string? TargetEntity { get; set; }
        
        [Display(Name = "Target Field")]
        public string? TargetField { get; set; }
        
        [Display(Name = "Operator")]
        public string? Operator { get; set; }
        
        [Display(Name = "Target Value")]
        public string? TargetValue { get; set; }
    }
} 