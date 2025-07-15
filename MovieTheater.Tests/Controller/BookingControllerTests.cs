using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Moq;
using MovieTheater.Controllers;
using MovieTheater.Hubs;
using MovieTheater.Models;
using MovieTheater.Repository;
using MovieTheater.Service;
using MovieTheater.ViewModels;
using Xunit;

namespace MovieTheater.Tests.Controllers
{
    public class BookingControllerTests
    {
        private readonly Mock<IBookingService> _bookingService = new();
        private readonly Mock<IMovieService> _movieService = new();
        private readonly Mock<ISeatService> _seatService = new();
        private readonly Mock<IAccountService> _accountService = new();
        private readonly Mock<ISeatTypeService> _seatTypeService = new();
        private readonly Mock<IMemberRepository> _memberRepo = new();
        private readonly Mock<IInvoiceService> _invoiceService = new();
        private readonly Mock<IVNPayService> _vnPayService = new();
        private readonly Mock<IVoucherService> _voucherService = new();
        private readonly Mock<IHubContext<DashboardHub>> _hubContext = new();
        private readonly Mock<IFoodService> _foodService = new();
        private readonly Mock<IFoodInvoiceService> _foodInvService = new();
        private readonly Mock<IBookingDomainService> _domainService = new();
        private readonly MovieTheaterContext _context = InMemoryDb.Create();

        private BookingController BuildController()
            => new BookingController(
                _bookingService.Object,
                _movieService.Object,
                _seatService.Object,
                _accountService.Object,
                _seatTypeService.Object,
                _memberRepo.Object,
                Mock.Of<Microsoft.Extensions.Logging.ILogger<BookingController>>(),
                _invoiceService.Object,
                _vnPayService.Object,
                _voucherService.Object,
                _hubContext.Object,
                _context,
                _foodService.Object,
                _foodInvService.Object,
                _domainService.Object
            );

        [Fact]
        public async Task TicketBooking_Get_ReturnsView_WithMoviesInViewBag()
        {
            // Arrange
            var expected = new List<Movie> { new() { MovieId = "M1", MovieNameEnglish = "X" } };
            _bookingService
              .Setup(s => s.GetAvailableMoviesAsync())
              .ReturnsAsync(expected);

            var ctrl = BuildController();

            // Act
            var result = await ctrl.TicketBooking(movieId: null) as ViewResult;

            // Assert
            Assert.NotNull(result);
            Assert.Same(expected, result.ViewData["MovieList"]);
            Assert.Null(result.ViewData["SelectedMovieId"]);
        }

        [Fact]
        public async Task TicketBooking_Get_WithMovieId_SetsShowsByDate()
        {
            // Arrange
            var showDate = DateOnly.FromDateTime(DateTime.Today);
            var schedule = new Schedule { ScheduleTime = new TimeOnly(9, 30) };
            var show = new MovieShow
            {
                MovieId = "M1",
                ShowDate = showDate,
                Schedule = schedule
            };
            _bookingService.Setup(s => s.GetAvailableMoviesAsync())
                           .ReturnsAsync(new List<Movie>());
            _movieService
              .Setup(m => m.GetMovieShows("M1"))
              .Returns(new List<MovieShow> { show });

            var ctrl = BuildController();

            // Act
            var result = await ctrl.TicketBooking(movieId: "M1") as ViewResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal("M1", result.ViewData["SelectedMovieId"]);
            var showsByDate = Assert.IsType<Dictionary<string, List<string>>>(result.ViewData["ShowsByDate"]);
            var key = showDate.ToString("dd/MM/yyyy");
            Assert.True(showsByDate.ContainsKey(key));
            Assert.Contains("09:30", showsByDate[key]);
        }

