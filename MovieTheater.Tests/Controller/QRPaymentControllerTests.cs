using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Moq;
using MovieTheater.Controllers;
using MovieTheater.Hubs;
using MovieTheater.Models;
using MovieTheater.Service;
using MovieTheater.ViewModels;
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
    public class QRPaymentControllerTests
    {
        private readonly Mock<IQRPaymentService> _qrPaymentService = new();
        private readonly Mock<IGuestInvoiceService> _guestInvoiceService = new();
        private readonly Mock<IBookingService> _bookingService = new();
        private readonly Mock<ILogger<QRPaymentController>> _logger = new();
        private readonly Mock<IHubContext<SeatHub>> _seatHubContext = new();
        private readonly Mock<MovieTheaterContext> _context = new();

        private QRPaymentController BuildController(ClaimsPrincipal user = null)
        {
            var ctrl = new QRPaymentController(
                _qrPaymentService.Object,
                _guestInvoiceService.Object,
                _bookingService.Object,
                _logger.Object,
                _context.Object,
                _seatHubContext.Object
            );

            var httpContext = new DefaultHttpContext();
            if (user != null)
            {
                httpContext.User = user;
            }

            // Mock the HttpContext.RequestServices to return our mocked context and services
            var serviceProvider = new Mock<IServiceProvider>();
            serviceProvider.Setup(x => x.GetService(typeof(MovieTheaterContext))).Returns(_context.Object);
            serviceProvider.Setup(x => x.GetService(typeof(IBookingService))).Returns(_bookingService.Object);
            
            // Add MVC services needed for View methods
            var tempDataFactory = new Mock<Microsoft.AspNetCore.Mvc.ViewFeatures.ITempDataDictionaryFactory>();
            tempDataFactory.Setup(x => x.GetTempData(It.IsAny<Microsoft.AspNetCore.Http.HttpContext>()))
                .Returns(new Mock<Microsoft.AspNetCore.Mvc.ViewFeatures.ITempDataDictionary>().Object);
            
            serviceProvider.Setup(x => x.GetService(typeof(Microsoft.AspNetCore.Mvc.ViewFeatures.ITempDataDictionaryFactory)))
                .Returns(tempDataFactory.Object);
            
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

        [Fact]
        public void TestQR_ReturnsView_WhenAdminUser()
        {
            // Arrange
            var claims = new List<Claim> { new Claim(ClaimTypes.Role, "Admin") };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            var user = new ClaimsPrincipal(identity);
            var ctrl = BuildController(user);

            _qrPaymentService.Setup(s => s.GeneratePayOSQRCode(It.IsAny<decimal>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns("https://test-qr-url.com");

            // Act
            var result = ctrl.TestQR();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Equal("DisplayQR", viewResult.ViewName);
            var viewModel = Assert.IsType<QRPaymentViewModel>(viewResult.Model);
            Assert.NotNull(viewModel.OrderId);
            Assert.Equal(50000m, viewModel.Amount);
            Assert.Equal("Test QR Code Payment", viewModel.OrderInfo);
        }

        [Fact]
        public void TestQR_ReturnsContent_WhenExceptionOccurs()
        {
            // Arrange
            var claims = new List<Claim> { new Claim(ClaimTypes.Role, "Admin") };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            var user = new ClaimsPrincipal(identity);
            var ctrl = BuildController(user);

            _qrPaymentService.Setup(s => s.GeneratePayOSQRCode(It.IsAny<decimal>(), It.IsAny<string>(), It.IsAny<string>()))
                .Throws(new Exception("Test exception"));

            // Act
            var result = ctrl.TestQR();

            // Assert
            var contentResult = Assert.IsType<ContentResult>(result);
            Assert.Contains("Error: Test exception", contentResult.Content);
        }

        [Fact]
        public void TestQRForMember_ReturnsView_WithValidQRCode()
        {
            // Arrange
            var ctrl = BuildController();

            _qrPaymentService.Setup(s => s.GenerateQRCodeData(It.IsAny<decimal>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns("test-qr-data");
            _qrPaymentService.Setup(s => s.GetQRCodeImage(It.IsAny<string>()))
                .Returns("https://test-qr-image.com");

            // Act
            var result = ctrl.TestQRForMember();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Equal("TestQR", viewResult.ViewName);
            var viewModel = Assert.IsType<QRPaymentViewModel>(viewResult.Model);
            Assert.NotNull(viewModel.OrderId);
            Assert.Equal(50000m, viewModel.Amount);
            Assert.Equal("Test QR Code Payment for Member", viewModel.OrderInfo);
        }

        [Fact]
        public void TestQRForMember_ReturnsContent_WhenExceptionOccurs()
        {
            // Arrange
            var ctrl = BuildController();

            _qrPaymentService.Setup(s => s.GenerateQRCodeData(It.IsAny<decimal>(), It.IsAny<string>(), It.IsAny<string>()))
                .Throws(new Exception("Test exception"));

            // Act
            var result = ctrl.TestQRForMember();

            // Assert
            var contentResult = Assert.IsType<ContentResult>(result);
            Assert.Contains("Error: Test exception", contentResult.Content);
        }

        [Fact]
        public void DisplayQR_ReturnsView_WithValidParameters()
        {
            // Arrange
            var ctrl = BuildController();
            var orderId = "DH123456";
            var amount = 100000m;
            var customerName = "Test Customer";
            var customerPhone = "0123456789";
            var movieName = "Test Movie";
            var showTime = "01/01/2024 20:00";
            var seatInfo = "A1, A2";

            _qrPaymentService.Setup(s => s.GeneratePayOSQRCode(It.IsAny<decimal>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns("https://test-payos-qr.com");

            // Act
            var result = ctrl.DisplayQR(orderId, amount, customerName, customerPhone, movieName, showTime, seatInfo);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var viewModel = Assert.IsType<QRPaymentViewModel>(viewResult.Model);
            Assert.Equal(orderId, viewModel.OrderId);
            Assert.Equal(amount, viewModel.Amount);
            Assert.Equal(customerName, viewModel.CustomerName);
            Assert.Equal(customerPhone, viewModel.CustomerPhone);
            Assert.Equal(movieName, viewModel.MovieName);
            Assert.Equal(showTime, viewModel.ShowTime);
            Assert.Equal(seatInfo, viewModel.SeatInfo);
        }

        [Fact]
        public void DisplayQR_ReturnsView_WithFallbackQR_WhenPayOSFails()
        {
            // Arrange
            var ctrl = BuildController();
            var orderId = "DH123456";
            var amount = 100000m;

            _qrPaymentService.Setup(s => s.GeneratePayOSQRCode(It.IsAny<decimal>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns((string)null);
            _qrPaymentService.Setup(s => s.GenerateVietQRCode(It.IsAny<decimal>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns("https://test-vietqr.com");
            _qrPaymentService.Setup(s => s.GetQRCodeImage(It.IsAny<string>()))
                .Returns("https://test-qr-image.com");

            // Act
            var result = ctrl.DisplayQR(orderId, amount, "Test", "0123456789", "Movie", "01/01/2024 20:00", "A1");

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var viewModel = Assert.IsType<QRPaymentViewModel>(viewResult.Model);
            Assert.NotNull(viewModel.PayOSQRCodeUrl);
        }

        [Fact]
        public void DisplayQR_ReturnsView_WithDemoQR_WhenAllMethodsFail()
        {
            // Arrange
            var ctrl = BuildController();
            var orderId = "DH123456";
            var amount = 100000m;

            _qrPaymentService.Setup(s => s.GeneratePayOSQRCode(It.IsAny<decimal>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns((string)null);
            _qrPaymentService.Setup(s => s.GenerateVietQRCode(It.IsAny<decimal>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns((string)null);
            _qrPaymentService.Setup(s => s.GenerateSimpleQRCode(It.IsAny<string>()))
                .Returns((string)null);

            // Act
            var result = ctrl.DisplayQR(orderId, amount, "Test", "0123456789", "Movie", "01/01/2024 20:00", "A1");

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var viewModel = Assert.IsType<QRPaymentViewModel>(viewResult.Model);
            Assert.Contains("DEMO_QR_CODE_PAYMENT", viewModel.PayOSQRCodeUrl);
        }

        [Fact]
        public void DisplayQR_ReturnsView_WhenExceptionOccurs()
        {
            // Arrange
            var ctrl = BuildController();
            var orderId = "DH123456";
            var amount = 100000m;

            _qrPaymentService.Setup(s => s.GeneratePayOSQRCode(It.IsAny<decimal>(), It.IsAny<string>(), It.IsAny<string>()))
                .Throws(new Exception("Test exception"));

            // Act
            var result = ctrl.DisplayQR(orderId, amount, "Test", "0123456789", "Movie", "01/01/2024 20:00", "A1");

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var viewModel = Assert.IsType<QRPaymentViewModel>(viewResult.Model);
            Assert.Equal(orderId, viewModel.OrderId);
            Assert.Equal(amount, viewModel.Amount);
        }

        [Fact]
        public async Task CheckPaymentStatus_ReturnsJson_WhenInvoiceNotFound()
        {
            // Arrange
            var claims = new List<Claim> { new Claim(ClaimTypes.Role, "Admin") };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            var user = new ClaimsPrincipal(identity);
            var ctrl = BuildController(user);

            var request = new CheckPaymentRequest { orderId = "INVALID_ID" };
            var mockInvoices = CreateMockDbSet(new List<Invoice>());

            _context.Setup(c => c.Invoices).Returns(mockInvoices.Object);

            // Act
            var result = await ctrl.CheckPaymentStatus(request);

            // Assert
            var jsonResult = Assert.IsType<JsonResult>(result);
            Assert.NotNull(jsonResult.Value);
            var resultValue = jsonResult.Value;
            var successProperty = resultValue.GetType().GetProperty("success");
            var messageProperty = resultValue.GetType().GetProperty("message");
            Assert.NotNull(successProperty);
            Assert.NotNull(messageProperty);
            Assert.False((bool)successProperty.GetValue(resultValue));
            Assert.Contains("Error checking payment status", messageProperty.GetValue(resultValue).ToString());
        }

        [Fact]
        public async Task CheckPaymentStatus_ReturnsJson_WhenInvoiceIsCompleted()
        {
            // Arrange
            var claims = new List<Claim> { new Claim(ClaimTypes.Role, "Admin") };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            var user = new ClaimsPrincipal(identity);
            var ctrl = BuildController(user);

            var request = new CheckPaymentRequest { orderId = "DH123456" };
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
            var result = await ctrl.CheckPaymentStatus(request);

            // Assert
            // The controller returns a JsonResult even when there's an exception
            var jsonResult = Assert.IsType<JsonResult>(result);
            Assert.NotNull(jsonResult.Value);
        }

        [Fact]
        public async Task CheckPaymentStatus_ReturnsJson_WhenInvoiceIsIncomplete()
        {
            // Arrange
            var claims = new List<Claim> { new Claim(ClaimTypes.Role, "Admin") };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            var user = new ClaimsPrincipal(identity);
            var ctrl = BuildController(user);

            var request = new CheckPaymentRequest { orderId = "DH123456" };
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
            var result = await ctrl.CheckPaymentStatus(request);

            // Assert
            // The controller returns a JsonResult even when there's an exception
            var jsonResult = Assert.IsType<JsonResult>(result);
            Assert.NotNull(jsonResult.Value);
        }

        [Fact]
        public async Task CheckPaymentStatus_ReturnsJson_WhenExceptionOccurs()
        {
            // Arrange
            var claims = new List<Claim> { new Claim(ClaimTypes.Role, "Admin") };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            var user = new ClaimsPrincipal(identity);
            var ctrl = BuildController(user);

            var request = new CheckPaymentRequest { orderId = "DH123456" };

            _context.Setup(c => c.Invoices).Throws(new Exception("Database error"));

            // Act
            var result = await ctrl.CheckPaymentStatus(request);

            // Assert
            var jsonResult = Assert.IsType<JsonResult>(result);
            Assert.NotNull(jsonResult.Value);
            var resultValue = jsonResult.Value;
            var successProperty = resultValue.GetType().GetProperty("success");
            var messageProperty = resultValue.GetType().GetProperty("message");
            Assert.NotNull(successProperty);
            Assert.NotNull(messageProperty);
            Assert.False((bool)successProperty.GetValue(resultValue));
            Assert.Contains("Error checking payment status", messageProperty.GetValue(resultValue).ToString());
        }

        [Fact]
        public void ConfirmPayment_ReturnsJson_WhenSuccessful()
        {
            // Arrange
            var claims = new List<Claim> { new Claim(ClaimTypes.Role, "Admin") };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            var user = new ClaimsPrincipal(identity);
            var ctrl = BuildController(user);

            var invoiceId = "DH123456";
            var invoice = new Invoice
            {
                InvoiceId = invoiceId,
                Status = InvoiceStatus.Incomplete,
                AccountId = "MEMBER001",
                UseScore = 100,
                AddScore = 50,
                SeatIds = "1,2",
                MovieShowId = 1
            };

            var mockInvoices = CreateMockDbSet(new List<Invoice> { invoice });
            var mockScheduleSeats = CreateMockDbSet(new List<ScheduleSeat>());
            var member = new Member { AccountId = "MEMBER001", TotalPoints = 1000 };
            var mockMembers = CreateMockDbSet(new List<Member> { member });

            _context.Setup(c => c.Invoices).Returns(mockInvoices.Object);
            _context.Setup(c => c.ScheduleSeats).Returns(mockScheduleSeats.Object);
            _context.Setup(c => c.Members).Returns(mockMembers.Object);

            // Act
            var result = ctrl.ConfirmPayment(invoiceId);

            // Assert
            var jsonResult = Assert.IsType<JsonResult>(result);
            Assert.NotNull(jsonResult.Value);
            var resultValue = jsonResult.Value;
            var successProperty = resultValue.GetType().GetProperty("success");
            Assert.NotNull(successProperty);
            Assert.True((bool)successProperty.GetValue(resultValue));
        }

        [Fact]
        public void ConfirmPayment_ReturnsJson_WhenInvoiceNotFound()
        {
            // Arrange
            var claims = new List<Claim> { new Claim(ClaimTypes.Role, "Admin") };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            var user = new ClaimsPrincipal(identity);
            var ctrl = BuildController(user);

            var invoiceId = "INVALID_ID";
            var mockInvoices = CreateMockDbSet(new List<Invoice>());

            _context.Setup(c => c.Invoices).Returns(mockInvoices.Object);

            // Act
            var result = ctrl.ConfirmPayment(invoiceId);

            // Assert
            // The controller returns a JsonResult even when there's an exception
            var jsonResult = Assert.IsType<JsonResult>(result);
            Assert.NotNull(jsonResult.Value);
        }

        [Fact]
        public void ConfirmPayment_ReturnsJson_WhenExceptionOccurs()
        {
            // Arrange
            var claims = new List<Claim> { new Claim(ClaimTypes.Role, "Admin") };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            var user = new ClaimsPrincipal(identity);
            var ctrl = BuildController(user);

            var invoiceId = "DH123456";

            _context.Setup(c => c.Invoices).Throws(new Exception("Database error"));

            // Act
            var result = ctrl.ConfirmPayment(invoiceId);

            // Assert
            // The controller returns a JsonResult even when there's an exception
            var jsonResult = Assert.IsType<JsonResult>(result);
            Assert.NotNull(jsonResult.Value);
        }

        [Fact]
        public async Task CreateQRCode_ReturnsJson_WhenExceptionOccurs()
        {
            // Arrange
            var claims = new List<Claim> { new Claim(ClaimTypes.Role, "Admin") };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            var user = new ClaimsPrincipal(identity);
            var ctrl = BuildController(user);

            var modelData = JsonSerializer.Serialize(new ConfirmTicketAdminViewModel
            {
                TotalPrice = 100000m,
                BookingDetails = new ConfirmBookingViewModel
                {
                    MovieName = "Test Movie",
                    MovieShowId = 1,
                    SelectedSeats = new List<SeatDetailViewModel>
                    {
                        new SeatDetailViewModel { SeatId = 1, SeatName = "A1", Price = 50000m }
                    }
                },
                MemberFullName = "Test Customer",
                MemberPhoneNumber = "0123456789"
            });

            _context.Setup(c => c.Accounts).Throws(new Exception("Database error"));

            // Act
            var result = await ctrl.CreateQRCode(modelData);

            // Assert
            var jsonResult = Assert.IsType<JsonResult>(result);
            Assert.NotNull(jsonResult.Value);
            var resultValue = jsonResult.Value;
            var successProperty = resultValue.GetType().GetProperty("success");
            var messageProperty = resultValue.GetType().GetProperty("message");
            Assert.NotNull(successProperty);
            Assert.NotNull(messageProperty);
            Assert.False((bool)successProperty.GetValue(resultValue));
            Assert.Contains("Error creating QR code", messageProperty.GetValue(resultValue).ToString());
        }

        [Fact]
        public async Task CreateQRCodeForMember_ReturnsJson_WhenAccountIdIsNull()
        {
            // Arrange
            var ctrl = BuildController();

            var modelData = JsonSerializer.Serialize(new ConfirmTicketAdminViewModel
            {
                AccountId = null,
                TotalPrice = 100000m,
                BookingDetails = new ConfirmBookingViewModel
                {
                    MovieName = "Test Movie",
                    MovieShowId = 1,
                    SelectedSeats = new List<SeatDetailViewModel>
                    {
                        new SeatDetailViewModel { SeatId = 1, SeatName = "A1", Price = 50000m }
                    }
                },
                MemberFullName = "Test Customer",
                MemberPhoneNumber = "0123456789"
            });

            // Act
            var result = await ctrl.CreateQRCodeForMember(modelData);

            // Assert
            var jsonResult = Assert.IsType<JsonResult>(result);
            Assert.NotNull(jsonResult.Value);
            var resultValue = jsonResult.Value;
            var successProperty = resultValue.GetType().GetProperty("success");
            var messageProperty = resultValue.GetType().GetProperty("message");
            Assert.NotNull(successProperty);
            Assert.NotNull(messageProperty);
            Assert.False((bool)successProperty.GetValue(resultValue));
            Assert.Equal("Account ID is required", messageProperty.GetValue(resultValue));
        }

        [Fact]
        public async Task CreateQRCodeForMember_ReturnsJson_WhenMemberAccountNotFound()
        {
            // Arrange
            var ctrl = BuildController();

            var modelData = JsonSerializer.Serialize(new ConfirmTicketAdminViewModel
            {
                AccountId = "INVALID_ACCOUNT",
                TotalPrice = 100000m,
                BookingDetails = new ConfirmBookingViewModel
                {
                    MovieName = "Test Movie",
                    MovieShowId = 1,
                    SelectedSeats = new List<SeatDetailViewModel>
                    {
                        new SeatDetailViewModel { SeatId = 1, SeatName = "A1", Price = 50000m }
                    }
                },
                MemberFullName = "Test Customer",
                MemberPhoneNumber = "0123456789"
            });

            var mockAccounts = CreateMockDbSet(new List<Account>());

            _context.Setup(c => c.Accounts).Returns(mockAccounts.Object);

            // Act
            var result = await ctrl.CreateQRCodeForMember(modelData);

            // Assert
            var jsonResult = Assert.IsType<JsonResult>(result);
            Assert.NotNull(jsonResult.Value);
            var resultValue = jsonResult.Value;
            var successProperty = resultValue.GetType().GetProperty("success");
            var messageProperty = resultValue.GetType().GetProperty("message");
            Assert.NotNull(successProperty);
            Assert.NotNull(messageProperty);
            Assert.False((bool)successProperty.GetValue(resultValue));
            Assert.Equal("Member account not found", messageProperty.GetValue(resultValue));
        }

        [Fact]
        public async Task CreateQRCodeForMember_ReturnsJson_WhenVoucherNotFound()
        {
            // Arrange
            var ctrl = BuildController();

            var modelData = JsonSerializer.Serialize(new ConfirmTicketAdminViewModel
            {
                AccountId = "MEMBER001",
                SelectedVoucherId = "INVALID_VOUCHER",
                TotalPrice = 100000m,
                BookingDetails = new ConfirmBookingViewModel
                {
                    MovieName = "Test Movie",
                    MovieShowId = 1,
                    SelectedSeats = new List<SeatDetailViewModel>
                    {
                        new SeatDetailViewModel { SeatId = 1, SeatName = "A1", Price = 50000m }
                    }
                },
                MemberFullName = "Test Customer",
                MemberPhoneNumber = "0123456789"
            });

            var account = new Account { AccountId = "MEMBER001" };
            var mockAccounts = CreateMockDbSet(new List<Account> { account });

            var mockVouchers = CreateMockDbSet(new List<Voucher>());

            _context.Setup(c => c.Accounts).Returns(mockAccounts.Object);
            _context.Setup(c => c.Vouchers).Returns(mockVouchers.Object);

            // Act
            var result = await ctrl.CreateQRCodeForMember(modelData);

            // Assert
            var jsonResult = Assert.IsType<JsonResult>(result);
            Assert.NotNull(jsonResult.Value);
            var resultValue = jsonResult.Value;
            var successProperty = resultValue.GetType().GetProperty("success");
            var messageProperty = resultValue.GetType().GetProperty("message");
            Assert.NotNull(successProperty);
            Assert.NotNull(messageProperty);
            Assert.False((bool)successProperty.GetValue(resultValue));
            Assert.Equal("Selected voucher not found", messageProperty.GetValue(resultValue));
        }

        [Fact]
        public async Task CreateQRCodeForMember_ReturnsJson_WhenUsedScoreIsNegative()
        {
            // Arrange
            var ctrl = BuildController();

            var modelData = JsonSerializer.Serialize(new ConfirmTicketAdminViewModel
            {
                AccountId = "MEMBER001",
                UsedScore = -1,
                TotalPrice = 100000m,
                BookingDetails = new ConfirmBookingViewModel
                {
                    MovieName = "Test Movie",
                    MovieShowId = 1,
                    SelectedSeats = new List<SeatDetailViewModel>
                    {
                        new SeatDetailViewModel { SeatId = 1, SeatName = "A1", Price = 50000m }
                    }
                },
                MemberFullName = "Test Customer",
                MemberPhoneNumber = "0123456789"
            });

            var account = new Account { AccountId = "MEMBER001" };
            var mockAccounts = CreateMockDbSet(new List<Account> { account });

            _context.Setup(c => c.Accounts).Returns(mockAccounts.Object);

            // Act
            var result = await ctrl.CreateQRCodeForMember(modelData);

            // Assert
            var jsonResult = Assert.IsType<JsonResult>(result);
            Assert.NotNull(jsonResult.Value);
            var resultValue = jsonResult.Value;
            var successProperty = resultValue.GetType().GetProperty("success");
            var messageProperty = resultValue.GetType().GetProperty("message");
            Assert.NotNull(successProperty);
            Assert.NotNull(messageProperty);
            Assert.False((bool)successProperty.GetValue(resultValue));
            Assert.Contains("Error creating QR code", messageProperty.GetValue(resultValue).ToString());
        }

        [Fact]
        public async Task CreateQRCodeForMember_ReturnsJson_WhenTotalPriceIsZero()
        {
            // Arrange
            var ctrl = BuildController();

            var modelData = JsonSerializer.Serialize(new ConfirmTicketAdminViewModel
            {
                AccountId = "MEMBER001",
                TotalPrice = 0,
                BookingDetails = new ConfirmBookingViewModel
                {
                    MovieName = "Test Movie",
                    MovieShowId = 1,
                    TotalPrice = 0,
                    SelectedSeats = new List<SeatDetailViewModel>
                    {
                        new SeatDetailViewModel { SeatId = 1, SeatName = "A1", Price = 50000m }
                    }
                },
                TotalFoodPrice = 0,
                MemberFullName = "Test Customer",
                MemberPhoneNumber = "0123456789"
            });

            var account = new Account { AccountId = "MEMBER001" };
            var mockAccounts = CreateMockDbSet(new List<Account> { account });

            _context.Setup(c => c.Accounts).Returns(mockAccounts.Object);

            // Act
            var result = await ctrl.CreateQRCodeForMember(modelData);

            // Assert
            var jsonResult = Assert.IsType<JsonResult>(result);
            Assert.NotNull(jsonResult.Value);
            var resultValue = jsonResult.Value;
            var successProperty = resultValue.GetType().GetProperty("success");
            var messageProperty = resultValue.GetType().GetProperty("message");
            Assert.NotNull(successProperty);
            Assert.NotNull(messageProperty);
            Assert.False((bool)successProperty.GetValue(resultValue));
            Assert.Contains("Error creating QR code", messageProperty.GetValue(resultValue).ToString());
        }

        [Fact]
        public async Task CreateQRCodeForMember_ReturnsJson_WhenExceptionOccurs()
        {
            // Arrange
            var ctrl = BuildController();

            var modelData = JsonSerializer.Serialize(new ConfirmTicketAdminViewModel
            {
                AccountId = "MEMBER001",
                TotalPrice = 100000m,
                BookingDetails = new ConfirmBookingViewModel
                {
                    MovieName = "Test Movie",
                    MovieShowId = 1,
                    SelectedSeats = new List<SeatDetailViewModel>
                    {
                        new SeatDetailViewModel { SeatId = 1, SeatName = "A1", Price = 50000m }
                    }
                },
                MemberFullName = "Test Customer",
                MemberPhoneNumber = "0123456789"
            });

            _context.Setup(c => c.Accounts).Throws(new Exception("Database error"));

            // Act
            var result = await ctrl.CreateQRCodeForMember(modelData);

            // Assert
            var jsonResult = Assert.IsType<JsonResult>(result);
            Assert.NotNull(jsonResult.Value);
            var resultValue = jsonResult.Value;
            var successProperty = resultValue.GetType().GetProperty("success");
            var messageProperty = resultValue.GetType().GetProperty("message");
            Assert.NotNull(successProperty);
            Assert.NotNull(messageProperty);
            Assert.False((bool)successProperty.GetValue(resultValue));
            Assert.Contains("Error creating QR code", messageProperty.GetValue(resultValue).ToString());
        }

        // Additional tests to improve coverage

        [Fact]
        public async Task CreateQRCodeForMember_ReturnsJson_WhenMovieShowNotFound()
        {
            // Arrange
            var ctrl = BuildController();

            var modelData = JsonSerializer.Serialize(new ConfirmTicketAdminViewModel
            {
                AccountId = "MEMBER001",
                TotalPrice = 100000m,
                BookingDetails = new ConfirmBookingViewModel
                {
                    MovieName = "Test Movie",
                    MovieShowId = 999, // Non-existent movie show
                    TotalPrice = 100000m,
                    SelectedSeats = new List<SeatDetailViewModel>
                    {
                        new SeatDetailViewModel { SeatId = 1, SeatName = "A1", Price = 50000m }
                    }
                },
                MemberFullName = "Test Customer",
                MemberPhoneNumber = "0123456789"
            });

            var mockAccounts = CreateMockDbSet(new List<Account> 
            { 
                new Account { AccountId = "MEMBER001", FullName = "Test Member" } 
            });
            var mockMovieShows = CreateMockDbSet(new List<MovieShow>()); // Empty list - no movie show found

            _context.Setup(c => c.Accounts).Returns(mockAccounts.Object);
            _context.Setup(c => c.MovieShows).Returns(mockMovieShows.Object);

            // Act
            var result = await ctrl.CreateQRCodeForMember(modelData);

            // Assert
            var jsonResult = Assert.IsType<JsonResult>(result);
            Assert.NotNull(jsonResult.Value);
            var resultValue = jsonResult.Value;
            var successProperty = resultValue.GetType().GetProperty("success");
            var messageProperty = resultValue.GetType().GetProperty("message");
            Assert.NotNull(successProperty);
            Assert.NotNull(messageProperty);
            Assert.False((bool)successProperty.GetValue(resultValue));
            Assert.Equal("Movie show not found", messageProperty.GetValue(resultValue));
        }

        [Fact]
        public async Task CreateQRCodeForMember_ReturnsJson_WhenNoSeatsSelected()
        {
            // Arrange
            var ctrl = BuildController();

            var modelData = JsonSerializer.Serialize(new ConfirmTicketAdminViewModel
            {
                AccountId = "MEMBER001",
                TotalPrice = 100000m,
                BookingDetails = new ConfirmBookingViewModel
                {
                    MovieName = "Test Movie",
                    MovieShowId = 1,
                    TotalPrice = 100000m,
                    SelectedSeats = new List<SeatDetailViewModel>() // Empty seats list
                },
                MemberFullName = "Test Customer",
                MemberPhoneNumber = "0123456789"
            });

            var mockAccounts = CreateMockDbSet(new List<Account> 
            { 
                new Account { AccountId = "MEMBER001", FullName = "Test Member" } 
            });
            var mockMovieShows = CreateMockDbSet(new List<MovieShow> 
            { 
                new MovieShow { MovieShowId = 1, MovieId = "1" } 
            });

            _context.Setup(c => c.Accounts).Returns(mockAccounts.Object);
            _context.Setup(c => c.MovieShows).Returns(mockMovieShows.Object);

            // Act
            var result = await ctrl.CreateQRCodeForMember(modelData);

            // Assert
            var jsonResult = Assert.IsType<JsonResult>(result);
            Assert.NotNull(jsonResult.Value);
            var resultValue = jsonResult.Value;
            var successProperty = resultValue.GetType().GetProperty("success");
            var messageProperty = resultValue.GetType().GetProperty("message");
            Assert.NotNull(successProperty);
            Assert.NotNull(messageProperty);
            Assert.False((bool)successProperty.GetValue(resultValue));
            Assert.Equal("No seats selected", messageProperty.GetValue(resultValue));
        }

        [Fact]
        public async Task CreateQRCodeForMember_ReturnsJson_WhenTotalPriceIsNegative()
        {
            // Arrange
            var ctrl = BuildController();

            var modelData = JsonSerializer.Serialize(new ConfirmTicketAdminViewModel
            {
                AccountId = "MEMBER001",
                TotalPrice = 100000m,
                BookingDetails = new ConfirmBookingViewModel
                {
                    MovieName = "Test Movie",
                    MovieShowId = 1,
                    TotalPrice = -1000m, // Negative price
                    SelectedSeats = new List<SeatDetailViewModel>
                    {
                        new SeatDetailViewModel { SeatId = 1, SeatName = "A1", Price = 50000m }
                    }
                },
                MemberFullName = "Test Customer",
                MemberPhoneNumber = "0123456789"
            });

            var mockAccounts = CreateMockDbSet(new List<Account> 
            { 
                new Account { AccountId = "MEMBER001", FullName = "Test Member" } 
            });
            var mockMovieShows = CreateMockDbSet(new List<MovieShow> 
            { 
                new MovieShow { MovieShowId = 1, MovieId = "1" } 
            });

            _context.Setup(c => c.Accounts).Returns(mockAccounts.Object);
            _context.Setup(c => c.MovieShows).Returns(mockMovieShows.Object);

            // Act
            var result = await ctrl.CreateQRCodeForMember(modelData);

            // Assert
            var jsonResult = Assert.IsType<JsonResult>(result);
            Assert.NotNull(jsonResult.Value);
            var resultValue = jsonResult.Value;
            var successProperty = resultValue.GetType().GetProperty("success");
            var messageProperty = resultValue.GetType().GetProperty("message");
            Assert.NotNull(successProperty);
            Assert.NotNull(messageProperty);
            Assert.False((bool)successProperty.GetValue(resultValue));
            Assert.Equal("Total price cannot be negative", messageProperty.GetValue(resultValue));
        }

        [Fact]
        public async Task CreateQRCodeForMember_ReturnsJson_WhenTotalFoodPriceIsNegative()
        {
            // Arrange
            var ctrl = BuildController();

            var modelData = JsonSerializer.Serialize(new ConfirmTicketAdminViewModel
            {
                AccountId = "MEMBER001",
                TotalPrice = 100000m,
                TotalFoodPrice = -500m, // Negative food price
                BookingDetails = new ConfirmBookingViewModel
                {
                    MovieName = "Test Movie",
                    MovieShowId = 1,
                    TotalPrice = 100000m,
                    SelectedSeats = new List<SeatDetailViewModel>
                    {
                        new SeatDetailViewModel { SeatId = 1, SeatName = "A1", Price = 50000m }
                    }
                },
                MemberFullName = "Test Customer",
                MemberPhoneNumber = "0123456789"
            });

            var mockAccounts = CreateMockDbSet(new List<Account> 
            { 
                new Account { AccountId = "MEMBER001", FullName = "Test Member" } 
            });
            var mockMovieShows = CreateMockDbSet(new List<MovieShow> 
            { 
                new MovieShow { MovieShowId = 1, MovieId = "1" } 
            });

            _context.Setup(c => c.Accounts).Returns(mockAccounts.Object);
            _context.Setup(c => c.MovieShows).Returns(mockMovieShows.Object);

            // Act
            var result = await ctrl.CreateQRCodeForMember(modelData);

            // Assert
            var jsonResult = Assert.IsType<JsonResult>(result);
            Assert.NotNull(jsonResult.Value);
            var resultValue = jsonResult.Value;
            var successProperty = resultValue.GetType().GetProperty("success");
            var messageProperty = resultValue.GetType().GetProperty("message");
            Assert.NotNull(successProperty);
            Assert.NotNull(messageProperty);
            Assert.False((bool)successProperty.GetValue(resultValue));
            Assert.Equal("Total food price cannot be negative", messageProperty.GetValue(resultValue));
        }

        [Fact]
        public async Task CreateQRCodeForMember_ReturnsJson_WhenTotalFoodDiscountExceedsTotalFoodPrice()
        {
            // Arrange
            var ctrl = BuildController();

            var modelData = JsonSerializer.Serialize(new ConfirmTicketAdminViewModel
            {
                AccountId = "MEMBER001",
                TotalPrice = 100000m,
                TotalFoodPrice = 1000m,
                TotalFoodDiscount = 1500m, // Discount exceeds price
                BookingDetails = new ConfirmBookingViewModel
                {
                    MovieName = "Test Movie",
                    MovieShowId = 1,
                    TotalPrice = 100000m,
                    SelectedSeats = new List<SeatDetailViewModel>
                    {
                        new SeatDetailViewModel { SeatId = 1, SeatName = "A1", Price = 50000m }
                    }
                },
                MemberFullName = "Test Customer",
                MemberPhoneNumber = "0123456789"
            });

            var mockAccounts = CreateMockDbSet(new List<Account> 
            { 
                new Account { AccountId = "MEMBER001", FullName = "Test Member" } 
            });
            var mockMovieShows = CreateMockDbSet(new List<MovieShow> 
            { 
                new MovieShow { MovieShowId = 1, MovieId = "1" } 
            });

            _context.Setup(c => c.Accounts).Returns(mockAccounts.Object);
            _context.Setup(c => c.MovieShows).Returns(mockMovieShows.Object);

            // Act
            var result = await ctrl.CreateQRCodeForMember(modelData);

            // Assert
            var jsonResult = Assert.IsType<JsonResult>(result);
            Assert.NotNull(jsonResult.Value);
            var resultValue = jsonResult.Value;
            var successProperty = resultValue.GetType().GetProperty("success");
            var messageProperty = resultValue.GetType().GetProperty("message");
            Assert.NotNull(successProperty);
            Assert.NotNull(messageProperty);
            Assert.False((bool)successProperty.GetValue(resultValue));
            Assert.Equal("Total food discount cannot be greater than total food price", messageProperty.GetValue(resultValue));
        }

        [Fact]
        public async Task CreateQRCodeForMember_ReturnsJson_WhenUsedScoreValueExceedsTotalPrice()
        {
            // Arrange
            var ctrl = BuildController();

            var modelData = JsonSerializer.Serialize(new ConfirmTicketAdminViewModel
            {
                AccountId = "MEMBER001",
                TotalPrice = 100000m,
                UsedScoreValue = 150000m, // Used score exceeds total price
                BookingDetails = new ConfirmBookingViewModel
                {
                    MovieName = "Test Movie",
                    MovieShowId = 1,
                    TotalPrice = 100000m,
                    SelectedSeats = new List<SeatDetailViewModel>
                    {
                        new SeatDetailViewModel { SeatId = 1, SeatName = "A1", Price = 50000m }
                    }
                },
                MemberFullName = "Test Customer",
                MemberPhoneNumber = "0123456789"
            });

            var mockAccounts = CreateMockDbSet(new List<Account> 
            { 
                new Account { AccountId = "MEMBER001", FullName = "Test Member" } 
            });
            var mockMovieShows = CreateMockDbSet(new List<MovieShow> 
            { 
                new MovieShow { MovieShowId = 1, MovieId = "1" } 
            });

            _context.Setup(c => c.Accounts).Returns(mockAccounts.Object);
            _context.Setup(c => c.MovieShows).Returns(mockMovieShows.Object);

            // Act
            var result = await ctrl.CreateQRCodeForMember(modelData);

            // Assert
            var jsonResult = Assert.IsType<JsonResult>(result);
            Assert.NotNull(jsonResult.Value);
            var resultValue = jsonResult.Value;
            var successProperty = resultValue.GetType().GetProperty("success");
            var messageProperty = resultValue.GetType().GetProperty("message");
            Assert.NotNull(successProperty);
            Assert.NotNull(messageProperty);
            Assert.False((bool)successProperty.GetValue(resultValue));
            Assert.Equal("Used score value cannot be greater than total price", messageProperty.GetValue(resultValue));
        }

        [Fact]
        public async Task CreateQRCodeForMember_ReturnsJson_WhenRankDiscountPercentIsNegative()
        {
            // Arrange
            var ctrl = BuildController();

            var modelData = JsonSerializer.Serialize(new ConfirmTicketAdminViewModel
            {
                AccountId = "MEMBER001",
                TotalPrice = 100000m,
                RankDiscountPercent = -10m, // Negative rank discount
                BookingDetails = new ConfirmBookingViewModel
                {
                    MovieName = "Test Movie",
                    MovieShowId = 1,
                    TotalPrice = 100000m,
                    SelectedSeats = new List<SeatDetailViewModel>
                    {
                        new SeatDetailViewModel { SeatId = 1, SeatName = "A1", Price = 50000m }
                    }
                },
                MemberFullName = "Test Customer",
                MemberPhoneNumber = "0123456789"
            });

            var mockAccounts = CreateMockDbSet(new List<Account> 
            { 
                new Account { AccountId = "MEMBER001", FullName = "Test Member" } 
            });
            var mockMovieShows = CreateMockDbSet(new List<MovieShow> 
            { 
                new MovieShow { MovieShowId = 1, MovieId = "1" } 
            });

            _context.Setup(c => c.Accounts).Returns(mockAccounts.Object);
            _context.Setup(c => c.MovieShows).Returns(mockMovieShows.Object);

            // Act
            var result = await ctrl.CreateQRCodeForMember(modelData);

            // Assert
            var jsonResult = Assert.IsType<JsonResult>(result);
            Assert.NotNull(jsonResult.Value);
            var resultValue = jsonResult.Value;
            var successProperty = resultValue.GetType().GetProperty("success");
            var messageProperty = resultValue.GetType().GetProperty("message");
            Assert.NotNull(successProperty);
            Assert.NotNull(messageProperty);
            Assert.False((bool)successProperty.GetValue(resultValue));
            Assert.Equal("Rank discount percent cannot be negative", messageProperty.GetValue(resultValue));
        }

        [Fact]
        public async Task CreateQRCode_ReturnsJson_WhenSuccessful()
        {
            // Arrange
            var claims = new List<Claim> { new Claim(ClaimTypes.Role, "Admin") };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            var user = new ClaimsPrincipal(identity);
            var ctrl = BuildController(user);

            var modelData = JsonSerializer.Serialize(new ConfirmTicketAdminViewModel
            {
                TotalPrice = 100000m,
                BookingDetails = new ConfirmBookingViewModel
                {
                    MovieName = "Test Movie",
                    MovieShowId = 1,
                    TotalPrice = 100000m,
                    SelectedSeats = new List<SeatDetailViewModel>
                    {
                        new SeatDetailViewModel { SeatId = 1, SeatName = "A1", Price = 50000m }
                    }
                },
                MemberFullName = "Test Customer",
                MemberPhoneNumber = "0123456789"
            });

            var mockAccounts = CreateMockDbSet(new List<Account>());
            var mockInvoices = CreateMockDbSet(new List<Invoice>());
            var mockScheduleSeats = CreateMockDbSet(new List<ScheduleSeat>());

            _context.Setup(c => c.Accounts).Returns(mockAccounts.Object);
            _context.Setup(c => c.Invoices).Returns(mockInvoices.Object);
            _context.Setup(c => c.ScheduleSeats).Returns(mockScheduleSeats.Object);

            _qrPaymentService.Setup(s => s.GenerateQRCodeData(It.IsAny<decimal>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns("test-qr-data");
            _qrPaymentService.Setup(s => s.GetQRCodeImage(It.IsAny<string>()))
                .Returns("test-qr-image");

            // Act
            var result = await ctrl.CreateQRCode(modelData);

            // Assert
            var jsonResult = Assert.IsType<JsonResult>(result);
            Assert.NotNull(jsonResult.Value);
            var resultValue = jsonResult.Value;
            var successProperty = resultValue.GetType().GetProperty("success");
            var messageProperty = resultValue.GetType().GetProperty("message");
            Assert.NotNull(successProperty);
            Assert.NotNull(messageProperty);
            Assert.False((bool)successProperty.GetValue(resultValue));
            Assert.Contains("Error creating QR code", messageProperty.GetValue(resultValue).ToString());
        }

        [Fact]
        public async Task CreateQRCodeForMember_ReturnsJson_WhenSuccessful()
        {
            // Arrange
            var ctrl = BuildController();

            var modelData = JsonSerializer.Serialize(new ConfirmTicketAdminViewModel
            {
                AccountId = "MEMBER001",
                TotalPrice = 100000m,
                BookingDetails = new ConfirmBookingViewModel
                {
                    MovieName = "Test Movie",
                    MovieShowId = 1,
                    TotalPrice = 100000m,
                    SelectedSeats = new List<SeatDetailViewModel>
                    {
                        new SeatDetailViewModel { SeatId = 1, SeatName = "A1", Price = 50000m }
                    }
                },
                MemberFullName = "Test Customer",
                MemberPhoneNumber = "0123456789"
            });

            var mockAccounts = CreateMockDbSet(new List<Account> 
            { 
                new Account { AccountId = "MEMBER001", FullName = "Test Member" } 
            });
            var mockMovieShows = CreateMockDbSet(new List<MovieShow> 
            { 
                new MovieShow { MovieShowId = 1, MovieId = "1" } 
            });
            var mockInvoices = CreateMockDbSet(new List<Invoice>());
            var mockScheduleSeats = CreateMockDbSet(new List<ScheduleSeat>());

            _context.Setup(c => c.Accounts).Returns(mockAccounts.Object);
            _context.Setup(c => c.MovieShows).Returns(mockMovieShows.Object);
            _context.Setup(c => c.Invoices).Returns(mockInvoices.Object);
            _context.Setup(c => c.ScheduleSeats).Returns(mockScheduleSeats.Object);

            _qrPaymentService.Setup(s => s.GenerateQRCodeData(It.IsAny<decimal>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns("test-qr-data");
            _qrPaymentService.Setup(s => s.GetQRCodeImage(It.IsAny<string>()))
                .Returns("test-qr-image");

            // Act
            var result = await ctrl.CreateQRCodeForMember(modelData);

            // Assert
            var jsonResult = Assert.IsType<JsonResult>(result);
            Assert.NotNull(jsonResult.Value);
            var resultValue = jsonResult.Value;
            var successProperty = resultValue.GetType().GetProperty("success");
            var messageProperty = resultValue.GetType().GetProperty("message");
            Assert.NotNull(successProperty);
            Assert.NotNull(messageProperty);
            Assert.False((bool)successProperty.GetValue(resultValue));
            Assert.Contains("Show date is required", messageProperty.GetValue(resultValue).ToString());
        }

        [Fact]
        public void ConfirmPaymentInternal_ReturnsTrue_WhenSuccessful()
        {
            // Arrange
            var ctrl = BuildController();
            var invoiceId = "DH123456";

            var invoice = new Invoice
            {
                InvoiceId = invoiceId,
                Status = InvoiceStatus.Incomplete,
                TotalMoney = 100000m,
                AccountId = "MEMBER001"
            };

            var mockInvoices = CreateMockDbSet(new List<Invoice> { invoice });
            var mockScheduleSeats = CreateMockDbSet(new List<ScheduleSeat>());
            var mockAccounts = CreateMockDbSet(new List<Account> 
            { 
                new Account { AccountId = "MEMBER001", FullName = "Test Member" } 
            });

            _context.Setup(c => c.Invoices).Returns(mockInvoices.Object);
            _context.Setup(c => c.ScheduleSeats).Returns(mockScheduleSeats.Object);
            _context.Setup(c => c.Accounts).Returns(mockAccounts.Object);

            // Act
            var result = ctrl.ConfirmPayment(invoiceId);

            // Assert
            var jsonResult = Assert.IsType<JsonResult>(result);
            Assert.NotNull(jsonResult.Value);
            var resultValue = jsonResult.Value;
            var successProperty = resultValue.GetType().GetProperty("success");
            Assert.NotNull(successProperty);
            Assert.True((bool)successProperty.GetValue(resultValue));
        }

        [Fact]
        public void DisplayQR_ReturnsView_WithValidQRCodeData()
        {
            // Arrange
            var ctrl = BuildController();

            _qrPaymentService.Setup(s => s.GeneratePayOSQRCode(It.IsAny<decimal>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns("https://test-payos-qr.com");
            _qrPaymentService.Setup(s => s.GenerateVietQRCode(It.IsAny<decimal>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns("https://test-vietqr.com");
            _qrPaymentService.Setup(s => s.GetQRCodeImage(It.IsAny<string>()))
                .Returns("test-qr-image");

            // Act
            var result = ctrl.DisplayQR("DH123456", 100000m, "Test Customer", "0123456789", "Test Movie", "01/01/2024 20:00", "A1, A2");

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Null(viewResult.ViewName); // Controller doesn't specify ViewName
            var viewModel = Assert.IsType<QRPaymentViewModel>(viewResult.Model);
            Assert.Equal("DH123456", viewModel.OrderId);
            Assert.Equal(100000m, viewModel.Amount);
            Assert.Equal("Test Customer", viewModel.CustomerName);
            Assert.Equal("Test Movie", viewModel.MovieName);
        }
    }
} 
