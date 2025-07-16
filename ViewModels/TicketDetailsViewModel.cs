using MovieTheater.Models;
using System.Collections.Generic;

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
        public decimal FoodTotal { get; set; }
        public List<FoodViewModel> SelectedFoods { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal PromotionDiscountPercent { get; set; }

    }
} 