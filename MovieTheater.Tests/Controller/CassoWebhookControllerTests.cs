using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using MovieTheater.Controllers;
using MovieTheater.Models;
using MovieTheater.Service;
using System.Text.Json;
using Xunit;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;

namespace MovieTheater.Tests.Controller
{
    public class CassoWebhookControllerTests
    {
        private readonly Mock<IInvoiceService> _invoiceService = new();
        private readonly Mock<ISeatService> _seatService = new();
        private readonly Mock<IScheduleSeatService> _scheduleSeatService = new();
        private readonly Mock<ILogger<CassoWebhookController>> _logger = new();

        private CassoWebhookController BuildController()
        {
            var ctrl = new CassoWebhookController(
                _logger.Object,
                _invoiceService.Object,
                _seatService.Object,
                _scheduleSeatService.Object
            );

            var httpContext = new DefaultHttpContext();
            ctrl.ControllerContext = new ControllerContext { HttpContext = httpContext };

            return ctrl;
        }

        private bool GetJsonPropertyBool(object value, string property)
        {
            if (value == null) return false;
            var propertyInfo = value.GetType().GetProperty(property);
            return propertyInfo?.GetValue(value) is bool boolValue && boolValue;
        }

        private string GetJsonPropertyString(object value, string property)
        {
            if (value == null) return string.Empty;
            var propertyInfo = value.GetType().GetProperty(property);
            return propertyInfo?.GetValue(value)?.ToString() ?? string.Empty;
        }

        private int GetJsonPropertyInt(object value, string property)
        {
            if (value == null) return 0;
            var propertyInfo = value.GetType().GetProperty(property);
            return propertyInfo?.GetValue(value) is int intValue ? intValue : 0;
        }

        [Fact]
        public void Ping_ReturnsOk()
        {
            // Arrange
            var ctrl = BuildController();

            // Act
            var result = ctrl.Ping();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal("Webhook endpoint is accessible", okResult.Value);
        }

        [Fact]
        public async Task TestPayment_ReturnsOk_WhenValidParameters()
        {
            // Arrange
            var ctrl = BuildController();
            var orderId = "DH123456";
            var amount = 100000m;

            var invoice = new Invoice
            {
                InvoiceId = orderId,
                Status = InvoiceStatus.Incomplete,
                TotalMoney = amount,
                BookingDate = DateTime.Now
            };

            _invoiceService.Setup(x => x.FindInvoiceByOrderId(orderId)).Returns(invoice);

            // Act
            var result = await ctrl.TestPayment(orderId, amount);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.True(GetJsonPropertyBool(okResult.Value, "success"));
            Assert.Contains("Test payment processed", GetJsonPropertyString(okResult.Value, "message"));
        }

        [Fact]
        public async Task TestPayment_ReturnsBadRequest_WhenExceptionOccurs()
        {
            // Arrange
            var ctrl = BuildController();

            _invoiceService.Setup(x => x.FindInvoiceByOrderId(It.IsAny<string>())).Throws(new Exception("Database error"));

            // Act
            var result = await ctrl.TestPayment("DH123456", 100000m);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.False(GetJsonPropertyBool(badRequestResult.Value, "success"));
        }

        [Fact]
        public async Task HandleWebhook_ReturnsOk_WhenValidV2DataObject()
        {
            // Arrange
            var ctrl = BuildController();
            var webhookData = new
            {
                data = new
                {
                    id = 12345,
                    description = "Thanh toan DH123456",
                    amount = 100000m,
                    type = "IN",
                    accountNumber = "1234567890"
                }
            };

            var jsonString = JsonSerializer.Serialize(webhookData);
            var jsonElement = JsonDocument.Parse(jsonString).RootElement;

            var invoice = new Invoice
            {
                InvoiceId = "DH123456",
                Status = InvoiceStatus.Incomplete,
                TotalMoney = 100000m,
                BookingDate = DateTime.Now
            };

            _invoiceService.Setup(x => x.FindInvoiceByOrderId("DH123456")).Returns(invoice);
            _invoiceService.Setup(x => x.GetById("DH123456")).Returns(invoice);
            _invoiceService.Setup(x => x.Update(It.IsAny<Invoice>()));
            _invoiceService.Setup(x => x.Save());

            // Act
            var result = await ctrl.HandleWebhook(jsonElement);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(0, GetJsonPropertyInt(okResult.Value, "error"));
        }

