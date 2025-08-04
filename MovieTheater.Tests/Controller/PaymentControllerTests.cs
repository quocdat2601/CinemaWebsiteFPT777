using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using MovieTheater.Controllers;
using MovieTheater.Hubs;
using MovieTheater.Models;
using MovieTheater.Service;
using MovieTheater.ViewModels;
using Newtonsoft.Json;
using System.Text;
using Xunit;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Mvc.Controllers;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Primitives;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Mvc.Infrastructure;

namespace MovieTheater.Tests.Controller
{
    public class PaymentControllerTests
    {
        private readonly Mock<IVNPayService> _vnPayService;
        private readonly Mock<ILogger<PaymentController>> _logger;
        private readonly Mock<IAccountService> _accountService;
        private readonly Mock<IHubContext<DashboardHub>> _dashboardHubContext;
        private readonly Mock<IFoodInvoiceService> _foodInvoiceService;
        private readonly Mock<IInvoiceService> _invoiceService;
        private readonly Mock<IVoucherService> _voucherService;
        private readonly Mock<ISeatService> _seatService;
        private readonly Mock<IScheduleSeatService> _scheduleSeatService;
        private readonly Mock<IMemberService> _memberService;
        private readonly Mock<IClientProxy> _clientProxy;
        private readonly Mock<IHubClients> _hubClients;
        private readonly Mock<IHubContext<SeatHub>> _seatHubContext;
        private readonly Mock<IClientProxy> _seatClientProxy;
        private readonly Mock<IHubClients> _seatHubClients;

        public PaymentControllerTests()
        {
            _vnPayService = new Mock<IVNPayService>();
            _logger = new Mock<ILogger<PaymentController>>();
            _accountService = new Mock<IAccountService>();
            _dashboardHubContext = new Mock<IHubContext<DashboardHub>>();
            _foodInvoiceService = new Mock<IFoodInvoiceService>();
            _invoiceService = new Mock<IInvoiceService>();
            _voucherService = new Mock<IVoucherService>();
            _seatService = new Mock<ISeatService>();
            _scheduleSeatService = new Mock<IScheduleSeatService>();
            _memberService = new Mock<IMemberService>();
            _clientProxy = new Mock<IClientProxy>();
            _hubClients = new Mock<IHubClients>();
            _seatHubContext = new Mock<IHubContext<SeatHub>>();
            _seatClientProxy = new Mock<IClientProxy>();
            _seatHubClients = new Mock<IHubClients>();

            // Setup dashboard hub
            _hubClients.Setup(h => h.All).Returns(_clientProxy.Object);
            _dashboardHubContext.Setup(h => h.Clients).Returns(_hubClients.Object);

            // Setup seat hub
            _seatHubClients.Setup(h => h.Group(It.IsAny<string>())).Returns(_seatClientProxy.Object);
            _seatHubContext.Setup(h => h.Clients).Returns(_seatHubClients.Object);
        }

        private PaymentController BuildController()
        {
            var controller = new PaymentController(
                _vnPayService.Object,
                _logger.Object,
                _accountService.Object,
                _dashboardHubContext.Object,
                _foodInvoiceService.Object,
                _invoiceService.Object,
                _voucherService.Object,
                _seatService.Object,
                _scheduleSeatService.Object,
                _memberService.Object
            );

            // Setup HttpContext with session
            var httpContext = new DefaultHttpContext();
            var session = new TestSession();
            httpContext.Session = session;
            
            // Setup TempData
            var tempDataProvider = new Mock<ITempDataProvider>();
            var tempDataDictionary = new TempDataDictionary(httpContext, tempDataProvider.Object);
            
            // Create a real service collection with required services
            var services = new ServiceCollection();
            services.AddRouting();
            services.AddMvc();
            services.AddSingleton<IUrlHelperFactory, UrlHelperFactory>();
            services.AddSingleton<IActionContextAccessor, ActionContextAccessor>();
            var serviceProvider = services.BuildServiceProvider();
            
            httpContext.RequestServices = serviceProvider;
            
            // Setup controller context
            var actionContext = new ActionContext(httpContext, new Microsoft.AspNetCore.Routing.RouteData(), new ControllerActionDescriptor());
            controller.ControllerContext = new ControllerContext(actionContext);
            controller.TempData = tempDataDictionary;
            
            return controller;
        }

