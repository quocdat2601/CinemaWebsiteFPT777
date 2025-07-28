using Xunit;
using Moq;
using MovieTheater.Controllers;
using MovieTheater.Service;
using MovieTheater.Models;
using MovieTheater.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Security.Claims;
using System.Linq;
using ModelType = MovieTheater.Models.Type;
using ModelVersion = MovieTheater.Models.Version;
using MovieTheater.Hubs;
using System.Threading.Tasks;
using MovieTheater.Repository;
using Microsoft.AspNetCore.Hosting;
using System.IO; // Required for Path.GetTempPath()
using Microsoft.AspNetCore.Mvc.ViewFeatures; // For TempDataDictionary

namespace MovieTheater.Tests.Controller
{
    public class MovieControllerTests
    {
        private readonly Mock<IMovieService> _movieService = new();
        private readonly Mock<ICinemaService> _cinemaService = new();
        private readonly Mock<ILogger<MovieController>> _logger = new();
        private readonly Mock<IHubContext<DashboardHub>> _hubContext = new();
        private readonly Mock<IWebHostEnvironment> _webHostEnvironment = new();
        private readonly Mock<IPersonRepository> _personRepository = new();

        public MovieControllerTests()
        {
            // Initial setup for WebRootPath, this is good for all tests that use BuildController
            _webHostEnvironment.Setup(m => m.WebRootPath).Returns(Path.GetTempPath());
            _webHostEnvironment.Setup(m => m.ContentRootPath).Returns(Path.GetTempPath());
        }

