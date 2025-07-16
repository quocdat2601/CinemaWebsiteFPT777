using System;
using System.Collections.Generic;
using System.Linq;
using Moq;
using MovieTheater.Models;
using MovieTheater.Service;
using MovieTheater.ViewModels;
using Xunit;

namespace MovieTheater.Tests.Service
{
    public class BookingPriceCalculationServiceTests
    {
        private readonly BookingPriceCalculationService _svc;

        public BookingPriceCalculationServiceTests()
        {
            // your implementation doesn't actually call into _promotionService,
            // but we still inject a mock so the constructor signature is satisfied.
            _svc = new  BookingPriceCalculationService();
        }

        private static SeatDetailViewModel MakeSeat(decimal price) =>
            new() { Price = price, OriginalPrice = price };

        private static Account MakeUser(decimal? discountPct = null, decimal? earningPct = null)
        {
            var acct = new Account();
            if (discountPct.HasValue || earningPct.HasValue)
                acct.Rank = new Rank
                {
                    DiscountPercentage = discountPct,
                    PointEarningPercentage = earningPct
                };
            return acct;
        }

        [Fact]
        public void CalculatePrice_NoDiscountsOrExtras_ReturnsSubtotalOnly()
        {
            // Arrange: two seats @100 and @150
            var seats = new List<SeatDetailViewModel>
            {
                MakeSeat(100),
                MakeSeat(150)
            };
            var user = MakeUser(); // no rank
            // Act
            var result = _svc.CalculatePrice(seats, movieShow: null, user, voucherAmount: null, useScore: null, foods: null);

            // Assert
            Assert.Equal(250m, result.Subtotal);
            Assert.Equal(0m, result.RankDiscount);
            Assert.Equal(0m, result.VoucherAmount);
            Assert.Equal(0m, result.UseScoreValue);
            Assert.Equal(250m, result.TotalPrice);
            // earningRate defaults to 1 => addScore = floor(250 * 1% / 1000) = floor(2.5 / 1000) = 0
            Assert.Equal(0, result.AddScore);
        }

        [Fact]
        public void CalculatePrice_WithRankDiscount_AppliesDiscount()
        {
            // Arrange
            var seats = new[] { MakeSeat(200m) }.ToList();
            var user = MakeUser(discountPct: 10m, earningPct: 1m);
            // Act
            var r = _svc.CalculatePrice(seats, null, user, null, null, null);
            // 10% of 200 = 20
            Assert.Equal(20m, r.RankDiscount);
            Assert.Equal(180m, r.SeatTotalAfterDiscounts);
            Assert.Equal(180m, r.TotalPrice);
        }

        [Fact]
        public void CalculatePrice_WithVoucherAndScore_ReducesSeatTotalAndNotNegative()
        {
            // Arrange
            var seats = new[] { MakeSeat(100m) }.ToList();
            var user = MakeUser(discountPct: 0m);
            // useScore = 1 => value = 1000
            // voucherAmount = 50
            // subtotal = 100, deductions = 1000 + 50 = 1050 => clipped to 0
            var r = _svc.CalculatePrice(seats, null, user, voucherAmount: 50m, useScore: 1, foods: null);

            Assert.Equal(0m, r.SeatTotalAfterDiscounts);
            Assert.Equal(0m, r.TotalPrice);
        }

        [Fact]
        public void CalculatePrice_WithFoods_AddsFoodPrice()
        {
            // Arrange
            var seats = new[] { MakeSeat(100m) }.ToList();
            var user = MakeUser();
            var foods = new List<Food>
            {
                new() { FoodId = 1, Price = 30m, Name = "Popcorn" },
                new() { FoodId = 2, Price = 20m, Name = "Soda" }
            };
            // Act
            var r = _svc.CalculatePrice(seats, null, user, null, null, foods);

            // subtotal=100, no discounts => seatTotal=100, foodTotal=50
            Assert.Equal(50m, r.TotalFoodPrice);
            Assert.Equal(100m + 50m, r.TotalPrice);
            // Verify FoodDetails populated
            Assert.Collection(r.FoodDetails,
                f => Assert.Equal(30m, f.Price),
                f => Assert.Equal(20m, f.Price));
        }