        [Fact]
        public async Task HandleWebhook_ReturnsOk_WhenValidV2DataArray()
        {
            // Arrange
            var ctrl = BuildController();
            var webhookData = new
            {
                data = new[]
                {
                    new
                    {
                        id = 12345,
                        description = "Thanh toan DH123456",
                        amount = 100000m,
                        type = "IN",
                        accountNumber = "1234567890"
                    }
                }
            };

            var jsonString = JsonSerializer.Serialize(webhookData);
            var jsonElement = JsonDocument.Parse(jsonString).RootElement;

            var invoice = new Invoice
            {
                InvoiceId = "DH123456",
                Status = InvoiceStatus.Incomplete,
                TotalMoney = 100000m,
                BookingDate = DateTime.Now
            };

            _invoiceService.Setup(x => x.FindInvoiceByOrderId("DH123456")).Returns(invoice);
            _invoiceService.Setup(x => x.GetById("DH123456")).Returns(invoice);
            _invoiceService.Setup(x => x.Update(It.IsAny<Invoice>()));
            _invoiceService.Setup(x => x.Save());

            // Act
            var result = await ctrl.HandleWebhook(jsonElement);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(0, GetJsonPropertyInt(okResult.Value, "error"));
        }

        [Fact]
        public async Task HandleWebhook_ReturnsOk_WhenValidJsonArray()
        {
            // Arrange
            var ctrl = BuildController();
            var webhookData = new[]
            {
                new
                {
                    id = 12345,
                    description = "Thanh toan DH123456",
                    amount = 100000m,
                    type = "IN",
                    accountNumber = "1234567890"
                }
            };

            var jsonString = JsonSerializer.Serialize(webhookData);
            var jsonElement = JsonDocument.Parse(jsonString).RootElement;

            var invoice = new Invoice
            {
                InvoiceId = "DH123456",
                Status = InvoiceStatus.Incomplete,
                TotalMoney = 100000m,
                BookingDate = DateTime.Now
            };

            _invoiceService.Setup(x => x.FindInvoiceByOrderId("DH123456")).Returns(invoice);
            _invoiceService.Setup(x => x.GetById("DH123456")).Returns(invoice);
            _invoiceService.Setup(x => x.Update(It.IsAny<Invoice>()));
            _invoiceService.Setup(x => x.Save());

            // Act
            var result = await ctrl.HandleWebhook(jsonElement);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(0, GetJsonPropertyInt(okResult.Value, "error"));
        }

        [Fact]
        public async Task HandleWebhook_ReturnsOk_WhenInvoiceNotFound()
        {
            // Arrange
            var ctrl = BuildController();
            var webhookData = new
            {
                data = new
                {
                    id = 12345,
                    description = "Thanh toan INVALID_ORDER",
                    amount = 100000m,
                    type = "IN",
                    accountNumber = "1234567890"
                }
            };

            var jsonString = JsonSerializer.Serialize(webhookData);
            var jsonElement = JsonDocument.Parse(jsonString).RootElement;

            _invoiceService.Setup(x => x.FindInvoiceByOrderId(It.IsAny<string>())).Returns((Invoice?)null);

            // Act
            var result = await ctrl.HandleWebhook(jsonElement);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(0, GetJsonPropertyInt(okResult.Value, "error"));
        }

        [Fact]
        public async Task HandleWebhook_ReturnsOk_WhenInvoiceAlreadyCompleted()
        {
            // Arrange
            var ctrl = BuildController();
            var webhookData = new
            {
                data = new
                {
                    id = 12345,
                    description = "Thanh toan DH123456",
                    amount = 100000m,
                    type = "IN",
                    accountNumber = "1234567890"
                }
            };

            var jsonString = JsonSerializer.Serialize(webhookData);
            var jsonElement = JsonDocument.Parse(jsonString).RootElement;

            var invoice = new Invoice
            {
                InvoiceId = "DH123456",
                Status = InvoiceStatus.Completed,
                TotalMoney = 100000m,
                BookingDate = DateTime.Now
            };

            _invoiceService.Setup(x => x.FindInvoiceByOrderId("DH123456")).Returns(invoice);

            // Act
            var result = await ctrl.HandleWebhook(jsonElement);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(0, GetJsonPropertyInt(okResult.Value, "error"));
        }

