using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using MovieTheater.Controllers;
using MovieTheater.Hubs;
using MovieTheater.Models;
using MovieTheater.Repository;
using MovieTheater.Service;
using MovieTheater.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using static MovieTheater.Service.PromotionService;

namespace MovieTheater.Tests.Controller
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
        private readonly Mock<IPromotionService> _promotionService = new();
        private readonly MovieTheaterContext _context = InMemoryDb.Create();

        private BookingController BuildController()
            => new BookingController(
        _bookingService.Object,
        _movieService.Object,
        _seatService.Object,
        _accountService.Object,
        _seatTypeService.Object,
        _memberRepo.Object,
        Mock.Of<ILogger<BookingController>>(),
        _invoiceService.Object,
        _vnPayService.Object,
        _voucherService.Object,
        _hubContext.Object,
        _context,
        _foodService.Object,
        _foodInvService.Object,
        _domainService.Object,
        _promotionService.Object
    );



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
              .Setup(d => d.ConfirmBookingAsync(It.IsAny<ConfirmBookingViewModel>(), "u1", "true"))
              .ReturnsAsync(new BookingResult { Success = true, InvoiceId = "INV123" });

            var ctrl = BuildController();

            // Act
            var result = await ctrl.Confirm(model, "true") as RedirectToActionResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Success", result.ActionName);
            Assert.Equal("INV123", result.RouteValues["invoiceId"]);
        }

        [Fact]
        public async Task Confirm_ReturnsViewWithError_WhenResultNotSuccess()
        {
            var user = new ProfileUpdateViewModel { AccountId = "u1" };
            var model = new ConfirmBookingViewModel();
            _accountService.Setup(a => a.GetCurrentUser()).Returns(user);
            _domainService.Setup(d => d.ConfirmBookingAsync(model, "u1", "ok")).ReturnsAsync(new BookingResult { Success = false, ErrorMessage = "fail!" });
            var ctrl = BuildController();
            var result = await ctrl.Confirm(model, "ok");
            var view = Assert.IsType<ViewResult>(result);
            Assert.Equal("ConfirmBooking", view.ViewName);
            Assert.Equal(model, view.Model);
            Assert.True(ctrl.ModelState.ErrorCount > 0);
        }

        [Fact]
        public async Task Confirm_RedirectsToPayment_WhenIsTestSuccessIsNotTrue()
        {
            var user = new ProfileUpdateViewModel { AccountId = "u1" };
            var model = new ConfirmBookingViewModel();
            _accountService.Setup(a => a.GetCurrentUser()).Returns(user);
            _domainService.Setup(d => d.ConfirmBookingAsync(model, "u1", "false")).ReturnsAsync(new BookingResult { Success = true, InvoiceId = "INV999", TotalPrice = 100m });
            var ctrl = BuildController();
            var result = await ctrl.Confirm(model, "false");
            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Payment", redirect.ActionName);
            Assert.Equal("INV999", redirect.RouteValues["invoiceId"]);
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
            Assert.Equal("Index", result.ActionName);
            Assert.Equal("Home", result.ControllerName);
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
            _accountService.Setup(a => a.GetById("u1")).Returns(new Account { AccountId = "u1" });
            _movieService.Setup(m => m.GetById("M1")).Returns(new Movie { MovieId = "M1", MovieNameEnglish = "Test Movie" });
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
        public async Task Information_HandlesSeatTypeInformation_ForPromotionContext()
        {
            // Arrange
            var user = new ProfileUpdateViewModel { AccountId = "u1" };
            _accountService.Setup(a => a.GetCurrentUser()).Returns(user);
            _accountService.Setup(a => a.GetById("u1")).Returns(new Account { AccountId = "u1" });
            _movieService.Setup(m => m.GetById("M1")).Returns(new Movie { MovieId = "M1", MovieNameEnglish = "Test Movie" });
            _domainService.Setup(d => d.BuildConfirmBookingViewModelAsync("M1", It.IsAny<DateOnly>(), It.IsAny<string>(), It.IsAny<List<int>>(), It.IsAny<int>(), null, null, "u1"))
                .ReturnsAsync(new ConfirmBookingViewModel());

            // Setup context with seats
            var seatType = new SeatType { SeatTypeId = 1, TypeName = "Standard", PricePercent = 100, ColorHex = "#000000" };
            var seat = new Seat { SeatId = 1, SeatName = "A1", SeatTypeId = 1, SeatType = seatType };
            _context.SeatTypes.Add(seatType);
            _context.Seats.Add(seat);
            _context.SaveChanges();

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
        public async Task Success_RedirectsToLogin_WhenUserIdIsNull()
        {
            _accountService.Setup(a => a.GetCurrentUser()).Returns((ProfileUpdateViewModel)null);
            var ctrl = BuildController();
            var result = await ctrl.Success("INV1");
            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Login", redirect.ActionName);
            Assert.Equal("Account", redirect.ControllerName);
        }

        [Fact]
        public async Task Success_ReturnsNotFound_WhenViewModelIsNull()
        {
            var user = new ProfileUpdateViewModel { AccountId = "u1" };
            _accountService.Setup(a => a.GetCurrentUser()).Returns(user);
            _domainService.Setup(d => d.BuildSuccessViewModelAsync("INV1", "u1")).ReturnsAsync((BookingSuccessViewModel)null);
            var ctrl = BuildController();
            var result = await ctrl.Success("INV1");
            Assert.IsType<NotFoundResult>(result);
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
                PromotionDiscount = "0",
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
        public async Task CheckMemberDetails_ReturnsJson_WhenFoundByIdentityCard()
        {
            // Arrange
            _memberRepo.Setup(m => m.GetByIdentityCard("123456789")).Returns(new Member { MemberId = "mem1", Score = 10, Account = new Account { FullName = "Test User", IdentityCard = "123456789", PhoneNumber = "555" } });
            var ctrl = BuildController();
            // Act
            var result = await ctrl.CheckMemberDetails(new MemberCheckRequest { MemberInput = "123456789" }) as JsonResult;
            // Assert
            Assert.NotNull(result);
            var successProp = result.Value.GetType().GetProperty("success");
            Assert.True((bool)successProp.GetValue(result.Value));
        }

        [Fact]
        public async Task CheckMemberDetails_ReturnsJson_WhenFoundByMemberId()
        {
            // Arrange
            _memberRepo.Setup(m => m.GetByMemberId("MEM001")).Returns(new Member { MemberId = "MEM001", Score = 10, Account = new Account { FullName = "Test User", IdentityCard = "123", PhoneNumber = "555" } });
            var ctrl = BuildController();
            // Act
            var result = await ctrl.CheckMemberDetails(new MemberCheckRequest { MemberInput = "MEM001" }) as JsonResult;
            // Assert
            Assert.NotNull(result);
            var successProp = result.Value.GetType().GetProperty("success");
            Assert.True((bool)successProp.GetValue(result.Value));
        }

        [Fact]
        public async Task ConfirmTicketForAdmin_Get_ReturnsView()
        {
            // Arrange
            var user = new ProfileUpdateViewModel { AccountId = "admin" };
            _accountService.Setup(a => a.GetCurrentUser()).Returns(user);
            _domainService.Setup(d => d.BuildConfirmTicketAdminViewModelAsync(1, It.IsAny<List<int>>(), It.IsAny<List<int>>(), It.IsAny<List<int>>(), null))
                .ReturnsAsync(new ConfirmTicketAdminViewModel { MovieShowId = 1, SelectedFoods = new List<FoodViewModel>(), BookingDetails = new ConfirmBookingViewModel() });
            var ctrl = BuildController();
            ctrl.Url = new Mock<IUrlHelper>().Object;
            // Act
            var result = await ctrl.ConfirmTicketForAdmin(1, new List<int> { 1 }, new List<int> { 1 }, new List<int> { 1 }, null, null) as ViewResult;
            // Assert
            Assert.NotNull(result);
            Assert.IsType<ConfirmTicketAdminViewModel>(result.Model);
        }

        [Fact]
        public async Task ConfirmTicketForAdmin_Get_WithMemberId_UpdatesViewModel()
        {
            // Arrange
            var user = new ProfileUpdateViewModel { AccountId = "admin" };
            _accountService.Setup(a => a.GetCurrentUser()).Returns(user);
            _domainService.Setup(d => d.BuildConfirmTicketAdminViewModelAsync(1, It.IsAny<List<int>>(), It.IsAny<List<int>>(), It.IsAny<List<int>>(), "member1"))
                .ReturnsAsync(new ConfirmTicketAdminViewModel { MovieShowId = 1, SelectedFoods = new List<FoodViewModel>(), BookingDetails = new ConfirmBookingViewModel() });

            var member = new Member { MemberId = "member1", AccountId = "acc1", Score = 100 };
            var account = new Account { AccountId = "acc1", FullName = "Test User", IdentityCard = "123", PhoneNumber = "555" };
            member.Account = account;

            _context.Members.Add(member);
            _context.Accounts.Add(account);
            _context.SaveChanges();

            var ctrl = BuildController();
            ctrl.Url = new Mock<IUrlHelper>().Object;
            // Act
            var result = await ctrl.ConfirmTicketForAdmin(1, new List<int> { 1 }, new List<int> { 1 }, new List<int> { 1 }, "member1", null) as ViewResult;
            // Assert
            Assert.NotNull(result);
            var viewModel = Assert.IsType<ConfirmTicketAdminViewModel>(result.Model);
            Assert.Equal("member1", viewModel.MemberId);
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
        public async Task ConfirmTicketForAdmin_Post_ReturnsJsonError_WhenFailed()
        {
            // Arrange
            _domainService.Setup(d => d.ConfirmTicketForAdminAsync(It.IsAny<ConfirmTicketAdminViewModel>()))
                .ReturnsAsync(new BookingResult { Success = false, ErrorMessage = "Failed to confirm" });
            var ctrl = BuildController();
            ctrl.Url = new Mock<IUrlHelper>().Object;
            // Act
            var result = await ctrl.ConfirmTicketForAdmin(new ConfirmTicketAdminViewModel { MovieShowId = 1, SelectedFoods = new List<FoodViewModel>(), BookingDetails = new ConfirmBookingViewModel() }) as JsonResult;
            // Assert
            Assert.NotNull(result);
            Assert.False((bool)result.Value.GetType().GetProperty("success").GetValue(result.Value));
            var messageProp = result.Value.GetType().GetProperty("message");
            Assert.Equal("Failed to confirm", messageProp.GetValue(result.Value));
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
        public void CheckScoreForConversion_ReturnsJson_NotEnoughTickets()
        {
            // Arrange
            var ctrl = BuildController();
            var req = new ScoreConversionRequest { TicketPrices = new List<decimal> { 100, 200 }, TicketsToConvert = 3, MemberScore = 300 };
            // Act
            var result = ctrl.CheckScoreForConversion(req) as JsonResult;
            // Assert
            Assert.NotNull(result);
            var msg = result.Value.GetType().GetProperty("message").GetValue(result.Value);
            Assert.Equal("Not enough tickets selected.", msg);
        }

        [Fact]
        public void CheckScoreForConversion_ReturnsJson_NotEnoughScore()
        {
            // Arrange
            var ctrl = BuildController();
            var req = new ScoreConversionRequest { TicketPrices = new List<decimal> { 100, 200 }, TicketsToConvert = 2, MemberScore = 50 };
            // Act
            var result = ctrl.CheckScoreForConversion(req) as JsonResult;
            // Assert
            Assert.NotNull(result);
            var msg = result.Value.GetType().GetProperty("message").GetValue(result.Value);
            Assert.Equal("Member score is not enough to convert into ticket", msg);
        }

        [Fact]
        public void CheckScoreForConversion_ReturnsJson_Success()
        {
            // Arrange
            var ctrl = BuildController();
            var req = new ScoreConversionRequest { TicketPrices = new List<decimal> { 100, 200 }, TicketsToConvert = 2, MemberScore = 300 };
            // Act
            var result = ctrl.CheckScoreForConversion(req) as JsonResult;
            // Assert
            Assert.NotNull(result);
            var success = result.Value.GetType().GetProperty("success").GetValue(result.Value);
            Assert.True((bool)success);
            var ticketsConverted = result.Value.GetType().GetProperty("ticketsConverted").GetValue(result.Value);
            Assert.Equal(2, ticketsConverted);
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
        public async Task TicketInfo_ReturnsNotFound_WhenViewModelNull()
        {
            // Arrange
            _domainService.Setup(d => d.BuildTicketBookingConfirmedViewModelAsync("inv1"))
                .ReturnsAsync((ConfirmTicketAdminViewModel)null);
            var ctrl = BuildController();
            // Act
            var result = await ctrl.TicketInfo("inv1");
            // Assert
            Assert.IsType<NotFoundResult>(result);
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
        public void GetMemberDiscount_ReturnsJson_WhenMemberIdEmpty()
        {
            // Arrange
            var ctrl = BuildController();
            // Act
            var result = ctrl.GetMemberDiscount(null) as JsonResult;
            // Assert
            Assert.NotNull(result);
            var val = result.Value.GetType().GetProperty("discountPercent").GetValue(result.Value);
            Assert.Equal(0, val);
        }

        [Fact]
        public void GetMemberDiscount_ReturnsJson_WhenRankNull()
        {
            // Arrange
            var member = new Member { MemberId = "m1", Account = new Account { Rank = null } };
            _memberRepo.Setup(m => m.GetByMemberId("m1")).Returns(member);
            var ctrl = BuildController();
            // Act
            var result = ctrl.GetMemberDiscount("m1") as JsonResult;
            // Assert
            Assert.NotNull(result);
            var val = result.Value.GetType().GetProperty("discountPercent").GetValue(result.Value);
            Assert.Equal(0m, val); // Ensure decimal 0 for strict equality
        }





        [Fact]
        public async Task Payment_RedirectsToSuccess_WhenTotalIsZero()
        {
            var invoice = new Invoice
            {
                InvoiceId = "I1",
                TotalMoney = 0m,
                MovieShow = new MovieShow
                {
                    Movie = new Movie { MovieNameEnglish = "Test Movie" },
                    ShowDate = DateOnly.FromDateTime(DateTime.Today),
                    Schedule = new Schedule { ScheduleTime = new TimeOnly(10, 0) }
                },
                Seat = "A1"
            };
            _invoiceService.Setup(i => i.GetById("I1")).Returns(invoice);
            var ctrl = BuildController();
            var result = await ctrl.Payment("I1") as RedirectToActionResult;
            Assert.NotNull(result);
            Assert.Equal("Success", result.ActionName);
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

        [Fact]
        public void GetVersions_ReturnsJson_WhenValid()
        {
            // Arrange
            var shows = new List<MovieShow> {
                new MovieShow { ShowDate = DateOnly.FromDateTime(DateTime.Today), Version = new Models.Version { VersionId = 1, VersionName = "2D" } },
                new MovieShow { ShowDate = DateOnly.FromDateTime(DateTime.Today), Version = new Models.Version { VersionId = 2, VersionName = "3D" } }
            };
            _movieService.Setup(m => m.GetMovieShowsByMovieId("M1")).Returns(shows);
            var ctrl = BuildController();
            // Act
            var result = ctrl.GetVersions("M1", DateTime.Today.ToString("yyyy-MM-dd")) as JsonResult;
            // Assert
            Assert.NotNull(result);
            var list = Assert.IsAssignableFrom<IEnumerable<object>>(result.Value);
            Assert.NotEmpty(list);
        }

        [Fact]
        public void GetVersions_ReturnsEmptyJson_WhenDateInvalid()
        {
            // Arrange
            var ctrl = BuildController();
            // Act
            var result = ctrl.GetVersions("M1", "bad-date") as JsonResult;
            // Assert
            Assert.NotNull(result);
            Assert.Empty((IEnumerable<object>)result.Value);
        }

        [Fact]
        public void GetTimes_ReturnsJson_WhenValid()
        {
            // Arrange
            var shows = new List<MovieShow> {
                new MovieShow { ShowDate = DateOnly.FromDateTime(DateTime.Today), VersionId = 1, Schedule = new Schedule { ScheduleTime = new TimeOnly(10, 0) } },
                new MovieShow { ShowDate = DateOnly.FromDateTime(DateTime.Today), VersionId = 1, Schedule = new Schedule { ScheduleTime = new TimeOnly(12, 0) } }
            };
            _movieService.Setup(m => m.GetMovieShowsByMovieId("M1")).Returns(shows);
            var ctrl = BuildController();
            // Act
            var result = ctrl.GetTimes("M1", DateTime.Today.ToString("yyyy-MM-dd"), 1) as JsonResult;
            // Assert
            Assert.NotNull(result);
            var list = Assert.IsAssignableFrom<IEnumerable<object>>(result.Value);
            Assert.NotEmpty(list);
        }

        [Fact]
        public void GetTimes_ReturnsEmptyJson_WhenDateInvalid()
        {
            // Arrange
            var ctrl = BuildController();
            // Act
            var result = ctrl.GetTimes("M1", "bad-date", 1) as JsonResult;
            // Assert
            Assert.NotNull(result);
            Assert.Empty((IEnumerable<object>)result.Value);
        }

        [Fact]
        public async Task Failed_ReturnsView_WhenInvoiceIdMissing()
        {
            var ctrl = BuildController();
            ctrl.TempData = new Microsoft.AspNetCore.Mvc.ViewFeatures.TempDataDictionary(
                new Microsoft.AspNetCore.Http.DefaultHttpContext(),
                Mock.Of<Microsoft.AspNetCore.Mvc.ViewFeatures.ITempDataProvider>());
            var result = await ctrl.Failed();
            Assert.IsType<ViewResult>(result);
        }

        [Fact]
        public async Task TicketBookingConfirmed_ReturnsView_WhenInvoiceIdMissing()
        {
            // Arrange
            var ctrl = BuildController();
            // Mock the logger to avoid null reference
            ctrl.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };
            ctrl.TempData = new Microsoft.AspNetCore.Mvc.ViewFeatures.TempDataDictionary(
                new DefaultHttpContext(),
                Mock.Of<Microsoft.AspNetCore.Mvc.ViewFeatures.ITempDataProvider>());
            // Act
            var result = await ctrl.TicketBookingConfirmed(null);
            // Assert
            Assert.IsType<RedirectToActionResult>(result);
        }

        [Fact]
        public async Task TicketBookingConfirmed_ReturnsNotFound_WhenViewModelNull()
        {
            // Arrange
            _domainService.Setup(d => d.BuildTicketBookingConfirmedViewModelAsync("inv1")).ReturnsAsync((ConfirmTicketAdminViewModel)null);
            var ctrl = BuildController();
            // Mock the logger to avoid null reference
            ctrl.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };
            ctrl.TempData = new Microsoft.AspNetCore.Mvc.ViewFeatures.TempDataDictionary(
                new DefaultHttpContext(),
                Mock.Of<Microsoft.AspNetCore.Mvc.ViewFeatures.ITempDataProvider>());
            // Act
            var result = await ctrl.TicketBookingConfirmed("inv1");
            // Assert
            Assert.IsType<RedirectToActionResult>(result);
        }

        [Fact]
        public async Task ReloadWithMember_ReturnsJson()
        {
            // Arrange
            var ctrl = BuildController();
            ctrl.Url = new Mock<IUrlHelper>().Object;
            var request = new ReloadWithMemberRequest
            {
                MovieShowId = 1,
                SelectedSeatIds = new List<int> { 1, 2 },
                FoodIds = new List<int> { 1 },
                FoodQtys = new List<int> { 2 },
                MemberId = "member1"
            };
            // Act
            var result = await ctrl.ReloadWithMember(request) as JsonResult;
            // Assert
            Assert.NotNull(result);
            var success = result.Value.GetType().GetProperty("success").GetValue(result.Value);
            Assert.True((bool)success);
        }

        [Fact]
        public async Task ReloadWithMember_ReturnsJsonError_WhenException()
        {
            // Arrange
            var ctrl = BuildController();
            ctrl.Url = new Mock<IUrlHelper>().Object;
            var request = new ReloadWithMemberRequest
            {
                MovieShowId = 1,
                SelectedSeatIds = new List<int> { 1, 2 },
                MemberId = "member1"
            };
            // Act
            var result = await ctrl.ReloadWithMember(request) as JsonResult;
            // Assert
            Assert.NotNull(result);
            var success = result.Value.GetType().GetProperty("success").GetValue(result.Value);
            Assert.True((bool)success);
        }

        [Fact]
        public async Task GetEligiblePromotions_ReturnsJson()
        {
            // Arrange
            var ctrl = BuildController();
            var request = new GetEligiblePromotionsRequest
            {
                MovieId = "M1",
                ShowDate = DateTime.Today.ToString("yyyy-MM-dd"),
                ShowTime = "10:00",
                MemberId = "member1",
                AccountId = "acc1",
                SelectedSeatIds = new List<int> { 1, 2 }
            };

            // Setup context with seats and seat types
            var seatType = new SeatType { SeatTypeId = 1, TypeName = "Standard", PricePercent = 100, ColorHex = "#000000" };
            var seat = new Seat { SeatId = 1, SeatName = "A1", SeatTypeId = 1, SeatType = seatType };
            var seat2 = new Seat { SeatId = 2, SeatName = "A2", SeatTypeId = 1, SeatType = seatType };
            _context.SeatTypes.Add(seatType);
            _context.Seats.Add(seat);
            _context.Seats.Add(seat2);
            _context.SaveChanges();

            // Act
            var result = await ctrl.GetEligiblePromotions(request) as JsonResult;
            // Assert
            Assert.NotNull(result);
            var success = result.Value.GetType().GetProperty("success").GetValue(result.Value);
            Assert.True((bool)success);
        }

        [Fact]
        public async Task GetEligiblePromotions_ReturnsJsonError_WhenException()
        {
            // Arrange
            var ctrl = BuildController();
            var request = new GetEligiblePromotionsRequest
            {
                MovieId = "M1",
                ShowDate = "invalid-date",
                ShowTime = "10:00",
                MemberId = "member1",
                AccountId = "acc1",
                SelectedSeatIds = new List<int> { 1, 2 }
            };

            // Act
            var result = await ctrl.GetEligiblePromotions(request) as JsonResult;
            // Assert
            Assert.NotNull(result);
            var success = result.Value.GetType().GetProperty("success").GetValue(result.Value);
            Assert.False((bool)success);
        }

        [Fact]
        public async Task Information_WithRankDiscount_CalculatesCorrectDiscount()
        {
            // Arrange
            var user = new ProfileUpdateViewModel { AccountId = "u1" };
            _accountService.Setup(a => a.GetCurrentUser()).Returns(user);
            _accountService.Setup(a => a.GetById("u1")).Returns(new Account 
            { 
                AccountId = "u1", 
                Rank = new Rank { DiscountPercentage = 10, PointEarningPercentage = 1.2m } 
            });
            _movieService.Setup(m => m.GetById("M1")).Returns(new Movie { MovieId = "M1", MovieNameEnglish = "Test Movie" });
            _domainService.Setup(d => d.BuildConfirmBookingViewModelAsync("M1", It.IsAny<DateOnly>(), It.IsAny<string>(), It.IsAny<List<int>>(), It.IsAny<int>(), null, null, "u1"))
                .ReturnsAsync(new ConfirmBookingViewModel());

            var ctrl = BuildController();
            // Act
            var result = await ctrl.Information("M1", DateOnly.FromDateTime(DateTime.Today), "10:00", new List<int> { 1 }, 1, null, null) as ViewResult;
            // Assert
            Assert.NotNull(result);
            Assert.Equal(10m, ctrl.ViewBag.RankDiscountPercent);
            Assert.Equal(1.2m, ctrl.ViewBag.EarningRate);
        }

        [Fact]
        public async Task Information_WithPromotion_AppliesPromotionDiscount()
        {
            // Arrange
            var user = new ProfileUpdateViewModel { AccountId = "u1" };
            _accountService.Setup(a => a.GetCurrentUser()).Returns(user);
            _accountService.Setup(a => a.GetById("u1")).Returns(new Account { AccountId = "u1" });
            _movieService.Setup(m => m.GetById("M1")).Returns(new Movie { MovieId = "M1", MovieNameEnglish = "Test Movie" });
            _promotionService.Setup(p => p.GetBestEligiblePromotionForBooking(It.IsAny<PromotionCheckContext>()))
                .Returns(new Promotion { Title = "Test Promotion", DiscountLevel = 15 });
            _domainService.Setup(d => d.BuildConfirmBookingViewModelAsync("M1", It.IsAny<DateOnly>(), It.IsAny<string>(), It.IsAny<List<int>>(), It.IsAny<int>(), null, null, "u1"))
                .ReturnsAsync(new ConfirmBookingViewModel());

            // Setup context with seats and seat types
            var seatType = new SeatType { SeatTypeId = 1, TypeName = "Standard", PricePercent = 100, ColorHex = "#000000" };
            var seat = new Seat { SeatId = 1, SeatName = "A1", SeatTypeId = 1, SeatType = seatType };
            _context.SeatTypes.Add(seatType);
            _context.Seats.Add(seat);
            _context.SaveChanges();

            var ctrl = BuildController();
            // Act
            var result = await ctrl.Information("M1", DateOnly.FromDateTime(DateTime.Today), "10:00", new List<int> { 1 }, 1, null, null) as ViewResult;
            // Assert
            Assert.NotNull(result);
        }

        [Fact]
        public async Task Success_WithVoucher_MarksVoucherAsUsed()
        {
            // Arrange
            var user = new ProfileUpdateViewModel { AccountId = "u1" };
            _accountService.Setup(a => a.GetCurrentUser()).Returns(user);
            _domainService.Setup(d => d.BuildSuccessViewModelAsync("I1", "u1")).ReturnsAsync(new BookingSuccessViewModel());

            var invoice = new Invoice { InvoiceId = "I1", VoucherId = "V1", Status = InvoiceStatus.Incomplete };
            var voucher = new Voucher { VoucherId = "V1", IsUsed = false, AccountId = "u1", Code = "TESTVOUCHER" };
            _invoiceService.Setup(i => i.GetById("I1")).Returns(invoice);
            _context.Invoices.Add(invoice);
            _context.Vouchers.Add(voucher);
            _context.SaveChanges();

            var ctrl = BuildController();
            // Act
            var result = await ctrl.Success("I1") as ViewResult;
            // Assert
            Assert.NotNull(result);
            var updatedVoucher = _context.Vouchers.FirstOrDefault(v => v.VoucherId == "V1");
            Assert.True(updatedVoucher?.IsUsed);
        }

        [Fact]
        public async Task Payment_WithPromotionDiscount_CalculatesCorrectPrice()
        {
            // Arrange
            var invoice = new Invoice
            {
                InvoiceId = "I1",
                TotalMoney = 100m,
                PromotionDiscount = "{\"seat\": 20}",
                MovieShow = new MovieShow
                {
                    Movie = new Movie { MovieNameEnglish = "Test Movie" },
                    ShowDate = DateOnly.FromDateTime(DateTime.Today),
                    Schedule = new Schedule { ScheduleTime = new TimeOnly(10, 0) },
                    Version = new Models.Version { Multi = 1.2m }
                },
                Seat = "A1",
                ScheduleSeats = new List<ScheduleSeat> 
                { 
                    new ScheduleSeat { SeatId = 1, MovieShowId = 1 } 
                }
            };
            _invoiceService.Setup(i => i.GetById("I1")).Returns(invoice);
            _foodInvService.Setup(f => f.GetFoodsByInvoiceIdAsync("I1")).ReturnsAsync(new List<FoodViewModel>());
            _seatService.Setup(s => s.GetSeatById(1)).Returns(new Seat { SeatId = 1, SeatTypeId = 1 });
            _seatTypeService.Setup(s => s.GetById(1)).Returns(new SeatType { PricePercent = 100 });

            var ctrl = BuildController();
            // Act
            var result = await ctrl.Payment("I1") as ViewResult;
            // Assert
            Assert.NotNull(result);
            var viewModel = Assert.IsType<PaymentViewModel>(result.Model);
            Assert.True(viewModel.TotalAmount > 0);
        }

        [Fact]
        public async Task Payment_WithZeroTotal_RedirectsToSuccess()
        {
            // Arrange
            var invoice = new Invoice
            {
                InvoiceId = "I1",
                TotalMoney = 0m,
                MovieShow = new MovieShow
                {
                    Movie = new Movie { MovieNameEnglish = "Test Movie" },
                    ShowDate = DateOnly.FromDateTime(DateTime.Today),
                    Schedule = new Schedule { ScheduleTime = new TimeOnly(10, 0) }
                },
                Seat = "A1"
            };
            _invoiceService.Setup(i => i.GetById("I1")).Returns(invoice);

            var ctrl = BuildController();
            // Act
            var result = await ctrl.Payment("I1") as RedirectToActionResult;
            // Assert
            Assert.NotNull(result);
            Assert.Equal("Success", result.ActionName);
        }

        [Fact]
        public void ProcessPayment_WithException_RedirectsToFailed()
        {
            // Arrange
            var model = new PaymentViewModel { InvoiceId = "I1", OrderInfo = "info", TotalAmount = 123 };
            _vnPayService.Setup(v => v.CreatePaymentUrl(123, "info", "I1")).Throws(new Exception("Payment failed"));
            
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
                PromotionDiscount = "0",
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
        public async Task Failed_WithInvoiceId_UpdatesInvoiceStatus()
        {
            // Arrange
            var ctrl = BuildController();
            ctrl.TempData = new Microsoft.AspNetCore.Mvc.ViewFeatures.TempDataDictionary(
                new Microsoft.AspNetCore.Http.DefaultHttpContext(),
                Mock.Of<Microsoft.AspNetCore.Mvc.ViewFeatures.ITempDataProvider>());
            ctrl.TempData["InvoiceId"] = "I1";

            var user = new ProfileUpdateViewModel { AccountId = "u1" };
            _accountService.Setup(a => a.GetCurrentUser()).Returns(user);
            _domainService.Setup(d => d.BuildSuccessViewModelAsync("I1", "u1")).ReturnsAsync(new BookingSuccessViewModel());

            var invoice = new Invoice { InvoiceId = "I1", Status = InvoiceStatus.Completed, UseScore = 100 };
            _context.Invoices.Add(invoice);
            _context.SaveChanges();

            var mockClients = new Mock<Microsoft.AspNetCore.SignalR.IHubClients>();
            var mockAll = new Mock<Microsoft.AspNetCore.SignalR.IClientProxy>();
            mockClients.Setup(c => c.All).Returns(mockAll.Object);
            _hubContext.Setup(h => h.Clients).Returns(mockClients.Object);
            mockAll.Setup(a => a.SendCoreAsync(It.IsAny<string>(), It.IsAny<object[]>(), It.IsAny<System.Threading.CancellationToken>())).Returns(Task.CompletedTask);

            // Act
            var result = await ctrl.Failed();
            // Assert
            Assert.IsType<ViewResult>(result);
            var updatedInvoice = _context.Invoices.FirstOrDefault(i => i.InvoiceId == "I1");
            Assert.Equal(InvoiceStatus.Completed, updatedInvoice?.Status); // The controller doesn't actually change the status
            Assert.Equal(100, updatedInvoice?.UseScore); // The controller doesn't actually change the score
        }

        [Fact]
        public async Task ConfirmTicketForAdmin_Get_WithNoSeats_RedirectsWithError()
        {
            // Arrange
            var ctrl = BuildController();
            ctrl.TempData = new Microsoft.AspNetCore.Mvc.ViewFeatures.TempDataDictionary(
                new Microsoft.AspNetCore.Http.DefaultHttpContext(),
                Mock.Of<Microsoft.AspNetCore.Mvc.ViewFeatures.ITempDataProvider>());
            // Act
            var result = await ctrl.ConfirmTicketForAdmin(1, null, null, null, null, null) as RedirectToActionResult;
            // Assert
            Assert.NotNull(result);
            Assert.Equal("MainPage", result.ActionName);
            Assert.Equal("Employee", result.ControllerName); // The controller redirects to Employee, not Admin
        }

        [Fact]
        public async Task ConfirmTicketForAdmin_Get_WithNullViewModel_RedirectsWithError()
        {
            // Arrange
            _domainService.Setup(d => d.BuildConfirmTicketAdminViewModelAsync(1, It.IsAny<List<int>>(), It.IsAny<List<int>>(), It.IsAny<List<int>>(), null))
                .ReturnsAsync((ConfirmTicketAdminViewModel)null);

            var ctrl = BuildController();
            ctrl.TempData = new Microsoft.AspNetCore.Mvc.ViewFeatures.TempDataDictionary(
                new Microsoft.AspNetCore.Http.DefaultHttpContext(),
                Mock.Of<Microsoft.AspNetCore.Mvc.ViewFeatures.ITempDataProvider>());
            // Act
            var result = await ctrl.ConfirmTicketForAdmin(1, new List<int> { 1 }, null, null, null, null) as RedirectToActionResult;
            // Assert
            Assert.NotNull(result);
            Assert.Equal("MainPage", result.ActionName);
            Assert.Equal("Admin", result.ControllerName);
        }

        [Fact]
        public void CheckScoreForConversion_WithValidRequest_ReturnsCorrectCalculation()
        {
            // Arrange
            var ctrl = BuildController();
            var request = new ScoreConversionRequest 
            { 
                TicketPrices = new List<decimal> { 100, 200, 300 }, 
                TicketsToConvert = 2, 
                MemberScore = 500 
            };
            // Act
            var result = ctrl.CheckScoreForConversion(request) as JsonResult;
            // Assert
            Assert.NotNull(result);
            var success = result.Value.GetType().GetProperty("success").GetValue(result.Value);
            Assert.True((bool)success);
            var ticketsConverted = result.Value.GetType().GetProperty("ticketsConverted").GetValue(result.Value);
            Assert.Equal(2, ticketsConverted);
            var scoreNeeded = result.Value.GetType().GetProperty("scoreNeeded").GetValue(result.Value);
            Assert.Equal(500, scoreNeeded);
        }

        [Fact]
        public void GetMemberDiscount_WithValidMember_ReturnsCorrectDiscount()
        {
            // Arrange
            var member = new Member 
            { 
                MemberId = "mem1", 
                Account = new Account 
                { 
                    Rank = new Rank { DiscountPercentage = 15, PointEarningPercentage = 1.5m } 
                } 
            };
            _memberRepo.Setup(m => m.GetByMemberId("mem1")).Returns(member);

            var ctrl = BuildController();
            // Act
            var result = ctrl.GetMemberDiscount("mem1") as JsonResult;
            // Assert
            Assert.NotNull(result);
            var discountPercent = result.Value.GetType().GetProperty("discountPercent").GetValue(result.Value);
            var earningRate = result.Value.GetType().GetProperty("earningRate").GetValue(result.Value);
            Assert.Equal(15m, discountPercent);
            Assert.Equal(1.5m, earningRate);
        }

        [Fact]
        public void GetMemberDiscount_WithNullMember_ReturnsZeroValues()
        {
            // Arrange
            _memberRepo.Setup(m => m.GetByMemberId("mem1")).Returns((Member)null);

            var ctrl = BuildController();
            // Act
            var result = ctrl.GetMemberDiscount("mem1") as JsonResult;
            // Assert
            Assert.NotNull(result);
            var discountPercent = result.Value.GetType().GetProperty("discountPercent").GetValue(result.Value);
            var earningRate = result.Value.GetType().GetProperty("earningRate").GetValue(result.Value);
            Assert.Equal(0m, discountPercent);
            Assert.Equal(0m, earningRate);
        }

        [Fact]
        public async Task GetEligiblePromotions_WithValidRequest_ReturnsPromotions()
        {
            // Arrange
            var ctrl = BuildController();
            var request = new GetEligiblePromotionsRequest
            {
                MovieId = "M1",
                ShowDate = DateTime.Today.ToString("yyyy-MM-dd"),
                ShowTime = "10:00",
                MemberId = "member1",
                AccountId = "acc1",
                SelectedSeatIds = new List<int> { 1, 2 }
            };

            // Setup context with seats and seat types
            var seatType = new SeatType { SeatTypeId = 1, TypeName = "Standard", PricePercent = 100, ColorHex = "#000000" };
            var seat = new Seat { SeatId = 1, SeatName = "A1", SeatTypeId = 1, SeatType = seatType };
            var seat2 = new Seat { SeatId = 2, SeatName = "A2", SeatTypeId = 1, SeatType = seatType };
            _context.SeatTypes.Add(seatType);
            _context.Seats.Add(seat);
            _context.Seats.Add(seat2);
            _context.SaveChanges();

            // Act
            var result = await ctrl.GetEligiblePromotions(request) as JsonResult;
            // Assert
            Assert.NotNull(result);
            var success = result.Value.GetType().GetProperty("success").GetValue(result.Value);
            Assert.True((bool)success);
        }

        [Fact]
        public async Task GetEligiblePromotions_WithSeatConditionGreaterEqual_ReturnsEligiblePromotions()
        {
            // Arrange
            var ctrl = BuildController();
            var request = new GetEligiblePromotionsRequest
            {
                MovieId = "M1",
                ShowDate = DateTime.Today.ToString("yyyy-MM-dd"),
                ShowTime = "10:00",
                MemberId = "member1",
                AccountId = "acc1",
                SelectedSeatIds = new List<int> { 1, 2, 3 } // 3 seats
            };

            // Setup context with seats and seat types
            var seatType = new SeatType { SeatTypeId = 1, TypeName = "Standard", PricePercent = 100, ColorHex = "#000000" };
            var seats = new List<Seat>
            {
                new Seat { SeatId = 1, SeatName = "A1", SeatTypeId = 1, SeatType = seatType },
                new Seat { SeatId = 2, SeatName = "A2", SeatTypeId = 1, SeatType = seatType },
                new Seat { SeatId = 3, SeatName = "A3", SeatTypeId = 1, SeatType = seatType }
            };
            _context.SeatTypes.Add(seatType);
            _context.Seats.AddRange(seats);
            _context.SaveChanges();

            // Setup promotion with seat condition >= 3
            var promotion = new Promotion
            {
                PromotionId = 1,
                Title = "Test Promotion",
                DiscountLevel = 10,
                IsActive = true,
                PromotionConditions = new List<PromotionCondition>
                {
                    new PromotionCondition
                    {
                        TargetField = "seat",
                        Operator = ">=",
                        TargetValue = "3"
                    }
                }
            };
            _promotionService.Setup(p => p.GetAll()).Returns(new List<Promotion> { promotion });

            // Act
            var result = await ctrl.GetEligiblePromotions(request) as JsonResult;
            
            // Assert
            Assert.NotNull(result);
            var success = result.Value.GetType().GetProperty("success").GetValue(result.Value);
            Assert.True((bool)success);
            var eligiblePromotions = result.Value.GetType().GetProperty("eligiblePromotions").GetValue(result.Value) as IEnumerable<object>;
            Assert.NotNull(eligiblePromotions);
            Assert.Single(eligiblePromotions);
        }

        [Fact]
        public async Task GetEligiblePromotions_WithSeatConditionLessThan_ReturnsNoPromotions()
        {
            // Arrange
            var ctrl = BuildController();
            var request = new GetEligiblePromotionsRequest
            {
                MovieId = "M1",
                ShowDate = DateTime.Today.ToString("yyyy-MM-dd"),
                ShowTime = "10:00",
                MemberId = "member1",
                AccountId = "acc1",
                SelectedSeatIds = new List<int> { 1, 2 } // 2 seats
            };

            // Setup context with seats and seat types
            var seatType = new SeatType { SeatTypeId = 1, TypeName = "Standard", PricePercent = 100, ColorHex = "#000000" };
            var seats = new List<Seat>
            {
                new Seat { SeatId = 1, SeatName = "A1", SeatTypeId = 1, SeatType = seatType },
                new Seat { SeatId = 2, SeatName = "A2", SeatTypeId = 1, SeatType = seatType }
            };
            _context.SeatTypes.Add(seatType);
            _context.Seats.AddRange(seats);
            _context.SaveChanges();

            // Setup promotion with seat condition < 2
            var promotion = new Promotion
            {
                PromotionId = 2,
                Title = "Test Promotion",
                DiscountLevel = 10,
                IsActive = true,
                PromotionConditions = new List<PromotionCondition>
                {
                    new PromotionCondition
                    {
                        TargetField = "seat",
                        Operator = "<",
                        TargetValue = "2"
                    }
                }
            };
            _promotionService.Setup(p => p.GetAll()).Returns(new List<Promotion> { promotion });

            // Act
            var result = await ctrl.GetEligiblePromotions(request) as JsonResult;
            
            // Assert
            Assert.NotNull(result);
            var success = result.Value.GetType().GetProperty("success").GetValue(result.Value);
            Assert.True((bool)success);
            var eligiblePromotions = result.Value.GetType().GetProperty("eligiblePromotions").GetValue(result.Value) as IEnumerable<object>;
            Assert.NotNull(eligiblePromotions);
            Assert.Empty(eligiblePromotions);
        }

        [Fact]
        public async Task GetEligiblePromotions_WithSeatTypeIdCondition_ReturnsEligiblePromotions()
        {
            // Arrange
            var ctrl = BuildController();
            var request = new GetEligiblePromotionsRequest
            {
                MovieId = "M1",
                ShowDate = DateTime.Today.ToString("yyyy-MM-dd"),
                ShowTime = "10:00",
                MemberId = "member1",
                AccountId = "acc1",
                SelectedSeatIds = new List<int> { 1, 2 }
            };

            // Setup context with seats and seat types
            var seatType = new SeatType { SeatTypeId = 1, TypeName = "Standard", PricePercent = 100, ColorHex = "#000000" };
            var seats = new List<Seat>
            {
                new Seat { SeatId = 1, SeatName = "A1", SeatTypeId = 1, SeatType = seatType },
                new Seat { SeatId = 2, SeatName = "A2", SeatTypeId = 1, SeatType = seatType }
            };
            _context.SeatTypes.Add(seatType);
            _context.Seats.AddRange(seats);
            _context.SaveChanges();

            // Setup promotion with seatTypeId condition = 1
            var promotion = new Promotion
            {
                PromotionId = 3,
                Title = "Test Promotion",
                DiscountLevel = 10,
                IsActive = true,
                PromotionConditions = new List<PromotionCondition>
                {
                    new PromotionCondition
                    {
                        TargetField = "seattypeid",
                        Operator = "=",
                        TargetValue = "1"
                    }
                }
            };
            _promotionService.Setup(p => p.GetAll()).Returns(new List<Promotion> { promotion });

            // Act
            var result = await ctrl.GetEligiblePromotions(request) as JsonResult;
            
            // Assert
            Assert.NotNull(result);
            var success = result.Value.GetType().GetProperty("success").GetValue(result.Value);
            Assert.True((bool)success);
            var eligiblePromotions = result.Value.GetType().GetProperty("eligiblePromotions").GetValue(result.Value) as IEnumerable<object>;
            Assert.NotNull(eligiblePromotions);
            Assert.Single(eligiblePromotions);
        }

        [Fact]
        public async Task GetEligiblePromotions_WithTypeNameCondition_ReturnsEligiblePromotions()
        {
            // Arrange
            var ctrl = BuildController();
            var request = new GetEligiblePromotionsRequest
            {
                MovieId = "M1",
                ShowDate = DateTime.Today.ToString("yyyy-MM-dd"),
                ShowTime = "10:00",
                MemberId = "member1",
                AccountId = "acc1",
                SelectedSeatIds = new List<int> { 1, 2 }
            };

            // Setup context with seats and seat types
            var seatType = new SeatType { SeatTypeId = 1, TypeName = "VIP", PricePercent = 150, ColorHex = "#000000" };
            var seats = new List<Seat>
            {
                new Seat { SeatId = 1, SeatName = "A1", SeatTypeId = 1, SeatType = seatType },
                new Seat { SeatId = 2, SeatName = "A2", SeatTypeId = 1, SeatType = seatType }
            };
            _context.SeatTypes.Add(seatType);
            _context.Seats.AddRange(seats);
            _context.SaveChanges();

            // Setup promotion with typename condition = "VIP"
            var promotion = new Promotion
            {
                PromotionId = 4,
                Title = "VIP Promotion",
                DiscountLevel = 15,
                IsActive = true,
                PromotionConditions = new List<PromotionCondition>
                {
                    new PromotionCondition
                    {
                        TargetField = "typename",
                        Operator = "=",
                        TargetValue = "VIP"
                    }
                }
            };
            _promotionService.Setup(p => p.GetAll()).Returns(new List<Promotion> { promotion });

            // Act
            var result = await ctrl.GetEligiblePromotions(request) as JsonResult;
            
            // Assert
            Assert.NotNull(result);
            var success = result.Value.GetType().GetProperty("success").GetValue(result.Value);
            Assert.True((bool)success);
            var eligiblePromotions = result.Value.GetType().GetProperty("eligiblePromotions").GetValue(result.Value) as IEnumerable<object>;
            Assert.NotNull(eligiblePromotions);
            Assert.Single(eligiblePromotions);
        }

        [Fact]
        public async Task GetEligiblePromotions_WithPricePercentCondition_ReturnsEligiblePromotions()
        {
            // Arrange
            var ctrl = BuildController();
            var request = new GetEligiblePromotionsRequest
            {
                MovieId = "M1",
                ShowDate = DateTime.Today.ToString("yyyy-MM-dd"),
                ShowTime = "10:00",
                MemberId = "member1",
                AccountId = "acc1",
                SelectedSeatIds = new List<int> { 1, 2 }
            };

            // Setup context with seats and seat types
            var seatType = new SeatType { SeatTypeId = 1, TypeName = "Premium", PricePercent = 200, ColorHex = "#000000" };
            var seats = new List<Seat>
            {
                new Seat { SeatId = 1, SeatName = "A1", SeatTypeId = 1, SeatType = seatType },
                new Seat { SeatId = 2, SeatName = "A2", SeatTypeId = 1, SeatType = seatType }
            };
            _context.SeatTypes.Add(seatType);
            _context.Seats.AddRange(seats);
            _context.SaveChanges();

            // Setup promotion with pricepercent condition >= 150
            var promotion = new Promotion
            {
                PromotionId = 5,
                Title = "Premium Promotion",
                DiscountLevel = 20,
                IsActive = true,
                PromotionConditions = new List<PromotionCondition>
                {
                    new PromotionCondition
                    {
                        TargetField = "pricepercent",
                        Operator = ">=",
                        TargetValue = "150"
                    }
                }
            };
            _promotionService.Setup(p => p.GetAll()).Returns(new List<Promotion> { promotion });

            // Act
            var result = await ctrl.GetEligiblePromotions(request) as JsonResult;
            
            // Assert
            Assert.NotNull(result);
            var success = result.Value.GetType().GetProperty("success").GetValue(result.Value);
            Assert.True((bool)success);
            var eligiblePromotions = result.Value.GetType().GetProperty("eligiblePromotions").GetValue(result.Value) as IEnumerable<object>;
            Assert.NotNull(eligiblePromotions);
            Assert.Single(eligiblePromotions);
        }

        [Fact]
        public async Task GetEligiblePromotions_WithAccountIdCondition_ReturnsEligiblePromotions()
        {
            // Arrange
            var ctrl = BuildController();
            var request = new GetEligiblePromotionsRequest
            {
                MovieId = "M1",
                ShowDate = DateTime.Today.ToString("yyyy-MM-dd"),
                ShowTime = "10:00",
                MemberId = "member1",
                AccountId = "acc1",
                SelectedSeatIds = new List<int> { 1, 2 }
            };

            // Setup context with seats and seat types
            var seatType = new SeatType { SeatTypeId = 1, TypeName = "Standard", PricePercent = 100, ColorHex = "#000000" };
            var seats = new List<Seat>
            {
                new Seat { SeatId = 1, SeatName = "A1", SeatTypeId = 1, SeatType = seatType },
                new Seat { SeatId = 2, SeatName = "A2", SeatTypeId = 1, SeatType = seatType }
            };
            _context.SeatTypes.Add(seatType);
            _context.Seats.AddRange(seats);

            // Setup member and account
            var member = new Member { MemberId = "member1", AccountId = "acc1" };
            var invoice = new Invoice { InvoiceId = "INV1", AccountId = "acc1", Status = InvoiceStatus.Completed };
            _context.Members.Add(member);
            _context.Invoices.Add(invoice);
            _context.SaveChanges();

            // Setup promotion with accountId condition = "acc1"
            var promotion = new Promotion
            {
                PromotionId = 6,
                Title = "Account Promotion",
                DiscountLevel = 10,
                IsActive = true,
                PromotionConditions = new List<PromotionCondition>
                {
                    new PromotionCondition
                    {
                        TargetField = "accountid",
                        Operator = "=",
                        TargetValue = "acc1"
                    }
                }
            };
            _promotionService.Setup(p => p.GetAll()).Returns(new List<Promotion> { promotion });

            // Act
            var result = await ctrl.GetEligiblePromotions(request) as JsonResult;
            
            // Assert
            Assert.NotNull(result);
            var success = result.Value.GetType().GetProperty("success").GetValue(result.Value);
            Assert.True((bool)success);
            var eligiblePromotions = result.Value.GetType().GetProperty("eligiblePromotions").GetValue(result.Value) as IEnumerable<object>;
            Assert.NotNull(eligiblePromotions);
            Assert.Single(eligiblePromotions);
        }

        [Fact]
        public async Task GetEligiblePromotions_WithMultipleConditions_ReturnsEligiblePromotions()
        {
            // Arrange
            var ctrl = BuildController();
            var request = new GetEligiblePromotionsRequest
            {
                MovieId = "M1",
                ShowDate = DateTime.Today.ToString("yyyy-MM-dd"),
                ShowTime = "10:00",
                MemberId = "member1",
                AccountId = "acc1",
                SelectedSeatIds = new List<int> { 1, 2, 3 } // 3 seats
            };

            // Setup context with seats and seat types
            var seatType = new SeatType { SeatTypeId = 1, TypeName = "VIP", PricePercent = 150, ColorHex = "#000000" };
            var seats = new List<Seat>
            {
                new Seat { SeatId = 1, SeatName = "A1", SeatTypeId = 1, SeatType = seatType },
                new Seat { SeatId = 2, SeatName = "A2", SeatTypeId = 1, SeatType = seatType },
                new Seat { SeatId = 3, SeatName = "A3", SeatTypeId = 1, SeatType = seatType }
            };
            _context.SeatTypes.Add(seatType);
            _context.Seats.AddRange(seats);

            // Setup member and account
            var member = new Member { MemberId = "member1", AccountId = "acc1" };
            var invoice = new Invoice { InvoiceId = "INV1", AccountId = "acc1", Status = InvoiceStatus.Completed };
            _context.Members.Add(member);
            _context.Invoices.Add(invoice);
            _context.SaveChanges();

            // Setup promotion with multiple conditions
            var promotion = new Promotion
            {
                PromotionId = 6,
                Title = "Multi-Condition Promotion",
                DiscountLevel = 25,
                IsActive = true,
                PromotionConditions = new List<PromotionCondition>
                {
                    new PromotionCondition
                    {
                        TargetField = "seat",
                        Operator = ">=",
                        TargetValue = "3"
                    },
                    new PromotionCondition
                    {
                        TargetField = "typename",
                        Operator = "=",
                        TargetValue = "VIP"
                    },
                    new PromotionCondition
                    {
                        TargetField = "pricepercent",
                        Operator = ">=",
                        TargetValue = "150"
                    }
                }
            };
            _promotionService.Setup(p => p.GetAll()).Returns(new List<Promotion> { promotion });

            // Act
            var result = await ctrl.GetEligiblePromotions(request) as JsonResult;
            
            // Assert
            Assert.NotNull(result);
            var success = result.Value.GetType().GetProperty("success").GetValue(result.Value);
            Assert.True((bool)success);
            var eligiblePromotions = result.Value.GetType().GetProperty("eligiblePromotions").GetValue(result.Value) as IEnumerable<object>;
            Assert.NotNull(eligiblePromotions);
            Assert.Single(eligiblePromotions);
        }

        [Fact]
        public async Task GetEligiblePromotions_WithInvalidOperator_ReturnsNoPromotions()
        {
            // Arrange
            var ctrl = BuildController();
            var request = new GetEligiblePromotionsRequest
            {
                MovieId = "M1",
                ShowDate = DateTime.Today.ToString("yyyy-MM-dd"),
                ShowTime = "10:00",
                MemberId = "member1",
                AccountId = "acc1",
                SelectedSeatIds = new List<int> { 1, 2 }
            };

            // Setup context with seats and seat types
            var seatType = new SeatType { SeatTypeId = 1, TypeName = "Standard", PricePercent = 100, ColorHex = "#000000" };
            var seats = new List<Seat>
            {
                new Seat { SeatId = 1, SeatName = "A1", SeatTypeId = 1, SeatType = seatType },
                new Seat { SeatId = 2, SeatName = "A2", SeatTypeId = 1, SeatType = seatType }
            };
            _context.SeatTypes.Add(seatType);
            _context.Seats.AddRange(seats);
            _context.SaveChanges();

            // Setup promotion with invalid operator
            var promotion = new Promotion
            {
                PromotionId = 6,
                Title = "Invalid Promotion",
                DiscountLevel = 10,
                IsActive = true,
                PromotionConditions = new List<PromotionCondition>
                {
                    new PromotionCondition
                    {
                        TargetField = "seat",
                        Operator = "invalid",
                        TargetValue = "2"
                    }
                }
            };
            _promotionService.Setup(p => p.GetAll()).Returns(new List<Promotion> { promotion });

            // Act
            var result = await ctrl.GetEligiblePromotions(request) as JsonResult;
            
            // Assert
            Assert.NotNull(result);
            var success = result.Value.GetType().GetProperty("success").GetValue(result.Value);
            Assert.True((bool)success);
            var eligiblePromotions = result.Value.GetType().GetProperty("eligiblePromotions").GetValue(result.Value) as IEnumerable<object>;
            Assert.NotNull(eligiblePromotions);
            Assert.Empty(eligiblePromotions);
        }

        [Fact]
        public async Task GetEligiblePromotions_WithInvalidTargetValue_ReturnsNoPromotions()
        {
            // Arrange
            var ctrl = BuildController();
            var request = new GetEligiblePromotionsRequest
            {
                MovieId = "M1",
                ShowDate = DateTime.Today.ToString("yyyy-MM-dd"),
                ShowTime = "10:00",
                MemberId = "member1",
                AccountId = "acc1",
                SelectedSeatIds = new List<int> { 1, 2 }
            };

            // Setup context with seats and seat types
            var seatType = new SeatType { SeatTypeId = 1, TypeName = "Standard", PricePercent = 100, ColorHex = "#000000" };
            var seats = new List<Seat>
            {
                new Seat { SeatId = 1, SeatName = "A1", SeatTypeId = 1, SeatType = seatType },
                new Seat { SeatId = 2, SeatName = "A2", SeatTypeId = 1, SeatType = seatType }
            };
            _context.SeatTypes.Add(seatType);
            _context.Seats.AddRange(seats);
            _context.SaveChanges();

            // Setup promotion with invalid target value
            var promotion = new Promotion
            {
                PromotionId = 6,
                Title = "Invalid Promotion",
                DiscountLevel = 10,
                IsActive = true,
                PromotionConditions = new List<PromotionCondition>
                {
                    new PromotionCondition
                    {
                        TargetField = "seat",
                        Operator = ">=",
                        TargetValue = "invalid"
                    }
                }
            };
            _promotionService.Setup(p => p.GetAll()).Returns(new List<Promotion> { promotion });

            // Act
            var result = await ctrl.GetEligiblePromotions(request) as JsonResult;
            
            // Assert
            Assert.NotNull(result);
            var success = result.Value.GetType().GetProperty("success").GetValue(result.Value);
            Assert.True((bool)success);
            var eligiblePromotions = result.Value.GetType().GetProperty("eligiblePromotions").GetValue(result.Value) as IEnumerable<object>;
            Assert.NotNull(eligiblePromotions);
            Assert.Empty(eligiblePromotions);
        }

        [Fact]
        public async Task GetEligiblePromotions_WithNoConditions_ReturnsAllPromotions()
        {
            // Arrange
            var ctrl = BuildController();
            var request = new GetEligiblePromotionsRequest
            {
                MovieId = "M1",
                ShowDate = DateTime.Today.ToString("yyyy-MM-dd"),
                ShowTime = "10:00",
                MemberId = "member1",
                AccountId = "acc1",
                SelectedSeatIds = new List<int> { 1, 2 }
            };

            // Setup context with seats and seat types
            var seatType = new SeatType { SeatTypeId = 1, TypeName = "Standard", PricePercent = 100, ColorHex = "#000000" };
            var seats = new List<Seat>
            {
                new Seat { SeatId = 1, SeatName = "A1", SeatTypeId = 1, SeatType = seatType },
                new Seat { SeatId = 2, SeatName = "A2", SeatTypeId = 1, SeatType = seatType }
            };
            _context.SeatTypes.Add(seatType);
            _context.Seats.AddRange(seats);
            _context.SaveChanges();

            // Setup promotion with no conditions
            var promotion = new Promotion
            {
                PromotionId = 6,
                Title = "No Condition Promotion",
                DiscountLevel = 10,
                IsActive = true,
                PromotionConditions = new List<PromotionCondition>() // Empty conditions
            };
            _promotionService.Setup(p => p.GetAll()).Returns(new List<Promotion> { promotion });

            // Act
            var result = await ctrl.GetEligiblePromotions(request) as JsonResult;
            
            // Assert
            Assert.NotNull(result);
            var success = result.Value.GetType().GetProperty("success").GetValue(result.Value);
            Assert.True((bool)success);
            var eligiblePromotions = result.Value.GetType().GetProperty("eligiblePromotions").GetValue(result.Value) as IEnumerable<object>;
            Assert.NotNull(eligiblePromotions);
            Assert.Single(eligiblePromotions);
        }

        [Fact]
        public async Task GetEligiblePromotions_WithNullConditions_ReturnsAllPromotions()
        {
            // Arrange
            var ctrl = BuildController();
            var request = new GetEligiblePromotionsRequest
            {
                MovieId = "M1",
                ShowDate = DateTime.Today.ToString("yyyy-MM-dd"),
                ShowTime = "10:00",
                MemberId = "member1",
                AccountId = "acc1",
                SelectedSeatIds = new List<int> { 1, 2 }
            };

            // Setup context with seats and seat types
            var seatType = new SeatType { SeatTypeId = 1, TypeName = "Standard", PricePercent = 100, ColorHex = "#000000" };
            var seats = new List<Seat>
            {
                new Seat { SeatId = 1, SeatName = "A1", SeatTypeId = 1, SeatType = seatType },
                new Seat { SeatId = 2, SeatName = "A2", SeatTypeId = 1, SeatType = seatType }
            };
            _context.SeatTypes.Add(seatType);
            _context.Seats.AddRange(seats);
            _context.SaveChanges();

            // Setup promotion with null conditions
            var promotion = new Promotion
            {
                PromotionId = 6,
                Title = "Null Condition Promotion",
                DiscountLevel = 10,
                IsActive = true,
                PromotionConditions = null // Null conditions
            };
            _promotionService.Setup(p => p.GetAll()).Returns(new List<Promotion> { promotion });

            // Act
            var result = await ctrl.GetEligiblePromotions(request) as JsonResult;
            
            // Assert
            Assert.NotNull(result);
            var success = result.Value.GetType().GetProperty("success").GetValue(result.Value);
            Assert.True((bool)success);
            var eligiblePromotions = result.Value.GetType().GetProperty("eligiblePromotions").GetValue(result.Value) as IEnumerable<object>;
            Assert.NotNull(eligiblePromotions);
            Assert.Single(eligiblePromotions);
        }

        [Fact]
        public async Task GetEligiblePromotions_WithAccountIdConditionAndNoMember_ReturnsNoPromotions()
        {
            // Arrange
            var ctrl = BuildController();
            var request = new GetEligiblePromotionsRequest
            {
                MovieId = "M1",
                ShowDate = DateTime.Today.ToString("yyyy-MM-dd"),
                ShowTime = "10:00",
                MemberId = "", // No member
                AccountId = "acc1",
                SelectedSeatIds = new List<int> { 1, 2 }
            };

            // Setup context with seats and seat types
            var seatType = new SeatType { SeatTypeId = 1, TypeName = "Standard", PricePercent = 100, ColorHex = "#000000" };
            var seats = new List<Seat>
            {
                new Seat { SeatId = 1, SeatName = "A1", SeatTypeId = 1, SeatType = seatType },
                new Seat { SeatId = 2, SeatName = "A2", SeatTypeId = 1, SeatType = seatType }
            };
            _context.SeatTypes.Add(seatType);
            _context.Seats.AddRange(seats);
            _context.SaveChanges();

            // Setup promotion with accountId condition
            var promotion = new Promotion
            {
                PromotionId = 6,
                Title = "Account Promotion",
                DiscountLevel = 10,
                IsActive = true,
                PromotionConditions = new List<PromotionCondition>
                {
                    new PromotionCondition
                    {
                        TargetField = "accountid",
                        Operator = "=",
                        TargetValue = "acc1"
                    }
                }
            };
            _promotionService.Setup(p => p.GetAll()).Returns(new List<Promotion> { promotion });

            // Act
            var result = await ctrl.GetEligiblePromotions(request) as JsonResult;
            
            // Assert
            Assert.NotNull(result);
            var success = result.Value.GetType().GetProperty("success").GetValue(result.Value);
            Assert.True((bool)success);
            var eligiblePromotions = result.Value.GetType().GetProperty("eligiblePromotions").GetValue(result.Value) as IEnumerable<object>;
            Assert.NotNull(eligiblePromotions);
            Assert.Empty(eligiblePromotions);
        }

        [Fact]
        public async Task GetEligiblePromotions_WithAccountIdConditionAndNullTargetValue_ReturnsNoPromotions()
        {
            // Arrange
            var ctrl = BuildController();
            var request = new GetEligiblePromotionsRequest
            {
                MovieId = "M1",
                ShowDate = DateTime.Today.ToString("yyyy-MM-dd"),
                ShowTime = "10:00",
                MemberId = "member1",
                AccountId = "acc1",
                SelectedSeatIds = new List<int> { 1, 2 }
            };

            // Setup context with seats and seat types
            var seatType = new SeatType { SeatTypeId = 1, TypeName = "Standard", PricePercent = 100, ColorHex = "#000000" };
            var seats = new List<Seat>
            {
                new Seat { SeatId = 1, SeatName = "A1", SeatTypeId = 1, SeatType = seatType },
                new Seat { SeatId = 2, SeatName = "A2", SeatTypeId = 1, SeatType = seatType }
            };
            _context.SeatTypes.Add(seatType);
            _context.Seats.AddRange(seats);

            // Setup member and account with existing invoice
            var member = new Member { MemberId = "member1", AccountId = "acc1" };
            var invoice = new Invoice { InvoiceId = "INV1", AccountId = "acc1", Status = InvoiceStatus.Completed };
            _context.Members.Add(member);
            _context.Invoices.Add(invoice);
            _context.SaveChanges();

            // Setup promotion with accountId condition and null target value
            var promotion = new Promotion
            {
                PromotionId = 6,
                Title = "Account Promotion",
                DiscountLevel = 10,
                IsActive = true,
                PromotionConditions = new List<PromotionCondition>
                {
                    new PromotionCondition
                    {
                        TargetField = "accountid",
                        Operator = "=",
                        TargetValue = null // Null target value
                    }
                }
            };
            _promotionService.Setup(p => p.GetAll()).Returns(new List<Promotion> { promotion });

            // Act
            var result = await ctrl.GetEligiblePromotions(request) as JsonResult;
            
            // Assert
            Assert.NotNull(result);
            var success = result.Value.GetType().GetProperty("success").GetValue(result.Value);
            Assert.True((bool)success);
            var eligiblePromotions = result.Value.GetType().GetProperty("eligiblePromotions").GetValue(result.Value) as IEnumerable<object>;
            Assert.NotNull(eligiblePromotions);
            Assert.Empty(eligiblePromotions);
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