        [Fact]
        public void CalculatePrice_CorrectlyComputesAddScore_WithCustomEarningRate()
        {
            // Arrange
            var seatsLow = new List<SeatDetailViewModel> { MakeSeat(10_000m) };
            // earningPct = 50% → 10 000 * 50% = 5 000 → ÷1 000 = 5
            var userLow = MakeUser(discountPct: 0m, earningPct: 50m);

            // Act
            var rLow = _svc.CalculatePrice(seatsLow, movieShow: null, userLow, voucherAmount: null, useScore: null, foods: null);

            // Assert
            Assert.Equal(5, rLow.AddScore);


            // Now a bigger subtotal
            var seatsHi = new List<SeatDetailViewModel> { MakeSeat(200_000m) };
            // 200 000 * 50% = 100 000 → ÷1 000 = 100
            var rHi = _svc.CalculatePrice(seatsHi, movieShow: null, userLow, voucherAmount: null, useScore: null, foods: null);

            Assert.Equal(100, rHi.AddScore);
        }

        [Fact]
        public void CalculatePrice_NullFoods_ProducesEmptyFoodDetails()
        {
            // Arrange
            var seats = new List<SeatDetailViewModel> { MakeSeat(100m) };
            var user = MakeUser();

            // Act
            var result = _svc.CalculatePrice(
                seats,
                movieShow: null,
                user,
                voucherAmount: null,
                useScore: null,
                foods: null // <— drives the "if (foods != null)" branch skip
            );

            // Assert
            Assert.NotNull(result.FoodDetails);
            Assert.Empty(result.FoodDetails);
            Assert.Equal(100m, result.Subtotal);
            Assert.Equal(100m, result.TotalPrice);
        }

        [Fact]
        public void CalculatePrice_SeatTotalNegative_ClampedToZero()
        {
            // Arrange: 1 seat @100, but redeem 1 point (1 000) plus voucher 200 => net -1 100
            var seats = new List<SeatDetailViewModel> { MakeSeat(100m) };
            var user = MakeUser(discountPct: 0m, earningPct: 1m);

            // Act
            var r = _svc.CalculatePrice(
                seats,
                movieShow: null,
                user,
                voucherAmount: 200m,
                useScore: 1,
                foods: null
            );

            // subtotal=100, discount/value=1000+200=1200 => seatTotalAfterDiscounts<0 → clamped to 0
            Assert.Equal(0m, r.SeatTotalAfterDiscounts);
            Assert.Equal(0m, r.TotalPrice);
        }

        [Fact]
        public void CalculatePrice_NullUser_DefaultsNoRankOrEarning()
        {
            // Arrange: no user → rankDiscountPct=0, earningRate=1
            var seats = new List<SeatDetailViewModel> { MakeSeat(500m) };

            // Act
            var r = _svc.CalculatePrice(
                seats,
                movieShow: null,
                user: null,
                voucherAmount: null,
                useScore: null,
                foods: null
            );

            // Assert
            Assert.Equal(500m, r.Subtotal);
            Assert.Equal(0m, r.RankDiscount);
            // earningRate=1%, so addScore = floor(500*1% /1000) = floor(5/1000) = 0
            Assert.Equal(0, r.AddScore);
        }

        [Fact]
        public void CalculatePrice_WithEmptyFoodsList_ProducesEmptyFoodDetails()
        {
            // Arrange
            var seats = new List<SeatDetailViewModel> { MakeSeat(100m) };
            var user = MakeUser();
            var foods = new List<Food>();
            // Act
            var result = _svc.CalculatePrice(seats, null, user, null, null, foods);
            // Assert
            Assert.NotNull(result.FoodDetails);
            Assert.Empty(result.FoodDetails);
            Assert.Equal(100m, result.Subtotal);
            Assert.Equal(100m, result.TotalPrice);
        }

        [Fact]
        public void CalculatePrice_WithMultipleFoods_SumsAllPrices()
        {
            // Arrange
            var seats = new List<SeatDetailViewModel> { MakeSeat(50m) };
            var user = MakeUser();
            var foods = new List<Food> {
                new() { FoodId = 1, Price = 10m, Name = "A" },
                new() { FoodId = 2, Price = 20m, Name = "B" },
                new() { FoodId = 3, Price = 30m, Name = "C" }
            };
            // Act
            var result = _svc.CalculatePrice(seats, null, user, null, null, foods);
            // Assert
            Assert.Equal(60m, result.TotalFoodPrice);
            Assert.Equal(110m, result.TotalPrice);
            Assert.Equal(3, result.FoodDetails.Count);
        }

        [Fact]
        public void CalculatePrice_WithNoSeats_ReturnsZeroes()
        {
            // Arrange
            var seats = new List<SeatDetailViewModel>();
            var user = MakeUser();
            // Act
            var result = _svc.CalculatePrice(seats, null, user, null, null, null);
            // Assert
            Assert.Equal(0m, result.Subtotal);
            Assert.Equal(0m, result.TotalPrice);
            Assert.Equal(0m, result.SeatTotalAfterDiscounts);
            Assert.Equal(0, result.AddScore);
        }
    }
}