        [Fact]
        public async Task HandleWebhook_ReturnsOk_WhenPaymentAmountIsLessThanExpected()
        {
            // Arrange
            var ctrl = BuildController();
            var webhookData = new
            {
                data = new
                {
                    id = 12345,
                    description = "Thanh toan DH123456",
                    amount = 50000m, // Less than expected
                    type = "IN",
                    accountNumber = "1234567890"
                }
            };

            var jsonString = JsonSerializer.Serialize(webhookData);
            var jsonElement = JsonDocument.Parse(jsonString).RootElement;

            var invoice = new Invoice
            {
                InvoiceId = "DH123456",
                Status = InvoiceStatus.Incomplete,
                TotalMoney = 100000m,
                BookingDate = DateTime.Now
            };

            _invoiceService.Setup(x => x.FindInvoiceByOrderId("DH123456")).Returns(invoice);

            // Act
            var result = await ctrl.HandleWebhook(jsonElement);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(0, GetJsonPropertyInt(okResult.Value, "error"));
        }

        [Fact]
        public async Task HandleWebhook_ReturnsOk_WhenPaymentAmountIsMoreThanExpected()
        {
            // Arrange
            var ctrl = BuildController();
            var webhookData = new
            {
                data = new
                {
                    id = 12345,
                    description = "Thanh toan DH123456",
                    amount = 150000m, // More than expected
                    type = "IN",
                    accountNumber = "1234567890"
                }
            };

            var jsonString = JsonSerializer.Serialize(webhookData);
            var jsonElement = JsonDocument.Parse(jsonString).RootElement;

            var invoice = new Invoice
            {
                InvoiceId = "DH123456",
                Status = InvoiceStatus.Incomplete,
                TotalMoney = 100000m,
                BookingDate = DateTime.Now
            };

            _invoiceService.Setup(x => x.FindInvoiceByOrderId("DH123456")).Returns(invoice);
            _invoiceService.Setup(x => x.GetById("DH123456")).Returns(invoice);
            _invoiceService.Setup(x => x.Update(It.IsAny<Invoice>()));
            _invoiceService.Setup(x => x.Save());

            // Act
            var result = await ctrl.HandleWebhook(jsonElement);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(0, GetJsonPropertyInt(okResult.Value, "error"));
        }

        [Fact]
        public async Task HandleWebhook_ReturnsOk_WhenExceptionOccurs()
        {
            // Arrange
            var ctrl = BuildController();
            var webhookData = new
            {
                data = new
                {
                    id = 12345,
                    description = "Thanh toan DH123456",
                    amount = 100000m,
                    type = "IN",
                    accountNumber = "1234567890"
                }
            };

            var jsonString = JsonSerializer.Serialize(webhookData);
            var jsonElement = JsonDocument.Parse(jsonString).RootElement;

            _invoiceService.Setup(x => x.FindInvoiceByOrderId(It.IsAny<string>())).Throws(new Exception("Database error"));

            // Act
            var result = await ctrl.HandleWebhook(jsonElement);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(0, GetJsonPropertyInt(okResult.Value, "error"));
        }

        [Fact]
        public async Task HandleWebhook_ReturnsBadRequest_WhenRequestTooLarge()
        {
            // Arrange
            var ctrl = BuildController();
            var webhookData = new { data = "large_data" };
            var jsonString = JsonSerializer.Serialize(webhookData);
            var jsonElement = JsonDocument.Parse(jsonString).RootElement;

            // Simulate large request
            var httpContext = new DefaultHttpContext();
            httpContext.Request.ContentLength = 2 * 1024 * 1024; // 2MB > 1MB limit
            ctrl.ControllerContext = new ControllerContext { HttpContext = httpContext };

            // Act
            var result = await ctrl.HandleWebhook(jsonElement);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal(1, GetJsonPropertyInt(badRequestResult.Value, "error"));
        }

        [Fact]
        public async Task HandleWebhook_ReturnsOk_WhenInvoiceFoundByPattern()
        {
            // Arrange
            var ctrl = BuildController();
            var webhookData = new
            {
                data = new
                {
                    id = 12345,
                    description = "Thanh toan QADOBF123456 cho ve xem phim",
                    amount = 100000m,
                    type = "IN",
                    accountNumber = "1234567890"
                }
            };

            var jsonString = JsonSerializer.Serialize(webhookData);
            var jsonElement = JsonDocument.Parse(jsonString).RootElement;

            var invoice = new Invoice
            {
                InvoiceId = "QADOBF123456",
                Status = InvoiceStatus.Incomplete,
                TotalMoney = 100000m,
                BookingDate = DateTime.Now
            };

            _invoiceService.Setup(x => x.FindInvoiceByOrderId("QADOBF123456")).Returns(invoice);
            _invoiceService.Setup(x => x.GetById("QADOBF123456")).Returns(invoice);
            _invoiceService.Setup(x => x.Update(It.IsAny<Invoice>()));
            _invoiceService.Setup(x => x.Save());

            // Act
            var result = await ctrl.HandleWebhook(jsonElement);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(0, GetJsonPropertyInt(okResult.Value, "error"));
        }

