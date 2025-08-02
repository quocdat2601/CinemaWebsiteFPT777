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
using System.Security.Claims;

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
        private readonly Mock<IPersonRepository> _personRepo = new();
        private readonly Mock<IVoucherService> _vouchSvc = new();
        private readonly Mock<IRankService> _rankSvc = new();
        private readonly Mock<IVersionRepository> _versionRepo = new();
        private readonly Mock<IDashboardService> _dashboardSvc = new();

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
                _personRepo.Object,
                _vouchSvc.Object,
                _rankSvc.Object,
                _versionRepo.Object,
                _dashboardSvc.Object
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

            var dashboardModel = new AdminDashboardViewModel
            {
                RevenueToday = 150m,
                BookingsToday = 2,
                TicketsSoldToday = 3
            };
            _dashboardSvc.Setup(x => x.GetDashboardViewModel(It.IsAny<int>())).Returns(dashboardModel);

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

        [Fact]
        public async void LoadTab_EmployeeMg_NoKeyword_ReturnsAllEmployees()
        {
            // Arrange
            var employees = new List<Employee> { new Employee { Account = new Account { FullName = "John" } } };
            _empSvc.Setup(e => e.GetAll()).Returns(employees);
            var ctrl = BuildController();

            // Act
            var result = await ctrl.LoadTab("EmployeeMg") as PartialViewResult;

            // Assert
            Assert.Equal("EmployeeMg", result.ViewName);
            Assert.Same(employees, result.Model);
        }

        [Fact]
        public async void LoadTab_EmployeeMg_WithKeyword_FiltersEmployees()
        {
            // Arrange
            var employees = new List<Employee> {
                new Employee { Account = new Account { FullName = "John", Email = "john@example.com", IdentityCard = "123", PhoneNumber = "555", Address = "Addr" } },
                new Employee { Account = new Account { FullName = "Jane" } }
            };
            _empSvc.Setup(e => e.GetAll()).Returns(employees);
            var ctrl = BuildController();

            // Act
            var result = await ctrl.LoadTab("EmployeeMg", "john") as PartialViewResult;

            // Assert
            var model = Assert.IsAssignableFrom<IEnumerable<Employee>>(result.Model);
            Assert.Single(model);
            Assert.Equal("John", model.First().Account.FullName);
        }

        [Fact]
        public async void LoadTab_ShowroomMg_ReturnsCinemaSeatTypesVersions()
        {
            // Arrange
            var cinema = new List<CinemaRoom> { new CinemaRoom() };
            var versions = new List<MovieTheater.Models.Version> { new MovieTheater.Models.Version() };
            _cinemaSvc.Setup(c => c.GetAll()).Returns(cinema);
            _movieSvc.Setup(m => m.GetAllVersions()).Returns(versions);
            var ctrl = BuildController();

            // Act
            var result = await ctrl.LoadTab("ShowroomMg") as PartialViewResult;

            // Assert
            Assert.Equal("ShowroomMg", result.ViewName);
            Assert.Null(result.Model); // ShowroomMg doesn't return a model, it uses ViewBag
            Assert.Same(versions, ctrl.ViewBag.Versions);
            Assert.NotNull(ctrl.ViewBag.ActiveRooms);
            Assert.NotNull(ctrl.ViewBag.HiddenRooms);
        }

        [Fact]
        public async void LoadTab_PromotionMg_ReturnsPromotions()
        {
            // Arrange
            var promotions = new List<Promotion> { new Promotion() };
            _promoSvc.Setup(p => p.GetAll()).Returns(promotions);
            var ctrl = BuildController();

            // Act
            var result = await ctrl.LoadTab("PromotionMg") as PartialViewResult;

            // Assert
            Assert.Equal("PromotionMg", result.ViewName);
            Assert.Same(promotions, result.Model);
        }

        [Fact]
        public async void LoadTab_BookingMg_NoKeyword_ReturnsInvoices()
        {
            // Arrange
            var invoices = new List<Invoice> { new Invoice { InvoiceId = "I1" } };
            _invSvc.Setup(i => i.GetAll()).Returns(invoices);
            var ctrl = BuildController();
            
            // Set up HTTP context with empty query string
            var context = new DefaultHttpContext();
            context.Request.QueryString = new QueryString();
            ctrl.ControllerContext = new ControllerContext { HttpContext = context };

            // Act
            var result = await ctrl.LoadTab("BookingMg") as PartialViewResult;

            // Assert
            Assert.Equal("BookingMg", result.ViewName);
            Assert.Same(invoices, result.Model);
        }

        [Fact]
        public async void LoadTab_BookingMg_WithKeyword_FiltersInvoices()
        {
            // Arrange
            var invoices = new List<Invoice> {
                new Invoice { InvoiceId = "I1", AccountId = "A1", Account = new Account { PhoneNumber = "555", IdentityCard = "123" } },
                new Invoice { InvoiceId = "I2", AccountId = "A2", Account = new Account { PhoneNumber = "999", IdentityCard = "456" } }
            };
            _invSvc.Setup(i => i.GetAll()).Returns(invoices);
            var ctrl = BuildController();
            
            // Set up HTTP context with empty query string
            var context = new DefaultHttpContext();
            context.Request.QueryString = new QueryString();
            ctrl.ControllerContext = new ControllerContext { HttpContext = context };

            // Act
            var result = await ctrl.LoadTab("BookingMg", "555") as PartialViewResult;

            // Assert
            var model = Assert.IsAssignableFrom<IEnumerable<Invoice>>(result.Model);
            Assert.Single(model);
            Assert.Equal("I1", model.First().InvoiceId);
        }

        [Fact]
        public async void LoadTab_FoodMg_ReturnsFoods()
        {
            // Arrange
            var foods = new List<FoodViewModel> { new FoodViewModel() };
            var foodList = new MovieTheater.ViewModels.FoodListViewModel { Foods = foods };
            _foodSvc.Setup(f => f.GetAllAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool?>())).ReturnsAsync(foodList);
            var ctrl = BuildController();
            var context = new DefaultHttpContext();
            context.Request.QueryString = new QueryString("?categoryFilter=&statusFilter=");
            ctrl.ControllerContext = new ControllerContext { HttpContext = context };

            // Act
            var result = await ctrl.LoadTab("FoodMg") as PartialViewResult;

            // Assert
            Assert.Equal("FoodMg", result.ViewName);
            Assert.Same(foods, ((MovieTheater.ViewModels.FoodListViewModel)result.Model).Foods);
        }

        [Fact]
        public async void LoadTab_VoucherMg_ReturnsFilteredVouchers()
        {
            // Arrange
            var vouchers = new List<Voucher> { new Voucher() };
            _vouchSvc.Setup(v => v.GetFilteredVouchers(It.IsAny<MovieTheater.Service.VoucherFilterModel>())).Returns(vouchers);
            var ctrl = BuildController();
            var context = new DefaultHttpContext();
            context.Request.QueryString = new QueryString("?keyword=&statusFilter=&expiryFilter=");
            ctrl.ControllerContext = new ControllerContext { HttpContext = context };

            // Act
            var result = await ctrl.LoadTab("VoucherMg") as PartialViewResult;

            // Assert
            Assert.Equal("VoucherMg", result.ViewName);
            Assert.Equal(vouchers.Count, ((IEnumerable<Voucher>)result.Model).Count());
        }

        [Fact]
        public async void LoadTab_RankMg_ReturnsRanks()
        {
            // Arrange
            var ranks = new List<RankInfoViewModel> { new RankInfoViewModel() };
            _rankSvc.Setup(r => r.GetAllRanks()).Returns(ranks);
            var ctrl = BuildController();

            // Act
            var result = await ctrl.LoadTab("RankMg") as PartialViewResult;

            // Assert
            Assert.Equal("RankMg", result.ViewName);
            Assert.Same(ranks, result.Model);
        }

        [Fact]
        public async void LoadTab_Default_ReturnsTabNotFound()
        {
            // Arrange
            var ctrl = BuildController();

            // Act
            var result = await ctrl.LoadTab("NonExistentTab") as ContentResult;

            // Assert
            Assert.Equal("Tab not found.", result.Content);
        }

        [Fact]
        public void Create_Get_ReturnsView()
        {
            // Arrange
            var ctrl = BuildController();

            // Act
            var result = ctrl.Create() as ViewResult;

            // Assert
            Assert.NotNull(result);
        }

        [Fact]
        public void Create_Post_RedirectsOnSuccess()
        {
            // Arrange
            var ctrl = BuildController();
            var form = new FormCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>());

            // Act
            var result = ctrl.Create(form) as RedirectToActionResult;

            // Assert
            Assert.Equal("Index", result.ActionName);
        }

        [Fact]
        public void Create_Post_ReturnsViewOnException()
        {
            // Arrange
            var ctrl = BuildController();
            var form = new FormCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>());
            // Simulate exception by throwing in RedirectToAction
            var controller = new TestableAdminController(_movieSvc.Object, _empSvc.Object, _promoSvc.Object, _cinemaSvc.Object, _seatType.Object, _memberRepo.Object, _acctSvc.Object, _seatSvc.Object, _invSvc.Object, _foodSvc.Object, _personRepo.Object, _vouchSvc.Object, _rankSvc.Object, _versionRepo.Object, _dashboardSvc.Object);
            controller.ThrowOnRedirect = true;

            // Act
            var result = controller.Create(form);

            // Assert
            Assert.IsType<ViewResult>(result);
        }

        class TestableAdminController : AdminController
        {
            public bool ThrowOnRedirect { get; set; }
            public TestableAdminController(
                IMovieService movieService, IEmployeeService employeeService, IPromotionService promotionService, ICinemaService cinemaService, ISeatTypeService seatTypeService, IMemberRepository memberRepository, IAccountService accountService, ISeatService seatService, IInvoiceService invoiceService, IFoodService foodService, IPersonRepository personRepository, IVoucherService voucherService, IRankService rankService, IVersionRepository versionRepository, IDashboardService dashboardService
            ) : base(movieService, employeeService, promotionService, cinemaService, seatTypeService, memberRepository, accountService, seatService, invoiceService, foodService, personRepository, voucherService, rankService, versionRepository, dashboardService) { }
            public override RedirectToActionResult RedirectToAction(string actionName)
            {
                if (ThrowOnRedirect) throw new Exception();
                return base.RedirectToAction(actionName);
            }
        }

        [Fact]
        public void Delete_Get_ReturnsView()
        {
            // Arrange
            var ctrl = BuildController();

            // Act
            var result = ctrl.Delete(1) as ViewResult;

            // Assert
            Assert.NotNull(result);
        }

        [Fact]
        public void Delete_Post_RedirectsOnSuccess()
        {
            // Arrange
            var ctrl = BuildController();
            var form = new FormCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>());

            // Act
            var result = ctrl.Delete(1, form) as RedirectToActionResult;

            // Assert
            Assert.Equal("Index", result.ActionName);
        }

        [Fact]
        public void Delete_Post_ReturnsViewOnException()
        {
            // Arrange
            var ctrl = BuildController();
            var form = new FormCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>());
            var controller = new TestableAdminController(_movieSvc.Object, _empSvc.Object, _promoSvc.Object, _cinemaSvc.Object, _seatType.Object, _memberRepo.Object, _acctSvc.Object, _seatSvc.Object, _invSvc.Object, _foodSvc.Object, _personRepo.Object, _vouchSvc.Object, _rankSvc.Object, _versionRepo.Object, _dashboardSvc.Object);
            controller.ThrowOnRedirect = true;

            // Act
            var result = controller.Delete(1, form);

            // Assert
            Assert.IsType<ViewResult>(result);
        }

        [Fact]
        public void Edit_Get_ReturnsNotFound_WhenAccountNull()
        {
            // Arrange
            _acctSvc.Setup(a => a.GetById(It.IsAny<string>())).Returns((Account)null);
            var ctrl = BuildController();

            // Act
            var result = ctrl.Edit("id");

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public void Edit_Get_ReturnsView_WhenAccountFound()
        {
            // Arrange
            var acc = new Account { AccountId = "id", Username = "u", FullName = "f", DateOfBirth = DateOnly.MinValue, Gender = "g", IdentityCard = "ic", Email = "e", Address = "a", PhoneNumber = "p", Image = "img" };
            _acctSvc.Setup(a => a.GetById("id")).Returns(acc);
            var ctrl = BuildController();

            // Act
            var result = ctrl.Edit("id") as ViewResult;

            // Assert
            Assert.NotNull(result);
            var model = Assert.IsType<RegisterViewModel>(result.Model);
            Assert.Equal("id", model.AccountId);
        }

        [Fact]
        public async void Edit_Post_BadRequest_WhenIdMismatch()
        {
            // Arrange
            var ctrl = BuildController();
            var model = new RegisterViewModel { AccountId = "other" };

            // Act
            var result = await ctrl.Edit("id", model);

            // Assert
            Assert.IsType<BadRequestResult>(result);
        }

        [Fact]
        public async void Edit_Post_ReturnsView_WhenModelStateInvalid()
        {
            // Arrange
            var ctrl = BuildController();
            ctrl.ModelState.AddModelError("x", "y");
            var model = new RegisterViewModel { AccountId = "id" };

            // Act
            var result = await ctrl.Edit("id", model) as ViewResult;

            // Assert
            Assert.Equal("EditMember", result.ViewName);
            Assert.Equal(model, result.Model);
        }

        [Fact]
        public async void Edit_Post_RedirectsOnSuccess()
        {
            // Arrange
            var ctrl = BuildController();
            ctrl.TempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>());
            
            // Mock User claims for Admin role
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Role, "Admin")
            };
            var identity = new ClaimsIdentity(claims, "Test");
            var principal = new ClaimsPrincipal(identity);
            ctrl.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = principal
                }
            };
            
            var model = new RegisterViewModel { AccountId = "id" };
            _acctSvc.Setup(a => a.Update("id", model)).Returns(true);

            // Act
            var result = await ctrl.Edit("id", model) as RedirectToActionResult;

            // Assert
            Assert.Equal("MainPage", result.ActionName);
            Assert.Equal("Admin", result.ControllerName);
            Assert.Equal("MemberMg", result.RouteValues["tab"]);
        }

        [Fact]
        public async void Edit_Post_ReturnsView_WhenUpdateFails()
        {
            // Arrange
            var ctrl = BuildController();
            var model = new RegisterViewModel { AccountId = "id" };
            _acctSvc.Setup(a => a.Update("id", model)).Returns(false);

            // Act
            var result = await ctrl.Edit("id", model) as ViewResult;

            // Assert
            Assert.Equal("EditMember", result.ViewName);
            Assert.Equal(model, result.Model);
        }

        [Fact]
        public async void Edit_Post_ReturnsView_OnException()
        {
            // Arrange
            var ctrl = BuildController();
            var model = new RegisterViewModel { AccountId = "id" };
            var controller = new TestableAdminController(_movieSvc.Object, _empSvc.Object, _promoSvc.Object, _cinemaSvc.Object, _seatType.Object, _memberRepo.Object, _acctSvc.Object, _seatSvc.Object, _invSvc.Object, _foodSvc.Object, _personRepo.Object, _vouchSvc.Object, _rankSvc.Object, _versionRepo.Object, _dashboardSvc.Object);
            controller.ThrowOnRedirect = true;

            // Act
            var result = await controller.Edit("id", model);

            // Assert
            Assert.IsType<ViewResult>(result);
        }
    }
}
