using MovieTheater.Models;
using MovieTheater.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MovieTheater.Service
{
    public class BookingPriceCalculationService : IBookingPriceCalculationService
    {
        private readonly IPromotionService _promotionService;
        public BookingPriceCalculationService(IPromotionService promotionService)
        {
            _promotionService = promotionService;
        }

        public BookingPriceResult CalculatePrice(
            List<SeatDetailViewModel> seats,
            MovieShow movieShow,
            Account user,
            decimal? voucherAmount,
            int? useScore,
            List<Food> foods = null
        )
        {
            // Use promo-discounted seat prices (as in the UI)
            decimal promoSubtotal = seats.Sum(s => s.Price); // s.Price is already after promotion
            decimal rankDiscountPercent = user?.Rank?.DiscountPercentage ?? 0;
            decimal rankDiscount = promoSubtotal * (rankDiscountPercent / 100m);
            decimal voucher = voucherAmount ?? 0;
            int usedScore = useScore ?? 0;
            decimal usedScoreValue = usedScore * 1000;
            decimal totalFoodPrice = 0;
            var foodDetails = new List<FoodViewModel>();
            if (foods != null)
            {
                foreach (var food in foods)
                {
                    foodDetails.Add(new FoodViewModel
                    {
                        FoodId = food.FoodId,
                        Name = food.Name,
                        Price = food.Price,
                        Quantity = 1 // or set from elsewhere
                    });
                    totalFoodPrice += food.Price;
                }
            }
            // Final seat total after all discounts
            decimal seatTotalAfterDiscounts = promoSubtotal - rankDiscount - usedScoreValue - voucher;
            if (seatTotalAfterDiscounts < 0) seatTotalAfterDiscounts = 0;
            // Add food for display only
            decimal totalPrice = seatTotalAfterDiscounts + totalFoodPrice;
            // AddScore is based only on seatTotalAfterDiscounts
            decimal earningRate = user?.Rank?.PointEarningPercentage ?? 1;
            int addScore = (int)Math.Floor(seatTotalAfterDiscounts * earningRate / 100 / 1000);
            return new BookingPriceResult
            {
                Subtotal = promoSubtotal,
                RankDiscountPercent = rankDiscountPercent,
                RankDiscount = rankDiscount,
                PromotionDiscountPercent = 0, // for display only, not used in math
                PromotionDiscount = 0,
                VoucherAmount = voucher,
                UseScore = usedScore,
                UseScoreValue = usedScoreValue,
                TotalFoodPrice = totalFoodPrice,
                TotalPrice = totalPrice,
                AddScore = addScore,
                SeatTotalAfterDiscounts = seatTotalAfterDiscounts,
                SeatDetails = seats,
                FoodDetails = foodDetails
            };
        }
    }
} 