        [Fact]
        public async Task HandleWebhook_ReturnsOk_WhenInvoiceFoundByAmount()
        {
            // Arrange
            var ctrl = BuildController();
            var webhookData = new
            {
                data = new
                {
                    id = 12345,
                    description = "Thanh toan cho ve xem phim",
                    amount = 100000m,
                    type = "IN",
                    accountNumber = "1234567890"
                }
            };

            var jsonString = JsonSerializer.Serialize(webhookData);
            var jsonElement = JsonDocument.Parse(jsonString).RootElement;

            var invoice = new Invoice
            {
                InvoiceId = "DH123456",
                Status = InvoiceStatus.Incomplete,
                TotalMoney = 100000m,
                BookingDate = DateTime.Now
            };

            _invoiceService.Setup(x => x.FindInvoiceByOrderId(It.IsAny<string>())).Returns((Invoice?)null);
            _invoiceService.Setup(x => x.FindInvoiceByAmountAndTime(100000m, null)).Returns(invoice);
            _invoiceService.Setup(x => x.GetById("DH123456")).Returns(invoice);
            _invoiceService.Setup(x => x.Update(It.IsAny<Invoice>()));
            _invoiceService.Setup(x => x.Save());

            // Act
            var result = await ctrl.HandleWebhook(jsonElement);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(0, GetJsonPropertyInt(okResult.Value, "error"));
        }

        [Fact]
        public async Task HandleWebhook_ReturnsOk_WhenInvoiceFoundByAmountAndRecentTime()
        {
            // Arrange
            var ctrl = BuildController();
            var webhookData = new
            {
                data = new
                {
                    id = 12345,
                    description = "Thanh toan cho ve xem phim",
                    amount = 100000m,
                    type = "IN",
                    accountNumber = "1234567890"
                }
            };

            var jsonString = JsonSerializer.Serialize(webhookData);
            var jsonElement = JsonDocument.Parse(jsonString).RootElement;

            var invoice = new Invoice
            {
                InvoiceId = "DH123456",
                Status = InvoiceStatus.Incomplete,
                TotalMoney = 100000m,
                BookingDate = DateTime.Now.AddHours(-1)
            };

            _invoiceService.Setup(x => x.FindInvoiceByOrderId(It.IsAny<string>())).Returns((Invoice?)null);
            _invoiceService.Setup(x => x.FindInvoiceByAmountAndTime(100000m, null)).Returns((Invoice?)null);
            _invoiceService.Setup(x => x.FindInvoiceByAmountAndTime(100000m, It.IsAny<DateTime?>())).Returns(invoice);
            _invoiceService.Setup(x => x.GetById("DH123456")).Returns(invoice);
            _invoiceService.Setup(x => x.Update(It.IsAny<Invoice>()));
            _invoiceService.Setup(x => x.Save());

            // Act
            var result = await ctrl.HandleWebhook(jsonElement);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(0, GetJsonPropertyInt(okResult.Value, "error"));
        }

        [Fact]
        public async Task HandleWebhook_ReturnsOk_WhenSeatsUpdatedToBooked()
        {
            // Arrange
            var ctrl = BuildController();
            var webhookData = new
            {
                data = new
                {
                    id = 12345,
                    description = "Thanh toan DH123456",
                    amount = 100000m,
                    type = "IN",
                    accountNumber = "1234567890"
                }
            };

            var jsonString = JsonSerializer.Serialize(webhookData);
            var jsonElement = JsonDocument.Parse(jsonString).RootElement;

            var invoice = new Invoice
            {
                InvoiceId = "DH123456",
                Status = InvoiceStatus.Incomplete,
                TotalMoney = 100000m,
                BookingDate = DateTime.Now,
                SeatIds = "1,2,3",
                MovieShowId = 1
            };

            _invoiceService.Setup(x => x.FindInvoiceByOrderId("DH123456")).Returns(invoice);
            _invoiceService.Setup(x => x.GetById("DH123456")).Returns(invoice);
            _invoiceService.Setup(x => x.Update(It.IsAny<Invoice>()));
            _invoiceService.Setup(x => x.Save());
            _seatService.Setup(x => x.UpdateSeatsStatusToBookedAsync(It.IsAny<List<int>>())).Returns(Task.CompletedTask);
            _scheduleSeatService.Setup(x => x.UpdateScheduleSeatsToBookedAsync("DH123456", 1, It.IsAny<List<int>>())).Returns(Task.CompletedTask);

            // Act
            var result = await ctrl.HandleWebhook(jsonElement);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(0, GetJsonPropertyInt(okResult.Value, "error"));
        }

