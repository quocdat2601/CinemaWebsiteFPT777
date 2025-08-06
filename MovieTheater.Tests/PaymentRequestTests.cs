using MovieTheater.Controllers;
using Xunit;

namespace MovieTheater.Tests
{
    public class PaymentRequestTests
    {
        [Fact]
        public void Can_Set_And_Get_Amount()
        {
            var model = new PaymentRequest();
            model.Amount = 100.5m;
            Assert.Equal(100.5m, model.Amount);
        }

        [Fact]
        public void Can_Set_And_Get_OrderInfo()
        {
            var model = new PaymentRequest();
            model.OrderInfo = "Test order info";
            Assert.Equal("Test order info", model.OrderInfo);
        }

        [Fact]
        public void Can_Set_And_Get_OrderId()
        {
            var model = new PaymentRequest();
            model.OrderId = "ORD123";
            Assert.Equal("ORD123", model.OrderId);
        }
    }
}