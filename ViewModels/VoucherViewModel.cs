using System.ComponentModel.DataAnnotations;

namespace MovieTheater.ViewModels
{
    public class VoucherViewModel
    {
        public string? VoucherId { get; set; }

        [Required(ErrorMessage = "Account ID is required")]
        public string AccountId { get; set; } = null!;

        [Required(ErrorMessage = "Code is required")]
        public string Code { get; set; } = null!;

        [Required(ErrorMessage = "Value is required")]
        [Range(0, double.MaxValue, ErrorMessage = "Value must be greater than 0")]
        public decimal Value { get; set; }

        [Required(ErrorMessage = "Created date is required")]
        public DateTime CreatedDate { get; set; }

        [Required(ErrorMessage = "Expiry date is required")]
        public DateTime ExpiryDate { get; set; }

        public bool IsUsed { get; set; }

        public string? Image { get; set; }
    }
} 