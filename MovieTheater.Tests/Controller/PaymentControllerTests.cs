using Xunit;
using Moq;
using Microsoft.Extensions.Logging;
using MovieTheater.Controllers;
using MovieTheater.Service;
using MovieTheater.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using MovieTheater.Hubs;
using MovieTheater.Models;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using System;
using ControllersPaymentRequest = MovieTheater.Controllers.PaymentRequest;

namespace MovieTheater.Tests.Controller
{
    public class PaymentControllerTests
    {
        private PaymentController CreateController(
            Mock<IVNPayService> vnPayServiceMock = null,
            Mock<ILogger<PaymentController>> loggerMock = null,
            Mock<IAccountService> accountServiceMock = null,
            Mock<MovieTheaterContext> contextMock = null,
            Mock<IFoodInvoiceService> foodInvoiceServiceMock = null,
            Mock<IHubContext<DashboardHub>> hubContextMock = null,
            ISession session = null
        )
        {
            vnPayServiceMock ??= new Mock<IVNPayService>();
            loggerMock ??= new Mock<ILogger<PaymentController>>();
            accountServiceMock ??= new Mock<IAccountService>();
            contextMock ??= new Mock<MovieTheaterContext>();
            foodInvoiceServiceMock ??= new Mock<IFoodInvoiceService>();
            hubContextMock ??= new Mock<IHubContext<DashboardHub>>();

            var controller = new PaymentController(
                vnPayServiceMock.Object,
                loggerMock.Object,
                accountServiceMock.Object,
                contextMock.Object,
                foodInvoiceServiceMock.Object,
                hubContextMock.Object
            );

            if (session != null)
            {
                var httpContext = new DefaultHttpContext();
                httpContext.Session = session;
                controller.ControllerContext = new ControllerContext
                {
                    HttpContext = httpContext
                };
            }

            return controller;
        }