        [Fact]
        public async Task HandleWebhook_ReturnsOk_WhenInvoiceHasNullSeatIds()
        {
            // Arrange
            var ctrl = BuildController();
            var webhookData = new
            {
                data = new
                {
                    id = 12345,
                    description = "Thanh toan DH123456",
                    amount = 100000m,
                    type = "IN",
                    accountNumber = "1234567890"
                }
            };

            var jsonString = JsonSerializer.Serialize(webhookData);
            var jsonElement = JsonDocument.Parse(jsonString).RootElement;

            var invoice = new Invoice
            {
                InvoiceId = "DH123456",
                Status = InvoiceStatus.Incomplete,
                TotalMoney = 100000m,
                BookingDate = DateTime.Now,
                SeatIds = null
            };

            _invoiceService.Setup(x => x.FindInvoiceByOrderId("DH123456")).Returns(invoice);
            _invoiceService.Setup(x => x.GetById("DH123456")).Returns(invoice);
            _invoiceService.Setup(x => x.Update(It.IsAny<Invoice>()));
            _invoiceService.Setup(x => x.Save());

            // Act
            var result = await ctrl.HandleWebhook(jsonElement);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(0, GetJsonPropertyInt(okResult.Value, "error"));
        }

        [Fact]
        public async Task HandleWebhook_ReturnsOk_WhenInvoiceHasEmptySeatIds()
        {
            // Arrange
            var ctrl = BuildController();
            var webhookData = new
            {
                data = new
                {
                    id = 12345,
                    description = "Thanh toan DH123456",
                    amount = 100000m,
                    type = "IN",
                    accountNumber = "1234567890"
                }
            };

            var jsonString = JsonSerializer.Serialize(webhookData);
            var jsonElement = JsonDocument.Parse(jsonString).RootElement;

            var invoice = new Invoice
            {
                InvoiceId = "DH123456",
                Status = InvoiceStatus.Incomplete,
                TotalMoney = 100000m,
                BookingDate = DateTime.Now,
                SeatIds = ""
            };

            _invoiceService.Setup(x => x.FindInvoiceByOrderId("DH123456")).Returns(invoice);
            _invoiceService.Setup(x => x.GetById("DH123456")).Returns(invoice);
            _invoiceService.Setup(x => x.Update(It.IsAny<Invoice>()));
            _invoiceService.Setup(x => x.Save());

            // Act
            var result = await ctrl.HandleWebhook(jsonElement);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(0, GetJsonPropertyInt(okResult.Value, "error"));
        }

        [Fact]
        public async Task HandleWebhook_ReturnsOk_WhenInvoiceHasInvalidSeatIds()
        {
            // Arrange
            var ctrl = BuildController();
            var webhookData = new
            {
                data = new
                {
                    id = 12345,
                    description = "Thanh toan DH123456",
                    amount = 100000m,
                    type = "IN",
                    accountNumber = "1234567890"
                }
            };

            var jsonString = JsonSerializer.Serialize(webhookData);
            var jsonElement = JsonDocument.Parse(jsonString).RootElement;

            var invoice = new Invoice
            {
                InvoiceId = "DH123456",
                Status = InvoiceStatus.Incomplete,
                TotalMoney = 100000m,
                BookingDate = DateTime.Now,
                SeatIds = "invalid,seat,ids"
            };

            _invoiceService.Setup(x => x.FindInvoiceByOrderId("DH123456")).Returns(invoice);
            _invoiceService.Setup(x => x.GetById("DH123456")).Returns(invoice);
            _invoiceService.Setup(x => x.Update(It.IsAny<Invoice>()));
            _invoiceService.Setup(x => x.Save());

            // Act
            var result = await ctrl.HandleWebhook(jsonElement);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(0, GetJsonPropertyInt(okResult.Value, "error"));
        }

