using MovieTheater.Models;

namespace MovieTheater.ViewModels
{
    public class ConfirmTicketAdminViewModel
    {
        // Booking details similar to ConfirmBookingViewModel
        public ConfirmBookingViewModel BookingDetails { get; set; }

        // Customer type and details for admin confirmation
        public string CustomerType { get; set; } = "member"; // "member" or "guest"
        public string MemberAccountId { get; set; }
        public string MemberIdInput { get; set; } // For input by admin
        public string MemberId { get; set; } // Displayed if member found
        public string MemberFullName { get; set; }
        public string MemberIdentityCard { get; set; }
        public string MemberPhoneNumber { get; set; }
        public int MemberScore { get; set; }
        public string MemberEmail { get; set; }
        public string MemberPhone { get; set; }
        public string? EmployeeId { get; set; }
        public string? EmployeeName { get; set; }
        public string? EmployeePhone { get; set; }
        public DateOnly? DateofBirth { get; set; }

        // Ticket conversion
        public int TicketsToConvert { get; set; }
        public decimal DiscountFromScore { get; set; }
        public string MemberCheckMessage { get; set; } // For displaying messages like "No member has found!" or "Member score is not enough!"
        public string ReturnUrl { get; set; } // For return button to seat selection
        public int MovieShowId { get; set; }

        public int UsedScore { get; set; }
        public int AddedScore { get; set; } // Points earned from this transaction
        public decimal UsedScoreValue { get; set; } // VND value of used points
        public decimal AddedScoreValue { get; set; } // VND value of earned points

        public decimal Subtotal { get; set; } // Sum of seat prices before discounts/points
        public decimal RankDiscount { get; set; } // Discount from member rank
        public decimal VoucherAmount { get; set; } // Discount from member voucher
        public decimal TotalPrice { get; set; } // Final price after all deductions

        public string SelectedVoucherId { get; set; }
        public string AccountId { get; set; }
        public decimal RankDiscountPercent { get; set; }

        // Food information
        public List<FoodViewModel> SelectedFoods { get; set; } = new List<FoodViewModel>();
        public decimal TotalFoodPrice { get; set; }
        public List<Promotion> EligibleFoodPromotions { get; set; } = new List<Promotion>();
        public decimal TotalFoodDiscount { get; set; }
    }
}