        // In-memory session implementation for tests
        public class TestSession : ISession
        {
            private readonly Dictionary<string, byte[]> _sessionStorage = new();
            public IEnumerable<string> Keys => _sessionStorage.Keys;
            public string Id => "TestSessionId";
            public bool IsAvailable => true;
            public void Clear() => _sessionStorage.Clear();
            public Task CommitAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
            public Task LoadAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
            public void Remove(string key) => _sessionStorage.Remove(key);
            public void Set(string key, byte[] value) => _sessionStorage[key] = value;
            public bool TryGetValue(string key, out byte[] value) => _sessionStorage.TryGetValue(key, out value);
            
            // Add the missing methods that the controller uses
            public string? GetString(string key)
            {
                if (_sessionStorage.TryGetValue(key, out var value))
                {
                    return System.Text.Encoding.UTF8.GetString(value);
                }
                return null;
            }
            
            public void SetString(string key, string value)
            {
                _sessionStorage[key] = System.Text.Encoding.UTF8.GetBytes(value);
            }
        }

        #region CreatePayment Tests

        [Fact]
        public void CreatePayment_ValidRequest_ReturnsOkWithPaymentUrl()
        {
            // Arrange
            var controller = BuildController();
            var request = new Controllers.PaymentRequest
            {
                Amount = 100000,
                OrderInfo = "Test payment",
                OrderId = "ORDER123"
            };
            var expectedPaymentUrl = "https://sandbox.vnpayment.vn/paymentv2/vpcpay.html?vnp_Amount=100000&vnp_Command=pay&vnp_CurrCode=VND&vnp_IpAddr=127.0.0.1&vnp_Locale=vn&vnp_OrderInfo=Test+payment&vnp_OrderType=other&vnp_ReturnUrl=https://localhost:5001/api/Payment/vnpay-return&vnp_TmnCode=TMNCODE&vnp_TxnRef=ORDER123&vnp_Version=2.1.0&vnp_SecureHash=abc123";
            
            _vnPayService.Setup(s => s.CreatePaymentUrl(
                request.Amount,
                request.OrderInfo,
                request.OrderId))
                .Returns(expectedPaymentUrl);

            // Act
            var result = controller.CreatePayment(request);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = okResult.Value;
            var paymentUrlProperty = response.GetType().GetProperty("paymentUrl");
            Assert.NotNull(paymentUrlProperty);
            Assert.Equal(expectedPaymentUrl, paymentUrlProperty.GetValue(response));
        }

        [Fact]
        public void CreatePayment_ServiceThrowsException_ReturnsBadRequest()
        {
            // Arrange
            var controller = BuildController();
            var request = new Controllers.PaymentRequest
            {
                Amount = 100000,
                OrderInfo = "Test payment",
                OrderId = "ORDER123"
            };
            
            _vnPayService.Setup(s => s.CreatePaymentUrl(
                It.IsAny<decimal>(),
                It.IsAny<string>(),
                It.IsAny<string>()))
                .Throws(new Exception("Payment service error"));

            // Act
            var result = controller.CreatePayment(request);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var response = badRequestResult.Value;
            var messageProperty = response.GetType().GetProperty("message");
            Assert.NotNull(messageProperty);
            Assert.Equal("Payment service error", messageProperty.GetValue(response));
        }

        [Fact]
        public void CreatePayment_NullRequest_ReturnsBadRequest()
        {
            // Arrange
            var controller = BuildController();

            // Act
            var result = controller.CreatePayment(null);

            // Assert
            Assert.IsType<BadRequestObjectResult>(result);
        }

        #endregion

        #region VNPayReturn Tests

