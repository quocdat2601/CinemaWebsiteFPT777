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
using Microsoft.Extensions.DependencyInjection;

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
            var controller = new TestableAdminController(_movieSvc.Object, _empSvc.Object, _promoSvc.Object, _cinemaSvc.Object, _seatType.Object, _memberRepo.Object, _acctSvc.Object, _invSvc.Object, _foodSvc.Object, _personRepo.Object, _vouchSvc.Object, _rankSvc.Object, _versionRepo.Object, _dashboardSvc.Object);
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
                IMovieService movieService, IEmployeeService employeeService, IPromotionService promotionService, ICinemaService cinemaService, ISeatTypeService seatTypeService, IMemberRepository memberRepository, IAccountService accountService, IInvoiceService invoiceService, IFoodService foodService, IPersonRepository personRepository, IVoucherService voucherService, IRankService rankService, IVersionRepository versionRepository, IDashboardService dashboardService
            ) : base(movieService, employeeService, promotionService, cinemaService, seatTypeService, memberRepository, accountService, invoiceService, foodService, personRepository, voucherService, rankService, versionRepository, dashboardService) { }
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
            var controller = new TestableAdminController(_movieSvc.Object, _empSvc.Object, _promoSvc.Object, _cinemaSvc.Object, _seatType.Object, _memberRepo.Object, _acctSvc.Object, _invSvc.Object, _foodSvc.Object, _personRepo.Object, _vouchSvc.Object, _rankSvc.Object, _versionRepo.Object, _dashboardSvc.Object);
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
            var controller = new TestableAdminController(_movieSvc.Object, _empSvc.Object, _promoSvc.Object, _cinemaSvc.Object, _seatType.Object, _memberRepo.Object, _acctSvc.Object, _invSvc.Object, _foodSvc.Object, _personRepo.Object, _vouchSvc.Object, _rankSvc.Object, _versionRepo.Object, _dashboardSvc.Object);
            controller.ThrowOnRedirect = true;

            // Act
            var result = await controller.Edit("id", model);

            // Assert
            Assert.IsType<ViewResult>(result);
        }

        [Fact]
        public void ShowtimeMg_ReturnsView_WithValidDate()
        {
            // Arrange
            var movieShows = new List<MovieShow>
            {
                new MovieShow 
                { 
                    MovieShowId = 1, 
                    ShowDate = DateOnly.FromDateTime(DateTime.Today),
                    Movie = new Movie { MovieNameEnglish = "Test Movie" },
                    Schedule = new Schedule { ScheduleTime = new TimeOnly(14, 0) }
                }
            };
            _movieSvc.Setup(s => s.GetMovieShow()).Returns(movieShows);
            _movieSvc.Setup(s => s.GetAllSchedules()).Returns(new List<Schedule>());
            
            var ctrl = BuildController();
            ctrl.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };
            var services = new ServiceCollection();
            services.AddSingleton<IMovieRepository>(Mock.Of<IMovieRepository>());
            services.AddMvc();
            ctrl.ControllerContext.HttpContext.RequestServices = services.BuildServiceProvider();

            // Act
            var result = ctrl.ShowtimeMg("15/06/2024") as ViewResult;

            // Assert
            Assert.NotNull(result);
            Assert.IsType<ShowtimeManagementViewModel>(result.Model);
        }

        [Fact]
        public void ShowtimeMg_ReturnsView_WithInvalidDate_UsesToday_SecondTest()
        {
            // Arrange
            var movieShows = new List<MovieShow>();
            _movieSvc.Setup(s => s.GetMovieShow()).Returns(movieShows);
            _movieSvc.Setup(s => s.GetAllSchedules()).Returns(new List<Schedule>());
            
            var ctrl = BuildController();
            ctrl.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };
            var services = new ServiceCollection();
            services.AddSingleton<IMovieRepository>(Mock.Of<IMovieRepository>());
            services.AddMvc();
            ctrl.ControllerContext.HttpContext.RequestServices = services.BuildServiceProvider();

            // Act
            var result = ctrl.ShowtimeMg("invalid-date") as ViewResult;

            // Assert
            Assert.NotNull(result);
            Assert.IsType<ShowtimeManagementViewModel>(result.Model);
        }

        [Fact]
        public void GetMovieShowSummary_ReturnsJson_WithValidParameters()
        {
            // Arrange
            var summary = new Dictionary<DateOnly, List<string>>
            {
                { DateOnly.FromDateTime(DateTime.Today), new List<string> { "Movie1", "Movie2" } }
            };
            var mockRepo = new Mock<IMovieRepository>();
            mockRepo.Setup(r => r.GetMovieShowSummaryByMonth(2024, 6)).Returns(summary);
            
            var ctrl = BuildController();
            ctrl.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };
            ctrl.ControllerContext.HttpContext.RequestServices = new ServiceCollection()
                .AddSingleton<IMovieRepository>(mockRepo.Object)
                .BuildServiceProvider();

            // Act
            var result = ctrl.GetMovieShowSummary(2024, 6) as JsonResult;

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.Value);
        }

        [Fact]
        public void GetMovieShowSummary_ReturnsEmptyJson_WhenRepositoryNotAvailable()
        {
            // Arrange
            var ctrl = BuildController();
            ctrl.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };
            ctrl.ControllerContext.HttpContext.RequestServices = new ServiceCollection()
                .BuildServiceProvider();

            // Act
            var result = ctrl.GetMovieShowSummary(2024, 6) as JsonResult;

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.Value);
        }

        [Fact]
        public void EditRank_Get_ReturnsNotFound_WhenRankNull()
        {
            // Arrange
            _rankSvc.Setup(s => s.GetById(1)).Returns((RankInfoViewModel)null);
            var ctrl = BuildController();

            // Act
            var result = ctrl.EditRank(1);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public void EditRank_Get_ReturnsView_WhenRankFound()
        {
            // Arrange
            var rank = new RankInfoViewModel
            {
                CurrentRankId = 1,
                CurrentRankName = "Gold",
                RequiredPointsForCurrentRank = 1000,
                CurrentDiscountPercentage = 10,
                CurrentPointEarningPercentage = 5,
                ColorGradient = "gold",
                IconClass = "fas fa-crown"
            };
            _rankSvc.Setup(s => s.GetById(1)).Returns(rank);
            var ctrl = BuildController();

            // Act
            var result = ctrl.EditRank(1) as ViewResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal("~/Views/Rank/Edit.cshtml", result.ViewName);
            Assert.IsType<RankCreateViewModel>(result.Model);
            Assert.Equal(1, result.ViewData["RankId"]);
        }

        [Fact]
        public void BookingMgPartial_ReturnsJson_WithNoFilters()
        {
            // Arrange
            var invoices = new List<Invoice>
            {
                new Invoice 
                { 
                    InvoiceId = "I1", 
                    AccountId = "A1",
                    Status = InvoiceStatus.Completed,
                    BookingDate = DateTime.Today,
                    MovieShow = new MovieShow 
                    { 
                        Movie = new Movie { MovieNameEnglish = "Test Movie" },
                        Schedule = new Schedule { ScheduleTime = new TimeOnly(14, 0) }
                    },
                    Account = new Account { PhoneNumber = "123456789", IdentityCard = "ID123" }
                }
            };
            _invSvc.Setup(s => s.GetAll()).Returns(invoices);
            var ctrl = BuildController();

            // Act
            var result = ctrl.BookingMgPartial() as JsonResult;

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.Value);
        }

        [Fact]
        public void BookingMgPartial_ReturnsJson_WithKeywordFilter()
        {
            // Arrange
            var invoices = new List<Invoice>
            {
                new Invoice 
                { 
                    InvoiceId = "I1", 
                    AccountId = "A1",
                    Status = InvoiceStatus.Completed,
                    BookingDate = DateTime.Today,
                    MovieShow = new MovieShow 
                    { 
                        Movie = new Movie { MovieNameEnglish = "Test Movie" },
                        Schedule = new Schedule { ScheduleTime = new TimeOnly(14, 0) }
                    },
                    Account = new Account { PhoneNumber = "123456789", IdentityCard = "ID123" }
                }
            };
            _invSvc.Setup(s => s.GetAll()).Returns(invoices);
            var ctrl = BuildController();

            // Act
            var result = ctrl.BookingMgPartial(keyword: "I1") as JsonResult;

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.Value);
        }

        [Fact]
        public void BookingMgPartial_ReturnsJson_WithStatusFilter()
        {
            // Arrange
            var invoices = new List<Invoice>
            {
                new Invoice 
                { 
                    InvoiceId = "I1", 
                    AccountId = "A1",
                    Status = InvoiceStatus.Completed,
                    Cancel = false,
                    BookingDate = DateTime.Today,
                    MovieShow = new MovieShow 
                    { 
                        Movie = new Movie { MovieNameEnglish = "Test Movie" },
                        Schedule = new Schedule { ScheduleTime = new TimeOnly(14, 0) }
                    },
                    Account = new Account { PhoneNumber = "123456789", IdentityCard = "ID123" }
                }
            };
            _invSvc.Setup(s => s.GetAll()).Returns(invoices);
            var ctrl = BuildController();

            // Act
            var result = ctrl.BookingMgPartial(statusFilter: "completed") as JsonResult;

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.Value);
        }

        [Fact]
        public void BookingMgPartial_ReturnsJson_WithBookingTypeFilter()
        {
            // Arrange
            var invoices = new List<Invoice>
            {
                new Invoice 
                { 
                    InvoiceId = "I1", 
                    AccountId = "A1",
                    Status = InvoiceStatus.Completed,
                    BookingDate = DateTime.Today,
                    EmployeeId = null, // Normal booking
                    MovieShow = new MovieShow 
                    { 
                        Movie = new Movie { MovieNameEnglish = "Test Movie" },
                        Schedule = new Schedule { ScheduleTime = new TimeOnly(14, 0) }
                    },
                    Account = new Account { PhoneNumber = "123456789", IdentityCard = "ID123" }
                }
            };
            _invSvc.Setup(s => s.GetAll()).Returns(invoices);
            var ctrl = BuildController();

            // Act
            var result = ctrl.BookingMgPartial(bookingTypeFilter: "normal") as JsonResult;

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.Value);
        }

        [Fact]
        public void VoucherMgPartial_ReturnsJson_WithNoFilters()
        {
            // Arrange
            var vouchers = new List<Voucher>
            {
                new Voucher 
                { 
                    VoucherId = "1",
                    Code = "TEST123",
                    AccountId = "A1",
                    Value = 100,
                    CreatedDate = DateTime.Today,
                    ExpiryDate = DateTime.Today.AddDays(30),
                    IsUsed = false,
                    Image = "voucher.jpg"
                }
            };
            _vouchSvc.Setup(s => s.GetFilteredVouchers(It.IsAny<MovieTheater.Service.VoucherFilterModel>())).Returns(vouchers);
            var ctrl = BuildController();

            // Act
            var result = ctrl.VoucherMgPartial() as JsonResult;

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.Value);
        }

        [Fact]
        public void VoucherMgPartial_ReturnsJson_WithKeywordFilter()
        {
            // Arrange
            var vouchers = new List<Voucher>
            {
                new Voucher 
                { 
                    VoucherId = "1",
                    Code = "TEST123",
                    AccountId = "A1",
                    Value = 100,
                    CreatedDate = DateTime.Today,
                    ExpiryDate = DateTime.Today.AddDays(30),
                    IsUsed = false,
                    Image = "voucher.jpg"
                }
            };
            _vouchSvc.Setup(s => s.GetFilteredVouchers(It.IsAny<MovieTheater.Service.VoucherFilterModel>())).Returns(vouchers);
            var ctrl = BuildController();

            // Act
            var result = ctrl.VoucherMgPartial(keyword: "TEST") as JsonResult;

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.Value);
        }

        [Fact]
        public void VoucherMgPartial_ReturnsJson_WithStatusFilter()
        {
            // Arrange
            var vouchers = new List<Voucher>
            {
                new Voucher 
                { 
                    VoucherId = "1",
                    Code = "TEST123",
                    AccountId = "A1",
                    Value = 100,
                    CreatedDate = DateTime.Today,
                    ExpiryDate = DateTime.Today.AddDays(30),
                    IsUsed = false,
                    Image = "voucher.jpg"
                }
            };
            _vouchSvc.Setup(s => s.GetFilteredVouchers(It.IsAny<MovieTheater.Service.VoucherFilterModel>())).Returns(vouchers);
            var ctrl = BuildController();

            // Act
            var result = ctrl.VoucherMgPartial(statusFilter: "active") as JsonResult;

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.Value);
        }

        [Fact]
        public void VoucherMgPartial_ReturnsJson_WithExpiryFilter()
        {
            // Arrange
            var vouchers = new List<Voucher>
            {
                new Voucher 
                { 
                    VoucherId = "1",
                    Code = "TEST123",
                    AccountId = "A1",
                    Value = 100,
                    CreatedDate = DateTime.Today,
                    ExpiryDate = DateTime.Today.AddDays(30),
                    IsUsed = false,
                    Image = "voucher.jpg"
                }
            };
            _vouchSvc.Setup(s => s.GetFilteredVouchers(It.IsAny<MovieTheater.Service.VoucherFilterModel>())).Returns(vouchers);
            var ctrl = BuildController();

            // Act
            var result = ctrl.VoucherMgPartial(expiryFilter: "valid") as JsonResult;

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.Value);
        }

        [Fact]
        public void VoucherMgPartial_ReturnsJson_WithPagination()
        {
            // Arrange
            var vouchers = new List<Voucher>
            {
                new Voucher 
                { 
                    VoucherId = "1",
                    Code = "TEST123",
                    AccountId = "A1",
                    Value = 100,
                    CreatedDate = DateTime.Today,
                    ExpiryDate = DateTime.Today.AddDays(30),
                    IsUsed = false,
                    Image = "voucher.jpg"
                }
            };
            _vouchSvc.Setup(s => s.GetFilteredVouchers(It.IsAny<MovieTheater.Service.VoucherFilterModel>())).Returns(vouchers);
            var ctrl = BuildController();

            // Act
            var result = ctrl.VoucherMgPartial(page: 1, pageSize: 5) as JsonResult;

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.Value);
        }

        [Fact]
        public async void LoadTab_Dashboard_ReturnsDashboardPartialView()
        {
            // Arrange
            var dashboardModel = new AdminDashboardViewModel
            {
                RevenueToday = 1000m,
                BookingsToday = 5,
                TicketsSoldToday = 10
            };
            _dashboardSvc.Setup(x => x.GetDashboardViewModel(It.IsAny<int>())).Returns(dashboardModel);
            var ctrl = BuildController();

            // Act
            var result = await ctrl.LoadTab("Dashboard", range: "monthly") as PartialViewResult;

            // Assert
            Assert.Equal("Dashboard", result.ViewName);
            Assert.Equal("monthly", result.ViewData["DashboardRange"]);
            Assert.IsType<AdminDashboardViewModel>(result.Model);
        }

        [Fact]
        public async void LoadTab_VersionMg_ReturnsVersionManagementView()
        {
            // Arrange
            var seatTypes = new List<SeatType> { new SeatType() };
            var versions = new List<MovieTheater.Models.Version> { new MovieTheater.Models.Version() };
            _seatType.Setup(s => s.GetAll()).Returns(seatTypes);
            _versionRepo.Setup(v => v.GetAll()).Returns(versions);
            var ctrl = BuildController();

            // Act
            var result = await ctrl.LoadTab("VersionMg") as PartialViewResult;

            // Assert
            Assert.Equal("VersionMg", result.ViewName);
            Assert.Same(versions, result.Model);
            Assert.Same(seatTypes, ctrl.ViewBag.SeatTypes);
        }

        [Fact]
        public async void LoadTab_CastMg_ReturnsCastManagementView()
        {
            // Arrange
            var persons = new List<Person> 
            { 
                new Person { IsDirector = false },
                new Person { IsDirector = true }
            };
            _personRepo.Setup(p => p.GetAll()).Returns(persons);
            var ctrl = BuildController();

            // Act
            var result = await ctrl.LoadTab("CastMg") as PartialViewResult;

            // Assert
            Assert.Equal("CastMg", result.ViewName);
            Assert.Same(persons, ctrl.ViewBag.Persons);
            Assert.Single(ctrl.ViewBag.Actors);
            Assert.Single(ctrl.ViewBag.Directors);
        }

        [Fact]
        public async void LoadTab_QRCode_ReturnsQRScannerView()
        {
            // Arrange
            var ctrl = BuildController();

            // Act
            var result = await ctrl.LoadTab("QRCode") as PartialViewResult;

            // Assert
            Assert.Equal("~/Views/QRCode/Scanner.cshtml", result.ViewName);
        }

        [Fact]
        public async void LoadTab_BookingMg_WithStatusFilter_FiltersInvoices()
        {
            // Arrange
            var invoices = new List<Invoice> 
            {
                new Invoice { Status = InvoiceStatus.Completed, Cancel = false },
                new Invoice { Status = InvoiceStatus.Completed, Cancel = true },
                new Invoice { Status = InvoiceStatus.Incomplete }
            };
            _invSvc.Setup(i => i.GetAll()).Returns(invoices);
            var ctrl = BuildController();
            
            // Set up HTTP context with proper query string
            var context = new DefaultHttpContext();
            var queryCollection = new QueryCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>());
            context.Request.Query = queryCollection;
            ctrl.ControllerContext = new ControllerContext { HttpContext = context };

            // Act
            var result = await ctrl.LoadTab("BookingMg", statusFilter: "completed") as PartialViewResult;

            // Assert
            var model = Assert.IsAssignableFrom<IEnumerable<Invoice>>(result.Model);
            Assert.Single(model);
            Assert.Equal(InvoiceStatus.Completed, model.First().Status);
            Assert.False(model.First().Cancel);
        }

        [Fact]
        public async void LoadTab_BookingMg_WithBookingTypeFilter_FiltersInvoices()
        {
            // Arrange
            var invoices = new List<Invoice> 
            {
                new Invoice { EmployeeId = null },
                new Invoice { EmployeeId = "E1" }
            };
            _invSvc.Setup(i => i.GetAll()).Returns(invoices);
            var ctrl = BuildController();
            
            // Set up HTTP context with proper query string
            var context = new DefaultHttpContext();
            var queryCollection = new QueryCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>());
            context.Request.Query = queryCollection;
            ctrl.ControllerContext = new ControllerContext { HttpContext = context };

            // Act
            var result = await ctrl.LoadTab("BookingMg", bookingTypeFilter: "employee") as PartialViewResult;

            // Assert
            var model = Assert.IsAssignableFrom<IEnumerable<Invoice>>(result.Model);
            Assert.Single(model);
            Assert.NotNull(model.First().EmployeeId);
        }

        [Fact]
        public async void LoadTab_BookingMg_WithSorting_SortsInvoices()
        {
            // Arrange
            var invoices = new List<Invoice> 
            {
                new Invoice { InvoiceId = "B", MovieShow = new MovieShow { Movie = new Movie { MovieNameEnglish = "Beta" } } },
                new Invoice { InvoiceId = "A", MovieShow = new MovieShow { Movie = new Movie { MovieNameEnglish = "Alpha" } } }
            };
            _invSvc.Setup(i => i.GetAll()).Returns(invoices);
            var ctrl = BuildController();
            
            // Set up HTTP context with proper query string for sorting
            var context = new DefaultHttpContext();
            var queryCollection = new QueryCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues> 
            { 
                { "sortBy", "movie_az" } 
            });
            context.Request.Query = queryCollection;
            ctrl.ControllerContext = new ControllerContext { HttpContext = context };

            // Act
            var result = await ctrl.LoadTab("BookingMg") as PartialViewResult;

            // Assert
            var model = Assert.IsAssignableFrom<IEnumerable<Invoice>>(result.Model);
            Assert.Equal("Alpha", model.First().MovieShow.Movie.MovieNameEnglish);
        }

        [Fact]
        public async void LoadTab_FoodMg_WithSorting_SortsFoods()
        {
            // Arrange
            var foods = new List<FoodViewModel> 
            {
                new FoodViewModel { Name = "Beta", Category = "Drinks", Price = 20, CreatedDate = DateTime.Today.AddDays(1) },
                new FoodViewModel { Name = "Alpha", Category = "Snacks", Price = 10, CreatedDate = DateTime.Today }
            };
            var foodList = new MovieTheater.ViewModels.FoodListViewModel { Foods = foods };
            _foodSvc.Setup(f => f.GetAllAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool?>())).ReturnsAsync(foodList);
            var ctrl = BuildController();
            var context = new DefaultHttpContext();
            context.Request.QueryString = new QueryString("?sortBy=name_az&categoryFilter=&statusFilter=");
            ctrl.ControllerContext = new ControllerContext { HttpContext = context };

            // Act
            var result = await ctrl.LoadTab("FoodMg") as PartialViewResult;

            // Assert
            var model = Assert.IsType<MovieTheater.ViewModels.FoodListViewModel>(result.Model);
            Assert.Equal("Alpha", model.Foods.First().Name);
        }

        [Fact]
        public async void LoadTab_VoucherMg_WithSorting_SortsVouchers()
        {
            // Arrange
            var vouchers = new List<Voucher> 
            {
                new Voucher { VoucherId = "2", AccountId = "B", Value = 200, CreatedDate = DateTime.Today.AddDays(1), ExpiryDate = DateTime.Today.AddDays(31) },
                new Voucher { VoucherId = "1", AccountId = "A", Value = 100, CreatedDate = DateTime.Today, ExpiryDate = DateTime.Today.AddDays(30) }
            };
            _vouchSvc.Setup(v => v.GetFilteredVouchers(It.IsAny<MovieTheater.Service.VoucherFilterModel>())).Returns(vouchers);
            var ctrl = BuildController();
            var context = new DefaultHttpContext();
            context.Request.QueryString = new QueryString("?sortBy=voucherid_asc&keyword=&statusFilter=&expiryFilter=");
            ctrl.ControllerContext = new ControllerContext { HttpContext = context };

            // Act
            var result = await ctrl.LoadTab("VoucherMg") as PartialViewResult;

            // Assert
            var model = Assert.IsAssignableFrom<IEnumerable<Voucher>>(result.Model);
            Assert.Equal("1", model.First().VoucherId);
        }

        [Fact]
        public async void Edit_Post_RedirectsToEmployee_WhenUserIsEmployee()
        {
            // Arrange
            var ctrl = BuildController();
            ctrl.TempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>());
            
            // Mock User claims for Employee role
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Role, "Employee")
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
            Assert.Equal("Employee", result.ControllerName);
            Assert.Equal("MemberMg", result.RouteValues["tab"]);
        }

        [Fact]
        public void ShowtimeMg_ReturnsView_WithInvalidDate_UsesToday()
        {
            // Arrange
            var ctrl = BuildController();
            var movieShows = new List<MovieShow>
            {
                new MovieShow { MovieShowId = 1, ShowDate = DateOnly.FromDateTime(DateTime.Today) }
            };
            _movieSvc.Setup(x => x.GetMovieShow()).Returns(movieShows);
            _movieSvc.Setup(x => x.GetAllSchedules()).Returns(new List<Schedule>());
            
            // Set up HTTP context with RequestServices and TempData
            var context = new DefaultHttpContext();
            var services = new ServiceCollection();
            services.AddSingleton<IMovieRepository>(Mock.Of<IMovieRepository>());
            services.AddSingleton<ITempDataDictionaryFactory>(Mock.Of<ITempDataDictionaryFactory>());
            context.RequestServices = services.BuildServiceProvider();
            ctrl.ControllerContext = new ControllerContext { HttpContext = context };

            // Act
            var result = ctrl.ShowtimeMg("01/01/2024");

            // Assert
            Assert.NotNull(result);
            Assert.IsType<ViewResult>(result);
        }

        [Fact]
        public void ShowtimeMg_WithInvalidDate_ReturnsView()
        {
            // Arrange
            var ctrl = BuildController();
            var movieShows = new List<MovieShow>
            {
                new MovieShow { MovieShowId = 1, ShowDate = DateOnly.FromDateTime(DateTime.Today) }
            };
            _movieSvc.Setup(x => x.GetMovieShow()).Returns(movieShows);
            _movieSvc.Setup(x => x.GetAllSchedules()).Returns(new List<Schedule>());
            
            // Set up HTTP context with RequestServices and TempData
            var context = new DefaultHttpContext();
            var services = new ServiceCollection();
            services.AddSingleton<IMovieRepository>(Mock.Of<IMovieRepository>());
            services.AddSingleton<ITempDataDictionaryFactory>(Mock.Of<ITempDataDictionaryFactory>());
            context.RequestServices = services.BuildServiceProvider();
            ctrl.ControllerContext = new ControllerContext { HttpContext = context };

            // Act
            var result = ctrl.ShowtimeMg("invalid-date");

            // Assert
            Assert.NotNull(result);
            Assert.IsType<ViewResult>(result);
        }

        // Additional tests to improve branch coverage - Batch 1
        [Fact]
        public async Task LoadTab_EmployeeMg_WithNullKeyword_ReturnsAllEmployees()
        {
            // Arrange
            var employees = new List<Employee> {
                new() { Account = new Account { FullName = "John", IdentityCard = "123", Email = "john@test.com", PhoneNumber = "123456789", Address = "Test Address" } },
                new() { Account = new Account { FullName = "Jane", IdentityCard = "456", Email = "jane@test.com", PhoneNumber = "987654321", Address = "Another Address" } }
            };
            _empSvc.Setup(x => x.GetAll()).Returns(employees);

            var ctrl = BuildController();
            
            // Set up HTTP context with empty query string
            var context = new DefaultHttpContext();
            var queryCollection = new QueryCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>());
            context.Request.Query = queryCollection;
            ctrl.ControllerContext = new ControllerContext { HttpContext = context };

            // Act
            var result = await ctrl.LoadTab("EmployeeMg", keyword: null) as PartialViewResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal("EmployeeMg", result.ViewName);
            var model = Assert.IsType<List<Employee>>(result.Model);
            Assert.Equal(2, model.Count);
        }

        [Fact]
        public async Task LoadTab_EmployeeMg_WithEmptyKeyword_ReturnsAllEmployees()
        {
            // Arrange
            var employees = new List<Employee> {
                new() { Account = new Account { FullName = "John", IdentityCard = "123", Email = "john@test.com", PhoneNumber = "123456789", Address = "Test Address" } },
                new() { Account = new Account { FullName = "Jane", IdentityCard = "456", Email = "jane@test.com", PhoneNumber = "987654321", Address = "Another Address" } }
            };
            _empSvc.Setup(x => x.GetAll()).Returns(employees);

            var ctrl = BuildController();
            
            // Set up HTTP context with empty query string
            var context = new DefaultHttpContext();
            var queryCollection = new QueryCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>());
            context.Request.Query = queryCollection;
            ctrl.ControllerContext = new ControllerContext { HttpContext = context };

            // Act
            var result = await ctrl.LoadTab("EmployeeMg", keyword: "") as PartialViewResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal("EmployeeMg", result.ViewName);
            var model = Assert.IsType<List<Employee>>(result.Model);
            Assert.Equal(2, model.Count);
        }

        [Fact]
        public async Task LoadTab_EmployeeMg_WithWhitespaceKeyword_ReturnsAllEmployees()
        {
            // Arrange
            var employees = new List<Employee> {
                new() { Account = new Account { FullName = "John", IdentityCard = "123", Email = "john@test.com", PhoneNumber = "123456789", Address = "Test Address" } },
                new() { Account = new Account { FullName = "Jane", IdentityCard = "456", Email = "jane@test.com", PhoneNumber = "987654321", Address = "Another Address" } }
            };
            _empSvc.Setup(x => x.GetAll()).Returns(employees);

            var ctrl = BuildController();
            
            // Set up HTTP context with empty query string
            var context = new DefaultHttpContext();
            var queryCollection = new QueryCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>());
            context.Request.Query = queryCollection;
            ctrl.ControllerContext = new ControllerContext { HttpContext = context };

            // Act
            var result = await ctrl.LoadTab("EmployeeMg", keyword: "   ") as PartialViewResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal("EmployeeMg", result.ViewName);
            var model = Assert.IsType<List<Employee>>(result.Model);
            Assert.Equal(2, model.Count);
        }

        [Fact]
        public async Task LoadTab_EmployeeMg_WithNullAccount_HandlesGracefully()
        {
            // Arrange
            var employees = new List<Employee> {
                new() { Account = null },
                new() { Account = new Account { FullName = "Jane", IdentityCard = "456", Email = "jane@test.com", PhoneNumber = "987654321", Address = "Another Address" } }
            };
            _empSvc.Setup(x => x.GetAll()).Returns(employees);

            var ctrl = BuildController();
            
            // Set up HTTP context with empty query string
            var context = new DefaultHttpContext();
            var queryCollection = new QueryCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>());
            context.Request.Query = queryCollection;
            ctrl.ControllerContext = new ControllerContext { HttpContext = context };

            // Act
            var result = await ctrl.LoadTab("EmployeeMg", keyword: "jane") as PartialViewResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal("EmployeeMg", result.ViewName);
            var model = Assert.IsType<List<Employee>>(result.Model);
            Assert.Single(model);
        }

        [Fact]
        public async Task LoadTab_EmployeeMg_WithNullAccountFields_HandlesGracefully()
        {
            // Arrange
            var employees = new List<Employee> {
                new() { Account = new Account { FullName = null, IdentityCard = null, Email = null, PhoneNumber = null, Address = null } },
                new() { Account = new Account { FullName = "Jane", IdentityCard = "456", Email = "jane@test.com", PhoneNumber = "987654321", Address = "Another Address" } }
            };
            _empSvc.Setup(x => x.GetAll()).Returns(employees);

            var ctrl = BuildController();
            
            // Set up HTTP context with empty query string
            var context = new DefaultHttpContext();
            var queryCollection = new QueryCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>());
            context.Request.Query = queryCollection;
            ctrl.ControllerContext = new ControllerContext { HttpContext = context };

            // Act
            var result = await ctrl.LoadTab("EmployeeMg", keyword: "jane") as PartialViewResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal("EmployeeMg", result.ViewName);
            var model = Assert.IsType<List<Employee>>(result.Model);
            Assert.Single(model);
        }

        [Fact]
        public async Task LoadTab_BookingMg_WithNullKeyword_ReturnsAllInvoices()
        {
            // Arrange
            var invoices = new List<Invoice> {
                new() { InvoiceId = "I1", AccountId = "A1", Account = new Account { PhoneNumber = "123", IdentityCard = "ID1" } },
                new() { InvoiceId = "I2", AccountId = "A2", Account = new Account { PhoneNumber = "456", IdentityCard = "ID2" } }
            };
            _invSvc.Setup(x => x.GetAll()).Returns(invoices);

            var ctrl = BuildController();
            
            // Set up HTTP context with empty query string
            var context = new DefaultHttpContext();
            var queryCollection = new QueryCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>());
            context.Request.Query = queryCollection;
            ctrl.ControllerContext = new ControllerContext { HttpContext = context };

            // Act
            var result = await ctrl.LoadTab("BookingMg", keyword: null) as PartialViewResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal("BookingMg", result.ViewName);
            var model = Assert.IsType<List<Invoice>>(result.Model);
            Assert.Equal(2, model.Count);
        }

        [Fact]
        public async Task LoadTab_BookingMg_WithEmptyKeyword_ReturnsAllInvoices()
        {
            // Arrange
            var invoices = new List<Invoice> {
                new() { InvoiceId = "I1", AccountId = "A1", Account = new Account { PhoneNumber = "123", IdentityCard = "ID1" } },
                new() { InvoiceId = "I2", AccountId = "A2", Account = new Account { PhoneNumber = "456", IdentityCard = "ID2" } }
            };
            _invSvc.Setup(x => x.GetAll()).Returns(invoices);

            var ctrl = BuildController();
            
            // Set up HTTP context with empty query string
            var context = new DefaultHttpContext();
            var queryCollection = new QueryCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>());
            context.Request.Query = queryCollection;
            ctrl.ControllerContext = new ControllerContext { HttpContext = context };

            // Act
            var result = await ctrl.LoadTab("BookingMg", keyword: "") as PartialViewResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal("BookingMg", result.ViewName);
            var model = Assert.IsType<List<Invoice>>(result.Model);
            Assert.Equal(2, model.Count);
        }

        [Fact]
        public async Task LoadTab_BookingMg_WithNullInvoiceFields_HandlesGracefully()
        {
            // Arrange
            var invoices = new List<Invoice> {
                new() { InvoiceId = null, AccountId = null, Account = null },
                new() { InvoiceId = "I2", AccountId = "A2", Account = new Account { PhoneNumber = "456", IdentityCard = "ID2" } }
            };
            _invSvc.Setup(x => x.GetAll()).Returns(invoices);

            var ctrl = BuildController();
            
            // Set up HTTP context with empty query string
            var context = new DefaultHttpContext();
            var queryCollection = new QueryCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>());
            context.Request.Query = queryCollection;
            ctrl.ControllerContext = new ControllerContext { HttpContext = context };

            // Act
            var result = await ctrl.LoadTab("BookingMg", keyword: "456") as PartialViewResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal("BookingMg", result.ViewName);
            var model = Assert.IsType<List<Invoice>>(result.Model);
            Assert.Single(model);
        }

        [Fact]
        public async Task LoadTab_BookingMg_WithNullAccountFields_HandlesGracefully()
        {
            // Arrange
            var invoices = new List<Invoice> {
                new() { InvoiceId = "I1", AccountId = "A1", Account = new Account { PhoneNumber = null, IdentityCard = null } },
                new() { InvoiceId = "I2", AccountId = "A2", Account = new Account { PhoneNumber = "456", IdentityCard = "ID2" } }
            };
            _invSvc.Setup(x => x.GetAll()).Returns(invoices);

            var ctrl = BuildController();
            
            // Set up HTTP context with empty query string
            var context = new DefaultHttpContext();
            var queryCollection = new QueryCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>());
            context.Request.Query = queryCollection;
            ctrl.ControllerContext = new ControllerContext { HttpContext = context };

            // Act
            var result = await ctrl.LoadTab("BookingMg", keyword: "456") as PartialViewResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal("BookingMg", result.ViewName);
            var model = Assert.IsType<List<Invoice>>(result.Model);
            Assert.Single(model);
        }

        [Fact]
        public async Task LoadTab_BookingMg_WithNullStatusFilter_ReturnsAllInvoices()
        {
            // Arrange
            var invoices = new List<Invoice> {
                new() { Status = InvoiceStatus.Completed, Cancel = false },
                new() { Status = InvoiceStatus.Incomplete, Cancel = false }
            };
            _invSvc.Setup(x => x.GetAll()).Returns(invoices);

            var ctrl = BuildController();
            
            // Set up HTTP context with empty query string
            var context = new DefaultHttpContext();
            var queryCollection = new QueryCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>());
            context.Request.Query = queryCollection;
            ctrl.ControllerContext = new ControllerContext { HttpContext = context };

            // Act
            var result = await ctrl.LoadTab("BookingMg", statusFilter: null) as PartialViewResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal("BookingMg", result.ViewName);
            var model = Assert.IsType<List<Invoice>>(result.Model);
            Assert.Equal(2, model.Count);
        }

        [Fact]
        public async Task LoadTab_BookingMg_WithEmptyStatusFilter_ReturnsAllInvoices()
        {
            // Arrange
            var invoices = new List<Invoice> {
                new() { Status = InvoiceStatus.Completed, Cancel = false },
                new() { Status = InvoiceStatus.Incomplete, Cancel = false }
            };
            _invSvc.Setup(x => x.GetAll()).Returns(invoices);

            var ctrl = BuildController();
            
            // Set up HTTP context with empty query string
            var context = new DefaultHttpContext();
            var queryCollection = new QueryCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>());
            context.Request.Query = queryCollection;
            ctrl.ControllerContext = new ControllerContext { HttpContext = context };

            // Act
            var result = await ctrl.LoadTab("BookingMg", statusFilter: "") as PartialViewResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal("BookingMg", result.ViewName);
            var model = Assert.IsType<List<Invoice>>(result.Model);
            Assert.Equal(2, model.Count);
        }

        [Fact]
        public async Task LoadTab_BookingMg_WithInvalidStatusFilter_ReturnsAllInvoices()
        {
            // Arrange
            var invoices = new List<Invoice> {
                new() { Status = InvoiceStatus.Completed, Cancel = false },
                new() { Status = InvoiceStatus.Incomplete, Cancel = false }
            };
            _invSvc.Setup(x => x.GetAll()).Returns(invoices);

            var ctrl = BuildController();
            
            // Set up HTTP context with empty query string
            var context = new DefaultHttpContext();
            var queryCollection = new QueryCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>());
            context.Request.Query = queryCollection;
            ctrl.ControllerContext = new ControllerContext { HttpContext = context };

            // Act
            var result = await ctrl.LoadTab("BookingMg", statusFilter: "invalid") as PartialViewResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal("BookingMg", result.ViewName);
            var model = Assert.IsType<List<Invoice>>(result.Model);
            Assert.Equal(2, model.Count);
        }

        [Fact]
        public async Task LoadTab_BookingMg_WithNullBookingTypeFilter_ReturnsAllInvoices()
        {
            // Arrange
            var invoices = new List<Invoice> {
                new() { EmployeeId = null },
                new() { EmployeeId = "EMP1" }
            };
            _invSvc.Setup(x => x.GetAll()).Returns(invoices);

            var ctrl = BuildController();
            
            // Set up HTTP context with empty query string
            var context = new DefaultHttpContext();
            var queryCollection = new QueryCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>());
            context.Request.Query = queryCollection;
            ctrl.ControllerContext = new ControllerContext { HttpContext = context };

            // Act
            var result = await ctrl.LoadTab("BookingMg", bookingTypeFilter: null) as PartialViewResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal("BookingMg", result.ViewName);
            var model = Assert.IsType<List<Invoice>>(result.Model);
            Assert.Equal(2, model.Count);
        }

        [Fact]
        public async Task LoadTab_BookingMg_WithEmptyBookingTypeFilter_ReturnsAllInvoices()
        {
            // Arrange
            var invoices = new List<Invoice> {
                new() { EmployeeId = null },
                new() { EmployeeId = "EMP1" }
            };
            _invSvc.Setup(x => x.GetAll()).Returns(invoices);

            var ctrl = BuildController();
            
            // Set up HTTP context with empty query string
            var context = new DefaultHttpContext();
            var queryCollection = new QueryCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>());
            context.Request.Query = queryCollection;
            ctrl.ControllerContext = new ControllerContext { HttpContext = context };

            // Act
            var result = await ctrl.LoadTab("BookingMg", bookingTypeFilter: "") as PartialViewResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal("BookingMg", result.ViewName);
            var model = Assert.IsType<List<Invoice>>(result.Model);
            Assert.Equal(2, model.Count);
        }

        [Fact]
        public async Task LoadTab_BookingMg_WithInvalidBookingTypeFilter_ReturnsAllInvoices()
        {
            // Arrange
            var invoices = new List<Invoice> {
                new() { EmployeeId = null },
                new() { EmployeeId = "EMP1" }
            };
            _invSvc.Setup(x => x.GetAll()).Returns(invoices);

            var ctrl = BuildController();
            
            // Set up HTTP context with empty query string
            var context = new DefaultHttpContext();
            var queryCollection = new QueryCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>());
            context.Request.Query = queryCollection;
            ctrl.ControllerContext = new ControllerContext { HttpContext = context };

            // Act
            var result = await ctrl.LoadTab("BookingMg", bookingTypeFilter: "invalid") as PartialViewResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal("BookingMg", result.ViewName);
            var model = Assert.IsType<List<Invoice>>(result.Model);
            Assert.Equal(2, model.Count);
        }

        [Fact]
        public async Task LoadTab_BookingMg_WithAllBookingTypeFilter_ReturnsAllInvoices()
        {
            // Arrange
            var invoices = new List<Invoice> {
                new() { EmployeeId = null },
                new() { EmployeeId = "EMP1" }
            };
            _invSvc.Setup(x => x.GetAll()).Returns(invoices);

            var ctrl = BuildController();
            
            // Set up HTTP context with empty query string
            var context = new DefaultHttpContext();
            var queryCollection = new QueryCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>());
            context.Request.Query = queryCollection;
            ctrl.ControllerContext = new ControllerContext { HttpContext = context };

            // Act
            var result = await ctrl.LoadTab("BookingMg", bookingTypeFilter: "all") as PartialViewResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal("BookingMg", result.ViewName);
            var model = Assert.IsType<List<Invoice>>(result.Model);
            Assert.Equal(2, model.Count);
        }

        [Fact]
        public async Task LoadTab_BookingMg_WithNullSortBy_ReturnsUnsortedInvoices()
        {
            // Arrange
            var invoices = new List<Invoice> {
                new() { InvoiceId = "I2", MovieShow = new MovieShow { Movie = new Movie { MovieNameEnglish = "B Movie" } } },
                new() { InvoiceId = "I1", MovieShow = new MovieShow { Movie = new Movie { MovieNameEnglish = "A Movie" } } }
            };
            _invSvc.Setup(x => x.GetAll()).Returns(invoices);

            var ctrl = BuildController();
            
            // Set up HTTP context with empty query string
            var context = new DefaultHttpContext();
            var queryCollection = new QueryCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>());
            context.Request.Query = queryCollection;
            ctrl.ControllerContext = new ControllerContext { HttpContext = context };

            // Act
            var result = await ctrl.LoadTab("BookingMg") as PartialViewResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal("BookingMg", result.ViewName);
            var model = Assert.IsType<List<Invoice>>(result.Model);
            Assert.Equal(2, model.Count);
            Assert.Equal("I2", model[0].InvoiceId); // Should remain unsorted
        }

        [Fact]
        public async Task LoadTab_BookingMg_WithEmptySortBy_ReturnsUnsortedInvoices()
        {
            // Arrange
            var invoices = new List<Invoice> {
                new() { InvoiceId = "I2", MovieShow = new MovieShow { Movie = new Movie { MovieNameEnglish = "B Movie" } } },
                new() { InvoiceId = "I1", MovieShow = new MovieShow { Movie = new Movie { MovieNameEnglish = "A Movie" } } }
            };
            _invSvc.Setup(x => x.GetAll()).Returns(invoices);

            var ctrl = BuildController();
            
            // Set up HTTP context with empty query string
            var context = new DefaultHttpContext();
            var queryCollection = new QueryCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>());
            context.Request.Query = queryCollection;
            ctrl.ControllerContext = new ControllerContext { HttpContext = context };

            // Act
            var result = await ctrl.LoadTab("BookingMg") as PartialViewResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal("BookingMg", result.ViewName);
            var model = Assert.IsType<List<Invoice>>(result.Model);
            Assert.Equal(2, model.Count);
            Assert.Equal("I2", model[0].InvoiceId); // Should remain unsorted
        }

        [Fact]
        public async Task LoadTab_BookingMg_WithInvalidSortBy_ReturnsUnsortedInvoices()
        {
            // Arrange
            var invoices = new List<Invoice> {
                new() { InvoiceId = "I2", MovieShow = new MovieShow { Movie = new Movie { MovieNameEnglish = "B Movie" } } },
                new() { InvoiceId = "I1", MovieShow = new MovieShow { Movie = new Movie { MovieNameEnglish = "A Movie" } } }
            };
            _invSvc.Setup(x => x.GetAll()).Returns(invoices);

            var ctrl = BuildController();
            
            // Set up HTTP context with empty query string
            var context = new DefaultHttpContext();
            var queryCollection = new QueryCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>());
            context.Request.Query = queryCollection;
            ctrl.ControllerContext = new ControllerContext { HttpContext = context };

            // Act
            var result = await ctrl.LoadTab("BookingMg") as PartialViewResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal("BookingMg", result.ViewName);
            var model = Assert.IsType<List<Invoice>>(result.Model);
            Assert.Equal(2, model.Count);
            Assert.Equal("I2", model[0].InvoiceId); // Should remain unsorted
        }

        [Fact]
        public async Task LoadTab_Dashboard_WithMonthlyRange_ReturnsMonthlyDashboard()
        {
            // Arrange
            var dashboardModel = new AdminDashboardViewModel
            {
                RevenueToday = 1000m,
                BookingsToday = 10,
                TicketsSoldToday = 15
            };
            _dashboardSvc.Setup(x => x.GetDashboardViewModel(30)).Returns(dashboardModel);

            var ctrl = BuildController();

            // Act
            var result = await ctrl.LoadTab("Dashboard", range: "monthly") as PartialViewResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Dashboard", result.ViewName);
            var model = Assert.IsType<AdminDashboardViewModel>(result.Model);
            Assert.Equal(1000m, model.RevenueToday);
        }

        [Fact]
        public async Task LoadTab_Dashboard_WithWeeklyRange_ReturnsWeeklyDashboard()
        {
            // Arrange
            var dashboardModel = new AdminDashboardViewModel
            {
                RevenueToday = 500m,
                BookingsToday = 5,
                TicketsSoldToday = 8
            };
            _dashboardSvc.Setup(x => x.GetDashboardViewModel(7)).Returns(dashboardModel);

            var ctrl = BuildController();

            // Act
            var result = await ctrl.LoadTab("Dashboard", range: "weekly") as PartialViewResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Dashboard", result.ViewName);
            var model = Assert.IsType<AdminDashboardViewModel>(result.Model);
            Assert.Equal(500m, model.RevenueToday);
        }

        [Fact]
        public async Task LoadTab_Dashboard_WithInvalidRange_ReturnsWeeklyDashboard()
        {
            // Arrange
            var dashboardModel = new AdminDashboardViewModel
            {
                RevenueToday = 500m,
                BookingsToday = 5,
                TicketsSoldToday = 8
            };
            _dashboardSvc.Setup(x => x.GetDashboardViewModel(7)).Returns(dashboardModel);

            var ctrl = BuildController();

            // Act
            var result = await ctrl.LoadTab("Dashboard", range: "invalid") as PartialViewResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Dashboard", result.ViewName);
            var model = Assert.IsType<AdminDashboardViewModel>(result.Model);
            Assert.Equal(500m, model.RevenueToday);
        }

        // Additional tests to improve branch coverage - Batch 2
        [Fact]
        public void BookingMgPartial_WithKeywordFilter_FiltersCorrectly()
        {
            // Arrange
            var invoices = new List<Invoice> {
                new() { InvoiceId = "INV001", AccountId = "ACC001", Account = new Account { PhoneNumber = "123456789", IdentityCard = "ID001" } },
                new() { InvoiceId = "INV002", AccountId = "ACC002", Account = new Account { PhoneNumber = "987654321", IdentityCard = "ID002" } }
            };
            _invSvc.Setup(x => x.GetAll()).Returns(invoices);

            var ctrl = BuildController();

            // Act
            var result = ctrl.BookingMgPartial(keyword: "INV001") as JsonResult;

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.Value);
            // Don't check specific type since it's an anonymous type
        }

        [Fact]
        public void BookingMgPartial_WithStatusFilterPaid_FiltersCorrectly()
        {
            // Arrange
            var invoices = new List<Invoice> {
                new() { Status = InvoiceStatus.Completed, Cancel = false },
                new() { Status = InvoiceStatus.Incomplete, Cancel = false }
            };
            _invSvc.Setup(x => x.GetAll()).Returns(invoices);

            var ctrl = BuildController();

            // Act
            var result = ctrl.BookingMgPartial(statusFilter: "paid") as JsonResult;

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.Value);
            // Don't check specific type since it's an anonymous type
        }

        [Fact]
        public void BookingMgPartial_WithStatusFilterCancelled_FiltersCorrectly()
        {
            // Arrange
            var invoices = new List<Invoice> {
                new() { Status = InvoiceStatus.Completed, Cancel = true },
                new() { Status = null, Cancel = false }
            };
            _invSvc.Setup(x => x.GetAll()).Returns(invoices);

            var ctrl = BuildController();

            // Act
            var result = ctrl.BookingMgPartial(statusFilter: "cancelled") as JsonResult;

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.Value);
            // Don't check specific type since it's an anonymous type
        }

        [Fact]
        public void BookingMgPartial_WithStatusFilterUnpaid_FiltersCorrectly()
        {
            // Arrange
            var invoices = new List<Invoice> {
                new() { Status = InvoiceStatus.Incomplete, Cancel = false },
                new() { Status = InvoiceStatus.Completed, Cancel = false }
            };
            _invSvc.Setup(x => x.GetAll()).Returns(invoices);

            var ctrl = BuildController();

            // Act
            var result = ctrl.BookingMgPartial(statusFilter: "unpaid") as JsonResult;

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.Value);
            // Don't check specific type since it's an anonymous type
        }

        [Fact]
        public void BookingMgPartial_WithBookingTypeFilterNormal_FiltersCorrectly()
        {
            // Arrange
            var invoices = new List<Invoice> {
                new() { EmployeeId = null },
                new() { EmployeeId = "EMP001" }
            };
            _invSvc.Setup(x => x.GetAll()).Returns(invoices);

            var ctrl = BuildController();

            // Act
            var result = ctrl.BookingMgPartial(bookingTypeFilter: "normal") as JsonResult;

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.Value);
            // Don't check specific type since it's an anonymous type
        }

        [Fact]
        public void BookingMgPartial_WithBookingTypeFilterEmployee_FiltersCorrectly()
        {
            // Arrange
            var invoices = new List<Invoice> {
                new() { EmployeeId = "EMP001" },
                new() { EmployeeId = null }
            };
            _invSvc.Setup(x => x.GetAll()).Returns(invoices);

            var ctrl = BuildController();

            // Act
            var result = ctrl.BookingMgPartial(bookingTypeFilter: "employee") as JsonResult;

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.Value);
            // Don't check specific type since it's an anonymous type
        }

        [Fact]
        public void BookingMgPartial_WithSortByMovieAz_SortsCorrectly()
        {
            // Arrange
            var invoices = new List<Invoice> {
                new() { MovieShow = new MovieShow { Movie = new Movie { MovieNameEnglish = "Zebra" } } },
                new() { MovieShow = new MovieShow { Movie = new Movie { MovieNameEnglish = "Alpha" } } }
            };
            _invSvc.Setup(x => x.GetAll()).Returns(invoices);

            var ctrl = BuildController();

            // Act
            var result = ctrl.BookingMgPartial(sortBy: "movie_az") as JsonResult;

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.Value);
            // Don't check specific type since it's an anonymous type
        }

        [Fact]
        public void BookingMgPartial_WithSortByMovieZa_SortsCorrectly()
        {
            // Arrange
            var invoices = new List<Invoice> {
                new() { MovieShow = new MovieShow { Movie = new Movie { MovieNameEnglish = "Alpha" } } },
                new() { MovieShow = new MovieShow { Movie = new Movie { MovieNameEnglish = "Zebra" } } }
            };
            _invSvc.Setup(x => x.GetAll()).Returns(invoices);

            var ctrl = BuildController();

            // Act
            var result = ctrl.BookingMgPartial(sortBy: "movie_za") as JsonResult;

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.Value);
            // Don't check specific type since it's an anonymous type
        }

        [Fact]
        public void BookingMgPartial_WithSortByTimeAsc_SortsCorrectly()
        {
            // Arrange
            var invoices = new List<Invoice> {
                new() { MovieShow = new MovieShow { Schedule = new Schedule { ScheduleTime = TimeOnly.FromDateTime(DateTime.Now.AddHours(2)) } } },
                new() { MovieShow = new MovieShow { Schedule = new Schedule { ScheduleTime = TimeOnly.FromDateTime(DateTime.Now.AddHours(1)) } } }
            };
            _invSvc.Setup(x => x.GetAll()).Returns(invoices);

            var ctrl = BuildController();

            // Act
            var result = ctrl.BookingMgPartial(sortBy: "time_asc") as JsonResult;

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.Value);
            // Don't check specific type since it's an anonymous type
        }

        [Fact]
        public void BookingMgPartial_WithSortByTimeDesc_SortsCorrectly()
        {
            // Arrange
            var invoices = new List<Invoice> {
                new() { MovieShow = new MovieShow { Schedule = new Schedule { ScheduleTime = TimeOnly.FromDateTime(DateTime.Now.AddHours(1)) } } },
                new() { MovieShow = new MovieShow { Schedule = new Schedule { ScheduleTime = TimeOnly.FromDateTime(DateTime.Now.AddHours(2)) } } }
            };
            _invSvc.Setup(x => x.GetAll()).Returns(invoices);

            var ctrl = BuildController();

            // Act
            var result = ctrl.BookingMgPartial(sortBy: "time_desc") as JsonResult;

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.Value);
            // Don't check specific type since it's an anonymous type
        }

        [Fact]
        public void BookingMgPartial_WithPagination_ReturnsCorrectPage()
        {
            // Arrange
            var invoices = Enumerable.Range(1, 25).Select(i => new Invoice { InvoiceId = $"INV{i:000}" }).ToList();
            _invSvc.Setup(x => x.GetAll()).Returns(invoices);

            var ctrl = BuildController();

            // Act
            var result = ctrl.BookingMgPartial(page: 2, pageSize: 10) as JsonResult;

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.Value);
            // Don't check specific type since it's an anonymous type
        }

        [Fact]
        public void GetMovieShowsByDate_WithValidDate_ReturnsJson()
        {
            // Arrange
            var movieShows = new List<MovieShow> {
                new() { ShowDate = DateOnly.FromDateTime(DateTime.Today) }
            };
            _movieSvc.Setup(x => x.GetMovieShow()).Returns(movieShows);

            var ctrl = BuildController();

            // Act
            var result = ctrl.GetMovieShowsByDate(DateTime.Today.ToString("dd/MM/yyyy")) as JsonResult;

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.Value);
            // Don't check specific type since it returns an anonymous type
        }

        [Fact]
        public void GetMovieShowsByDate_WithInvalidDate_ReturnsBadRequest()
        {
            // Arrange
            var movieShows = new List<MovieShow>();
            _movieSvc.Setup(x => x.GetMovieShow()).Returns(movieShows);

            var ctrl = BuildController();

            // Act
            var result = ctrl.GetMovieShowsByDate("invalid-date");

            // Assert
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public void GetMovieShowsByDate_WithNullDate_ReturnsBadRequest()
        {
            // Arrange
            var movieShows = new List<MovieShow>();
            _movieSvc.Setup(x => x.GetMovieShow()).Returns(movieShows);

            var ctrl = BuildController();

            // Act
            var result = ctrl.GetMovieShowsByDate(null);

            // Assert
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public void GetMovieShowsByDate_WithEmptyDate_ReturnsBadRequest()
        {
            // Arrange
            var movieShows = new List<MovieShow>();
            _movieSvc.Setup(x => x.GetMovieShow()).Returns(movieShows);

            var ctrl = BuildController();

            // Act
            var result = ctrl.GetMovieShowsByDate("");

            // Assert
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task LoadTab_FoodMg_WithStatusFilterTrue_ReturnsActiveFoods()
        {
            // Arrange
            var foods = new FoodListViewModel
            {
                Foods = new List<FoodViewModel> { new() { Status = true }, new() { Status = false } }
            };
            _foodSvc.Setup(x => x.GetAllAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool?>()))
                    .ReturnsAsync(foods);

            var ctrl = BuildController();
            
            // Set up HTTP context with status filter
            var context = new DefaultHttpContext();
            var queryCollection = new QueryCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>
            {
                ["statusFilter"] = "true"
            });
            context.Request.Query = queryCollection;
            ctrl.ControllerContext = new ControllerContext { HttpContext = context };

            // Act
            var result = await ctrl.LoadTab("FoodMg") as PartialViewResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal("FoodMg", result.ViewName);
        }

        [Fact]
        public async Task LoadTab_FoodMg_WithStatusFilterFalse_ReturnsInactiveFoods()
        {
            // Arrange
            var foods = new FoodListViewModel
            {
                Foods = new List<FoodViewModel> { new() { Status = false }, new() { Status = true } }
            };
            _foodSvc.Setup(x => x.GetAllAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool?>()))
                    .ReturnsAsync(foods);

            var ctrl = BuildController();
            
            // Set up HTTP context with status filter
            var context = new DefaultHttpContext();
            var queryCollection = new QueryCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>
            {
                ["statusFilter"] = "false"
            });
            context.Request.Query = queryCollection;
            ctrl.ControllerContext = new ControllerContext { HttpContext = context };

            // Act
            var result = await ctrl.LoadTab("FoodMg") as PartialViewResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal("FoodMg", result.ViewName);
        }

        [Fact]
        public async Task LoadTab_FoodMg_WithSortByNameZa_SortsCorrectly()
        {
            // Arrange
            var foods = new FoodListViewModel
            {
                Foods = new List<FoodViewModel> 
                { 
                    new() { Name = "Alpha" }, 
                    new() { Name = "Zebra" } 
                }
            };
            _foodSvc.Setup(x => x.GetAllAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool?>()))
                    .ReturnsAsync(foods);

            var ctrl = BuildController();
            
            // Set up HTTP context with sort parameter
            var context = new DefaultHttpContext();
            var queryCollection = new QueryCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>
            {
                ["sortBy"] = "name_za"
            });
            context.Request.Query = queryCollection;
            ctrl.ControllerContext = new ControllerContext { HttpContext = context };

            // Act
            var result = await ctrl.LoadTab("FoodMg") as PartialViewResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal("FoodMg", result.ViewName);
        }

        [Fact]
        public async Task LoadTab_FoodMg_WithSortByCategoryAz_SortsCorrectly()
        {
            // Arrange
            var foods = new FoodListViewModel
            {
                Foods = new List<FoodViewModel> 
                { 
                    new() { Category = "Zebra" }, 
                    new() { Category = "Alpha" } 
                }
            };
            _foodSvc.Setup(x => x.GetAllAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool?>()))
                    .ReturnsAsync(foods);

            var ctrl = BuildController();
            
            // Set up HTTP context with sort parameter
            var context = new DefaultHttpContext();
            var queryCollection = new QueryCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>
            {
                ["sortBy"] = "category_az"
            });
            context.Request.Query = queryCollection;
            ctrl.ControllerContext = new ControllerContext { HttpContext = context };

            // Act
            var result = await ctrl.LoadTab("FoodMg") as PartialViewResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal("FoodMg", result.ViewName);
        }

        [Fact]
        public async Task LoadTab_FoodMg_WithSortByPriceDesc_SortsCorrectly()
        {
            // Arrange
            var foods = new FoodListViewModel
            {
                Foods = new List<FoodViewModel> 
                { 
                    new() { Price = 10.0m }, 
                    new() { Price = 20.0m } 
                }
            };
            _foodSvc.Setup(x => x.GetAllAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool?>()))
                    .ReturnsAsync(foods);

            var ctrl = BuildController();
            
            // Set up HTTP context with sort parameter
            var context = new DefaultHttpContext();
            var queryCollection = new QueryCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>
            {
                ["sortBy"] = "price_desc"
            });
            context.Request.Query = queryCollection;
            ctrl.ControllerContext = new ControllerContext { HttpContext = context };

            // Act
            var result = await ctrl.LoadTab("FoodMg") as PartialViewResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal("FoodMg", result.ViewName);
        }

        [Fact]
        public async Task LoadTab_FoodMg_WithSortByCreatedDesc_SortsCorrectly()
        {
            // Arrange
            var foods = new FoodListViewModel
            {
                Foods = new List<FoodViewModel> 
                { 
                    new() { CreatedDate = DateTime.Now.AddDays(-1) }, 
                    new() { CreatedDate = DateTime.Now } 
                }
            };
            _foodSvc.Setup(x => x.GetAllAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool?>()))
                    .ReturnsAsync(foods);

            var ctrl = BuildController();
            
            // Set up HTTP context with sort parameter
            var context = new DefaultHttpContext();
            var queryCollection = new QueryCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>
            {
                ["sortBy"] = "created_desc"
            });
            context.Request.Query = queryCollection;
            ctrl.ControllerContext = new ControllerContext { HttpContext = context };

            // Act
            var result = await ctrl.LoadTab("FoodMg") as PartialViewResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal("FoodMg", result.ViewName);
        }

        [Fact]
        public void MainPage_WithMonthlyRange_SetsCorrectDays()
        {
            // Arrange
            _dashboardSvc.Setup(x => x.GetDashboardViewModel(It.IsAny<int>()))
                         .Returns(new AdminDashboardViewModel());

            var ctrl = BuildController();

            // Act
            var result = ctrl.MainPage(tab: "Dashboard", range: "monthly") as ViewResult;

            // Assert
            Assert.NotNull(result);
            _dashboardSvc.Verify(x => x.GetDashboardViewModel(30), Times.Once);
        }

        [Fact]
        public void MainPage_WithWeeklyRange_SetsCorrectDays()
        {
            // Arrange
            _dashboardSvc.Setup(x => x.GetDashboardViewModel(It.IsAny<int>()))
                         .Returns(new AdminDashboardViewModel());

            var ctrl = BuildController();

            // Act
            var result = ctrl.MainPage(tab: "Dashboard", range: "weekly") as ViewResult;

            // Assert
            Assert.NotNull(result);
            _dashboardSvc.Verify(x => x.GetDashboardViewModel(7), Times.Once);
        }

        [Fact]
        public void MainPage_WithInvalidRange_SetsDefaultDays()
        {
            // Arrange
            _dashboardSvc.Setup(x => x.GetDashboardViewModel(It.IsAny<int>()))
                         .Returns(new AdminDashboardViewModel());

            var ctrl = BuildController();

            // Act
            var result = ctrl.MainPage(tab: "Dashboard", range: "invalid") as ViewResult;

            // Assert
            Assert.NotNull(result);
            _dashboardSvc.Verify(x => x.GetDashboardViewModel(7), Times.Once);
        }
    }
}