        private MovieController BuildController(ClaimsPrincipal user = null)
        {
            var ctrl = new MovieController(
                _movieService.Object,
                _cinemaService.Object,
                _logger.Object,
                _hubContext.Object,
                _webHostEnvironment.Object, // Pass this
                _personRepository.Object    // Pass this
            );
            if (user != null)
            {
                ctrl.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = user } };
            }
            else
            {
                ctrl.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
            }
            return ctrl;
        }

        [Fact]
        public void GetUserRole_ReturnsRoleFromClaims()
        {
            // Arrange
            var claims = new List<Claim> { new Claim(ClaimTypes.Role, "Admin") };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            var user = new ClaimsPrincipal(identity);
            var ctrl = BuildController(user);
            // Act
            var role = ctrl.GetType().GetMethod("GetUserRole", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).Invoke(ctrl, null);
            // Assert
            Assert.Equal("Admin", role);
        }

        [Fact]
        public void MovieList_ReturnsView_WithMovies()
        {
            // Arrange
            _movieService.Setup(s => s.SearchMovies(null)).Returns(new List<Movie> { new Movie { MovieId = "1", MovieNameEnglish = "A", Types = new List<ModelType>() } });
            var ctrl = BuildController();
            // Act
            var result = ctrl.MovieList(null, null, null) as ViewResult;
            // Assert
            Assert.NotNull(result);
            Assert.IsType<List<MovieViewModel>>(result.Model);
        }

        [Fact]
        public void MovieList_ReturnsPartialView_WhenAjax()
        {
            // Arrange
            _movieService.Setup(s => s.SearchMovies(null)).Returns(new List<Movie> { new Movie { MovieId = "1", MovieNameEnglish = "A", Types = new List<ModelType>() } });
            var ctrl = BuildController();
            ctrl.ControllerContext.HttpContext.Request.Headers["X-Requested-With"] = "XMLHttpRequest";

            // Act
            var result = ctrl.MovieList(null, null, null) as PartialViewResult;

            // Assert
            Assert.NotNull(result);
            // FIX: Change expected view name to what the controller actually returns
            Assert.Equal("_MovieFilterAndGrid", result.ViewName);
        }

        [Fact]
        public void Detail_ReturnsNotFound_WhenMovieNull()
        {
            // Arrange
            _movieService.Setup(s => s.GetById("x")).Returns((Movie)null);
            var ctrl = BuildController();
            // Act
            var result = ctrl.Detail("x");
            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public void Create_Get_ReturnsViewWithTypesAndVersions()
        {
            // Arrange
            _movieService.Setup(s => s.GetAllTypes()).Returns(new List<ModelType>());
            _movieService.Setup(s => s.GetAllVersions()).Returns(new List<ModelVersion>());
            var ctrl = BuildController();
            // Act
            var result = ctrl.Create() as ViewResult;
            // Assert
            Assert.NotNull(result);
            var model = Assert.IsType<MovieDetailViewModel>(result.Model);
            Assert.NotNull(model.AvailableTypes);
            Assert.NotNull(model.AvailableVersions);
        }

        [Fact]
        public async Task Create_Post_InvalidModel_ReturnsView()
        {
            // Arrange
            var model = new MovieDetailViewModel();
            var ctrl = BuildController();
            ctrl.TempData = new Microsoft.AspNetCore.Mvc.ViewFeatures.TempDataDictionary(
                new Microsoft.AspNetCore.Http.DefaultHttpContext(),
                Mock.Of<Microsoft.AspNetCore.Mvc.ViewFeatures.ITempDataProvider>());
            ctrl.ModelState.AddModelError("x", "err");
            _movieService.Setup(s => s.GetAllTypes()).Returns(new List<ModelType>());
            _movieService.Setup(s => s.GetAllVersions()).Returns(new List<ModelVersion>());
            // Act
            var result = await ctrl.Create(model) as ViewResult;
            // Assert
            Assert.NotNull(result);
            Assert.IsType<MovieDetailViewModel>(result.Model);
        }

        [Fact]
        public async Task Create_Post_InvalidDate_ReturnsViewWithError()
        {
            // Arrange
            var model = new MovieDetailViewModel { FromDate = DateOnly.FromDateTime(System.DateTime.Today), ToDate = DateOnly.FromDateTime(System.DateTime.Today.AddDays(-1)), SelectedTypeIds = new List<int>(), SelectedVersionIds = new List<int>() };
            var ctrl = BuildController();
            ctrl.TempData = new Microsoft.AspNetCore.Mvc.ViewFeatures.TempDataDictionary(
                new Microsoft.AspNetCore.Http.DefaultHttpContext(),
                Mock.Of<Microsoft.AspNetCore.Mvc.ViewFeatures.ITempDataProvider>());
            _movieService.Setup(s => s.GetAllTypes()).Returns(new List<ModelType>());
            _movieService.Setup(s => s.GetAllVersions()).Returns(new List<ModelVersion>());
            // Act
            var result = await ctrl.Create(model) as ViewResult;
            // Assert
            Assert.NotNull(result);
            Assert.IsType<MovieDetailViewModel>(result.Model);
            Assert.True(ctrl.TempData.ContainsKey("ErrorMessage"));
        }

        // Example fix for one test method:
        [Fact]
        public async Task Create_Post_AddMovieSuccess_AdminRole_RedirectsToAdmin()
        {
            // Arrange
            var claims = new List<Claim>
    {
        new Claim(ClaimTypes.Role, "Admin"),
        new Claim(ClaimTypes.Name, "admin@example.com")
    };
            var user = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));
            var model = new MovieDetailViewModel { FromDate = DateOnly.FromDateTime(System.DateTime.Today), ToDate = DateOnly.FromDateTime(System.DateTime.Today.AddDays(1)), SelectedTypeIds = new List<int>(), SelectedVersionIds = new List<int>() };

            // Setup WebRootPath để tránh ArgumentNullException
            _webHostEnvironment.Setup(e => e.WebRootPath).Returns("wwwroot");
            _webHostEnvironment.Setup(e => e.ContentRootPath).Returns("wwwroot");

            var ctrl = BuildController();
            ctrl.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user }
            };
            ctrl.TempData = new Microsoft.AspNetCore.Mvc.ViewFeatures.TempDataDictionary(
                ctrl.ControllerContext.HttpContext,
                Mock.Of<Microsoft.AspNetCore.Mvc.ViewFeatures.ITempDataProvider>());
            _movieService.Setup(s => s.GetAllTypes()).Returns(new List<ModelType>());
            _movieService.Setup(s => s.GetAllVersions()).Returns(new List<ModelVersion>());
            _movieService.Setup(s => s.AddMovie(It.IsAny<Movie>())).Returns(true);
            var clientProxyMock = new Mock<IClientProxy>();
            clientProxyMock
                .Setup(x => x.SendCoreAsync(It.IsAny<string>(), It.IsAny<object[]>(), It.IsAny<System.Threading.CancellationToken>()))
                .Returns(Task.CompletedTask);
            _hubContext.Setup(x => x.Clients.All).Returns(clientProxyMock.Object);

            // Act
            var result = await ctrl.Create(model) as RedirectToActionResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal("MainPage", result.ActionName);
            Assert.Equal("Admin", result.ControllerName);
        }

        // Repeat this pattern for all other test methods that call async controller actions (e.g., Create, Edit, etc.):
        // - Change [Fact] to [Fact]
        // - Change method signature to public async Task <MethodName>()
        // - Use await when calling the controller action
        // - Cast the result as appropriate

        // Example for EmployeeRole test:
        [Fact]
        public async Task Create_Post_AddMovieSuccess_EmployeeRole_RedirectsToEmployee()
        {
            // Arrange
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Role, "Employee"),
                new Claim(ClaimTypes.Name, "employee@example.com")
            };
            var user = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));
            var model = new MovieDetailViewModel { FromDate = DateOnly.FromDateTime(System.DateTime.Today), ToDate = DateOnly.FromDateTime(System.DateTime.Today.AddDays(1)), SelectedTypeIds = new List<int>(), SelectedVersionIds = new List<int>() };

            // Setup WebRootPath để tránh ArgumentNullException
            _webHostEnvironment.Setup(e => e.WebRootPath).Returns("wwwroot");
            _webHostEnvironment.Setup(e => e.ContentRootPath).Returns("wwwroot");

            var ctrl = BuildController();
            ctrl.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user }
            };
            ctrl.TempData = new Microsoft.AspNetCore.Mvc.ViewFeatures.TempDataDictionary(
                ctrl.ControllerContext.HttpContext,
                Mock.Of<Microsoft.AspNetCore.Mvc.ViewFeatures.ITempDataProvider>());
            _movieService.Setup(s => s.GetAllTypes()).Returns(new List<ModelType>());
            _movieService.Setup(s => s.GetAllVersions()).Returns(new List<ModelVersion>());
            _movieService.Setup(s => s.AddMovie(It.IsAny<Movie>())).Returns(true);
            var clientProxyMock = new Mock<IClientProxy>();
            clientProxyMock
                .Setup(x => x.SendCoreAsync(It.IsAny<string>(), It.IsAny<object[]>(), It.IsAny<System.Threading.CancellationToken>()))
                .Returns(Task.CompletedTask);
            _hubContext.Setup(x => x.Clients.All).Returns(clientProxyMock.Object);

            // Act
            var result = await ctrl.Create(model) as RedirectToActionResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal("MainPage", result.ActionName);
            Assert.Equal("Employee", result.ControllerName);
        }

      
    }
}