        [Fact]
        public async Task VNPayReturn_SuccessfulPayment_ReturnsRedirectToSuccess()
        {
            // Arrange
            var controller = BuildController();
            var session = (TestSession)((DefaultHttpContext)controller.ControllerContext.HttpContext).Session;
            
            session.SetString("SelectedFoods_INV123", JsonConvert.SerializeObject(new List<FoodViewModel>()));

            var invoice = CreateTestInvoice();

            _invoiceService.Setup(s => s.GetById("INV123")).Returns(invoice);
            _seatService.Setup(s => s.GetSeatsByNames(It.IsAny<List<string>>())).Returns(new List<Seat>());
            _seatService.Setup(s => s.GetSeatsWithTypeByIds(It.IsAny<List<int>>())).Returns(new List<Seat>());
            _scheduleSeatService.Setup(s => s.GetByInvoiceId("INV123")).Returns(new List<ScheduleSeat>());
            _scheduleSeatService.Setup(s => s.UpdateScheduleSeatsStatusAsync("INV123", 2)).Returns(Task.CompletedTask);
            _invoiceService.Setup(s => s.Update(It.IsAny<Invoice>())).Verifiable();
            _invoiceService.Setup(s => s.Save()).Verifiable();

            // Setup SignalR
            _clientProxy.Setup(c => c.SendCoreAsync(
                "DashboardUpdated",
                It.IsAny<object[]>(),
                It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            var model = new VnPayReturnModel
            {
                vnp_ResponseCode = "00",
                vnp_TxnRef = "INV123"
            };

            // Act
            var result = await controller.VNPayReturn(model);

            // Assert
            Assert.IsType<RedirectToActionResult>(result);
            var redirectResult = (RedirectToActionResult)result;
            Assert.Equal("Success", redirectResult.ActionName);
            Assert.Equal("Booking", redirectResult.ControllerName);
        }

        [Fact]
        public async Task VNPayReturn_FailedPayment_ReturnsRedirectToFailed()
        {
            // Arrange
            var controller = BuildController();
            var invoice = CreateTestInvoice();

            _invoiceService.Setup(s => s.GetById("INV123")).Returns(invoice);
            _invoiceService.Setup(s => s.Update(It.IsAny<Invoice>())).Verifiable();
            _invoiceService.Setup(s => s.Save()).Verifiable();

            var model = new VnPayReturnModel
            {
                vnp_Amount = "10000000",
                vnp_OrderInfo = "Thanh toan don hang",
                vnp_ResponseCode = "99", // Failed response code
                vnp_TmnCode = "TMNCODE",
                vnp_TransactionStatus = "99",
                vnp_TxnRef = "INV123",
                vnp_SecureHash = "valid_hash"
            };

            // Act
            var result = await controller.VNPayReturn(model);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Failed", redirectResult.ActionName);
            Assert.Equal("Booking", redirectResult.ControllerName);

            // Verify invoice was updated to incomplete status
            _invoiceService.Verify(s => s.Update(It.Is<Invoice>(i => i.Status == InvoiceStatus.Incomplete)), Times.Once);
            _invoiceService.Verify(s => s.Save(), Times.Once);
        }

        [Fact]
        public async Task VNPayReturn_NullModel_ReturnsRedirectToFailed()
        {
            // Arrange
            var controller = BuildController();

            // Act
            var result = await controller.VNPayReturn(null);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Failed", redirectResult.ActionName);
            Assert.Equal("Booking", redirectResult.ControllerName);
        }

        [Fact]
        public async Task VNPayReturn_InvoiceNotFound_HandlesGracefully()
        {
            // Arrange
            var controller = BuildController();
            var session = (TestSession)((DefaultHttpContext)controller.ControllerContext.HttpContext).Session;
            
            // Set session data
            session.SetString("SelectedFoods_INV123", JsonConvert.SerializeObject(new List<FoodViewModel>()));

            _invoiceService.Setup(s => s.GetById("INV123")).Returns((Invoice)null);

            var model = new VnPayReturnModel
            {
                vnp_Amount = "10000000",
                vnp_OrderInfo = "Thanh toan don hang",
                vnp_ResponseCode = "00",
                vnp_TmnCode = "TMNCODE",
                vnp_TransactionStatus = "00",
                vnp_TxnRef = "INV123",
                vnp_SecureHash = "valid_hash"
            };

            // Act
            var result = await controller.VNPayReturn(model);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Success", redirectResult.ActionName);
            Assert.Equal("Booking", redirectResult.ControllerName);
        }

        [Fact]
        public async Task VNPayReturn_WithVoucher_MarksVoucherAsUsed()
        {
            // Arrange
            var controller = BuildController();
            var session = (TestSession)((DefaultHttpContext)controller.ControllerContext.HttpContext).Session;
            
            session.SetString("SelectedFoods_INV123", JsonConvert.SerializeObject(new List<FoodViewModel>()));

            var invoice = CreateTestInvoice();
            invoice.VoucherId = "VOUCHER123";

            _invoiceService.Setup(s => s.GetById("INV123")).Returns(invoice);
            _seatService.Setup(s => s.GetSeatsByNames(It.IsAny<List<string>>())).Returns(new List<Seat>());
            _seatService.Setup(s => s.GetSeatsWithTypeByIds(It.IsAny<List<int>>())).Returns(new List<Seat>());
            _scheduleSeatService.Setup(s => s.GetByInvoiceId("INV123")).Returns(new List<ScheduleSeat>());
            _scheduleSeatService.Setup(s => s.UpdateScheduleSeatsStatusAsync("INV123", 2)).Returns(Task.CompletedTask);
            _invoiceService.Setup(s => s.Update(It.IsAny<Invoice>())).Verifiable();
            _invoiceService.Setup(s => s.Save()).Verifiable();
            _voucherService.Setup(s => s.MarkVoucherAsUsedAsync("VOUCHER123")).Returns(Task.CompletedTask).Verifiable();

            // Setup SignalR
            _clientProxy.Setup(c => c.SendCoreAsync(
                "DashboardUpdated",
                It.IsAny<object[]>(),
                It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            var model = new VnPayReturnModel
            {
                vnp_ResponseCode = "00",
                vnp_TxnRef = "INV123"
            };

            // Act
            var result = await controller.VNPayReturn(model);

            // Assert
            Assert.IsType<RedirectToActionResult>(result);
            _voucherService.Verify(s => s.MarkVoucherAsUsedAsync("VOUCHER123"), Times.Once);
        }

        [Fact]
        public async Task VNPayReturn_WithFoodOrders_SavesFoodOrders()
        {
            // Arrange
            var controller = BuildController();
            var session = (TestSession)((DefaultHttpContext)controller.ControllerContext.HttpContext).Session;
            
            var foods = new List<FoodViewModel>
            {
                new FoodViewModel { FoodId = 1, Name = "Popcorn", Price = 50000, Quantity = 2 },
                new FoodViewModel { FoodId = 2, Name = "Coke", Price = 30000, Quantity = 1 }
            };
            session.SetString("SelectedFoods_INV123", JsonConvert.SerializeObject(foods));

            var invoice = CreateTestInvoice();

            _invoiceService.Setup(s => s.GetById("INV123")).Returns(invoice);
            _seatService.Setup(s => s.GetSeatsByNames(It.IsAny<List<string>>())).Returns(new List<Seat>());
            _seatService.Setup(s => s.GetSeatsWithTypeByIds(It.IsAny<List<int>>())).Returns(new List<Seat>());
            _scheduleSeatService.Setup(s => s.GetByInvoiceId("INV123")).Returns(new List<ScheduleSeat>());
            _scheduleSeatService.Setup(s => s.UpdateScheduleSeatsStatusAsync("INV123", 2)).Returns(Task.CompletedTask);
            _invoiceService.Setup(s => s.Update(It.IsAny<Invoice>())).Verifiable();
            _invoiceService.Setup(s => s.Save()).Verifiable();
            _foodInvoiceService.Setup(s => s.SaveFoodOrderAsync("INV123", It.IsAny<List<FoodViewModel>>())).Returns(Task.FromResult(true)).Verifiable();

            // Setup SignalR
            _clientProxy.Setup(c => c.SendCoreAsync(
                "DashboardUpdated",
                It.IsAny<object[]>(),
                It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            var model = new VnPayReturnModel
            {
                vnp_ResponseCode = "00",
                vnp_TxnRef = "INV123"
            };

            // Act
            var result = await controller.VNPayReturn(model);

            // Assert
            Assert.IsType<RedirectToActionResult>(result);
            _foodInvoiceService.Verify(s => s.SaveFoodOrderAsync("INV123", It.IsAny<List<FoodViewModel>>()), Times.Once);
        }

        [Fact]
        public async Task VNPayReturn_WithPromotionDiscount_CalculatesCorrectPrices()
        {
            // Arrange
            var controller = BuildController();
            var session = (TestSession)((DefaultHttpContext)controller.ControllerContext.HttpContext).Session;
            
            session.SetString("SelectedFoods_INV123", JsonConvert.SerializeObject(new List<FoodViewModel>()));

            var invoice = CreateTestInvoice();
            invoice.PromotionDiscount = "{\"seat\": 10}"; // 10% discount

            var seats = CreateTestSeats();
            var scheduleSeats = CreateTestScheduleSeats();

            _invoiceService.Setup(s => s.GetById("INV123")).Returns(invoice);
            _seatService.Setup(s => s.GetSeatsByNames(It.IsAny<List<string>>())).Returns(seats);
            _seatService.Setup(s => s.GetSeatsWithTypeByIds(It.IsAny<List<int>>())).Returns(seats);
            _scheduleSeatService.Setup(s => s.GetByInvoiceId("INV123")).Returns(scheduleSeats);
            _scheduleSeatService.Setup(s => s.CreateMultipleScheduleSeatsAsync(It.IsAny<List<ScheduleSeat>>())).Returns(Task.FromResult(true));
            _scheduleSeatService.Setup(s => s.UpdateScheduleSeatsStatusAsync("INV123", 2)).Returns(Task.CompletedTask);
            _scheduleSeatService.Setup(s => s.Update(It.IsAny<ScheduleSeat>())).Verifiable();
            _scheduleSeatService.Setup(s => s.Save()).Verifiable();
            _invoiceService.Setup(s => s.Update(It.IsAny<Invoice>())).Verifiable();
            _invoiceService.Setup(s => s.Save()).Verifiable();

            // Setup SignalR
            _clientProxy.Setup(c => c.SendCoreAsync(
                "DashboardUpdated",
                It.IsAny<object[]>(),
                It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            var model = new VnPayReturnModel
            {
                vnp_ResponseCode = "00",
                vnp_TxnRef = "INV123"
            };

            // Act
            var result = await controller.VNPayReturn(model);

            // Assert
            Assert.IsType<RedirectToActionResult>(result);
            _scheduleSeatService.Verify(s => s.Update(It.Is<ScheduleSeat>(ss => ss.BookedPrice.HasValue)), Times.AtLeastOnce);
        }

        #endregion

        #region VNPayIpn Tests

        [Fact]
        public void VNPayIpn_ValidSignature_ReturnsSuccess()
        {
            // Arrange
            var controller = BuildController();
            
            // Setup query parameters
            var queryCollection = new QueryCollection(new Dictionary<string, StringValues>
            {
                { "vnp_Amount", "10000000" },
                { "vnp_OrderInfo", "Test payment" },
                { "vnp_ResponseCode", "00" },
                { "vnp_TxnRef", "INV123" },
                { "vnp_SecureHash", "valid_hash" }
            });

            controller.ControllerContext.HttpContext.Request.Query = queryCollection;

            _vnPayService.Setup(s => s.ValidateSignature(It.IsAny<Dictionary<string, string>>(), "valid_hash"))
                .Returns(true);

            // Act
            var result = controller.VNPayIpn();

            // Assert
            var contentResult = Assert.IsType<ContentResult>(result);
            Assert.Equal("00", contentResult.Content);
        }

        [Fact]
        public void VNPayIpn_InvalidSignature_ReturnsError()
        {
            // Arrange
            var controller = BuildController();
            
            // Setup query parameters
            var queryCollection = new QueryCollection(new Dictionary<string, StringValues>
            {
                { "vnp_Amount", "10000000" },
                { "vnp_OrderInfo", "Test payment" },
                { "vnp_ResponseCode", "00" },
                { "vnp_TxnRef", "INV123" },
                { "vnp_SecureHash", "invalid_hash" }
            });

            controller.ControllerContext.HttpContext.Request.Query = queryCollection;

            _vnPayService.Setup(s => s.ValidateSignature(It.IsAny<Dictionary<string, string>>(), "invalid_hash"))
                .Returns(false);

            // Act
            var result = controller.VNPayIpn();

            // Assert
            var contentResult = Assert.IsType<ContentResult>(result);
            Assert.Equal("97", contentResult.Content);
        }

        #endregion

        #region Helper Methods

        private Invoice CreateTestInvoice()
        {
            return new Invoice
            {
                InvoiceId = "INV123",
                MovieShowId = 1,
                Seat = "A1,A2",
                TotalMoney = 100000,
                AccountId = "ACC123",
                Status = InvoiceStatus.Incomplete,
                BookingDate = DateTime.Now,
                PromotionDiscount = "0",
                AddScore = 0,
                UseScore = 0,
                SeatIds = "1,2",
                MovieShow = new MovieShow
                {
                    MovieShowId = 1,
                    Movie = new Movie { MovieNameEnglish = "Test Movie" },
                    ShowDate = DateOnly.FromDateTime(DateTime.Now),
                    Schedule = new Schedule { ScheduleTime = TimeOnly.FromTimeSpan(TimeSpan.FromHours(10)) },
                    CinemaRoom = new CinemaRoom { CinemaRoomName = "Room 1" },
                    Version = new MovieTheater.Models.Version { VersionName = "2D", Multi = 1.0m }
                },
                ScheduleSeats = new List<ScheduleSeat>
                {
                    new ScheduleSeat
                    {
                        ScheduleSeatId = 1,
                        SeatId = 1,
                        BookedPrice = 50000,
                        Seat = new Seat
                        {
                            SeatId = 1,
                            SeatName = "A1",
                            SeatType = new SeatType { TypeName = "Standard", PricePercent = 1.0m }
                        }
                    },
                    new ScheduleSeat
                    {
                        ScheduleSeatId = 2,
                        SeatId = 2,
                        BookedPrice = 50000,
                        Seat = new Seat
                        {
                            SeatId = 2,
                            SeatName = "A2",
                            SeatType = new SeatType { TypeName = "Standard", PricePercent = 1.0m }
                        }
                    }
                }
            };
        }

        private List<Seat> CreateTestSeats()
        {
            return new List<Seat>
            {
                new Seat
                {
                    SeatId = 1,
                    SeatName = "A1",
                    SeatType = new SeatType { TypeName = "Standard", PricePercent = 1.0m }
                },
                new Seat
                {
                    SeatId = 2,
                    SeatName = "A2",
                    SeatType = new SeatType { TypeName = "Standard", PricePercent = 1.0m }
                }
            };
        }

        private List<ScheduleSeat> CreateTestScheduleSeats()
        {
            return new List<ScheduleSeat>
            {
                new ScheduleSeat
                {
                    ScheduleSeatId = 1,
                    SeatId = 1,
                    BookedPrice = 50000,
                    Seat = new Seat
                    {
                        SeatId = 1,
                        SeatName = "A1",
                        SeatType = new SeatType { TypeName = "Standard", PricePercent = 1.0m }
                    }
                },
                new ScheduleSeat
                {
                    ScheduleSeatId = 2,
                    SeatId = 2,
                    BookedPrice = 50000,
                    Seat = new Seat
                    {
                        SeatId = 2,
                        SeatName = "A2",
                        SeatType = new SeatType { TypeName = "Standard", PricePercent = 1.0m }
                    }
                }
            };
        }

        private Member CreateTestMember()
        {
            return new Member
            {
                MemberId = "MEMBER123",
                Account = new Account
                {
                    AccountId = "ACC123",
                    Rank = new Rank
                    {
                        RankId = 1,
                        PointEarningPercentage = 1.0m
                    }
                }
            };
        }

        #endregion
    }
} 