        [Fact]
        public async Task HandleWebhook_ReturnsOk_WhenSeatServiceThrowsException()
        {
            // Arrange
            var ctrl = BuildController();
            var webhookData = new
            {
                data = new
                {
                    id = 12345,
                    description = "Thanh toan DH123456",
                    amount = 100000m,
                    type = "IN",
                    accountNumber = "1234567890"
                }
            };

            var jsonString = JsonSerializer.Serialize(webhookData);
            var jsonElement = JsonDocument.Parse(jsonString).RootElement;

            var invoice = new Invoice
            {
                InvoiceId = "DH123456",
                Status = InvoiceStatus.Incomplete,
                TotalMoney = 100000m,
                BookingDate = DateTime.Now,
                SeatIds = "1,2,3",
                MovieShowId = 1
            };

            _invoiceService.Setup(x => x.FindInvoiceByOrderId("DH123456")).Returns(invoice);
            _invoiceService.Setup(x => x.GetById("DH123456")).Returns(invoice);
            _invoiceService.Setup(x => x.Update(It.IsAny<Invoice>()));
            _invoiceService.Setup(x => x.Save());
            _seatService.Setup(x => x.UpdateSeatsStatusToBookedAsync(It.IsAny<List<int>>())).Throws(new Exception("Seat service error"));

            // Act
            var result = await ctrl.HandleWebhook(jsonElement);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(0, GetJsonPropertyInt(okResult.Value, "error"));
        }

        [Fact]
        public async Task HandleWebhook_ReturnsOk_WhenDatabaseTimeout()
        {
            // Arrange
            var ctrl = BuildController();
            var webhookData = new
            {
                data = new
                {
                    id = 12345,
                    description = "Thanh toan DH123456",
                    amount = 100000m,
                    type = "IN",
                    accountNumber = "1234567890"
                }
            };

            var jsonString = JsonSerializer.Serialize(webhookData);
            var jsonElement = JsonDocument.Parse(jsonString).RootElement;

            _invoiceService.Setup(x => x.FindInvoiceByOrderId(It.IsAny<string>())).Throws(new TaskCanceledException("Database timeout"));

            // Act
            var result = await ctrl.HandleWebhook(jsonElement);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(0, GetJsonPropertyInt(okResult.Value, "error"));
        }

        [Fact]
        public async Task HandleWebhook_ReturnsOk_WhenInvalidJsonStructure()
        {
            // Arrange
            var ctrl = BuildController();
            var webhookData = new
            {
                invalid = "structure"
            };

            var jsonString = JsonSerializer.Serialize(webhookData);
            var jsonElement = JsonDocument.Parse(jsonString).RootElement;

            // Act
            var result = await ctrl.HandleWebhook(jsonElement);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(0, GetJsonPropertyInt(okResult.Value, "error"));
        }

        [Fact]
        public async Task HandleWebhook_ReturnsOk_WhenEmptyDataArray()
        {
            // Arrange
            var ctrl = BuildController();
            var webhookData = new
            {
                data = new object[] { }
            };

            var jsonString = JsonSerializer.Serialize(webhookData);
            var jsonElement = JsonDocument.Parse(jsonString).RootElement;

            // Act
            var result = await ctrl.HandleWebhook(jsonElement);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(0, GetJsonPropertyInt(okResult.Value, "error"));
        }

        [Fact]
        public async Task HandleWebhook_ReturnsOk_WhenNoCassoId()
        {
            // Arrange
            var ctrl = BuildController();
            var webhookData = new
            {
                data = new
                {
                    description = "Thanh toan DH123456",
                    amount = 100000m,
                    type = "IN",
                    accountNumber = "1234567890"
                }
            };

            var jsonString = JsonSerializer.Serialize(webhookData);
            var jsonElement = JsonDocument.Parse(jsonString).RootElement;

            var invoice = new Invoice
            {
                InvoiceId = "DH123456",
                Status = InvoiceStatus.Incomplete,
                TotalMoney = 100000m,
                BookingDate = DateTime.Now
            };

            _invoiceService.Setup(x => x.FindInvoiceByOrderId("DH123456")).Returns(invoice);
            _invoiceService.Setup(x => x.GetById("DH123456")).Returns(invoice);
            _invoiceService.Setup(x => x.Update(It.IsAny<Invoice>()));
            _invoiceService.Setup(x => x.Save());

            // Act
            var result = await ctrl.HandleWebhook(jsonElement);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(0, GetJsonPropertyInt(okResult.Value, "error"));
        }