        [Fact]
        public async Task GetDates_ReturnsJsonListOfStrings()
        {
            // Arrange
            var dates = new List<DateOnly> {
                DateOnly.Parse("2025-07-20"),
                DateOnly.Parse("2025-07-21")
            };
            _bookingService.Setup(s => s.GetShowDatesAsync("M1"))
                           .ReturnsAsync(dates);

            var ctrl = BuildController();

            // Act
            var json = await ctrl.GetDates("M1") as JsonResult;
            var list = Assert.IsAssignableFrom<IEnumerable<string>>(json.Value);

            // Assert
            Assert.Contains("2025-07-20", list);
            Assert.Contains("2025-07-21", list);
        }

        [Fact]
        public async Task Confirm_Post_InvalidModel_ReturnsConfirmViewWithError()
        {
            // Arrange
            var ctrl = BuildController();
            // Simulate not logged in
            _accountService.Setup(a => a.GetCurrentUser()).Returns((ProfileUpdateViewModel)null);

            // Act
            var redirect = await ctrl.Confirm(new ConfirmBookingViewModel(), "true") as RedirectToActionResult;

            // Assert
            Assert.NotNull(redirect);
            Assert.Equal("Login", redirect.ActionName);
            Assert.Equal("Account", redirect.ControllerName);
        }

        [Fact]
        public async Task Confirm_Post_ValidModel_RedirectsToSuccess()
        {
            // Arrange
            var user = new ProfileUpdateViewModel { AccountId = "u1" };
            var model = new ConfirmBookingViewModel();
            _accountService.Setup(a => a.GetCurrentUser()).Returns(user);
            _domainService
              .Setup(d => d.ConfirmBookingAsync(model, "u1", "ok"))
              .ReturnsAsync(new BookingResult { Success = true, InvoiceId = "INV123" });

            var ctrl = BuildController();

            // Act
            var result = await ctrl.Confirm(model, "ok") as RedirectToActionResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Success", result.ActionName);
            Assert.Equal("INV123", result.RouteValues["invoiceId"]);
        }

        [Fact]
        public async Task Information_RedirectsToTicketBooking_WhenNoSeatsSelected()
        {
            // Arrange
            var ctrl = BuildController();
            ctrl.TempData = new Microsoft.AspNetCore.Mvc.ViewFeatures.TempDataDictionary(
                new Microsoft.AspNetCore.Http.DefaultHttpContext(),
                Mock.Of<Microsoft.AspNetCore.Mvc.ViewFeatures.ITempDataProvider>());
            // Act
            var result = await ctrl.Information("M1", DateOnly.FromDateTime(DateTime.Today), "10:00", null, 1, null, null) as RedirectToActionResult;
            // Assert
            Assert.NotNull(result);
            Assert.Equal("TicketBooking", result.ActionName);
            Assert.Equal("M1", result.RouteValues["movieId"]);
        }

        [Fact]
        public async Task Information_RedirectsToLogin_WhenNotLoggedIn()
        {
            // Arrange
            _accountService.Setup(a => a.GetCurrentUser()).Returns((ProfileUpdateViewModel)null);
            var ctrl = BuildController();
            // Act
            var result = await ctrl.Information("M1", DateOnly.FromDateTime(DateTime.Today), "10:00", new List<int> { 1 }, 1, null, null) as RedirectToActionResult;
            // Assert
            Assert.NotNull(result);
            Assert.Equal("Login", result.ActionName);
            Assert.Equal("Account", result.ControllerName);
        }

        [Fact]
        public async Task Information_ReturnsView_WhenValid()
        {
            // Arrange
            var user = new ProfileUpdateViewModel { AccountId = "u1" };
            _accountService.Setup(a => a.GetCurrentUser()).Returns(user);
            _domainService.Setup(d => d.BuildConfirmBookingViewModelAsync("M1", It.IsAny<DateOnly>(), It.IsAny<string>(), It.IsAny<List<int>>(), It.IsAny<int>(), null, null, "u1"))
                .ReturnsAsync(new ConfirmBookingViewModel());
            var ctrl = BuildController();
            // Act
            var result = await ctrl.Information("M1", DateOnly.FromDateTime(DateTime.Today), "10:00", new List<int> { 1 }, 1, null, null) as ViewResult;
            // Assert
            Assert.NotNull(result);
            Assert.IsType<ConfirmBookingViewModel>(result.Model);
        }