        [Fact]
        public void CreatePayment_ReturnsOk_WhenSuccess()
        {
            // Arrange
            var vnPayServiceMock = new Mock<IVNPayService>();
            vnPayServiceMock.Setup(s => s.CreatePaymentUrl(It.IsAny<decimal>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns("http://test-payment-url");
            var controller = CreateController(vnPayServiceMock: vnPayServiceMock);
            var request = new ControllersPaymentRequest { Amount = 100, OrderInfo = "info", OrderId = "id" };

            // Act
            var result = controller.CreatePayment(request) as OkObjectResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal(200, result.StatusCode);
            Assert.Contains("paymentUrl", result.Value.ToString());
        }

        [Fact]
        public void CreatePayment_ReturnsBadRequest_OnException()
        {
            // Arrange
            var vnPayServiceMock = new Mock<IVNPayService>();
            vnPayServiceMock.Setup(s => s.CreatePaymentUrl(It.IsAny<decimal>(), It.IsAny<string>(), It.IsAny<string>()))
                .Throws(new System.Exception("error"));
            var controller = CreateController(vnPayServiceMock: vnPayServiceMock);
            var request = new ControllersPaymentRequest { Amount = 100, OrderInfo = "info", OrderId = "id" };

            // Act
            var result = controller.CreatePayment(request) as BadRequestObjectResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal(400, result.StatusCode);
            Assert.Contains("message", result.Value.ToString());
        }

        [Fact]
        public async Task VNPayIpn_Returns00_WhenSignatureValid()
        {
            // Arrange
            var vnPayServiceMock = new Mock<IVNPayService>();
            vnPayServiceMock.Setup(s => s.ValidateSignature(It.IsAny<IDictionary<string, string>>(), It.IsAny<string>())).Returns(true);
            var controller = CreateController(vnPayServiceMock: vnPayServiceMock);
            var httpContext = new DefaultHttpContext();
            httpContext.Request.QueryString = new QueryString("?vnp_SecureHash=abc&vnp_Amount=100");
            controller.ControllerContext = new ControllerContext { HttpContext = httpContext };

            // Act
            var result = controller.VNPayIpn() as ContentResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal("00", result.Content);
        }

        [Fact]
        public void VNPayIpn_Returns97_WhenSignatureInvalid()
        {
            // Arrange
            var vnPayServiceMock = new Mock<IVNPayService>();
            vnPayServiceMock.Setup(s => s.ValidateSignature(It.IsAny<IDictionary<string, string>>(), It.IsAny<string>())).Returns(false);
            var controller = CreateController(vnPayServiceMock: vnPayServiceMock);
            var httpContext = new DefaultHttpContext();
            httpContext.Request.QueryString = new QueryString("?vnp_SecureHash=abc&vnp_Amount=100");
            controller.ControllerContext = new ControllerContext { HttpContext = httpContext };

            // Act
            var result = controller.VNPayIpn() as ContentResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal("97", result.Content);
        }

        // Lưu ý: Test cho VNPayReturn rất phức tạp do phụ thuộc nhiều vào DbContext, TempData, Session, các entity liên quan.
        // Nếu bạn cần test sâu hơn cho VNPayReturn, nên sử dụng in-memory database hoặc integration test.

        [Fact]
        public async Task VNPayReturn_SuccessfulPayment_UpdatesInvoiceAndRedirects()
        {
            // Arrange: Tạo in-memory DbContext với tracking và logging
            var options = new DbContextOptionsBuilder<MovieTheater.Models.MovieTheaterContext>()
                .UseInMemoryDatabase(databaseName: "VNPayReturn_Success_" + Guid.NewGuid().ToString())
                .EnableSensitiveDataLogging()
                .EnableDetailedErrors()
                .Options;
            using var context = new MovieTheater.Models.MovieTheaterContext(options);

            // Seed dữ liệu theo thứ tự đúng để đảm bảo foreign key constraints
            var rank = new Rank { RankId = 1, PointEarningPercentage = 1 };
            context.Ranks.Add(rank);
            context.SaveChanges();

            var account = new Account { AccountId = "acc1", RankId = 1 };
            context.Accounts.Add(account);
            context.SaveChanges();

            var member = new Member { MemberId = "mem1", AccountId = "acc1" };
            context.Members.Add(member);
            context.SaveChanges();

            var movie = new Movie { MovieId = "M1", MovieNameEnglish = "Test Movie" };
            context.Movies.Add(movie);
            context.SaveChanges();

            var cinemaRoom = new CinemaRoom { CinemaRoomId = 1, CinemaRoomName = "Room 1" };
            context.CinemaRooms.Add(cinemaRoom);
            context.SaveChanges();

            var schedule = new Schedule { ScheduleId = 1, ScheduleTime = TimeOnly.Parse("10:00") };
            context.Schedules.Add(schedule);
            context.SaveChanges();

            var movieShow = new MovieShow 
            { 
                MovieShowId = 1, 
                MovieId = "M1", 
                CinemaRoomId = 1, 
                ScheduleId = 1, 
                ShowDate = DateOnly.FromDateTime(DateTime.Today) 
            };
            context.MovieShows.Add(movieShow);
            context.SaveChanges();

            var seatType = new SeatType { SeatTypeId = 1, PricePercent = 100, ColorHex = "#FFFFFF", TypeName = "Standard" };
            context.SeatTypes.Add(seatType);
            context.SaveChanges();

            var seat = new Seat { SeatId = 1, SeatName = "A1", SeatTypeId = 1 };
            context.Seats.Add(seat);
            context.SaveChanges();

            var invoice = new Invoice
            {
                InvoiceId = "INV1",
                MovieShowId = 1,
                Seat = "A1",
                Status = InvoiceStatus.Incomplete,
                TotalMoney = 100,
                AccountId = "acc1",
                BookingDate = DateTime.Now
            };
            context.Invoices.Add(invoice);
            context.SaveChanges();

            // Kiểm tra dữ liệu đã được seed đúng cách
            var checkInvoice = context.Invoices
                .Include(i => i.MovieShow)
                    .ThenInclude(ms => ms.Movie)
                .Include(i => i.MovieShow)
                    .ThenInclude(ms => ms.CinemaRoom)
                .Include(i => i.MovieShow)
                    .ThenInclude(ms => ms.Schedule)
                .FirstOrDefault(i => i.InvoiceId == "INV1");

            Assert.NotNull(checkInvoice);
            Assert.NotNull(checkInvoice.Seat);
            Assert.NotNull(checkInvoice.MovieShow);
            Assert.NotNull(checkInvoice.MovieShow.Movie);
            Assert.NotNull(checkInvoice.MovieShow.CinemaRoom);
            Assert.NotNull(checkInvoice.MovieShow.Schedule);

            // Mock các service phụ thuộc
            var vnPayServiceMock = new Mock<IVNPayService>();
            var loggerMock = new Mock<ILogger<PaymentController>>();
            var accountServiceMock = new Mock<IAccountService>();
            var foodInvoiceServiceMock = new Mock<IFoodInvoiceService>();
            var hubContextMock = new Mock<IHubContext<DashboardHub>>();

            // Mock session
            var httpContext = new DefaultHttpContext();
            httpContext.Session = new TestSession();

            // Mock TempData
            var tempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>());
            tempData["MovieShowId"] = 1;
            tempData["InvoiceId"] = "INV1";

            // Controller
            var controller = new PaymentController(
                vnPayServiceMock.Object,
                loggerMock.Object,
                accountServiceMock.Object,
                context,
                foodInvoiceServiceMock.Object,
                hubContextMock.Object
            )
            {
                ControllerContext = new ControllerContext { HttpContext = httpContext },
                TempData = tempData
            };

            // Model trả về từ VNPay
            var model = new VnPayReturnModel
            {
                vnp_TxnRef = "INV1",
                vnp_ResponseCode = "00"
            };

            // Act
            var result = await controller.VNPayReturn(model);

            // Assert
            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Success", redirect.ActionName);
            Assert.Equal("Booking", redirect.ControllerName);

            // Kiểm tra invoice đã được cập nhật trạng thái
            var updatedInvoice = context.Invoices.First(i => i.InvoiceId == "INV1");
            Assert.Equal(InvoiceStatus.Completed, updatedInvoice.Status);
        }
    }

    // Thêm class TestSession để giả lập ISession
    public class TestSession : ISession
    {
        private readonly Dictionary<string, byte[]> _sessionStorage = new();
        public IEnumerable<string> Keys => _sessionStorage.Keys;
        public string Id => Guid.NewGuid().ToString();
        public bool IsAvailable => true;
        public void Clear() => _sessionStorage.Clear();
        public Task CommitAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task LoadAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public void Remove(string key) => _sessionStorage.Remove(key);
        public void Set(string key, byte[] value) => _sessionStorage[key] = value;
        public bool TryGetValue(string key, out byte[] value) => _sessionStorage.TryGetValue(key, out value);
    }
} 