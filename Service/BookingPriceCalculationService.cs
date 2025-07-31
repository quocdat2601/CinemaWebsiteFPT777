using MovieTheater.Models;
using MovieTheater.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MovieTheater.Service
{
    public class BookingPriceCalculationService : IBookingPriceCalculationService
    {
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
                // Giả sử không có promotion, truyền OriginalPrice = Price
                foreach (var food in foods)
                {
                    foodDetails.Add(new FoodViewModel
                    {
                        FoodId = food.FoodId,
                        Name = food.Name,
                        Price = food.Price,
                        OriginalPrice = food.Price,
                        PromotionDiscount = 0,
                        PromotionName = null,
                        Quantity = 1 // or set from elsewhere
                    });
                    totalFoodPrice += food.Price;
                }
            }
            
            // SỬA: Lấy promotion discount percent từ seat data đã được tính toán chính xác
            decimal promotionDiscountPercent = 0;
            if (seats != null && seats.Count > 0)
            {
                // Lấy promotion discount percent từ seat đầu tiên có promotion
                var seatWithPromotion = seats.FirstOrDefault(s => s.PromotionDiscount.HasValue && s.PromotionDiscount.Value > 0);
                if (seatWithPromotion != null && seatWithPromotion.OriginalPrice.HasValue && seatWithPromotion.OriginalPrice.Value > 0)
                {
                    promotionDiscountPercent = Math.Round((seatWithPromotion.PromotionDiscount.Value / seatWithPromotion.OriginalPrice.Value) * 100);
                }
            }
            
            // SỬA: Tính toán đúng total price
            // Seat total after rank discount and used score
            decimal seatTotalAfterRankAndScore = promoSubtotal - rankDiscount - usedScoreValue;
            if (seatTotalAfterRankAndScore < 0) seatTotalAfterRankAndScore = 0;
            
            // Add food price
            decimal seatAndFoodTotal = seatTotalAfterRankAndScore + totalFoodPrice;
            
            // Apply voucher to total (seat + food)
            decimal totalAfterVoucher = seatAndFoodTotal - voucher;
            if (totalAfterVoucher < 0) totalAfterVoucher = 0;
            
            // AddScore is based only on seat portion after discounts
            decimal earningRate = user?.Rank?.PointEarningPercentage ?? 1;
            decimal baseForEarning = promoSubtotal - rankDiscount - usedScoreValue;
            int addScore = (int)Math.Floor(baseForEarning * earningRate / 100 / 1000);
            
            // Debug logging
            System.Diagnostics.Debug.WriteLine($"AddScore calculation: promoSubtotal={promoSubtotal}, rankDiscount={rankDiscount}, usedScoreValue={usedScoreValue}, baseForEarning={baseForEarning}, earningRate={earningRate}, addScore={addScore}");
            
            return new BookingPriceResult
            {
                Subtotal = promoSubtotal,
                RankDiscountPercent = rankDiscountPercent,
                RankDiscount = rankDiscount,
                PromotionDiscountPercent = promotionDiscountPercent, // set đúng discount percent
                PromotionDiscount = promotionDiscountPercent,
                VoucherAmount = voucher,
                UseScore = usedScore,
                UseScoreValue = usedScoreValue,
                TotalFoodPrice = totalFoodPrice,
                TotalPrice = totalAfterVoucher, // Đã sửa: tổng cuối cùng đúng logic
                AddScore = addScore,
                SeatTotalAfterDiscounts = seatTotalAfterRankAndScore,
                SeatDetails = seats,
                FoodDetails = foodDetails
            };
        }
    }
} 