        [Fact]
        public async Task Success_ReturnsNotFound_WhenInvoiceMissing()
        {
            // Arrange
            _domainService.Setup(d => d.BuildSuccessViewModelAsync("bad", "u1")).ReturnsAsync((BookingSuccessViewModel)null);
            var user = new ProfileUpdateViewModel { AccountId = "u1" };
            _accountService.Setup(a => a.GetCurrentUser()).Returns(user);
            var ctrl = BuildController();
            // Act
            var result = await ctrl.Success("bad") as NotFoundResult;
            // Assert
            Assert.NotNull(result);
        }

        [Fact]
        public async Task Success_ReturnsView_WhenInvoiceExists()
        {
            // Arrange
            var user = new ProfileUpdateViewModel { AccountId = "u1" };
            _accountService.Setup(a => a.GetCurrentUser()).Returns(user);
            _domainService.Setup(d => d.BuildSuccessViewModelAsync("I1", "u1")).ReturnsAsync(new BookingSuccessViewModel());
            var ctrl = BuildController();
            // Act
            var result = await ctrl.Success("I1") as ViewResult;
            // Assert
            Assert.NotNull(result);
            Assert.IsType<BookingSuccessViewModel>(result.Model);
        }

        [Fact]
        public async Task Payment_ReturnsNotFound_WhenInvoiceMissing()
        {
            _invoiceService.Setup(i => i.GetById("bad")).Returns((Invoice)null);
            var ctrl = BuildController();
            var result = await ctrl.Payment("bad") as NotFoundResult;
            Assert.NotNull(result);
        }

        [Fact]
        public async Task Payment_ReturnsView_WhenInvoiceExists()
        {
            var invoice = new Invoice
            {
                InvoiceId = "I1",
                MovieShow = new MovieShow
                {
                    Movie = new Movie { MovieNameEnglish = "Test Movie" },
                    ShowDate = DateOnly.FromDateTime(DateTime.Today),
                    Schedule = new Schedule { ScheduleTime = new TimeOnly(10, 0) }
                },
                Seat = "A1",
                TotalMoney = 100m
            };
            _invoiceService.Setup(i => i.GetById("I1")).Returns(invoice);
            _foodInvService.Setup(f => f.GetFoodsByInvoiceIdAsync("I1")).ReturnsAsync(new List<FoodViewModel>());
            var ctrl = BuildController();
            var result = await ctrl.Payment("I1") as ViewResult;
            Assert.NotNull(result);
            Assert.IsType<PaymentViewModel>(result.Model);
        }

        [Fact]
        public void ProcessPayment_RedirectsToPaymentUrl_WhenSuccess()
        {
            // Arrange
            var model = new PaymentViewModel { InvoiceId = "I1", OrderInfo = "info", TotalAmount = 123 };
            _vnPayService.Setup(v => v.CreatePaymentUrl(123, "info", "I1")).Returns("http://pay");
            var ctrl = BuildController();
            // Act
            var result = ctrl.ProcessPayment(model) as RedirectResult;
            // Assert
            Assert.NotNull(result);
            Assert.Equal("http://pay", result.Url);
        }

        [Fact]
        public void ProcessPayment_RedirectsToFailed_WhenNoUrl()
        {
            // Arrange
            var model = new PaymentViewModel { InvoiceId = "I1", OrderInfo = "info", TotalAmount = 123 };
            _vnPayService.Setup(v => v.CreatePaymentUrl(123, "info", "I1")).Throws(new Exception("fail"));
            var invoice = new Invoice
            {
                InvoiceId = "I1",
                MovieShow = new MovieShow
                {
                    Movie = new Movie { MovieNameEnglish = "Test Movie" },
                    ShowDate = DateOnly.FromDateTime(DateTime.Today),
                    Schedule = new Schedule { ScheduleTime = new TimeOnly(10, 0) },
                    CinemaRoom = new CinemaRoom { CinemaRoomName = "Room1" }
                },
                Seat = "A1",
                TotalMoney = 100m,
                ScheduleSeats = new List<ScheduleSeat> { new ScheduleSeat { SeatId = 1, MovieShowId = 1 } },
                PromotionDiscount = 0,
                VoucherId = null,
                UseScore = 0
            };
            _invoiceService.Setup(i => i.GetById("I1")).Returns(invoice);
            var ctrl = BuildController();
            ctrl.TempData = new Microsoft.AspNetCore.Mvc.ViewFeatures.TempDataDictionary(
                new Microsoft.AspNetCore.Http.DefaultHttpContext(),
                Mock.Of<Microsoft.AspNetCore.Mvc.ViewFeatures.ITempDataProvider>());
            // Act
            var result = ctrl.ProcessPayment(model) as RedirectToActionResult;
            // Assert
            Assert.NotNull(result);
            Assert.Equal("Failed", result.ActionName);
        }

