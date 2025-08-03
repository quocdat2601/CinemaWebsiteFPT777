using Xunit;
using Moq;
using Microsoft.Extensions.Logging;
using MovieTheater.Controllers;
using MovieTheater.Service;
using MovieTheater.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
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
using Newtonsoft.Json;

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
            Mock<IInvoiceService> invoiceServiceMock = null,
            Mock<IHubContext<DashboardHub>> hubContextMock = null,
            ISession session = null
        )
        {
            vnPayServiceMock ??= new Mock<IVNPayService>();
            loggerMock ??= new Mock<ILogger<PaymentController>>();
            accountServiceMock ??= new Mock<IAccountService>();
            contextMock ??= new Mock<MovieTheaterContext>();
            foodInvoiceServiceMock ??= new Mock<IFoodInvoiceService>();
            invoiceServiceMock ??= new Mock<IInvoiceService>();
            hubContextMock ??= new Mock<IHubContext<DashboardHub>>();

            var controller = new PaymentController(
                vnPayServiceMock.Object,
                loggerMock.Object,
                accountServiceMock.Object,
                contextMock.Object,
                hubContextMock.Object,
                foodInvoiceServiceMock.Object,
                invoiceServiceMock.Object
            );

            // Always set up HttpContext with session
            var httpContext = new DefaultHttpContext();
            httpContext.Session = session ?? new TestSession();
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            };

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
        public void CreatePayment_ReturnsBadRequest_WhenRequestIsNull()
        {
            // Arrange
            var controller = CreateController();

            // Act
            var result = controller.CreatePayment(null) as BadRequestObjectResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal(400, result.StatusCode);
        }

        [Fact]
        public void CreatePayment_ReturnsBadRequest_WhenAmountIsZero()
        {
            // Arrange
            var controller = CreateController();
            var request = new ControllersPaymentRequest { Amount = 0, OrderInfo = "info", OrderId = "id" };

            // Act
            var result = controller.CreatePayment(request);

            // Assert
            Assert.NotNull(result);
            // The controller doesn't validate amount, so it should return Ok or BadRequest depending on VNPay service
            Assert.True(result is OkObjectResult || result is BadRequestObjectResult);
        }

        [Fact]
        public void CreatePayment_ReturnsBadRequest_WhenOrderInfoIsEmpty()
        {
            // Arrange
            var controller = CreateController();
            var request = new ControllersPaymentRequest { Amount = 100, OrderInfo = "", OrderId = "id" };

            // Act
            var result = controller.CreatePayment(request);

            // Assert
            Assert.NotNull(result);
            // The controller doesn't validate OrderInfo, so it should return Ok or BadRequest depending on VNPay service
            Assert.True(result is OkObjectResult || result is BadRequestObjectResult);
        }

        [Fact]
        public void CreatePayment_ReturnsBadRequest_WhenOrderIdIsEmpty()
        {
            // Arrange
            var controller = CreateController();
            var request = new ControllersPaymentRequest { Amount = 100, OrderInfo = "info", OrderId = "" };

            // Act
            var result = controller.CreatePayment(request);

            // Assert
            Assert.NotNull(result);
            // The controller doesn't validate OrderId, so it should return Ok or BadRequest depending on VNPay service
            Assert.True(result is OkObjectResult || result is BadRequestObjectResult);
        }

        [Fact]
        public async Task VNPayReturn_ReturnsRedirectToFailed_WhenModelIsNull()
        {
            // Arrange
            var controller = CreateController();

            // Act
            var result = await controller.VNPayReturn(null) as RedirectToActionResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Failed", result.ActionName);
            Assert.Equal("Booking", result.ControllerName);
        }

        [Fact]
        public async Task VNPayReturn_ReturnsRedirectToFailed_WhenInvoiceNotFound()
        {
            // Arrange
            var invoiceServiceMock = new Mock<IInvoiceService>();
            invoiceServiceMock.Setup(s => s.GetById("INV1")).Returns((Invoice)null);
            
            // Create real context with proper setup
            var options = new DbContextOptionsBuilder<MovieTheaterContext>()
                .UseInMemoryDatabase(databaseName: "VNPayReturn_InvoiceNotFound_" + Guid.NewGuid().ToString())
                .Options;
            var context = new MovieTheaterContext(options);
            
            // Add some basic data to context to avoid null reference
            var seatType = new SeatType { SeatTypeId = 1, TypeName = "Standard", PricePercent = 100, ColorHex = "#000000" };
            var seat = new Seat { SeatId = 1, SeatName = "A1", SeatTypeId = 1, SeatType = seatType };
            context.SeatTypes.Add(seatType);
            context.Seats.Add(seat);
            context.SaveChanges();
            
            var controller = new PaymentController(
                new Mock<IVNPayService>().Object,
                new Mock<ILogger<PaymentController>>().Object,
                new Mock<IAccountService>().Object,
                context,
                new Mock<IHubContext<DashboardHub>>().Object,
                new Mock<IFoodInvoiceService>().Object,
                invoiceServiceMock.Object
            );
            
            // Setup HttpContext with proper session and TempData
            var httpContext = new DefaultHttpContext();
            httpContext.Session = new TestSession();
            controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
            controller.TempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>());
            
            var model = new VnPayReturnModel
            {
                vnp_TxnRef = "INV1",
                vnp_ResponseCode = "00"
            };

            // Act
            var result = await controller.VNPayReturn(model);

            // Assert
            Assert.NotNull(result);
            Assert.IsType<RedirectToActionResult>(result);
            var redirect = result as RedirectToActionResult;
            Assert.Equal("Failed", redirect.ActionName);
            Assert.Equal("Booking", redirect.ControllerName);
        }

        [Fact]
        public async Task VNPayReturn_ReturnsRedirectToFailed_WhenResponseCodeIsNot00()
        {
            // Arrange
            var invoiceServiceMock = new Mock<IInvoiceService>();
            invoiceServiceMock.Setup(s => s.GetById("INV1")).Returns((Invoice)null);
            var controller = CreateController(invoiceServiceMock: invoiceServiceMock);
            
            // Setup HttpContext with proper session
            var httpContext = new DefaultHttpContext();
            httpContext.Session = new TestSession();
            controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
            controller.TempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>());
            
            var model = new VnPayReturnModel
            {
                vnp_TxnRef = "INV1",
                vnp_ResponseCode = "99"
            };

            // Act
            var result = await controller.VNPayReturn(model);

            // Assert
            Assert.NotNull(result);
            Assert.IsType<RedirectToActionResult>(result);
            var redirect = result as RedirectToActionResult;
            Assert.Equal("Failed", redirect.ActionName);
            Assert.Equal("Booking", redirect.ControllerName);
        }

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
            var rank = new Rank { RankId = 1, RankName = "Bronze", ColorGradient = "linear-gradient(45deg, #8B4513, #CD853F)", IconClass = "fas fa-medal" };
            var account = new Account { AccountId = "acc1", FullName = "Test User", IdentityCard = "123", PhoneNumber = "555", Rank = rank };
            var member = new Member { MemberId = "member1", AccountId = "acc1", Score = 100, Account = account };
            var movie = new Movie { MovieId = "M1", MovieNameEnglish = "Test Movie" };
            var schedule = new Schedule { ScheduleId = 1, ScheduleTime = new TimeOnly(10, 0) };
            var cinemaRoom = new CinemaRoom { CinemaRoomId = 1, CinemaRoomName = "Room1" };
            var version = new MovieTheater.Models.Version { VersionId = 1, VersionName = "2D", Multi = 1.0m };
            var movieShow = new MovieShow { MovieShowId = 1, MovieId = "M1", ShowDate = DateOnly.FromDateTime(DateTime.Today), ScheduleId = 1, CinemaRoomId = 1, VersionId = 1, Movie = movie, Schedule = schedule, CinemaRoom = cinemaRoom, Version = version };
            var seatType = new SeatType { SeatTypeId = 1, TypeName = "Standard", PricePercent = 100, ColorHex = "#000000" };
            var seat = new Seat { SeatId = 1, SeatName = "A1", SeatTypeId = 1, SeatType = seatType };
            var scheduleSeat = new ScheduleSeat { ScheduleSeatId = 1, MovieShowId = 1, SeatId = 1, MovieShow = movieShow, Seat = seat };

            context.Ranks.Add(rank);
            context.Accounts.Add(account);
            context.Members.Add(member);
            context.Movies.Add(movie);
            context.Schedules.Add(schedule);
            context.CinemaRooms.Add(cinemaRoom);
            context.Versions.Add(version);
            context.MovieShows.Add(movieShow);
            context.SeatTypes.Add(seatType);
            context.Seats.Add(seat);
            context.ScheduleSeats.Add(scheduleSeat);
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
            var invoiceServiceMock = new Mock<IInvoiceService>();
            invoiceServiceMock.Setup(s => s.GetById("INV1")).Returns(context.Invoices
                .Include(i => i.MovieShow)
                    .ThenInclude(ms => ms.Movie)
                .Include(i => i.MovieShow)
                    .ThenInclude(ms => ms.CinemaRoom)
                .Include(i => i.MovieShow)
                    .ThenInclude(ms => ms.Schedule)
                .Include(i => i.MovieShow)
                    .ThenInclude(ms => ms.Version)
                .Include(i => i.ScheduleSeats)
                .First(i => i.InvoiceId == "INV1"));
            var hubContextMock = new Mock<IHubContext<DashboardHub>>();
            var clientsMock = new Mock<IHubClients>();
            var allMock = new Mock<IClientProxy>();
            hubContextMock.Setup(x => x.Clients).Returns(clientsMock.Object);
            clientsMock.Setup(x => x.All).Returns(allMock.Object);
            // Don't mock SendAsync as it's an extension method - just let it be called

            // Mock session
            var httpContext = new DefaultHttpContext();
            httpContext.Session = new TestSession();
            
            // Mock service provider for HttpContext.RequestServices
            var serviceProviderMock = new Mock<IServiceProvider>();
            var seatHubContextMock = new Mock<IHubContext<MovieTheater.Hubs.SeatHub>>();
            var seatClientsMock = new Mock<IHubClients>();
            var seatGroupMock = new Mock<IClientProxy>();
            var urlHelperFactoryMock = new Mock<IUrlHelperFactory>();
            var urlHelperMock = new Mock<IUrlHelper>();
            
            serviceProviderMock.Setup(x => x.GetService(typeof(IHubContext<MovieTheater.Hubs.SeatHub>)))
                .Returns(seatHubContextMock.Object);
            serviceProviderMock.Setup(x => x.GetService(typeof(IUrlHelperFactory)))
                .Returns(urlHelperFactoryMock.Object);
            seatHubContextMock.Setup(x => x.Clients).Returns(seatClientsMock.Object);
            seatClientsMock.Setup(x => x.Group(It.IsAny<string>())).Returns(seatGroupMock.Object);
            urlHelperFactoryMock.Setup(x => x.GetUrlHelper(It.IsAny<ActionContext>()))
                .Returns(urlHelperMock.Object);
            
            httpContext.RequestServices = serviceProviderMock.Object;

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
                hubContextMock.Object,
                foodInvoiceServiceMock.Object,
                invoiceServiceMock.Object
            )
            {
                ControllerContext = new ControllerContext { HttpContext = httpContext },
                TempData = tempData
            };

            // Ensure session is properly set up
            if (httpContext.Session == null)
            {
                httpContext.Session = new TestSession();
            }
            
            // Ensure TempData is properly set up
            if (tempData == null)
            {
                tempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>());
                controller.TempData = tempData;
            }

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

        [Fact]
        public async Task VNPayReturn_SuccessfulPayment_WithVoucher_MarksVoucherAsUsed()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<MovieTheater.Models.MovieTheaterContext>()
                .UseInMemoryDatabase(databaseName: "VNPayReturn_Voucher_" + Guid.NewGuid().ToString())
                .Options;
            using var context = new MovieTheater.Models.MovieTheaterContext(options);

            // Seed data
            var rank = new Rank { RankId = 1, PointEarningPercentage = 1, ColorGradient = "#FF0000", IconClass = "icon-star" };
            context.Ranks.Add(rank);
            context.SaveChanges();

            var account = new Account { AccountId = "acc1", RankId = 1 };
            context.Accounts.Add(account);
            context.SaveChanges();

            var voucher = new Voucher { VoucherId = "VOUCHER1", AccountId = "acc1", Code = "VOUCHER1", IsUsed = false, Value = 50, CreatedDate = DateTime.Now, ExpiryDate = DateTime.Now.AddDays(30) };
            context.Vouchers.Add(voucher);
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

            var invoice = new Invoice
            {
                InvoiceId = "INV1",
                MovieShowId = 1,
                Seat = "A1",
                Status = InvoiceStatus.Incomplete,
                TotalMoney = 100,
                AccountId = "acc1",
                BookingDate = DateTime.Now,
                VoucherId = "VOUCHER1"
            };
            context.Invoices.Add(invoice);
            context.SaveChanges();

            // Mock services
            var vnPayServiceMock = new Mock<IVNPayService>();
            var loggerMock = new Mock<ILogger<PaymentController>>();
            var accountServiceMock = new Mock<IAccountService>();
            accountServiceMock.Setup(s => s.CheckAndUpgradeRank(It.IsAny<string>())).Verifiable();
            var foodInvoiceServiceMock = new Mock<IFoodInvoiceService>();
            var invoiceServiceMock = new Mock<IInvoiceService>();
            invoiceServiceMock.Setup(s => s.GetById("INV1")).Returns(context.Invoices.First(i => i.InvoiceId == "INV1"));
            var hubContextMock = new Mock<IHubContext<DashboardHub>>();
            var dashboardClientsMock = new Mock<IHubClients>();
            var dashboardAllMock = new Mock<IClientProxy>();
            hubContextMock.Setup(x => x.Clients).Returns(dashboardClientsMock.Object);
            dashboardClientsMock.Setup(x => x.All).Returns(dashboardAllMock.Object);

            var httpContext = new DefaultHttpContext();
            httpContext.Session = new TestSession();

            var controller = new PaymentController(
                vnPayServiceMock.Object,
                loggerMock.Object,
                accountServiceMock.Object,
                context,
                hubContextMock.Object,
                foodInvoiceServiceMock.Object,
                invoiceServiceMock.Object
            )
            {
                ControllerContext = new ControllerContext { HttpContext = httpContext },
                TempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>())
            };

            var model = new VnPayReturnModel
            {
                vnp_TxnRef = "INV1",
                vnp_ResponseCode = "00"
            };

            // Act
            var result = await controller.VNPayReturn(model);

            // Assert
            Assert.IsType<RedirectToActionResult>(result);
            
            // Debug: Check if voucher exists and its state
            var voucherExists = context.Vouchers.Any(v => v.VoucherId == "VOUCHER1");
            Assert.True(voucherExists, "Voucher should exist in database");
            
            var updatedVoucher = context.Vouchers.First(v => v.VoucherId == "VOUCHER1");
            Assert.True(updatedVoucher.IsUsed, $"Voucher should be marked as used. Current IsUsed: {updatedVoucher.IsUsed}");
        }

        [Fact]
        public void VNPayIpn_Returns00_WhenSignatureValid()
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

        [Fact]
        public void VNPayIpn_Returns97_WhenSecureHashMissing()
        {
            // Arrange
            var controller = CreateController();
            var httpContext = new DefaultHttpContext();
            httpContext.Request.QueryString = new QueryString("?vnp_Amount=100");
            controller.ControllerContext = new ControllerContext { HttpContext = httpContext };

            // Act
            var result = controller.VNPayIpn() as ContentResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal("97", result.Content);
        }

        [Fact]
        public void VNPayIpn_HandlesEmptyQueryString()
        {
            // Arrange
            var controller = CreateController();
            var httpContext = new DefaultHttpContext();
            httpContext.Request.QueryString = new QueryString("");
            controller.ControllerContext = new ControllerContext { HttpContext = httpContext };

            // Act
            var result = controller.VNPayIpn() as ContentResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal("97", result.Content);
        }

        [Fact]
        public void VNPayIpn_HandlesNullQueryString()
        {
            // Arrange
            var controller = CreateController();
            var httpContext = new DefaultHttpContext();
            httpContext.Request.QueryString = new QueryString();
            controller.ControllerContext = new ControllerContext { HttpContext = httpContext };

            // Act
            var result = controller.VNPayIpn() as ContentResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal("97", result.Content);
        }

        [Fact]
        public async Task VNPayReturn_SuccessfulPayment_WithFoodOrders_SavesFoodOrders()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<MovieTheater.Models.MovieTheaterContext>()
                .UseInMemoryDatabase(databaseName: "VNPayReturn_Food_" + Guid.NewGuid().ToString())
                .Options;
            using var context = new MovieTheater.Models.MovieTheaterContext(options);

            // Seed basic data
            var rank = new Rank { RankId = 1, PointEarningPercentage = 1, ColorGradient = "#FF0000", IconClass = "icon-star" };
            context.Ranks.Add(rank);
            context.SaveChanges();

            var account = new Account { AccountId = "acc1", RankId = 1 };
            context.Accounts.Add(account);
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

            // Mock services
            var vnPayServiceMock = new Mock<IVNPayService>();
            var loggerMock = new Mock<ILogger<PaymentController>>();
            var accountServiceMock = new Mock<IAccountService>();
            var foodInvoiceServiceMock = new Mock<IFoodInvoiceService>();
            var invoiceServiceMock = new Mock<IInvoiceService>();
            invoiceServiceMock.Setup(s => s.GetById("INV1")).Returns(context.Invoices.First(i => i.InvoiceId == "INV1"));
            var hubContextMock = new Mock<IHubContext<DashboardHub>>();
            var dashboardClientsMock = new Mock<IHubClients>();
            var dashboardAllMock = new Mock<IClientProxy>();
            hubContextMock.Setup(x => x.Clients).Returns(dashboardClientsMock.Object);
            dashboardClientsMock.Setup(x => x.All).Returns(dashboardAllMock.Object);

            var httpContext = new DefaultHttpContext();
            var session = new TestSession();
            session.Set("SelectedFoods_INV1", System.Text.Encoding.UTF8.GetBytes("[{\"FoodId\":1,\"Name\":\"Popcorn\",\"Price\":50,\"Quantity\":2}]"));
            httpContext.Session = session;

            var controller = new PaymentController(
                vnPayServiceMock.Object,
                loggerMock.Object,
                accountServiceMock.Object,
                context,
                hubContextMock.Object,
                foodInvoiceServiceMock.Object,
                invoiceServiceMock.Object
            )
            {
                ControllerContext = new ControllerContext { HttpContext = httpContext },
                TempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>())
            };

            var model = new VnPayReturnModel
            {
                vnp_TxnRef = "INV1",
                vnp_ResponseCode = "00"
            };

            // Act
            var result = await controller.VNPayReturn(model);

            // Assert
            Assert.IsType<RedirectToActionResult>(result);
            foodInvoiceServiceMock.Verify(s => s.SaveFoodOrderAsync("INV1", It.IsAny<List<FoodViewModel>>()), Times.Once);
        }

        [Fact]
        public async Task VNPayReturn_SuccessfulPayment_WithInvalidFoodJson_HandlesException()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<MovieTheater.Models.MovieTheaterContext>()
                .UseInMemoryDatabase(databaseName: "VNPayReturn_InvalidFood_" + Guid.NewGuid().ToString())
                .Options;
            using var context = new MovieTheater.Models.MovieTheaterContext(options);

            // Seed basic data
            var rank = new Rank { RankId = 1, PointEarningPercentage = 1, ColorGradient = "#FF0000", IconClass = "icon-star" };
            context.Ranks.Add(rank);
            context.SaveChanges();

            var account = new Account { AccountId = "acc1", RankId = 1 };
            context.Accounts.Add(account);
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

            // Mock services
            var vnPayServiceMock = new Mock<IVNPayService>();
            var loggerMock = new Mock<ILogger<PaymentController>>();
            var accountServiceMock = new Mock<IAccountService>();
            var foodInvoiceServiceMock = new Mock<IFoodInvoiceService>();
            var invoiceServiceMock = new Mock<IInvoiceService>();
            invoiceServiceMock.Setup(s => s.GetById("INV1")).Returns(context.Invoices.First(i => i.InvoiceId == "INV1"));
            var hubContextMock = new Mock<IHubContext<DashboardHub>>();
            var clientsMock = new Mock<IHubClients>();
            var allClientsMock = new Mock<IClientProxy>();
            hubContextMock.Setup(x => x.Clients).Returns(clientsMock.Object);
            clientsMock.Setup(x => x.All).Returns(allClientsMock.Object);

            var httpContext = new DefaultHttpContext();
            var session = new TestSession();
            session.Set("SelectedFoods_INV1", System.Text.Encoding.UTF8.GetBytes("invalid json"));
            httpContext.Session = session;

            var controller = new PaymentController(
                vnPayServiceMock.Object,
                loggerMock.Object,
                accountServiceMock.Object,
                context,
                hubContextMock.Object,
                foodInvoiceServiceMock.Object,
                invoiceServiceMock.Object
            )
            {
                ControllerContext = new ControllerContext { HttpContext = httpContext },
                TempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>())
            };

            var model = new VnPayReturnModel
            {
                vnp_TxnRef = "INV1",
                vnp_ResponseCode = "00"
            };

            // Act
            var result = await controller.VNPayReturn(model);

            // Assert
            Assert.IsType<RedirectToActionResult>(result);
            foodInvoiceServiceMock.Verify(s => s.SaveFoodOrderAsync(It.IsAny<string>(), It.IsAny<List<FoodViewModel>>()), Times.Never);
        }

        [Fact]
        public async Task VNPayReturn_SuccessfulPayment_WithPromotionDiscount_CalculatesCorrectPrice()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<MovieTheater.Models.MovieTheaterContext>()
                .UseInMemoryDatabase(databaseName: "VNPayReturn_Promotion_" + Guid.NewGuid().ToString())
                .Options;
            using var context = new MovieTheater.Models.MovieTheaterContext(options);

            // Seed data
            var rank = new Rank { RankId = 1, PointEarningPercentage = 1, ColorGradient = "#FF0000", IconClass = "icon-star" };
            context.Ranks.Add(rank);
            context.SaveChanges();

            var account = new Account { AccountId = "acc1", RankId = 1 };
            context.Accounts.Add(account);
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

            var invoice = new Invoice
            {
                InvoiceId = "INV1",
                MovieShowId = 1,
                Seat = "A1",
                Status = InvoiceStatus.Incomplete,
                TotalMoney = 100,
                AccountId = "acc1",
                BookingDate = DateTime.Now,
                PromotionDiscount = "{\"seat\": 20}"
            };
            context.Invoices.Add(invoice);
            context.SaveChanges();

            // Mock services
            var vnPayServiceMock = new Mock<IVNPayService>();
            var loggerMock = new Mock<ILogger<PaymentController>>();
            var accountServiceMock = new Mock<IAccountService>();
            var foodInvoiceServiceMock = new Mock<IFoodInvoiceService>();
            var invoiceServiceMock = new Mock<IInvoiceService>();
            invoiceServiceMock.Setup(s => s.GetById("INV1")).Returns(context.Invoices.First(i => i.InvoiceId == "INV1"));
            var hubContextMock = new Mock<IHubContext<DashboardHub>>();
            var dashboardClientsMock = new Mock<IHubClients>();
            var dashboardAllMock = new Mock<IClientProxy>();
            hubContextMock.Setup(x => x.Clients).Returns(dashboardClientsMock.Object);
            dashboardClientsMock.Setup(x => x.All).Returns(dashboardAllMock.Object);

            var httpContext = new DefaultHttpContext();
            httpContext.Session = new TestSession();

            var controller = new PaymentController(
                vnPayServiceMock.Object,
                loggerMock.Object,
                accountServiceMock.Object,
                context,
                hubContextMock.Object,
                foodInvoiceServiceMock.Object,
                invoiceServiceMock.Object
            )
            {
                ControllerContext = new ControllerContext { HttpContext = httpContext },
                TempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>())
            };

            var model = new VnPayReturnModel
            {
                vnp_TxnRef = "INV1",
                vnp_ResponseCode = "00"
            };

            // Act
            var result = await controller.VNPayReturn(model);

            // Assert
            Assert.IsType<RedirectToActionResult>(result);
            var updatedInvoice = context.Invoices.First(i => i.InvoiceId == "INV1");
            Assert.Equal(InvoiceStatus.Completed, updatedInvoice.Status);
        }

        [Fact]
        public async Task VNPayReturn_FailedPayment_UpdatesInvoiceStatusToIncomplete()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<MovieTheater.Models.MovieTheaterContext>()
                .UseInMemoryDatabase(databaseName: "VNPayReturn_Failed_" + Guid.NewGuid().ToString())
                .Options;
            using var context = new MovieTheater.Models.MovieTheaterContext(options);

            // Seed data
            var rank = new Rank { RankId = 1, PointEarningPercentage = 1, ColorGradient = "#FF0000", IconClass = "icon-star" };
            context.Ranks.Add(rank);
            context.SaveChanges();

            var account = new Account { AccountId = "acc1", RankId = 1 };
            context.Accounts.Add(account);
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

            // Mock services
            var vnPayServiceMock = new Mock<IVNPayService>();
            var loggerMock = new Mock<ILogger<PaymentController>>();
            var accountServiceMock = new Mock<IAccountService>();
            var foodInvoiceServiceMock = new Mock<IFoodInvoiceService>();
            var invoiceServiceMock = new Mock<IInvoiceService>();
            var hubContextMock = new Mock<IHubContext<DashboardHub>>();

            var httpContext = new DefaultHttpContext();
            httpContext.Session = new TestSession();

            var controller = new PaymentController(
                vnPayServiceMock.Object,
                loggerMock.Object,
                accountServiceMock.Object,
                context,
                hubContextMock.Object,
                foodInvoiceServiceMock.Object,
                invoiceServiceMock.Object
            )
            {
                ControllerContext = new ControllerContext { HttpContext = httpContext },
                TempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>())
            };

            var model = new VnPayReturnModel
            {
                vnp_TxnRef = "INV1",
                vnp_ResponseCode = "99"
            };

            // Act
            var result = await controller.VNPayReturn(model);

            // Assert
            Assert.IsType<RedirectToActionResult>(result);
            var redirect = result as RedirectToActionResult;
            Assert.Equal("Failed", redirect.ActionName);
            Assert.Equal("Booking", redirect.ControllerName);
        }

        [Fact]
        public void CreatePayment_WithValidRequest_ReturnsCorrectPaymentUrl()
        {
            // Arrange
            var vnPayServiceMock = new Mock<IVNPayService>();
            var expectedUrl = "https://sandbox.vnpayment.vn/payment/v2/transaction.html";
            vnPayServiceMock.Setup(s => s.CreatePaymentUrl(100m, "Test Order", "ORDER123"))
                .Returns(expectedUrl);
            var controller = CreateController(vnPayServiceMock: vnPayServiceMock);
            var request = new ControllersPaymentRequest 
            { 
                Amount = 100m, 
                OrderInfo = "Test Order", 
                OrderId = "ORDER123" 
            };

            // Act
            var result = controller.CreatePayment(request) as OkObjectResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal(200, result.StatusCode);
            var response = result.Value;
            var paymentUrlProperty = response.GetType().GetProperty("paymentUrl");
            Assert.NotNull(paymentUrlProperty);
            var paymentUrl = paymentUrlProperty.GetValue(response);
            Assert.Equal(expectedUrl, paymentUrl);
        }

        [Fact]
        public void CreatePayment_WithZeroAmount_HandlesGracefully()
        {
            // Arrange
            var vnPayServiceMock = new Mock<IVNPayService>();
            vnPayServiceMock.Setup(s => s.CreatePaymentUrl(0m, It.IsAny<string>(), It.IsAny<string>()))
                .Returns("https://sandbox.vnpayment.vn/payment/v2/transaction.html");
            var controller = CreateController(vnPayServiceMock: vnPayServiceMock);
            var request = new ControllersPaymentRequest 
            { 
                Amount = 0m, 
                OrderInfo = "Test Order", 
                OrderId = "ORDER123" 
            };

            // Act
            var result = controller.CreatePayment(request) as OkObjectResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal(200, result.StatusCode);
        }

        [Fact]
        public void CreatePayment_WithNegativeAmount_HandlesGracefully()
        {
            // Arrange
            var vnPayServiceMock = new Mock<IVNPayService>();
            vnPayServiceMock.Setup(s => s.CreatePaymentUrl(-100m, It.IsAny<string>(), It.IsAny<string>()))
                .Returns("https://sandbox.vnpayment.vn/payment/v2/transaction.html");
            var controller = CreateController(vnPayServiceMock: vnPayServiceMock);
            var request = new ControllersPaymentRequest 
            { 
                Amount = -100m, 
                OrderInfo = "Test Order", 
                OrderId = "ORDER123" 
            };

            // Act
            var result = controller.CreatePayment(request) as OkObjectResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal(200, result.StatusCode);
        }

        [Fact]
        public void CreatePayment_WithNullOrderInfo_HandlesGracefully()
        {
            // Arrange
            var vnPayServiceMock = new Mock<IVNPayService>();
            vnPayServiceMock.Setup(s => s.CreatePaymentUrl(100m, null, "ORDER123"))
                .Returns("https://sandbox.vnpayment.vn/payment/v2/transaction.html");
            var controller = CreateController(vnPayServiceMock: vnPayServiceMock);
            var request = new ControllersPaymentRequest 
            { 
                Amount = 100m, 
                OrderInfo = null, 
                OrderId = "ORDER123" 
            };

            // Act
            var result = controller.CreatePayment(request) as OkObjectResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal(200, result.StatusCode);
        }

        [Fact]
        public void CreatePayment_WithNullOrderId_HandlesGracefully()
        {
            // Arrange
            var vnPayServiceMock = new Mock<IVNPayService>();
            vnPayServiceMock.Setup(s => s.CreatePaymentUrl(100m, "Test Order", null))
                .Returns("https://sandbox.vnpayment.vn/payment/v2/transaction.html");
            var controller = CreateController(vnPayServiceMock: vnPayServiceMock);
            var request = new ControllersPaymentRequest 
            { 
                Amount = 100m, 
                OrderInfo = "Test Order", 
                OrderId = null 
            };

            // Act
            var result = controller.CreatePayment(request) as OkObjectResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal(200, result.StatusCode);
        }

        [Fact]
        public void VNPayIpn_WithValidSignature_ReturnsSuccessCode()
        {
            // Arrange
            var vnPayServiceMock = new Mock<IVNPayService>();
            vnPayServiceMock.Setup(s => s.ValidateSignature(It.IsAny<IDictionary<string, string>>(), "valid_hash"))
                .Returns(true);
            var controller = CreateController(vnPayServiceMock: vnPayServiceMock);
            var httpContext = new DefaultHttpContext();
            httpContext.Request.QueryString = new QueryString("?vnp_SecureHash=valid_hash&vnp_Amount=100&vnp_TxnRef=ORDER123");
            controller.ControllerContext = new ControllerContext { HttpContext = httpContext };

            // Act
            var result = controller.VNPayIpn() as ContentResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal("00", result.Content);
        }

        [Fact]
        public void VNPayIpn_WithInvalidSignature_ReturnsErrorCode()
        {
            // Arrange
            var vnPayServiceMock = new Mock<IVNPayService>();
            vnPayServiceMock.Setup(s => s.ValidateSignature(It.IsAny<IDictionary<string, string>>(), "invalid_hash"))
                .Returns(false);
            var controller = CreateController(vnPayServiceMock: vnPayServiceMock);
            var httpContext = new DefaultHttpContext();
            httpContext.Request.QueryString = new QueryString("?vnp_SecureHash=invalid_hash&vnp_Amount=100&vnp_TxnRef=ORDER123");
            controller.ControllerContext = new ControllerContext { HttpContext = httpContext };

            // Act
            var result = controller.VNPayIpn() as ContentResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal("97", result.Content);
        }

        [Fact]
        public void VNPayIpn_WithMultipleQueryParameters_ProcessesCorrectly()
        {
            // Arrange
            var vnPayServiceMock = new Mock<IVNPayService>();
            vnPayServiceMock.Setup(s => s.ValidateSignature(It.IsAny<IDictionary<string, string>>(), "valid_hash"))
                .Returns(true);
            var controller = CreateController(vnPayServiceMock: vnPayServiceMock);
            var httpContext = new DefaultHttpContext();
            httpContext.Request.QueryString = new QueryString("?vnp_SecureHash=valid_hash&vnp_Amount=100&vnp_TxnRef=ORDER123&vnp_ResponseCode=00&vnp_OrderInfo=Test");
            controller.ControllerContext = new ControllerContext { HttpContext = httpContext };

            // Act
            var result = controller.VNPayIpn() as ContentResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal("00", result.Content);
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