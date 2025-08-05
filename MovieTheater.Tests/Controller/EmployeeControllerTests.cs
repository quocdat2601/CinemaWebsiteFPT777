using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;
using MovieTheater.Controllers;
using MovieTheater.Models;
using MovieTheater.Repository;
using MovieTheater.Service;
using MovieTheater.ViewModels;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Xunit;
using System.Linq;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Controllers;
using System.Text;
using Microsoft.Extensions.Primitives;

namespace MovieTheater.Tests.Controller
{
    public class EmployeeControllerTests
    {
        private readonly Mock<IEmployeeService> _employeeServiceMock = new();
        private readonly Mock<IMovieService> _movieServiceMock = new();
        private readonly Mock<IMemberRepository> _memberRepoMock = new();
        private readonly Mock<IAccountService> _accountServiceMock = new();
        private readonly Mock<IInvoiceService> _invoiceServiceMock = new();
        private readonly Mock<ICinemaService> _cinemaServiceMock = new();
        private readonly Mock<IPromotionService> _promotionServiceMock = new();
        private readonly Mock<IFoodService> _foodServiceMock = new();
        private readonly Mock<IVoucherService> _voucherServiceMock = new();
        private readonly Mock<IPersonRepository> _personRepoMock = new();

        private readonly EmployeeController _controller;

        public EmployeeControllerTests()
        {
            _controller = new EmployeeController(
                _employeeServiceMock.Object,
                _movieServiceMock.Object,
                _memberRepoMock.Object,
                _accountServiceMock.Object,
                _invoiceServiceMock.Object,
                _cinemaServiceMock.Object,
                _promotionServiceMock.Object,
                _foodServiceMock.Object,
                _voucherServiceMock.Object,
                _personRepoMock.Object
            );

            // Setup TempData
            _controller.TempData = new TempDataDictionary(
                new DefaultHttpContext(),
                Mock.Of<ITempDataProvider>());

            // Setup ControllerContext with HttpContext
            var httpContext = new DefaultHttpContext();
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext,
                RouteData = new RouteData(),
                ActionDescriptor = new ControllerActionDescriptor()
            };
        }

        #region MainPage Tests
        [Fact]
        public void MainPage_ReturnsView_WithDefaultTab()
        {
            // Act
            var result = _controller.MainPage() as ViewResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal("MovieMg", _controller.ViewData["ActiveTab"]);
        }

        [Fact]
        public void MainPage_ReturnsView_WithCustomTab()
        {
            // Act
            var result = _controller.MainPage("BookingMg") as ViewResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal("BookingMg", _controller.ViewData["ActiveTab"]);
        }
        #endregion

        #region MemberList Tests
        [Fact]
        public void MemberList_ReturnsPartialView_WithMembers()
        {
            // Arrange
            var members = new List<Member>
            {
                new Member { MemberId = "M1", AccountId = "A1" },
                new Member { MemberId = "M2", AccountId = "A2" }
            };
            _memberRepoMock.Setup(r => r.GetAll()).Returns(members);

            // Act
            var result = _controller.MemberList() as PartialViewResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal("MemberMg", result.ViewName);
            Assert.Equal(members, result.Model);
        }
        #endregion

        #region LoadTab Tests
        [Fact]
        public async Task LoadTab_MovieMg_ReturnsPartialView_WithMovies()
        {
            // Arrange
            var movies = new List<Movie>
            {
                new Movie { MovieId = "MV001", MovieNameEnglish = "Movie 1" },
                new Movie { MovieId = "MV002", MovieNameEnglish = "Movie 2" }
            };
            _movieServiceMock.Setup(s => s.GetAll()).Returns(movies);

            // Act
            var result = await _controller.LoadTab("MovieMg") as PartialViewResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal("MovieMg", result.ViewName);
            Assert.Equal(movies, result.Model);
        }

        [Fact]
        public async Task LoadTab_BookingMg_ReturnsPartialView_WithInvoices()
        {
            // Arrange
            var invoices = new List<Invoice>
            {
                new Invoice { InvoiceId = "INV1", AccountId = "ACC1", Status = InvoiceStatus.Completed },
                new Invoice { InvoiceId = "INV2", AccountId = "ACC2", Status = InvoiceStatus.Incomplete }
            };
            _invoiceServiceMock.Setup(s => s.GetAll()).Returns(invoices);

            // Act
            var result = await _controller.LoadTab("BookingMg") as PartialViewResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal("BookingMg", result.ViewName);
            Assert.Equal(invoices, result.Model);
        }

        [Fact]
        public async Task LoadTab_BookingMg_WithKeyword_FiltersInvoices()
        {
            // Arrange
            var invoices = new List<Invoice>
            {
                new Invoice { InvoiceId = "INV1", AccountId = "ACC1", Status = InvoiceStatus.Completed },
                new Invoice { InvoiceId = "INV2", AccountId = "ACC2", Status = InvoiceStatus.Incomplete }
            };
            _invoiceServiceMock.Setup(s => s.GetAll()).Returns(invoices);

            // Act
            var result = await _controller.LoadTab("BookingMg", keyword: "INV1") as PartialViewResult;

            // Assert
            Assert.NotNull(result);
            var filteredInvoices = result.Model as List<Invoice>;
            Assert.Single(filteredInvoices);
            Assert.Equal("INV1", filteredInvoices[0].InvoiceId);
        }

