using MovieTheater.Models;
using MovieTheater.ViewModels;
using System.Collections.Generic;

namespace MovieTheater.Service
{
    public class BookingPriceResult
    {
        public decimal Subtotal { get; set; }
        public decimal RankDiscountPercent { get; set; }
        public decimal RankDiscount { get; set; }
        public decimal PromotionDiscountPercent { get; set; }
        public decimal PromotionDiscount { get; set; }
        public decimal VoucherAmount { get; set; }
        public int UseScore { get; set; }
        public decimal UseScoreValue { get; set; }
        public decimal TotalFoodPrice { get; set; }
        public decimal TotalPrice { get; set; }
        public int AddScore { get; set; }
        public decimal SeatTotalAfterDiscounts { get; set; } // Added for correct invoice saving
        public List<SeatDetailViewModel> SeatDetails { get; set; }
        public List<FoodViewModel> FoodDetails { get; set; }
    }

    public interface IBookingPriceCalculationService
    {
        BookingPriceResult CalculatePrice(
            List<ViewModels.SeatDetailViewModel> seats,
            Models.MovieShow movieShow,
            Models.Account user,
            decimal? voucherAmount,
            int? useScore,
            List<Models.Food> foods = null
        );
    }
} 