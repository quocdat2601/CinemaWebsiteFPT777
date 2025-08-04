using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using MovieTheater.Controllers;
using MovieTheater.Models;
using MovieTheater.Service;
using System.Security.Claims;
using System.Text.Json;
using Xunit;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Linq.Expressions;
using System;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace MovieTheater.Tests.Controller
{
    public class CassoWebhookControllerTests
    {
        private readonly Mock<IInvoiceService> _invoiceService = new();
        private readonly Mock<ILogger<CassoWebhookController>> _logger = new();
        private readonly Mock<MovieTheaterContext> _context = new();

        private CassoWebhookController BuildController()
        {
            var ctrl = new CassoWebhookController(
                _context.Object,
                _logger.Object,
                _invoiceService.Object
            );

            var httpContext = new DefaultHttpContext();
            var serviceProvider = new Mock<IServiceProvider>();
            serviceProvider.Setup(x => x.GetService(typeof(MovieTheaterContext))).Returns(_context.Object);
            httpContext.RequestServices = serviceProvider.Object;

            ctrl.ControllerContext = new ControllerContext { HttpContext = httpContext };

            return ctrl;
        }

        private Mock<DbSet<T>> CreateMockDbSet<T>(List<T> list) where T : class
        {
            var mockSet = new Mock<DbSet<T>>();
            var queryable = list.AsQueryable();
            mockSet.As<IQueryable<T>>().Setup(m => m.Provider).Returns(queryable.Provider);
            mockSet.As<IQueryable<T>>().Setup(m => m.Expression).Returns(queryable.Expression);
            mockSet.As<IQueryable<T>>().Setup(m => m.ElementType).Returns(queryable.ElementType);
            mockSet.As<IQueryable<T>>().Setup(m => m.GetEnumerator()).Returns(queryable.GetEnumerator());
            return mockSet;
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

            var mockInvoices = CreateMockDbSet(new List<Invoice> { invoice });
            var mockSeatStatuses = CreateMockDbSet(new List<SeatStatus> 
            { 
                new SeatStatus { SeatStatusId = 2, StatusName = "Booked" } 
            });

            _context.Setup(c => c.Invoices).Returns(mockInvoices.Object);
            _context.Setup(c => c.SeatStatuses).Returns(mockSeatStatuses.Object);

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

            _context.Setup(c => c.Invoices).Throws(new Exception("Database error"));

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

            var mockInvoices = CreateMockDbSet(new List<Invoice> { invoice });
            var mockSeatStatuses = CreateMockDbSet(new List<SeatStatus> 
            { 
                new SeatStatus { SeatStatusId = 2, StatusName = "Booked" } 
            });

            _context.Setup(c => c.Invoices).Returns(mockInvoices.Object);
            _context.Setup(c => c.SeatStatuses).Returns(mockSeatStatuses.Object);

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

            var mockInvoices = CreateMockDbSet(new List<Invoice> { invoice });
            var mockSeatStatuses = CreateMockDbSet(new List<SeatStatus> 
            { 
                new SeatStatus { SeatStatusId = 2, StatusName = "Booked" } 
            });

            _context.Setup(c => c.Invoices).Returns(mockInvoices.Object);
            _context.Setup(c => c.SeatStatuses).Returns(mockSeatStatuses.Object);

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

            var mockInvoices = CreateMockDbSet(new List<Invoice> { invoice });
            var mockSeatStatuses = CreateMockDbSet(new List<SeatStatus> 
            { 
                new SeatStatus { SeatStatusId = 2, StatusName = "Booked" } 
            });

            _context.Setup(c => c.Invoices).Returns(mockInvoices.Object);
            _context.Setup(c => c.SeatStatuses).Returns(mockSeatStatuses.Object);

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

            var mockInvoices = CreateMockDbSet(new List<Invoice>());

            _context.Setup(c => c.Invoices).Returns(mockInvoices.Object);

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

            var mockInvoices = CreateMockDbSet(new List<Invoice> { invoice });

            _context.Setup(c => c.Invoices).Returns(mockInvoices.Object);

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

            var mockInvoices = CreateMockDbSet(new List<Invoice> { invoice });

            _context.Setup(c => c.Invoices).Returns(mockInvoices.Object);

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

            var mockInvoices = CreateMockDbSet(new List<Invoice> { invoice });
            var mockSeatStatuses = CreateMockDbSet(new List<SeatStatus> 
            { 
                new SeatStatus { SeatStatusId = 2, StatusName = "Booked" } 
            });

            _context.Setup(c => c.Invoices).Returns(mockInvoices.Object);
            _context.Setup(c => c.SeatStatuses).Returns(mockSeatStatuses.Object);

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

            _context.Setup(c => c.Invoices).Throws(new Exception("Database error"));

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
        public async Task HandleWebhook_ReturnsOk_WhenOrderIdExtractedFromDescription()
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

            var mockInvoices = CreateMockDbSet(new List<Invoice> { invoice });
            var mockSeatStatuses = CreateMockDbSet(new List<SeatStatus> 
            { 
                new SeatStatus { SeatStatusId = 2, StatusName = "Booked" } 
            });

            _context.Setup(c => c.Invoices).Returns(mockInvoices.Object);
            _context.Setup(c => c.SeatStatuses).Returns(mockSeatStatuses.Object);

            // Act
            var result = await ctrl.HandleWebhook(jsonElement);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(0, GetJsonPropertyInt(okResult.Value, "error"));
        }

        [Fact]
        public async Task HandleWebhook_ReturnsOk_WhenOrderIdExtractedFromReference()
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

            var mockInvoices = CreateMockDbSet(new List<Invoice> { invoice });
            var mockSeatStatuses = CreateMockDbSet(new List<SeatStatus> 
            { 
                new SeatStatus { SeatStatusId = 2, StatusName = "Booked" } 
            });

            _context.Setup(c => c.Invoices).Returns(mockInvoices.Object);
            _context.Setup(c => c.SeatStatuses).Returns(mockSeatStatuses.Object);

            // Act
            var result = await ctrl.HandleWebhook(jsonElement);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(0, GetJsonPropertyInt(okResult.Value, "error"));
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
                InvoiceId = "DH123456",
                Status = InvoiceStatus.Incomplete,
                TotalMoney = 100000m,
                BookingDate = DateTime.Now
            };

            var mockInvoices = CreateMockDbSet(new List<Invoice> { invoice });
            var mockSeatStatuses = CreateMockDbSet(new List<SeatStatus> 
            { 
                new SeatStatus { SeatStatusId = 2, StatusName = "Booked" } 
            });

            _context.Setup(c => c.Invoices).Returns(mockInvoices.Object);
            _context.Setup(c => c.SeatStatuses).Returns(mockSeatStatuses.Object);

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

            var mockInvoices = CreateMockDbSet(new List<Invoice> { invoice });
            var mockSeatStatuses = CreateMockDbSet(new List<SeatStatus> 
            { 
                new SeatStatus { SeatStatusId = 2, StatusName = "Booked" } 
            });

            _context.Setup(c => c.Invoices).Returns(mockInvoices.Object);
            _context.Setup(c => c.SeatStatuses).Returns(mockSeatStatuses.Object);

            // Act
            var result = await ctrl.HandleWebhook(jsonElement);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(0, GetJsonPropertyInt(okResult.Value, "error"));
        }

        [Fact]
        public async Task HandleWebhook_ReturnsOk_WhenInvoiceFoundByRecentTime()
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
                BookingDate = DateTime.Now.AddHours(-1) // Recent invoice
            };

            var mockInvoices = CreateMockDbSet(new List<Invoice> { invoice });
            var mockSeatStatuses = CreateMockDbSet(new List<SeatStatus> 
            { 
                new SeatStatus { SeatStatusId = 2, StatusName = "Booked" } 
            });

            _context.Setup(c => c.Invoices).Returns(mockInvoices.Object);
            _context.Setup(c => c.SeatStatuses).Returns(mockSeatStatuses.Object);

            // Act
            var result = await ctrl.HandleWebhook(jsonElement);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(0, GetJsonPropertyInt(okResult.Value, "error"));
        }

        [Fact]
        public async Task HandleWebhook_ReturnsOk_WhenInvoiceHasSeatIds()
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

            var mockInvoices = CreateMockDbSet(new List<Invoice> { invoice });
            var mockSeatStatuses = CreateMockDbSet(new List<SeatStatus> 
            { 
                new SeatStatus { SeatStatusId = 2, StatusName = "Booked" } 
            });
            var mockSeats = CreateMockDbSet(new List<Seat> 
            { 
                new Seat { SeatId = 1 },
                new Seat { SeatId = 2 },
                new Seat { SeatId = 3 }
            });
            var mockScheduleSeats = CreateMockDbSet(new List<ScheduleSeat> 
            { 
                new ScheduleSeat { SeatId = 1, MovieShowId = 1 },
                new ScheduleSeat { SeatId = 2, MovieShowId = 1 },
                new ScheduleSeat { SeatId = 3, MovieShowId = 1 }
            });

            _context.Setup(c => c.Invoices).Returns(mockInvoices.Object);
            _context.Setup(c => c.SeatStatuses).Returns(mockSeatStatuses.Object);
            _context.Setup(c => c.Seats).Returns(mockSeats.Object);
            _context.Setup(c => c.ScheduleSeats).Returns(mockScheduleSeats.Object);

            // Act
            var result = await ctrl.HandleWebhook(jsonElement);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(0, GetJsonPropertyInt(okResult.Value, "error"));
        }

        [Fact]
        public async Task HandleWebhook_ReturnsOk_WhenInvoiceHasNullTotalMoney()
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
                TotalMoney = null, // Null total money
                BookingDate = DateTime.Now
            };

            var mockInvoices = CreateMockDbSet(new List<Invoice> { invoice });

            _context.Setup(c => c.Invoices).Returns(mockInvoices.Object);

            // Act
            var result = await ctrl.HandleWebhook(jsonElement);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(0, GetJsonPropertyInt(okResult.Value, "error"));
        }

        [Fact]
        public async Task HandleWebhook_ReturnsOk_WhenNoOrderIdFound()
        {
            // Arrange
            var ctrl = BuildController();
            var webhookData = new
            {
                data = new
                {
                    id = 12345,
                    description = "Thanh toan cho ve xem phim", // No order ID in description
                    amount = 100000m,
                    type = "IN",
                    accountNumber = "1234567890"
                }
            };

            var jsonString = JsonSerializer.Serialize(webhookData);
            var jsonElement = JsonDocument.Parse(jsonString).RootElement;

            var mockInvoices = CreateMockDbSet(new List<Invoice>());

            _context.Setup(c => c.Invoices).Returns(mockInvoices.Object);

            // Act
            var result = await ctrl.HandleWebhook(jsonElement);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(0, GetJsonPropertyInt(okResult.Value, "error"));
        }

        [Fact]
        public async Task HandleWebhook_ReturnsOk_WhenInvalidSecureToken()
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

            // Set invalid secure token in headers
            var httpContext = new DefaultHttpContext();
            httpContext.Request.Headers["Secure-Token"] = "invalid_token";
            ctrl.ControllerContext = new ControllerContext { HttpContext = httpContext };

            var invoice = new Invoice
            {
                InvoiceId = "DH123456",
                Status = InvoiceStatus.Incomplete,
                TotalMoney = 100000m,
                BookingDate = DateTime.Now
            };

            var mockInvoices = CreateMockDbSet(new List<Invoice> { invoice });

            _context.Setup(c => c.Invoices).Returns(mockInvoices.Object);

            // Act
            var result = await ctrl.HandleWebhook(jsonElement);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(0, GetJsonPropertyInt(okResult.Value, "error"));
        }

        [Fact]
        public async Task HandleWebhook_ReturnsOk_WhenNoSecureTokenHeader()
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

            // No secure token header
            var httpContext = new DefaultHttpContext();
            ctrl.ControllerContext = new ControllerContext { HttpContext = httpContext };

            var invoice = new Invoice
            {
                InvoiceId = "DH123456",
                Status = InvoiceStatus.Incomplete,
                TotalMoney = 100000m,
                BookingDate = DateTime.Now
            };

            var mockInvoices = CreateMockDbSet(new List<Invoice> { invoice });

            _context.Setup(c => c.Invoices).Returns(mockInvoices.Object);

            // Act
            var result = await ctrl.HandleWebhook(jsonElement);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(0, GetJsonPropertyInt(okResult.Value, "error"));
        }

        [Fact]
        public async Task HandleWebhook_ReturnsOk_WhenValidSecureToken()
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

            // Set valid secure token in headers
            var httpContext = new DefaultHttpContext();
            httpContext.Request.Headers["Secure-Token"] = "AK_CS.0eafb1406d2811f0b7f9c39f1519547d.SgAfzKpqf62yKUOnIl5qG4z4heJhXAy0oo5UtfrcSBEaMKmzGcz2w56HEyGF1e9xqwiAWqwB";
            ctrl.ControllerContext = new ControllerContext { HttpContext = httpContext };

            var invoice = new Invoice
            {
                InvoiceId = "DH123456",
                Status = InvoiceStatus.Incomplete,
                TotalMoney = 100000m,
                BookingDate = DateTime.Now
            };

            var mockInvoices = CreateMockDbSet(new List<Invoice> { invoice });
            var mockSeatStatuses = CreateMockDbSet(new List<SeatStatus> 
            { 
                new SeatStatus { SeatStatusId = 2, StatusName = "Booked" } 
            });

            _context.Setup(c => c.Invoices).Returns(mockInvoices.Object);
            _context.Setup(c => c.SeatStatuses).Returns(mockSeatStatuses.Object);

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

            var invoices = new List<Invoice>
            {
                new Invoice
                {
                    InvoiceId = "DH123456",
                    Status = InvoiceStatus.Incomplete,
                    TotalMoney = 100000m,
                    BookingDate = DateTime.Now
                },
                new Invoice
                {
                    InvoiceId = "DH123457",
                    Status = InvoiceStatus.Incomplete,
                    TotalMoney = 150000m,
                    BookingDate = DateTime.Now
                }
            };

            var mockInvoices = CreateMockDbSet(invoices);
            var mockSeatStatuses = CreateMockDbSet(new List<SeatStatus> 
            { 
                new SeatStatus { SeatStatusId = 2, StatusName = "Booked" } 
            });

            _context.Setup(c => c.Invoices).Returns(mockInvoices.Object);
            _context.Setup(c => c.SeatStatuses).Returns(mockSeatStatuses.Object);

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

            var mockInvoices = CreateMockDbSet(new List<Invoice> { invoice });
            var mockSeatStatuses = CreateMockDbSet(new List<SeatStatus> 
            { 
                new SeatStatus { SeatStatusId = 2, StatusName = "Booked" } 
            });

            _context.Setup(c => c.Invoices).Returns(mockInvoices.Object);
            _context.Setup(c => c.SeatStatuses).Returns(mockSeatStatuses.Object);

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

            var mockInvoices = CreateMockDbSet(new List<Invoice> { invoice });

            _context.Setup(c => c.Invoices).Returns(mockInvoices.Object);

            // Act
            var result = await ctrl.HandleWebhook(jsonElement);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(0, GetJsonPropertyInt(okResult.Value, "error"));
        }
    }
} 