        [Fact]
        public async Task LoadTab_BookingMg_WithStatusFilter_FiltersByStatus()
        {
            // Arrange
            var invoices = new List<Invoice>
            {
                new Invoice { InvoiceId = "INV1", Status = InvoiceStatus.Completed, Cancel = false },
                new Invoice { InvoiceId = "INV2", Status = InvoiceStatus.Completed, Cancel = true },
                new Invoice { InvoiceId = "INV3", Status = InvoiceStatus.Incomplete }
            };
            _invoiceServiceMock.Setup(s => s.GetAll()).Returns(invoices);

            // Act
            var result = await _controller.LoadTab("BookingMg", statusFilter: "completed") as PartialViewResult;

            // Assert
            Assert.NotNull(result);
            var filteredInvoices = result.Model as List<Invoice>;
            Assert.Single(filteredInvoices);
            Assert.Equal("INV1", filteredInvoices[0].InvoiceId);
        }

        [Fact]
        public async Task LoadTab_FoodMg_ReturnsPartialView_WithFoods()
        {
            // Arrange
            var foodListViewModel = new FoodListViewModel
            {
                Foods = new List<FoodViewModel>
                {
                    new FoodViewModel { FoodId = 1, Name = "Popcorn", Category = "Snacks" },
                    new FoodViewModel { FoodId = 2, Name = "Coke", Category = "Drinks" }
                }
            };
            _foodServiceMock.Setup(s => s.GetAllAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool?>()))
                           .ReturnsAsync(foodListViewModel);