        [Fact]
        public async Task CheckMemberDetails_ReturnsJson_WhenFound()
        {
            // Arrange
            _memberRepo.Setup(m => m.GetByAccountId("mem1")).Returns(new Member { MemberId = "mem1", Score = 10, Account = new Account { FullName = "Test User", IdentityCard = "123", PhoneNumber = "555" } });
            var ctrl = BuildController();
            // Act
            var result = await ctrl.CheckMemberDetails(new MemberCheckRequest { MemberInput = "mem1" }) as JsonResult;
            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.Value);
            var successProp = result.Value.GetType().GetProperty("success");
            Assert.NotNull(successProp);
            Assert.True((bool)successProp.GetValue(result.Value));
        }

        [Fact]
        public async Task CheckMemberDetails_ReturnsJsonError_WhenNotFound()
        {
            // Arrange
            _memberRepo.Setup(m => m.GetByAccountId("mem2")).Returns((Member)null);
            var ctrl = BuildController();
            // Act
            var result = await ctrl.CheckMemberDetails(new MemberCheckRequest { MemberInput = "mem2" }) as JsonResult;
            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.Value);
            var successProp = result.Value.GetType().GetProperty("success");
            Assert.NotNull(successProp);
            Assert.False((bool)successProp.GetValue(result.Value));
        }

        [Fact]
        public async Task ConfirmTicketForAdmin_Get_ReturnsView()
        {
            // Arrange
            var user = new ProfileUpdateViewModel { AccountId = "admin" };
            _accountService.Setup(a => a.GetCurrentUser()).Returns(user);
            _domainService.Setup(d => d.BuildConfirmTicketAdminViewModelAsync(1, It.IsAny<List<int>>(), It.IsAny<List<int>>(), It.IsAny<List<int>>()))
                .ReturnsAsync(new ConfirmTicketAdminViewModel { MovieShowId = 1, SelectedFoods = new List<FoodViewModel>(), BookingDetails = new ConfirmBookingViewModel() });
            var ctrl = BuildController();
            ctrl.Url = new Mock<IUrlHelper>().Object;
            // Act
            var result = await ctrl.ConfirmTicketForAdmin(1, new List<int> { 1 }, new List<int> { 1 }, new List<int> { 1 }) as ViewResult;
            // Assert
            Assert.NotNull(result);
            Assert.IsType<ConfirmTicketAdminViewModel>(result.Model);
        }

        [Fact]
        public async Task ConfirmTicketForAdmin_Post_ReturnsJson()
        {
            // Arrange
            _domainService.Setup(d => d.ConfirmTicketForAdminAsync(It.IsAny<ConfirmTicketAdminViewModel>()))
                .ReturnsAsync(new BookingResult { Success = true });
            var mockClients = new Mock<Microsoft.AspNetCore.SignalR.IHubClients>();
            var mockAll = new Mock<Microsoft.AspNetCore.SignalR.IClientProxy>();
            mockClients.Setup(c => c.All).Returns(mockAll.Object);
            _hubContext.Setup(h => h.Clients).Returns(mockClients.Object);
            mockAll.Setup(a => a.SendCoreAsync(It.IsAny<string>(), It.IsAny<object[]>(), It.IsAny<System.Threading.CancellationToken>())).Returns(Task.CompletedTask);
            var ctrl = BuildController();
            ctrl.Url = new Mock<IUrlHelper>().Object;
            // Act
            var result = await ctrl.ConfirmTicketForAdmin(new ConfirmTicketAdminViewModel { MovieShowId = 1, SelectedFoods = new List<FoodViewModel>(), BookingDetails = new ConfirmBookingViewModel() }) as JsonResult;
            // Assert
            Assert.NotNull(result);
            Assert.True((bool)result.Value.GetType().GetProperty("success").GetValue(result.Value));
        }

        [Fact]
        public void CheckScoreForConversion_ReturnsJson()
        {
            // Arrange
            var ctrl = BuildController();
            var req = new ScoreConversionRequest { TicketPrices = new List<decimal> { 100, 200 }, TicketsToConvert = 2, MemberScore = 300 };
            // Act
            var result = ctrl.CheckScoreForConversion(req) as JsonResult;
            // Assert
            Assert.NotNull(result);
            Assert.True(result.Value is object);
        }

        [Fact]
        public async Task TicketInfo_ReturnsView()
        {
            // Arrange
            _domainService.Setup(d => d.BuildTicketBookingConfirmedViewModelAsync("inv1"))
                .ReturnsAsync(new ConfirmTicketAdminViewModel { MovieShowId = 1, SelectedFoods = new List<FoodViewModel>(), BookingDetails = new ConfirmBookingViewModel() });
            var ctrl = BuildController();
            // Act
            var result = await ctrl.TicketInfo("inv1") as ViewResult;
            // Assert
            Assert.NotNull(result);
            Assert.IsType<ConfirmTicketAdminViewModel>(result.Model);
        }

        [Fact]
        public async Task GetAllMembers_ReturnsView()
        {
            // Arrange
            _memberRepo.Setup(m => m.GetAll()).Returns(new List<Member> { new Member { AccountId = "m1", Account = new Account { AccountId = "m1", FullName = "Test User" } } });
            var ctrl = BuildController();
            // Act
            var result = ctrl.GetAllMembers() as JsonResult;
            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.Value);
        }

        [Fact]
        public void InitiateTicketSellingForMember_ReturnsView()
        {
            // Arrange
            var ctrl = BuildController();
            ctrl.TempData = new Microsoft.AspNetCore.Mvc.ViewFeatures.TempDataDictionary(
                new Microsoft.AspNetCore.Http.DefaultHttpContext(),
                Mock.Of<Microsoft.AspNetCore.Mvc.ViewFeatures.ITempDataProvider>());
            ctrl.Url = new Mock<IUrlHelper>().Object;
            // Act
            var result = ctrl.InitiateTicketSellingForMember("mem1") as RedirectToActionResult;
            // Assert
            Assert.NotNull(result);
            Assert.Equal("Select", result.ActionName);
            Assert.Equal("Showtime", result.ControllerName);
        }

        [Fact]
        public void GetMemberDiscount_ReturnsJson()
        {
            _memberRepo.Setup(m => m.GetById("mem1")).Returns(new Member { AccountId = "mem1" });
            var ctrl = BuildController();
            var result = ctrl.GetMemberDiscount("mem1") as JsonResult;
            Assert.NotNull(result);
            Assert.True(result.Value is object);
        }

        [Fact]
        public async Task GetFoods_ReturnsJson()
        {
            // Arrange
            var foodList = new FoodListViewModel { Foods = new List<FoodViewModel>() };
            _foodService.Setup(f => f.GetAllAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool?>())).ReturnsAsync(foodList);
            var ctrl = BuildController();
            // Act
            var result = await ctrl.GetFoods() as JsonResult;
            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.Value);
            Assert.Equal(foodList.Foods, result.Value);
        }
    }

    // Helper for in-memory EF Core
    static class InMemoryDb
    {
        public static MovieTheaterContext Create()
        {
            var opts = new DbContextOptionsBuilder<MovieTheaterContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            return new MovieTheaterContext(opts);
        }
    }
}
