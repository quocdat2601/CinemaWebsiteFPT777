using MovieTheater.Models;

namespace MovieTheater.ViewModels
{
    public class TicketDetailsViewModel
    {
        public Invoice Booking { get; set; }
        public List<SeatDetailViewModel> SeatDetails { get; set; }
        public decimal? VoucherAmount { get; set; }
        public string VoucherCode { get; set; }
        public decimal Subtotal { get; set; }
        public decimal RankDiscount { get; set; }
        public decimal UsedScoreValue { get; set; }
        public List<FoodViewModel> FoodDetails { get; set; } // List of foods for this ticket
        public decimal TotalFoodPrice { get; set; } // Total price of food
        public decimal TotalAmount { get; set; }
        public decimal PromotionDiscountPercent { get; set; }

    }
}