        [Fact]
        public async Task HandleWebhook_ReturnsOk_WhenReferenceTakesPriority()
        {
            // Arrange
            var ctrl = BuildController();
            var webhookData = new
            {
                data = new
                {
                    id = 12345,
                    description = "Thanh toan cho ve xem phim",
                    reference = "DH123456",
                    amount = 100000m,
                    type = "IN",
                    accountNumber = "1234567890"
                }
            };

            var jsonString = JsonSerializer.Serialize(webhookData);
            var jsonElement = JsonDocument.Parse(jsonString).RootElement;

            var invoice = new Invoice
            {
                InvoiceId = "DH123456",
                Status = InvoiceStatus.Incomplete,
                TotalMoney = 100000m,
                BookingDate = DateTime.Now
            };

            _invoiceService.Setup(x => x.FindInvoiceByOrderId("DH123456")).Returns(invoice);
            _invoiceService.Setup(x => x.GetById("DH123456")).Returns(invoice);
            _invoiceService.Setup(x => x.Update(It.IsAny<Invoice>()));
            _invoiceService.Setup(x => x.Save());

            // Act
            var result = await ctrl.HandleWebhook(jsonElement);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(0, GetJsonPropertyInt(okResult.Value, "error"));
        }

        [Fact]
        public async Task HandleWebhook_ReturnsOk_WhenExtractOrderIdFromDescription()
        {
            // Arrange
            var ctrl = BuildController();
            var webhookData = new
            {
                data = new
                {
                    id = 12345,
                    description = "Thanh toan DH123456 cho ve xem phim",
                    amount = 100000m,
                    type = "IN",
                    accountNumber = "1234567890"
                }
            };

            var jsonString = JsonSerializer.Serialize(webhookData);
            var jsonElement = JsonDocument.Parse(jsonString).RootElement;

            var invoice = new Invoice
            {
                InvoiceId = "DH123456",
                Status = InvoiceStatus.Incomplete,
                TotalMoney = 100000m,
                BookingDate = DateTime.Now
            };

            _invoiceService.Setup(x => x.FindInvoiceByOrderId("DH123456")).Returns(invoice);
            _invoiceService.Setup(x => x.GetById("DH123456")).Returns(invoice);
            _invoiceService.Setup(x => x.Update(It.IsAny<Invoice>()));
            _invoiceService.Setup(x => x.Save());

            // Act
            var result = await ctrl.HandleWebhook(jsonElement);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(0, GetJsonPropertyInt(okResult.Value, "error"));
        }

        [Fact]
        public async Task HandleWebhook_ReturnsOk_WhenExtractOrderIdFromInvPattern()
        {
            // Arrange
            var ctrl = BuildController();
            var webhookData = new
            {
                data = new
                {
                    id = 12345,
                    description = "Thanh toan INV123456 cho ve xem phim",
                    amount = 100000m,
                    type = "IN",
                    accountNumber = "1234567890"
                }
            };

            var jsonString = JsonSerializer.Serialize(webhookData);
            var jsonElement = JsonDocument.Parse(jsonString).RootElement;

            var invoice = new Invoice
            {
                InvoiceId = "INV123456",
                Status = InvoiceStatus.Incomplete,
                TotalMoney = 100000m,
                BookingDate = DateTime.Now
            };

            _invoiceService.Setup(x => x.FindInvoiceByOrderId("INV123456")).Returns(invoice);
            _invoiceService.Setup(x => x.GetById("INV123456")).Returns(invoice);
            _invoiceService.Setup(x => x.Update(It.IsAny<Invoice>()));
            _invoiceService.Setup(x => x.Save());

            // Act
            var result = await ctrl.HandleWebhook(jsonElement);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(0, GetJsonPropertyInt(okResult.Value, "error"));
        }

        [Fact]
        public async Task HandleWebhook_ReturnsOk_WhenCaseInsensitiveOrderId()
        {
            // Arrange
            var ctrl = BuildController();
            var webhookData = new
            {
                data = new
                {
                    id = 12345,
                    description = "Thanh toan dh123456 cho ve xem phim",
                    amount = 100000m,
                    type = "IN",
                    accountNumber = "1234567890"
                }
            };

            var jsonString = JsonSerializer.Serialize(webhookData);
            var jsonElement = JsonDocument.Parse(jsonString).RootElement;

            var invoice = new Invoice
            {
                InvoiceId = "DH123456",
                Status = InvoiceStatus.Incomplete,
                TotalMoney = 100000m,
                BookingDate = DateTime.Now
            };

            _invoiceService.Setup(x => x.FindInvoiceByOrderId("dh123456")).Returns(invoice);
            _invoiceService.Setup(x => x.GetById("DH123456")).Returns(invoice);
            _invoiceService.Setup(x => x.Update(It.IsAny<Invoice>()));
            _invoiceService.Setup(x => x.Save());

            // Act
            var result = await ctrl.HandleWebhook(jsonElement);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(0, GetJsonPropertyInt(okResult.Value, "error"));
        }

