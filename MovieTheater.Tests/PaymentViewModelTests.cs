using MovieTheater.ViewModels;
using System;
using Xunit;

namespace MovieTheater.Tests
{
    public class PaymentViewModelTests
    {
        [Fact]
        public void Can_Set_And_Get_MovieName()
        {
            var model = new PaymentViewModel();
            model.MovieName = "Test Movie";
            Assert.Equal("Test Movie", model.MovieName);
        }

        [Fact]
        public void Can_Set_And_Get_ShowDate()
        {
            var model = new PaymentViewModel();
            var date = DateOnly.FromDateTime(DateTime.Today);
            model.ShowDate = date;
            Assert.Equal(date, model.ShowDate);
        }

        [Fact]
        public void Can_Set_And_Get_ShowTime()
        {
            var model = new PaymentViewModel();
            model.ShowTime = "18:00";
            Assert.Equal("18:00", model.ShowTime);
        }

        [Fact]
        public void Can_Set_And_Get_TotalAmount()
        {
            var model = new PaymentViewModel();
            model.TotalAmount = 123.45m;
            Assert.Equal(123.45m, model.TotalAmount);
        }

        [Fact]
        public void Can_Set_And_Get_TotalFoodPrice()
        {
            var model = new PaymentViewModel();
            model.TotalFoodPrice = 50.5m;
            Assert.Equal(50.5m, model.TotalFoodPrice);
        }

        [Fact]
        public void Can_Set_And_Get_TotalSeatPrice()
        {
            var model = new PaymentViewModel();
            model.TotalSeatPrice = 73.2m;
            Assert.Equal(73.2m, model.TotalSeatPrice);
        }
    }
} 