            // Act
            var result = await _controller.LoadTab("FoodMg") as PartialViewResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal("FoodMg", result.ViewName);
            Assert.Equal(foodListViewModel, result.Model);
        }

        [Fact]
        public async Task LoadTab_VoucherMg_ReturnsPartialView_WithVouchers()
        {
            // Arrange
            var vouchers = new List<Voucher>
            {
                new Voucher { VoucherId = "V1", AccountId = "ACC1", Value = 100 },
                new Voucher { VoucherId = "V2", AccountId = "ACC2", Value = 200 }
            };
            _voucherServiceMock.Setup(s => s.GetFilteredVouchers(It.IsAny<MovieTheater.Service.VoucherFilterModel>()))
                              .Returns(vouchers);

            // Act
            var result = await _controller.LoadTab("VoucherMg") as PartialViewResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal("VoucherMg", result.ViewName);
            Assert.Equal(vouchers, result.Model);
        }

        [Fact]
        public async Task LoadTab_CastMg_ReturnsPartialView_WithPersons()
        {
            // Arrange
            var persons = new List<Person>
            {
                new Person { PersonId = 1, Name = "Actor 1", IsDirector = false },
                new Person { PersonId = 2, Name = "Director 1", IsDirector = true }
            };
            _personRepoMock.Setup(r => r.GetAll()).Returns(persons);

            // Act
            var result = await _controller.LoadTab("CastMg") as PartialViewResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal("CastMg", result.ViewName);
        }

        [Fact]
        public async Task LoadTab_QRCode_ReturnsPartialView()
        {
            // Act
            var result = await _controller.LoadTab("QRCode") as PartialViewResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal("QRCode", result.ViewName);
        }

        [Fact]
        public async Task LoadTab_UnknownTab_ReturnsContent()
        {
            // Act
            var result = await _controller.LoadTab("UnknownTab") as ContentResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Tab not found.", result.Content);
        }

        #region LoadTab BookingMg Additional Tests
        [Fact]
        public async Task LoadTab_BookingMg_WithSortByMovieAz_SortsCorrectly()
        {
            // Arrange
            var invoices = new List<Invoice>
            {
                new Invoice { InvoiceId = "INV1", MovieShow = new MovieShow { Movie = new Movie { MovieNameEnglish = "Zebra" } } },
                new Invoice { InvoiceId = "INV2", MovieShow = new MovieShow { Movie = new Movie { MovieNameEnglish = "Alpha" } } }
            };
            _invoiceServiceMock.Setup(x => x.GetAll()).Returns(invoices);

            // Setup query string
            var queryCollection = new QueryCollection(new Dictionary<string, StringValues> { { "sortBy", "movie_az" } });
            _controller.HttpContext.Request.Query = queryCollection;

            // Act
            var result = await _controller.LoadTab("BookingMg") as PartialViewResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal("BookingMg", result.ViewName);
            var resultInvoices = result.Model as List<Invoice>;
            Assert.NotNull(resultInvoices);
            Assert.Equal("Alpha", resultInvoices[0].MovieShow.Movie.MovieNameEnglish);
        }

        [Fact]
        public async Task LoadTab_BookingMg_WithSortByMovieZa_SortsCorrectly()
        {
            // Arrange
            var invoices = new List<Invoice>
            {
                new Invoice { InvoiceId = "INV1", MovieShow = new MovieShow { Movie = new Movie { MovieNameEnglish = "Alpha" } } },
                new Invoice { InvoiceId = "INV2", MovieShow = new MovieShow { Movie = new Movie { MovieNameEnglish = "Zebra" } } }
            };
            _invoiceServiceMock.Setup(x => x.GetAll()).Returns(invoices);

            // Setup query string
            var queryCollection = new QueryCollection(new Dictionary<string, StringValues> { { "sortBy", "movie_za" } });
            _controller.HttpContext.Request.Query = queryCollection;

            // Act
            var result = await _controller.LoadTab("BookingMg") as PartialViewResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal("BookingMg", result.ViewName);
            var resultInvoices = result.Model as List<Invoice>;
            Assert.NotNull(resultInvoices);
            Assert.Equal("Zebra", resultInvoices[0].MovieShow.Movie.MovieNameEnglish);
        }

        [Fact]
        public async Task LoadTab_BookingMg_WithSortByIdAsc_SortsCorrectly()
        {
            // Arrange
            var invoices = new List<Invoice>
            {
                new Invoice { InvoiceId = "INV2" },
                new Invoice { InvoiceId = "INV1" }
            };
            _invoiceServiceMock.Setup(x => x.GetAll()).Returns(invoices);

            // Setup query string
            var queryCollection = new QueryCollection(new Dictionary<string, StringValues> { { "sortBy", "id_asc" } });
            _controller.HttpContext.Request.Query = queryCollection;

            // Act
            var result = await _controller.LoadTab("BookingMg") as PartialViewResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal("BookingMg", result.ViewName);
            var resultInvoices = result.Model as List<Invoice>;
            Assert.NotNull(resultInvoices);
            Assert.Equal("INV1", resultInvoices[0].InvoiceId);
        }

        [Fact]
        public async Task LoadTab_BookingMg_WithSortByIdDesc_SortsCorrectly()
        {
            // Arrange
            var invoices = new List<Invoice>
            {
                new Invoice { InvoiceId = "INV1" },
                new Invoice { InvoiceId = "INV2" }
            };
            _invoiceServiceMock.Setup(x => x.GetAll()).Returns(invoices);

            // Setup query string
            var queryCollection = new QueryCollection(new Dictionary<string, StringValues> { { "sortBy", "id_desc" } });
            _controller.HttpContext.Request.Query = queryCollection;

            // Act
            var result = await _controller.LoadTab("BookingMg") as PartialViewResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal("BookingMg", result.ViewName);
            var resultInvoices = result.Model as List<Invoice>;
            Assert.NotNull(resultInvoices);
            Assert.Equal("INV2", resultInvoices[0].InvoiceId);
        }

        [Fact]
        public async Task LoadTab_BookingMg_WithSortByAccountAz_SortsCorrectly()
        {
            // Arrange
            var invoices = new List<Invoice>
            {
                new Invoice { AccountId = "USER2" },
                new Invoice { AccountId = "USER1" }
            };
            _invoiceServiceMock.Setup(x => x.GetAll()).Returns(invoices);

            // Setup query string
            var queryCollection = new QueryCollection(new Dictionary<string, StringValues> { { "sortBy", "account_az" } });
            _controller.HttpContext.Request.Query = queryCollection;

            // Act
            var result = await _controller.LoadTab("BookingMg") as PartialViewResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal("BookingMg", result.ViewName);
            var resultInvoices = result.Model as List<Invoice>;
            Assert.NotNull(resultInvoices);
            Assert.Equal("USER1", resultInvoices[0].AccountId);
        }

        [Fact]
        public async Task LoadTab_BookingMg_WithSortByAccountZa_SortsCorrectly()
        {
            // Arrange
            var invoices = new List<Invoice>
            {
                new Invoice { AccountId = "USER1" },
                new Invoice { AccountId = "USER2" }
            };
            _invoiceServiceMock.Setup(x => x.GetAll()).Returns(invoices);

            // Setup query string
            var queryCollection = new QueryCollection(new Dictionary<string, StringValues> { { "sortBy", "account_za" } });
            _controller.HttpContext.Request.Query = queryCollection;

            // Act
            var result = await _controller.LoadTab("BookingMg") as PartialViewResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal("BookingMg", result.ViewName);
            var resultInvoices = result.Model as List<Invoice>;
            Assert.NotNull(resultInvoices);
            Assert.Equal("USER2", resultInvoices[0].AccountId);
        }

        [Fact]
        public async Task LoadTab_BookingMg_WithSortByIdentityAz_SortsCorrectly()
        {
            // Arrange
            var invoices = new List<Invoice>
            {
                new Invoice { Account = new Account { IdentityCard = "ID2" } },
                new Invoice { Account = new Account { IdentityCard = "ID1" } }
            };
            _invoiceServiceMock.Setup(x => x.GetAll()).Returns(invoices);

            // Setup query string
            var queryCollection = new QueryCollection(new Dictionary<string, StringValues> { { "sortBy", "identity_az" } });
            _controller.HttpContext.Request.Query = queryCollection;

            // Act
            var result = await _controller.LoadTab("BookingMg") as PartialViewResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal("BookingMg", result.ViewName);
            var resultInvoices = result.Model as List<Invoice>;
            Assert.NotNull(resultInvoices);
            Assert.Equal("ID1", resultInvoices[0].Account.IdentityCard);
        }

        [Fact]
        public async Task LoadTab_BookingMg_WithSortByIdentityZa_SortsCorrectly()
        {
            // Arrange
            var invoices = new List<Invoice>
            {
                new Invoice { Account = new Account { IdentityCard = "ID1" } },
                new Invoice { Account = new Account { IdentityCard = "ID2" } }
            };
            _invoiceServiceMock.Setup(x => x.GetAll()).Returns(invoices);

            // Setup query string
            var queryCollection = new QueryCollection(new Dictionary<string, StringValues> { { "sortBy", "identity_za" } });
            _controller.HttpContext.Request.Query = queryCollection;

            // Act
            var result = await _controller.LoadTab("BookingMg") as PartialViewResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal("BookingMg", result.ViewName);
            var resultInvoices = result.Model as List<Invoice>;
            Assert.NotNull(resultInvoices);
            Assert.Equal("ID2", resultInvoices[0].Account.IdentityCard);
        }

        [Fact]
        public async Task LoadTab_BookingMg_WithSortByPhoneAz_SortsCorrectly()
        {
            // Arrange
            var invoices = new List<Invoice>
            {
                new Invoice { Account = new Account { PhoneNumber = "0987654321" } },
                new Invoice { Account = new Account { PhoneNumber = "0123456789" } }
            };
            _invoiceServiceMock.Setup(x => x.GetAll()).Returns(invoices);

            // Setup query string
            var queryCollection = new QueryCollection(new Dictionary<string, StringValues> { { "sortBy", "phone_az" } });
            _controller.HttpContext.Request.Query = queryCollection;

            // Act
            var result = await _controller.LoadTab("BookingMg") as PartialViewResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal("BookingMg", result.ViewName);
            var resultInvoices = result.Model as List<Invoice>;
            Assert.NotNull(resultInvoices);
            Assert.Equal("0123456789", resultInvoices[0].Account.PhoneNumber);
        }

        [Fact]
        public async Task LoadTab_BookingMg_WithSortByPhoneZa_SortsCorrectly()
        {
            // Arrange
            var invoices = new List<Invoice>
            {
                new Invoice { Account = new Account { PhoneNumber = "0123456789" } },
                new Invoice { Account = new Account { PhoneNumber = "0987654321" } }
            };
            _invoiceServiceMock.Setup(x => x.GetAll()).Returns(invoices);

            // Setup query string
            var queryCollection = new QueryCollection(new Dictionary<string, StringValues> { { "sortBy", "phone_za" } });
            _controller.HttpContext.Request.Query = queryCollection;

            // Act
            var result = await _controller.LoadTab("BookingMg") as PartialViewResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal("BookingMg", result.ViewName);
            var resultInvoices = result.Model as List<Invoice>;
            Assert.NotNull(resultInvoices);
            Assert.Equal("0987654321", resultInvoices[0].Account.PhoneNumber);
        }

        [Fact]
        public async Task LoadTab_BookingMg_WithSortByTimeAsc_SortsCorrectly()
        {
            // Arrange
            var invoices = new List<Invoice>
            {
                new Invoice { MovieShow = new MovieShow { Schedule = new Schedule { ScheduleTime = new TimeOnly(15, 0) } } },
                new Invoice { MovieShow = new MovieShow { Schedule = new Schedule { ScheduleTime = new TimeOnly(14, 0) } } }
            };
            _invoiceServiceMock.Setup(x => x.GetAll()).Returns(invoices);

            // Setup query string
            var queryCollection = new QueryCollection(new Dictionary<string, StringValues> { { "sortBy", "time_asc" } });
            _controller.HttpContext.Request.Query = queryCollection;

            // Act
            var result = await _controller.LoadTab("BookingMg") as PartialViewResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal("BookingMg", result.ViewName);
            var resultInvoices = result.Model as List<Invoice>;
            Assert.NotNull(resultInvoices);
            Assert.Equal(new TimeOnly(14, 0), resultInvoices[0].MovieShow.Schedule.ScheduleTime);
        }

        [Fact]
        public async Task LoadTab_BookingMg_WithSortByTimeDesc_SortsCorrectly()
        {
            // Arrange
            var invoices = new List<Invoice>
            {
                new Invoice { MovieShow = new MovieShow { Schedule = new Schedule { ScheduleTime = new TimeOnly(14, 0) } } },
                new Invoice { MovieShow = new MovieShow { Schedule = new Schedule { ScheduleTime = new TimeOnly(15, 0) } } }
            };
            _invoiceServiceMock.Setup(x => x.GetAll()).Returns(invoices);

            // Setup query string
            var queryCollection = new QueryCollection(new Dictionary<string, StringValues> { { "sortBy", "time_desc" } });
            _controller.HttpContext.Request.Query = queryCollection;

            // Act
            var result = await _controller.LoadTab("BookingMg") as PartialViewResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal("BookingMg", result.ViewName);
            var resultInvoices = result.Model as List<Invoice>;
            Assert.NotNull(resultInvoices);
            Assert.Equal(new TimeOnly(15, 0), resultInvoices[0].MovieShow.Schedule.ScheduleTime);
        }

        [Fact]
        public async Task LoadTab_BookingMg_WithNullAccount_HandlesGracefully()
        {
            // Arrange
            var invoices = new List<Invoice>
            {
                new Invoice { Account = null },
                new Invoice { Account = new Account { IdentityCard = "ID1" } }
            };
            _invoiceServiceMock.Setup(x => x.GetAll()).Returns(invoices);

            // Setup query string
            var queryCollection = new QueryCollection(new Dictionary<string, StringValues> { { "sortBy", "identity_az" } });
            _controller.HttpContext.Request.Query = queryCollection;

            // Act
            var result = await _controller.LoadTab("BookingMg") as PartialViewResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal("BookingMg", result.ViewName);
            var resultInvoices = result.Model as List<Invoice>;
            Assert.NotNull(resultInvoices);
            // Should handle null account gracefully
        }
        #endregion
        #endregion

        #region Details Tests
        [Fact]
        public void Details_ReturnsView()
        {
            // Act
            var result = _controller.Details(1) as ViewResult;

            // Assert
            Assert.NotNull(result);
        }
        #endregion

        #region Create Tests
        [Fact]
        public void Create_ReturnsView_WithRegisterViewModel()
        {
            // Act
            var result = _controller.Create() as ViewResult;

            // Assert
            Assert.NotNull(result);
            Assert.IsType<RegisterViewModel>(result.Model);
        }
        #endregion

        #region CreateAsync Tests
        [Fact]
        public async Task CreateAsync_InvalidModel_ReturnsView_WithErrors()
        {
            // Arrange
            var model = new RegisterViewModel();
            _controller.ModelState.AddModelError("Username", "Username is required");

            // Act
            var result = await _controller.CreateAsync(model) as ViewResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal(model, result.Model);
            Assert.Equal("Validation failed: Username is required", _controller.TempData["ErrorMessage"]);
        }

        [Fact]
        public async Task CreateAsync_ValidModel_WithoutImage_RegistersSuccessfully()
        {
            // Arrange
            var model = new RegisterViewModel
            {
                Username = "testuser",
                Password = "password123",
                FullName = "Test User",
                Email = "test@example.com",
                PhoneNumber = "1234567890"
            };
            _employeeServiceMock.Setup(s => s.Register(model)).Returns(true);

            // Act
            var result = await _controller.CreateAsync(model) as RedirectToActionResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal("MainPage", result.ActionName);
            Assert.Equal("Admin", result.ControllerName);
            Assert.Equal("EmployeeMg", result.RouteValues["tab"]);
            Assert.Equal("Employee Created Succesfully!", _controller.TempData["ToastMessage"]);
        }

        [Fact]
        public async Task CreateAsync_ValidModel_WithImage_RegistersSuccessfully()
        {
            // Arrange
            var model = new RegisterViewModel
            {
                Username = "testuser",
                Password = "password123",
                FullName = "Test User",
                Email = "test@example.com",
                PhoneNumber = "1234567890",
                ImageFile = CreateMockFormFile("test.jpg", "image/jpeg")
            };
            _employeeServiceMock.Setup(s => s.Register(model)).Returns(true);

            // Act
            var result = await _controller.CreateAsync(model) as RedirectToActionResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal("MainPage", result.ActionName);
            Assert.Equal("Admin", result.ControllerName);
            Assert.Equal("EmployeeMg", result.RouteValues["tab"]);
            Assert.Equal("Employee Created Succesfully!", _controller.TempData["ToastMessage"]);
        }

        [Fact]
        public async Task CreateAsync_RegistrationFails_ReturnsView_WithError()
        {
            // Arrange
            var model = new RegisterViewModel
            {
                Username = "existinguser",
                Password = "password123",
                FullName = "Test User",
                Email = "test@example.com",
                PhoneNumber = "1234567890"
            };
            _employeeServiceMock.Setup(s => s.Register(model)).Returns(false);

            // Act
            var result = await _controller.CreateAsync(model) as ViewResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal(model, result.Model);
            Assert.Equal("Registration failed - Username already exists", _controller.TempData["ErrorMessage"]);
        }

        [Fact]
        public async Task CreateAsync_ThrowsException_ReturnsView_WithError()
        {
            // Arrange
            var model = new RegisterViewModel
            {
                Username = "testuser",
                Password = "password123",
                FullName = "Test User",
                Email = "test@example.com",
                PhoneNumber = "1234567890"
            };
            _employeeServiceMock.Setup(s => s.Register(model)).Throws(new Exception("Test exception"));

            // Act
            var result = await _controller.CreateAsync(model) as ViewResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal(model, result.Model);
            Assert.Contains("Error during registration: Test exception", _controller.TempData["ErrorMessage"].ToString());
        }
        #endregion

        #region Edit Tests
        [Fact]
        public void Edit_EmployeeFound_ReturnsView_WithEmployeeEditViewModel()
        {
            // Arrange
            var employeeId = "EMP001";
            var employee = new Employee
            {
                EmployeeId = employeeId,
                AccountId = "ACC001",
                Status = true,
                Account = new Account
                {
                    Username = "testuser",
                    FullName = "Test User",
                    DateOfBirth = new DateOnly(1990, 1, 1),
                    Gender = "Male",
                    IdentityCard = "123456789",
                    Email = "test@example.com",
                    Address = "Test Address",
                    PhoneNumber = "1234567890",
                    Image = "/image/test.jpg"
                }
            };
            _employeeServiceMock.Setup(s => s.GetById(employeeId)).Returns(employee);

            // Act
            var result = _controller.Edit(employeeId) as ViewResult;

            // Assert
            Assert.NotNull(result);
            var model = result.Model as EmployeeEditViewModel;
            Assert.NotNull(model);
            Assert.Equal("testuser", model.Username);
            Assert.Equal("Test User", model.FullName);
            Assert.Equal("ACC001", model.AccountId);
            Assert.True(model.Status);
        }

        [Fact]
        public void Edit_EmployeeNotFound_ReturnsNotFound()
        {
            // Arrange
            var employeeId = "EMP999";
            _employeeServiceMock.Setup(s => s.GetById(employeeId)).Returns((Employee)null);

            // Act
            var result = _controller.Edit(employeeId);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }
        #endregion

        #region EditAsync Tests
        [Fact]
        public void EditAsync_InvalidModel_ReturnsView()
        {
            // Arrange
            var model = new EmployeeEditViewModel();
            _controller.ModelState.AddModelError("Username", "Username is required");

            // Act
            var result = _controller.EditAsync("EMP001", model) as ViewResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal(model, result.Model);
        }

        [Fact]
        public void EditAsync_EmployeeNotFound_ReturnsView_WithError()
        {
            // Arrange
            var model = new EmployeeEditViewModel { Username = "testuser" };
            _employeeServiceMock.Setup(s => s.GetById("EMP001")).Returns((Employee)null);

            // Act
            var result = _controller.EditAsync("EMP001", model) as ViewResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal(model, result.Model);
            Assert.Equal("Employee not found.", _controller.TempData["ErrorMessage"]);
        }

        [Fact]
        public void EditAsync_StatusChanged_TogglesStatus()
        {
            // Arrange
            var employeeId = "EMP001";
            var employee = new Employee
            {
                EmployeeId = employeeId,
                Status = true,
                Account = new Account { Username = "testuser", Password = "oldpassword" }
            };
            var model = new EmployeeEditViewModel
            {
                Username = "testuser",
                Status = false,
                AccountId = "ACC001"
            };
            _employeeServiceMock.Setup(s => s.GetById(employeeId)).Returns(employee);
            _employeeServiceMock.Setup(s => s.Update(employeeId, It.IsAny<RegisterViewModel>())).Returns(true);

            // Act
            var result = _controller.EditAsync(employeeId, model) as RedirectToActionResult;

            // Assert
            Assert.NotNull(result);
            _employeeServiceMock.Verify(s => s.ToggleStatus(employeeId), Times.Once);
        }

        [Fact]
        public void EditAsync_PasswordMismatch_ReturnsView_WithError()
        {
            // Arrange
            var employeeId = "EMP001";
            var employee = new Employee
            {
                EmployeeId = employeeId,
                Status = true,
                Account = new Account { Username = "testuser", Password = "oldpassword" }
            };
            var model = new EmployeeEditViewModel
            {
                Username = "testuser",
                Password = "newpassword",
                ConfirmPassword = "differentpassword",
                AccountId = "ACC001"
            };
            _employeeServiceMock.Setup(s => s.GetById(employeeId)).Returns(employee);

            // Act
            var result = _controller.EditAsync(employeeId, model) as ViewResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal(model, result.Model);
            Assert.Equal("Password and Confirm Password do not match", _controller.TempData["ErrorMessage"]);
        }

        [Fact]
        public void EditAsync_PasswordUpdateFails_ReturnsView_WithError()
        {
            // Arrange
            var employeeId = "EMP001";
            var employee = new Employee
            {
                EmployeeId = employeeId,
                Status = true,
                Account = new Account { Username = "testuser", Password = "oldpassword" }
            };
            var model = new EmployeeEditViewModel
            {
                Username = "testuser",
                Password = "newpassword",
                ConfirmPassword = "newpassword",
                AccountId = "ACC001"
            };
            _employeeServiceMock.Setup(s => s.GetById(employeeId)).Returns(employee);
            _accountServiceMock.Setup(s => s.UpdatePasswordByUsername("testuser", "newpassword")).Returns(false);

            // Act
            var result = _controller.EditAsync(employeeId, model) as ViewResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal(model, result.Model);
            Assert.Equal("Failed to update password", _controller.TempData["ErrorMessage"]);
        }

        [Fact]
        public void EditAsync_UpdateFails_RedirectsWithError()
        {
            // Arrange
            var employeeId = "EMP001";
            var employee = new Employee
            {
                EmployeeId = employeeId,
                Status = true,
                Account = new Account { Username = "testuser", Password = "oldpassword" }
            };
            var model = new EmployeeEditViewModel
            {
                Username = "testuser",
                AccountId = "ACC001"
            };
            _employeeServiceMock.Setup(s => s.GetById(employeeId)).Returns(employee);
            _employeeServiceMock.Setup(s => s.Update(employeeId, It.IsAny<RegisterViewModel>())).Returns(false);

            // Act
            var result = _controller.EditAsync(employeeId, model) as RedirectToActionResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal("MainPage", result.ActionName);
            Assert.Equal("Admin", result.ControllerName);
            Assert.Equal("EmployeeMg", result.RouteValues["tab"]);
            Assert.Equal("Update failed - Username already exists", _controller.TempData["ErrorMessage"]);
        }

        [Fact]
        public void EditAsync_UpdateSuccess_RedirectsWithSuccess()
        {
            // Arrange
            var employeeId = "EMP001";
            var employee = new Employee
            {
                EmployeeId = employeeId,
                Status = true,
                Account = new Account { Username = "testuser", Password = "oldpassword" }
            };
            var model = new EmployeeEditViewModel
            {
                Username = "testuser",
                AccountId = "ACC001"
            };
            _employeeServiceMock.Setup(s => s.GetById(employeeId)).Returns(employee);
            _employeeServiceMock.Setup(s => s.Update(employeeId, It.IsAny<RegisterViewModel>())).Returns(true);

            // Act
            var result = _controller.EditAsync(employeeId, model) as RedirectToActionResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal("MainPage", result.ActionName);
            Assert.Equal("Admin", result.ControllerName);
            Assert.Equal("EmployeeMg", result.RouteValues["tab"]);
            Assert.Equal("Employee Updated Successfully!", _controller.TempData["ToastMessage"]);
        }
        #endregion

        #region Delete Tests
        [Fact]
        public void Delete_Get_ReturnsView_WhenEmployeeFound()
        {
            // Arrange
            var employeeId = "EMP001";
            var mockEmployee = new Employee { EmployeeId = employeeId };
            _employeeServiceMock.Setup(s => s.GetById(employeeId)).Returns(mockEmployee);

            // Act
            var result = _controller.Delete(employeeId) as ViewResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal(mockEmployee, result.Model);
        }

        [Fact]
        public void Delete_Get_Redirects_WhenEmployeeNotFound()
        {
            // Arrange
            var employeeId = "EMP999";
            _employeeServiceMock.Setup(s => s.GetById(employeeId)).Returns((Employee)null);

            // Act
            var result = _controller.Delete(employeeId) as RedirectToActionResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal("MainPage", result.ActionName);
            Assert.Equal("Admin", result.ControllerName);
            Assert.Equal("EmployeeMg", result.RouteValues["tab"]);
        }

        [Fact]
        public void Delete_Post_DeletesAndRedirects_WhenSuccessful()
        {
            // Arrange
            var employeeId = "EMP001";
            var mockEmployee = new Employee { EmployeeId = employeeId };
            _employeeServiceMock.Setup(s => s.GetById(employeeId)).Returns(mockEmployee);
            _employeeServiceMock.Setup(s => s.Delete(employeeId)).Returns(true);

            // Act
            var result = _controller.Delete(employeeId, new FormCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>())) as RedirectToActionResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal("MainPage", result.ActionName);
            Assert.Equal("Admin", result.ControllerName);
            Assert.Equal("EmployeeMg", result.RouteValues["tab"]);
            Assert.Equal("Employee deleted successfully!", _controller.TempData["ToastMessage"]);
        }

        [Fact]
        public void Delete_Post_ReturnsError_WhenDeleteFails()
        {
            // Arrange
            var employeeId = "EMP001";
            var mockEmployee = new Employee { EmployeeId = employeeId };
            _employeeServiceMock.Setup(s => s.GetById(employeeId)).Returns(mockEmployee);
            _employeeServiceMock.Setup(s => s.Delete(employeeId)).Returns(false);

            // Act
            var result = _controller.Delete(employeeId, new FormCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>())) as RedirectToActionResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal("MainPage", result.ActionName);
            Assert.Equal("Admin", result.ControllerName);
            Assert.Equal("Failed to delete employee.", _controller.TempData["ToastMessage"]);
        }

        [Fact]
        public void Delete_Post_InvalidId_RedirectsWithError()
        {
            // Act
            var result = _controller.Delete("", new FormCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>())) as RedirectToActionResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal("MainPage", result.ActionName);
            Assert.Equal("Admin", result.ControllerName);
            Assert.Equal("Invalid employee ID.", _controller.TempData["ToastMessage"]);
        }

        [Fact]
        public void Delete_Post_EmployeeNotFound_RedirectsWithError()
        {
            // Arrange
            var employeeId = "EMP999";
            _employeeServiceMock.Setup(s => s.GetById(employeeId)).Returns((Employee)null);

            // Act
            var result = _controller.Delete(employeeId, new FormCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>())) as RedirectToActionResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal("MainPage", result.ActionName);
            Assert.Equal("Admin", result.ControllerName);
            Assert.Equal("Employee not found.", _controller.TempData["ToastMessage"]);
        }

        [Fact]
        public void Delete_Post_ThrowsException_RedirectsWithError()
        {
            // Arrange
            var employeeId = "EMP001";
            var mockEmployee = new Employee { EmployeeId = employeeId };
            _employeeServiceMock.Setup(s => s.GetById(employeeId)).Returns(mockEmployee);
            _employeeServiceMock.Setup(s => s.Delete(employeeId)).Throws(new Exception("Test exception"));

            // Act
            var result = _controller.Delete(employeeId, new FormCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>())) as RedirectToActionResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal("MainPage", result.ActionName);
            Assert.Equal("Admin", result.ControllerName);
            Assert.Contains("An error occurred during deletion: Test exception", _controller.TempData["ToastMessage"].ToString());
        }
        #endregion

        #region ToggleStatus Tests
        [Fact]
        public void ToggleStatus_ValidId_UpdatesStatus()
        {
            // Arrange
            var id = "EMP001";
            var employee = new Employee { EmployeeId = id };
            _employeeServiceMock.Setup(s => s.GetById(id)).Returns(employee);

            // Act
            var result = _controller.ToggleStatus(id) as RedirectToActionResult;

            // Assert
            _employeeServiceMock.Verify(s => s.ToggleStatus(id), Times.Once);
            Assert.NotNull(result);
            Assert.Equal("MainPage", result.ActionName);
            Assert.Equal("Admin", result.ControllerName);
            Assert.Equal("EmployeeMg", result.RouteValues["tab"]);
            Assert.Equal("Employee status updated successfully!", _controller.TempData["ToastMessage"]);
        }

        [Fact]
        public void ToggleStatus_InvalidId_RedirectsWithError()
        {
            // Act
            var result = _controller.ToggleStatus(null) as RedirectToActionResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal("MainPage", result.ActionName);
            Assert.Equal("Admin", result.ControllerName);
            Assert.Equal("EmployeeMg", result.RouteValues["tab"]);
            Assert.Equal("Invalid employee ID.", _controller.TempData["ErrorMessage"]);
            _employeeServiceMock.Verify(s => s.ToggleStatus(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public void ToggleStatus_EmployeeNotFound_RedirectsWithError()
        {
            // Arrange
            var id = "EMP999";
            _employeeServiceMock.Setup(s => s.GetById(id)).Returns((Employee)null);

            // Act
            var result = _controller.ToggleStatus(id) as RedirectToActionResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal("MainPage", result.ActionName);
            Assert.Equal("Admin", result.ControllerName);
            Assert.Equal("EmployeeMg", result.RouteValues["tab"]);
            Assert.Equal("Employee not found.", _controller.TempData["ErrorMessage"]);
        }

        [Fact]
        public void ToggleStatus_ThrowsArgumentException_RedirectsWithError()
        {
            // Arrange
            var id = "EMP001";
            var employee = new Employee { EmployeeId = id };
            _employeeServiceMock.Setup(s => s.GetById(id)).Returns(employee);
            _employeeServiceMock.Setup(s => s.ToggleStatus(id)).Throws(new ArgumentException("Test argument exception"));

            // Act
            var result = _controller.ToggleStatus(id) as RedirectToActionResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal("MainPage", result.ActionName);
            Assert.Equal("Admin", result.ControllerName);
            Assert.Equal("EmployeeMg", result.RouteValues["tab"]);
            Assert.Equal("Test argument exception", _controller.TempData["ErrorMessage"]);
        }

        [Fact]
        public void ToggleStatus_ThrowsException_RedirectsWithError()
        {
            // Arrange
            var id = "EMP001";
            var employee = new Employee { EmployeeId = id };
            _employeeServiceMock.Setup(s => s.GetById(id)).Returns(employee);
            _employeeServiceMock.Setup(s => s.ToggleStatus(id)).Throws(new Exception("Test exception"));

            // Act
            var result = _controller.ToggleStatus(id) as RedirectToActionResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal("MainPage", result.ActionName);
            Assert.Equal("Admin", result.ControllerName);
            Assert.Equal("EmployeeMg", result.RouteValues["tab"]);
            Assert.Contains("An unexpected error occurred: Test exception", _controller.TempData["ErrorMessage"].ToString());
        }
        #endregion

        #region Helper Methods
        private IFormFile CreateMockFormFile(string fileName, string contentType)
        {
            var content = "test content";
            var bytes = Encoding.UTF8.GetBytes(content);
            var stream = new MemoryStream(bytes);
            return new FormFile(stream, 0, bytes.Length, "ImageFile", fileName)
            {
                Headers = new HeaderDictionary(),
                ContentType = contentType
            };
        }
        #endregion
    }
}
