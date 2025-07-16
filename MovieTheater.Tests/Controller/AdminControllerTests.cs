using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Moq;
using MovieTheater.Controllers;
using MovieTheater.Models;
using MovieTheater.Repository;
using MovieTheater.Service;
using MovieTheater.ViewModels;
using Xunit;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace MovieTheater.Tests.Controller
{
    public class AdminControllerTests
    {
        private readonly Mock<IMovieService> _movieSvc = new();
        private readonly Mock<IEmployeeService> _empSvc = new();
        private readonly Mock<IPromotionService> _promoSvc = new();
        private readonly Mock<ICinemaService> _cinemaSvc = new();
        private readonly Mock<ISeatTypeService> _seatType = new();
        private readonly Mock<IMemberRepository> _memberRepo = new();
        private readonly Mock<IAccountService> _acctSvc = new();
        private readonly Mock<IInvoiceService> _invSvc = new();
        private readonly Mock<ISeatService> _seatSvc = new();
        private readonly Mock<IFoodService> _foodSvc = new();
        private readonly Mock<IVoucherService> _vouchSvc = new();
        private readonly Mock<IRankService> _rankSvc = new();

        private AdminController BuildController()
            => new AdminController(
                _movieSvc.Object,
                _empSvc.Object,
                _promoSvc.Object,
                _cinemaSvc.Object,
                _seatType.Object,
                _memberRepo.Object,
                _acctSvc.Object,
                _seatSvc.Object,
                _invSvc.Object,
                _foodSvc.Object,
                _vouchSvc.Object,
                _rankSvc.Object
            );

        [Fact]
        public void MainPage_SetsActiveTabAndReturnsViewModel()
        {
            // Arrange
            var invoices = new List<Invoice> {
                new() { InvoiceId="I1", Status=InvoiceStatus.Completed, BookingDate=DateTime.Today, Seat="A1,A2", TotalMoney=100, MovieShow = new MovieShow { Movie = new Movie { MovieNameEnglish = "Movie1" } } },
                new() { InvoiceId="I2", Status=InvoiceStatus.Completed, BookingDate=DateTime.Today, Seat="B1", TotalMoney=50, MovieShow = new MovieShow { Movie = new Movie { MovieNameEnglish = "Movie2" } } }
            };
            _invSvc.Setup(x => x.GetAll()).Returns(invoices);
            _seatSvc.Setup(x => x.GetAllSeatsAsync()).ReturnsAsync(new List<Seat> {
                new() { SeatId=1 }, new() { SeatId=2 }, new() { SeatId=3 }
            });
            _memberRepo.Setup(x => x.GetAll()).Returns(new List<Member> {
                new() { MemberId="M1", Account=new Account{FullName="X", RegisterDate = DateOnly.FromDateTime(DateTime.Today)} }
            });

            var ctrl = BuildController();

            // Act
            var result = ctrl.MainPage(tab: "Foo") as ViewResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Foo", result.ViewData["ActiveTab"]);
            var vm = Assert.IsType<AdminDashboardViewModel>(result.Model);
            Assert.Equal(150m, vm.RevenueToday);
            Assert.Equal(2, vm.BookingsToday);
            Assert.Equal(3, vm.TicketsSoldToday);  // A1,A2,B1
        }

        [Fact]
        public async void LoadTab_MemberMg_ReturnsMemberList()
        {
            // Arrange
            var members = new List<Member> {
                new() { MemberId="M1" }, new() { MemberId="M2" }
            };
            _memberRepo.Setup(r => r.GetAll()).Returns(members);

            var ctrl = BuildController();

            // Act
            var result = await ctrl.LoadTab("MemberMg", keyword: null) as PartialViewResult;

            // Assert
            Assert.Equal("MemberMg", result.ViewName);
            Assert.Same(members, result.Model);
        }

        [Fact]
        public async void LoadTab_MovieMg_WithKeyword_FiltersMovies()
        {
            // Arrange
            var movies = new List<Movie> {
                new(){ MovieNameEnglish="Alpha" },
                new(){ MovieNameEnglish="Gamma" }
            };
            _movieSvc.Setup(m => m.GetAll()).Returns(movies);

            var ctrl = BuildController();

            // Act
            var result = await ctrl.LoadTab("MovieMg", keyword: "Alpha") as PartialViewResult;

            // Assert
            var model = Assert.IsAssignableFrom<IEnumerable<Movie>>(result.Model);
            Assert.Equal(2, model.Count());
            Assert.Contains(model, m => m.MovieNameEnglish == "Alpha");
            Assert.Contains(model, m => m.MovieNameEnglish == "Gamma");
        }

        [Fact]
        public void CreateRank_Get_ReturnsEmptyViewModel()
        {
            // Arrange
            var ctrl = BuildController();

            // Act
            var result = ctrl.CreateRank() as ViewResult;

            // Assert
            Assert.Equal("~/Views/Rank/Create.cshtml", result.ViewName);
            Assert.IsType<RankCreateViewModel>(result.Model);
        }

        [Fact]
        public void CreateRank_Post_InvalidModel_ReturnsViewWithError()
        {
            // Arrange
            var ctrl = BuildController();
            ctrl.TempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>());
            ctrl.ModelState.AddModelError("CurrentRankName", "Required");
            var model = new RankCreateViewModel();

            // Act
            var result = ctrl.CreateRank(model) as ViewResult;

            // Assert
            Assert.Equal("~/Views/Rank/Create.cshtml", result.ViewName);
            Assert.Equal(model, result.Model);
            Assert.True(ctrl.TempData.ContainsKey("ErrorMessage"));
        }

        [Fact]
        public void CreateRank_Post_Success_RedirectsToRankMg()
        {
            // Arrange
            var model = new RankCreateViewModel { CurrentRankName = "R", RequiredPointsForCurrentRank = 0 };
            _rankSvc.Setup(r => r.Create(It.IsAny<RankInfoViewModel>())).Verifiable();

            var ctrl = BuildController();
            ctrl.TempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>());

            // Act
            var result = ctrl.CreateRank(model) as RedirectToActionResult;

            // Assert
            Assert.Equal("MainPage", result.ActionName);
            Assert.Equal("Admin", result.ControllerName);
            Assert.Equal("RankMg", result.RouteValues["tab"]);
            _rankSvc.Verify();
        }

        [Fact]
        public void EditRank_Get_NotFound_WhenMissing()
        {
            // Arrange
            _rankSvc.Setup(r => r.GetById(5)).Returns((RankInfoViewModel)null);
            var ctrl = BuildController();

            // Act
            var result = ctrl.EditRank(5);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public void EditRank_Get_ReturnsView_WhenFound()
        {
            // Arrange
            var info = new RankInfoViewModel
            {
                CurrentRankId = 7,
                CurrentRankName = "X",
                RequiredPointsForCurrentRank = 100,
                CurrentDiscountPercentage = 10,
                CurrentPointEarningPercentage = 5,
                ColorGradient = "g",
                IconClass = "i"
            };
            _rankSvc.Setup(r => r.GetById(7)).Returns(info);

            var ctrl = BuildController();

            // Act
            var result = ctrl.EditRank(7) as ViewResult;

            // Assert
            Assert.Equal("~/Views/Rank/Edit.cshtml", result.ViewName);
            Assert.Equal(info.CurrentRankName, ((RankCreateViewModel)result.Model).CurrentRankName);
            Assert.Equal(7, ctrl.ViewBag.RankId);
        }

        [Fact]
        public void DeleteRank_Post_Redirects_AfterDelete()
        {
            // Arrange
            var ctrl = BuildController();
            ctrl.TempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>());

            // Act
            var result = ctrl.DeleteRank(9) as RedirectToActionResult;

            // Assert
            _rankSvc.Verify(r => r.Delete(9), Times.Once);
            Assert.Equal("MainPage", result.ActionName);
            Assert.Equal("RankMg", result.RouteValues["tab"]);
        }
    }
}