        [Fact]
        public async Task HandleWebhook_ReturnsOk_WhenMultipleTransactions()
        {
            // Arrange
            var ctrl = BuildController();
            var webhookData = new
            {
                data = new[]
                {
                    new
                    {
                        id = 12345,
                        description = "Thanh toan DH123456",
                        amount = 100000m,
                        type = "IN",
                        accountNumber = "1234567890"
                    },
                    new
                    {
                        id = 12346,
                        description = "Thanh toan DH123457",
                        amount = 150000m,
                        type = "IN",
                        accountNumber = "1234567890"
                    }
                }
            };

            var jsonString = JsonSerializer.Serialize(webhookData);
            var jsonElement = JsonDocument.Parse(jsonString).RootElement;

            var invoice1 = new Invoice
            {
                InvoiceId = "DH123456",
                Status = InvoiceStatus.Incomplete,
                TotalMoney = 100000m,
                BookingDate = DateTime.Now
            };

            var invoice2 = new Invoice
            {
                InvoiceId = "DH123457",
                Status = InvoiceStatus.Incomplete,
                TotalMoney = 150000m,
                BookingDate = DateTime.Now
            };

            _invoiceService.Setup(x => x.FindInvoiceByOrderId("DH123456")).Returns(invoice1);
            _invoiceService.Setup(x => x.FindInvoiceByOrderId("DH123457")).Returns(invoice2);
            _invoiceService.Setup(x => x.GetById("DH123456")).Returns(invoice1);
            _invoiceService.Setup(x => x.GetById("DH123457")).Returns(invoice2);
            _invoiceService.Setup(x => x.Update(It.IsAny<Invoice>()));
            _invoiceService.Setup(x => x.Save());

            // Act
            var result = await ctrl.HandleWebhook(jsonElement);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(0, GetJsonPropertyInt(okResult.Value, "error"));
        }

        [Fact]
        public async Task HandleWebhook_ReturnsOk_WhenAmountWithTolerance()
        {
            // Arrange
            var ctrl = BuildController();
            var webhookData = new
            {
                data = new
                {
                    id = 12345,
                    description = "Thanh toan DH123456",
                    amount = 95000m, // Within tolerance of 100000
                    type = "IN",
                    accountNumber = "1234567890"
                }
            };

            var jsonString = JsonSerializer.Serialize(webhookData);
            var jsonElement = JsonDocument.Parse(jsonString).RootElement;

            var invoice = new Invoice
            {
                InvoiceId = "DH123456",
                Status = InvoiceStatus.Incomplete,
                TotalMoney = 100000m,
                BookingDate = DateTime.Now
            };

            _invoiceService.Setup(x => x.FindInvoiceByOrderId("DH123456")).Returns(invoice);
            _invoiceService.Setup(x => x.GetById("DH123456")).Returns(invoice);
            _invoiceService.Setup(x => x.Update(It.IsAny<Invoice>()));
            _invoiceService.Setup(x => x.Save());

            // Act
            var result = await ctrl.HandleWebhook(jsonElement);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(0, GetJsonPropertyInt(okResult.Value, "error"));
        }

        [Fact]
        public async Task HandleWebhook_ReturnsOk_WhenAmountExceedsTolerance()
        {
            // Arrange
            var ctrl = BuildController();
            var webhookData = new
            {
                data = new
                {
                    id = 12345,
                    description = "Thanh toan DH123456",
                    amount = 80000m, // Below tolerance of 100000
                    type = "IN",
                    accountNumber = "1234567890"
                }
            };

            var jsonString = JsonSerializer.Serialize(webhookData);
            var jsonElement = JsonDocument.Parse(jsonString).RootElement;

            var invoice = new Invoice
            {
                InvoiceId = "DH123456",
                Status = InvoiceStatus.Incomplete,
                TotalMoney = 100000m,
                BookingDate = DateTime.Now
            };

            _invoiceService.Setup(x => x.FindInvoiceByOrderId("DH123456")).Returns(invoice);

            // Act
            var result = await ctrl.HandleWebhook(jsonElement);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(0, GetJsonPropertyInt(okResult.Value, "error"));
        }
    }
} 