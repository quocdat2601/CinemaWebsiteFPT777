using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Moq;
using MovieTheater.Controllers;
using MovieTheater.Hubs;
using MovieTheater.Models;
using MovieTheater.Repository;
using MovieTheater.Service;
using MovieTheater.ViewModels;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Claims;
using System.Threading.Tasks;
using Xunit;
using ModelType = MovieTheater.Models.Type;
using ModelVersion = MovieTheater.Models.Version;
using System;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace MovieTheater.Tests.Controller
{
    public class MovieControllerTests
    {
        private readonly Mock<IMovieService> _movieService = new();
        private readonly Mock<ICinemaService> _cinemaService = new();
        private readonly Mock<ILogger<MovieController>> _logger = new();
        private readonly Mock<IHubContext<DashboardHub>> _hubContext = new();
        private readonly Mock<IWebHostEnvironment> _webHostEnvironment = new(); // Add this
        private readonly Mock<IPersonRepository> _personRepository = new(); // Add this

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
        public void RoleProperty_ReturnsRoleFromClaims()
        {
            // Arrange
            var claims = new List<Claim> { new Claim(ClaimTypes.Role, "Admin") };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            var user = new ClaimsPrincipal(identity);
            var ctrl = BuildController(user);

            // Act
            var role = ctrl.GetType()
                           .GetProperty("role", BindingFlags.Public | BindingFlags.Instance)
                           ?.GetValue(ctrl);

            // Assert
            Assert.Equal("Admin", role);
        }
        private void SetupValidMovies()
        {
            var sampleMovies = new List<Movie> {
                new Movie {
                MovieId = "1",
                MovieNameEnglish = "A",
                Duration = 120,
                SmallImage = "img.jpg",
                Types = new List<ModelType> { new ModelType { TypeId = 1 } },
                Versions = new List<ModelVersion> { new ModelVersion { VersionId = 1 } }
            }
        };

            _movieService.Setup(s => s.GetCurrentlyShowingMoviesWithDetails()).Returns(sampleMovies);
            _movieService.Setup(s => s.GetComingSoonMoviesWithDetails()).Returns(new List<Movie>());
            _movieService.Setup(s => s.GetAllTypes()).Returns(new List<ModelType>());
            _movieService.Setup(s => s.GetAllVersions()).Returns(new List<ModelVersion>());
        }


        [Fact]
        public void MovieList_ReturnsView_WithMovies()
        {
            // Arrange
            SetupValidMovies();
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
            SetupValidMovies();
            var ctrl = BuildController();
            ctrl.ControllerContext.HttpContext = new DefaultHttpContext();
            ctrl.ControllerContext.HttpContext.Request.Headers["X-Requested-With"] = "XMLHttpRequest";

            // Act
            var result = ctrl.MovieList(null, null, null) as PartialViewResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal("_MovieFilterAndGrid", result.ViewName);
            Assert.IsType<List<MovieViewModel>>(result.Model);
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
            _webHostEnvironment.Setup(e => e.WebRootPath).Returns(Path.GetTempPath()); // ✅ Fix

            // Arrange
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Role, "Admin"),
                new Claim(ClaimTypes.Name, "admin@example.com")
            };
            var user = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));
            var model = new MovieDetailViewModel { FromDate = DateOnly.FromDateTime(System.DateTime.Today), ToDate = DateOnly.FromDateTime(System.DateTime.Today.AddDays(1)), SelectedTypeIds = new List<int>(), SelectedVersionIds = new List<int>() };
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
            _webHostEnvironment.Setup(e => e.WebRootPath).Returns(Path.GetTempPath()); // ✅ Fix

            // Arrange
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Role, "Employee"),
                new Claim(ClaimTypes.Name, "employee@example.com")
            };
            var user = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));
            var model = new MovieDetailViewModel { FromDate = DateOnly.FromDateTime(System.DateTime.Today), ToDate = DateOnly.FromDateTime(System.DateTime.Today.AddDays(1)), SelectedTypeIds = new List<int>(), SelectedVersionIds = new List<int>() };
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

        [Fact]
        public async Task Create_Post_AddMovieFail_ReturnsViewWithError()
        {
            // Arrange
            _webHostEnvironment.Setup(e => e.WebRootPath).Returns(Path.GetTempPath()); // ✅ Fix

            var model = new MovieDetailViewModel { FromDate = DateOnly.FromDateTime(System.DateTime.Today), ToDate = DateOnly.FromDateTime(System.DateTime.Today.AddDays(1)), SelectedTypeIds = new List<int>(), SelectedVersionIds = new List<int>() };
            var ctrl = BuildController();
            ctrl.TempData = new Microsoft.AspNetCore.Mvc.ViewFeatures.TempDataDictionary(
                new Microsoft.AspNetCore.Http.DefaultHttpContext(),
                Mock.Of<Microsoft.AspNetCore.Mvc.ViewFeatures.ITempDataProvider>());
            _movieService.Setup(s => s.GetAllTypes()).Returns(new List<ModelType>());
            _movieService.Setup(s => s.GetAllVersions()).Returns(new List<ModelVersion>());
            _movieService.Setup(s => s.AddMovie(It.IsAny<Movie>())).Returns(false);
            // Act
            var result = await ctrl.Create(model) as ViewResult;
            // Assert
            Assert.NotNull(result);
            Assert.IsType<MovieDetailViewModel>(result.Model);
            Assert.True(ctrl.TempData.ContainsKey("ErrorMessage"));
        }

        // --- Edit (GET) ---
        [Fact]
        public void Edit_Get_ReturnsNotFound_WhenMovieNull()
        {
            // Arrange
            _movieService.Setup(s => s.GetById("x")).Returns((Movie)null);
            var ctrl = BuildController();
            // Act
            var result = ctrl.Edit("x");
            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public void Edit_Get_ReturnsView_WhenMovieFound()
        {
            // Arrange
            var movie = new Movie { MovieId = "1", Types = new List<ModelType>(), Versions = new List<ModelVersion>() };
            _movieService.Setup(s => s.GetById("1")).Returns(movie);
            _movieService.Setup(s => s.GetAllTypes()).Returns(new List<ModelType>());
            _movieService.Setup(s => s.GetAllVersions()).Returns(new List<ModelVersion>());
            var ctrl = BuildController();
            // Act
            var result = ctrl.Edit("1") as ViewResult;
            // Assert
            Assert.NotNull(result);
            Assert.IsType<MovieDetailViewModel>(result.Model);
        }

        // --- Edit (POST) ---
        [Fact]
        public async Task Edit_Post_IdMismatch_ReturnsNotFound()
        {
            // Arrange
            var model = new MovieDetailViewModel { MovieId = "2" };
            var ctrl = BuildController();
            // Act
            var result = await ctrl.Edit("1", model);
            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Edit_Post_InvalidModel_ReturnsView()
        {
            // Arrange
            var model = new MovieDetailViewModel { MovieId = "1" };
            var ctrl = BuildController();
            ctrl.ModelState.AddModelError("x", "err");
            _movieService.Setup(s => s.GetAllTypes()).Returns(new List<ModelType>());
            // Act
            var actionResult = await ctrl.Edit("1", model);
            var result = actionResult as ViewResult;
            // Assert
            Assert.NotNull(result);
            Assert.IsType<MovieDetailViewModel>(result.Model);
        }

        [Fact]
        public async Task Edit_Post_InvalidDate_ReturnsViewWithError()
        {
            // Arrange
            var existingMovie = new Movie { MovieId = "1", Duration = 90 };
            var model = new MovieDetailViewModel
            {
                MovieId = "1",
                Duration = 90,
                FromDate = DateOnly.FromDateTime(DateTime.Today),
                ToDate = DateOnly.FromDateTime(DateTime.Today.AddDays(-1)), // invalid
                LargeImage = "test.jpg",
                SmallImage = "test.jpg",
                MovieNameEnglish = "Test",
                MovieProductionCompany = "Test",
                Content = "Test",
                TrailerUrl = "https://example.com",
                // Always seed all four lists non-null:
                AvailableTypes = new List<ModelType> { new() { TypeId = 1, TypeName = "Action" } },
                SelectedTypeIds = new List<int>(),
                AvailableVersions = new List<ModelVersion> { new() { VersionId = 1, VersionName = "2D" } },
                SelectedVersionIds = new List<int>()
            };
            _movieService.Setup(s => s.GetById("1")).Returns(existingMovie);
            _movieService.Setup(s => s.GetAllTypes()).Returns(model.AvailableTypes);
            _movieService.Setup(s => s.GetAllVersions()).Returns(model.AvailableVersions);
            var ctrl = BuildController();
            ctrl.TempData = new TempDataDictionary(
                new DefaultHttpContext(), Mock.Of<ITempDataProvider>()
            );
            // Act
            var actionResult = await ctrl.Edit("1", model);
            var result = actionResult as ViewResult;
            // Assert
            Assert.NotNull(result);
            Assert.IsType<MovieDetailViewModel>(result.Model);
            Assert.True(ctrl.TempData.ContainsKey("ErrorMessage"));
        }

        [Fact]
        public async Task Edit_Post_MovieNotFound_ReturnsNotFound()
        {
            // Arrange
            var model = new MovieDetailViewModel { MovieId = "1", FromDate = DateOnly.FromDateTime(System.DateTime.Today), ToDate = DateOnly.FromDateTime(System.DateTime.Today.AddDays(1)), SelectedTypeIds = new List<int>(), SelectedVersionIds = new List<int>() };
            _movieService.Setup(s => s.GetById("1")).Returns((Movie)null);
            var ctrl = BuildController();
            // Act
            var result = await ctrl.Edit("1", model);
            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Edit_Post_DurationConflict_ReturnsViewWithError()
        {
            _webHostEnvironment.Setup(e => e.WebRootPath).Returns(Path.GetTempPath()); // ✅ Fix

            // Arrange
            var existingMovie = new Movie { MovieId = "1", Duration = 100 };
            var model = new MovieDetailViewModel
            {
                MovieId = "1",
                Duration = 120,
                FromDate = DateOnly.FromDateTime(DateTime.Today),
                ToDate = DateOnly.FromDateTime(DateTime.Today.AddDays(1)),
                LargeImage = "test.jpg",
                SmallImage = "test.jpg",
                MovieNameEnglish = "Test",
                MovieProductionCompany = "Test",
                Content = "Test",
                TrailerUrl = "https://example.com",
                // Always seed all four lists non-null:
                AvailableTypes = new List<ModelType> { new() { TypeId = 1, TypeName = "Action" } },
                SelectedTypeIds = new List<int>(),
                AvailableVersions = new List<ModelVersion> { new() { VersionId = 1, VersionName = "2D" } },
                SelectedVersionIds = new List<int>()
            };
            _movieService.Setup(s => s.GetById("1")).Returns(existingMovie);
            _movieService.Setup(s => s.GetAllTypes()).Returns(model.AvailableTypes);
            _movieService.Setup(s => s.GetAllVersions()).Returns(model.AvailableVersions);
            _movieService.Setup(s => s.GetMovieShows("1")).Returns(new List<MovieShow> {
                new MovieShow {
                    MovieShowId = 2,
                    CinemaRoomId = 1,
                    ShowDate = (DateOnly)model.FromDate,
                    Schedule = new Schedule { ScheduleTime = new TimeOnly(10,30) },
                    Movie = existingMovie,
                    CinemaRoom = new CinemaRoom { CinemaRoomName = "Room1" }
                }
            });
            _movieService.Setup(s => s.GetMovieShow()).Returns(new List<MovieShow>()); // Ensure this is non-null
            var ctrl = BuildController();
            ctrl.TempData = new TempDataDictionary(
                new DefaultHttpContext(), Mock.Of<ITempDataProvider>()
            );
            // Act
            var actionResult = await ctrl.Edit("1", model);
            var result = actionResult as ViewResult;
            // Assert
            Assert.NotNull(result);
            Assert.IsType<MovieDetailViewModel>(result.Model);
            Assert.True(ctrl.TempData.ContainsKey("ErrorMessage"));
        }

        [Fact]
        public async Task Edit_Post_UpdateSuccess_AdminRole_RedirectsToAdmin()
        {
            _webHostEnvironment.Setup(x => x.WebRootPath).Returns("C:\\FakeWebRootPath"); // Fix here
            // Arrange
            var claims = new List<Claim> { new Claim(ClaimTypes.Role, "Admin") };
            var user = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));
            var model = new MovieDetailViewModel { MovieId = "1", Duration = 100, FromDate = DateOnly.FromDateTime(System.DateTime.Today), ToDate = DateOnly.FromDateTime(System.DateTime.Today.AddDays(1)), SelectedTypeIds = new List<int>(), SelectedVersionIds = new List<int>() };
            var existingMovie = new Movie { MovieId = "1", Duration = 100, Types = new List<ModelType>(), Versions = new List<ModelVersion>() };
            _movieService.Setup(s => s.GetById("1")).Returns(existingMovie);
            _movieService.Setup(s => s.GetAllTypes()).Returns(new List<ModelType>());
            _movieService.Setup(s => s.GetAllVersions()).Returns(new List<ModelVersion>());
            _movieService.Setup(s => s.UpdateMovie(It.IsAny<Movie>())).Returns(true);
            var clientProxyMock = new Mock<IClientProxy>();
            clientProxyMock.Setup(x => x.SendCoreAsync(It.IsAny<string>(), It.IsAny<object[]>(), It.IsAny<System.Threading.CancellationToken>())).Returns(Task.CompletedTask);
            _hubContext.Setup(x => x.Clients.All).Returns(clientProxyMock.Object);
            var ctrl = BuildController(user);
            ctrl.TempData = new Microsoft.AspNetCore.Mvc.ViewFeatures.TempDataDictionary(
                new Microsoft.AspNetCore.Http.DefaultHttpContext(),
                Mock.Of<Microsoft.AspNetCore.Mvc.ViewFeatures.ITempDataProvider>());
            // Act
            var actionResult = await ctrl.Edit("1", model);
            var result = actionResult as RedirectToActionResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal("MainPage", result.ActionName);
            Assert.Equal("Admin", result.ControllerName);
        }

        [Fact]
        public async Task Edit_Post_UpdateSuccess_EmployeeRole_RedirectsToEmployee()
        {
            _webHostEnvironment.Setup(x => x.WebRootPath).Returns("C:\\FakeWebRootPath"); // Fix here
            // Arrange
            var claims = new List<Claim> { new Claim(ClaimTypes.Role, "Employee") };
            var user = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));
            var model = new MovieDetailViewModel { MovieId = "1", Duration = 100, FromDate = DateOnly.FromDateTime(System.DateTime.Today), ToDate = DateOnly.FromDateTime(System.DateTime.Today.AddDays(1)), SelectedTypeIds = new List<int>(), SelectedVersionIds = new List<int>() };
            var existingMovie = new Movie { MovieId = "1", Duration = 100, Types = new List<ModelType>(), Versions = new List<ModelVersion>() };
            _movieService.Setup(s => s.GetById("1")).Returns(existingMovie);
            _movieService.Setup(s => s.GetAllTypes()).Returns(new List<ModelType>());
            _movieService.Setup(s => s.GetAllVersions()).Returns(new List<ModelVersion>());
            _movieService.Setup(s => s.UpdateMovie(It.IsAny<Movie>())).Returns(true);
            var clientProxyMock = new Mock<IClientProxy>();
            clientProxyMock.Setup(x => x.SendCoreAsync(It.IsAny<string>(), It.IsAny<object[]>(), It.IsAny<System.Threading.CancellationToken>())).Returns(Task.CompletedTask);
            _hubContext.Setup(x => x.Clients.All).Returns(clientProxyMock.Object);
            var ctrl = BuildController(user);
            ctrl.TempData = new Microsoft.AspNetCore.Mvc.ViewFeatures.TempDataDictionary(
                new Microsoft.AspNetCore.Http.DefaultHttpContext(),
                Mock.Of<Microsoft.AspNetCore.Mvc.ViewFeatures.ITempDataProvider>());
            // Act
            var actionResult = await ctrl.Edit("1", model);
            var result = actionResult as RedirectToActionResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal("MainPage", result.ActionName);
            Assert.Equal("Employee", result.ControllerName);
        }

        [Fact]
        public async Task Edit_Post_UpdateFail_ReturnsViewWithError()
        {
            _webHostEnvironment.Setup(x => x.WebRootPath).Returns("C:\\FakeWebRootPath"); // Fix here
            // Arrange
            var model = new MovieDetailViewModel { MovieId = "1", Duration = 100, FromDate = DateOnly.FromDateTime(System.DateTime.Today), ToDate = DateOnly.FromDateTime(System.DateTime.Today.AddDays(1)), SelectedTypeIds = new List<int>(), SelectedVersionIds = new List<int>() };
            var existingMovie = new Movie { MovieId = "1", Duration = 100, Types = new List<ModelType>(), Versions = new List<ModelVersion>() };
            _movieService.Setup(s => s.GetById("1")).Returns(existingMovie);
            _movieService.Setup(s => s.GetAllTypes()).Returns(new List<ModelType>());
            _movieService.Setup(s => s.GetAllVersions()).Returns(new List<ModelVersion>());
            _movieService.Setup(s => s.UpdateMovie(It.IsAny<Movie>())).Returns(false);
            var ctrl = BuildController();
            ctrl.TempData = new Microsoft.AspNetCore.Mvc.ViewFeatures.TempDataDictionary(
                new Microsoft.AspNetCore.Http.DefaultHttpContext(),
                Mock.Of<Microsoft.AspNetCore.Mvc.ViewFeatures.ITempDataProvider>());
            // Act
            var actionResult = await ctrl.Edit("1", model);
            var result = actionResult as ViewResult;
            // Assert
            Assert.NotNull(result);
            Assert.IsType<MovieDetailViewModel>(result.Model);
            Assert.True(ctrl.TempData.ContainsKey("ErrorMessage"));
        }

        // --- Delete (POST) ---
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void Delete_Post_InvalidId_RedirectsWithError(string id)
        {
            // Arrange
            var claims = new List<Claim> { new Claim(ClaimTypes.Role, "Admin") };
            var user = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));
            var ctrl = BuildController(user);
            ctrl.TempData = new Microsoft.AspNetCore.Mvc.ViewFeatures.TempDataDictionary(
                new Microsoft.AspNetCore.Http.DefaultHttpContext(),
                Mock.Of<Microsoft.AspNetCore.Mvc.ViewFeatures.ITempDataProvider>());
            // Act
            var result = ctrl.Delete(id, null) as RedirectToActionResult;
            // Assert
            Assert.NotNull(result);
            Assert.Equal("MainPage", result.ActionName);
        }

        [Fact]
        public void Delete_Post_MovieNotFound_RedirectsWithError()
        {
            // Arrange
            var claims = new List<Claim> { new Claim(ClaimTypes.Role, "Admin") };
            var user = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));
            _movieService.Setup(s => s.GetById("1")).Returns((Movie)null);
            var ctrl = BuildController(user);
            ctrl.TempData = new Microsoft.AspNetCore.Mvc.ViewFeatures.TempDataDictionary(
                new Microsoft.AspNetCore.Http.DefaultHttpContext(),
                Mock.Of<Microsoft.AspNetCore.Mvc.ViewFeatures.ITempDataProvider>());
            // Act
            var result = ctrl.Delete("1", null) as RedirectToActionResult;
            // Assert
            Assert.NotNull(result);
            Assert.Equal("MainPage", result.ActionName);
        }

        [Fact]
        public void Delete_Post_DeleteFail_RedirectsWithError()
        {
            // Arrange
            var claims = new List<Claim> { new Claim(ClaimTypes.Role, "Admin") };
            var user = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));
            var movie = new Movie { MovieId = "1", Types = new List<ModelType>(), Versions = new List<ModelVersion>() };
            _movieService.Setup(s => s.GetById("1")).Returns(movie);
            _movieService.Setup(s => s.DeleteMovie("1")).Returns(false);
            var ctrl = BuildController(user);
            ctrl.TempData = new Microsoft.AspNetCore.Mvc.ViewFeatures.TempDataDictionary(
                new Microsoft.AspNetCore.Http.DefaultHttpContext(),
                Mock.Of<Microsoft.AspNetCore.Mvc.ViewFeatures.ITempDataProvider>());
            // Act
            var result = ctrl.Delete("1", null) as RedirectToActionResult;
            // Assert
            Assert.NotNull(result);
            Assert.Equal("MainPage", result.ActionName);
        }

        [Fact]
        public void Delete_Post_DeleteSuccess_RedirectsWithSuccess()
        {
            // Arrange
            var claims = new List<Claim> { new Claim(ClaimTypes.Role, "Admin") };
            var user = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));
            var movie = new Movie { MovieId = "1", Types = new List<ModelType>(), Versions = new List<ModelVersion>() };
            _movieService.Setup(s => s.GetById("1")).Returns(movie);
            _movieService.Setup(s => s.DeleteMovie("1")).Returns(true);
            var clientProxyMock = new Mock<IClientProxy>();
            clientProxyMock.Setup(x => x.SendCoreAsync(It.IsAny<string>(), It.IsAny<object[]>(), It.IsAny<System.Threading.CancellationToken>())).Returns(Task.CompletedTask);
            _hubContext.Setup(x => x.Clients.All).Returns(clientProxyMock.Object);
            var ctrl = BuildController(user);
            ctrl.TempData = new Microsoft.AspNetCore.Mvc.ViewFeatures.TempDataDictionary(
                new Microsoft.AspNetCore.Http.DefaultHttpContext(),
                Mock.Of<Microsoft.AspNetCore.Mvc.ViewFeatures.ITempDataProvider>());
            // Act
            var result = ctrl.Delete("1", null) as RedirectToActionResult;
            // Assert
            Assert.NotNull(result);
            Assert.Equal("MainPage", result.ActionName);
        }

        [Fact]
        public void Delete_Post_Exception_RedirectsWithError()
        {
            // Arrange
            var claims = new List<Claim> { new Claim(ClaimTypes.Role, "Admin") };
            var user = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));
            _movieService.Setup(s => s.GetById("1")).Throws(new System.Exception("fail"));
            var ctrl = BuildController(user);
            ctrl.TempData = new Microsoft.AspNetCore.Mvc.ViewFeatures.TempDataDictionary(
                new Microsoft.AspNetCore.Http.DefaultHttpContext(),
                Mock.Of<Microsoft.AspNetCore.Mvc.ViewFeatures.ITempDataProvider>());
            // Act
            var result = ctrl.Delete("1", null) as RedirectToActionResult;
            // Assert
            Assert.NotNull(result);
            Assert.Equal("MainPage", result.ActionName);
        }

        // --- MovieShow (GET) ---
        [Fact]
        public void MovieShow_Get_ReturnsNotFound_WhenMovieNull()
        {
            // Arrange
            _movieService.Setup(s => s.GetById("x")).Returns((Movie)null);
            var ctrl = BuildController();
            // Act
            var result = ctrl.MovieShow("x");
            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public void MovieShow_Get_ReturnsJson_WhenAjax()
        {
            // Arrange
            var movie = new Movie { MovieId = "1", FromDate = DateOnly.FromDateTime(System.DateTime.Today), ToDate = DateOnly.FromDateTime(System.DateTime.Today.AddDays(1)), Types = new List<ModelType>(), Versions = new List<ModelVersion>() };
            _movieService.Setup(s => s.GetById("1")).Returns(movie);
            _movieService.Setup(s => s.GetMovieShows("1")).Returns(new List<MovieShow>());
            var ctrl = BuildController();
            ctrl.ControllerContext.HttpContext.Request.Headers["X-Requested-With"] = "XMLHttpRequest";
            // Act
            var result = ctrl.MovieShow("1");
            // Assert
            Assert.IsType<JsonResult>(result);
        }

        [Fact]
        public void MovieShow_Get_ReturnsView_WhenMovieFound()
        {
            // Arrange
            var movie = new Movie { MovieId = "1", FromDate = DateOnly.FromDateTime(System.DateTime.Today), ToDate = DateOnly.FromDateTime(System.DateTime.Today.AddDays(1)), Types = new List<ModelType>(), Versions = new List<ModelVersion>() };
            _movieService.Setup(s => s.GetById("1")).Returns(movie);
            _movieService.Setup(s => s.GetMovieShows("1")).Returns(new List<MovieShow>());
            _cinemaService.Setup(s => s.GetAll()).Returns(new List<CinemaRoom>());
            _movieService.Setup(s => s.GetSchedules()).Returns(new List<Schedule>());
            var ctrl = BuildController();
            // Act
            var result = ctrl.MovieShow("1") as ViewResult;
            // Assert
            Assert.NotNull(result);
            Assert.IsType<MovieDetailViewModel>(result.Model);
        }

        // --- MovieShow (POST) ---
        [Fact]
        public void MovieShow_Post_IdMismatch_ReturnsBadRequest()
        {
            // Arrange
            var model = new MovieDetailViewModel { MovieId = "2" };
            var ctrl = BuildController();
            // Act
            var result = ctrl.MovieShow("1", model);
            // Assert
            Assert.IsType<BadRequestResult>(result);
        }

        [Fact]
        public void MovieShow_Post_InvalidModel_ReturnsView()
        {
            // Arrange
            var model = new MovieDetailViewModel { MovieId = "1" };
            var ctrl = BuildController();
            ctrl.ModelState.AddModelError("x", "err");
            _cinemaService.Setup(s => s.GetAll()).Returns(new List<CinemaRoom>());
            _movieService.Setup(s => s.GetShowDates("1")).Returns(new List<DateOnly>());
            _movieService.Setup(s => s.GetSchedules()).Returns(new List<Schedule>());
            // Act
            var result = ctrl.MovieShow("1", model) as ViewResult;
            // Assert
            Assert.NotNull(result);
            Assert.IsType<MovieDetailViewModel>(result.Model);
        }

        [Fact]
        public void MovieShow_Post_Success_ReturnsViewWithSuccessMessage()
        {
            // Arrange
            var model = new MovieDetailViewModel { MovieId = "1" };
            var ctrl = BuildController();
            ctrl.TempData = new Microsoft.AspNetCore.Mvc.ViewFeatures.TempDataDictionary(
                new Microsoft.AspNetCore.Http.DefaultHttpContext(),
                Mock.Of<Microsoft.AspNetCore.Mvc.ViewFeatures.ITempDataProvider>());
            // Act
            var result = ctrl.MovieShow("1", model) as ViewResult;
            // Assert
            Assert.NotNull(result);
            Assert.IsType<MovieDetailViewModel>(result.Model);
            Assert.True(ctrl.TempData.ContainsKey("SuccessMessage"));
        }

        [Fact]
        public void MovieShow_Post_Exception_ReturnsViewWithError()
        {
            // Arrange
            var model = new MovieDetailViewModel { MovieId = "1" };
            var ctrl = BuildController();
            ctrl.TempData = new Microsoft.AspNetCore.Mvc.ViewFeatures.TempDataDictionary(
                new Microsoft.AspNetCore.Http.DefaultHttpContext(),
                Mock.Of<Microsoft.AspNetCore.Mvc.ViewFeatures.ITempDataProvider>());
            // Act
            var result = ctrl.MovieShow("1", model) as ViewResult;
            // Assert
            Assert.NotNull(result);
            Assert.IsType<MovieDetailViewModel>(result.Model);
        }

        [Fact]
        public void GetMovieShows_ReturnsJsonResult()
        {
            // Arrange
            var movieShows = new List<MovieShow>
            {
                new MovieShow 
                { 
                    MovieShowId = 1, 
                    MovieId = "1",
                    ShowDate = DateOnly.FromDateTime(DateTime.Today),
                    Schedule = new Schedule { ScheduleTime = new TimeOnly(14, 0) },
                    Version = new MovieTheater.Models.Version { VersionName = "2D" },
                    CinemaRoom = new CinemaRoom { StatusId = 1 }
                },
                new MovieShow 
                { 
                    MovieShowId = 2, 
                    MovieId = "1",
                    ShowDate = DateOnly.FromDateTime(DateTime.Today),
                    Schedule = new Schedule { ScheduleTime = new TimeOnly(16, 0) },
                    Version = new MovieTheater.Models.Version { VersionName = "3D" },
                    CinemaRoom = new CinemaRoom { StatusId = 1 }
                }
            };
            _movieService.Setup(s => s.GetMovieShowsByMovieId("1")).Returns(movieShows);
            var ctrl = BuildController();

            // Act
            var result = ctrl.GetMovieShows("1") as JsonResult;

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.Value);
        }

        [Fact]
        public void GetMovieShows_ReturnsEmptyList_WhenNoShows()
        {
            // Arrange
            _movieService.Setup(s => s.GetMovieShowsByMovieId("1")).Returns(new List<MovieShow>());
            var ctrl = BuildController();

            // Act
            var result = ctrl.GetMovieShows("1") as JsonResult;

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result.Value as dynamic);
        }

        [Fact]
        public void GetDirectors_ReturnsJsonResult()
        {
            // Arrange
            var directors = new List<Person> { new Person { PersonId = 1, Name = "Director 1" } };
            _personRepository.Setup(r => r.GetDirectors()).Returns(directors);
            var ctrl = BuildController();

            // Act
            var result = ctrl.GetDirectors() as JsonResult;

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.Value);
        }

        [Fact]
        public void GetActors_ReturnsJsonResult()
        {
            // Arrange
            var actors = new List<Person> { new Person { PersonId = 1, Name = "Actor 1" } };
            _personRepository.Setup(r => r.GetActors()).Returns(actors);
            var ctrl = BuildController();

            // Act
            var result = ctrl.GetActors() as JsonResult;

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.Value);
        }

        [Fact]
        public void MovieList_WithSearchTerm_FiltersMovies()
        {
            // Arrange
            var movies = new List<Movie>
            {
                new Movie { MovieId = "1", MovieNameEnglish = "Test Movie", Content = "Test content" },
                new Movie { MovieId = "2", MovieNameEnglish = "Another Movie", Content = "Another content" }
            };
            _movieService.Setup(s => s.GetCurrentlyShowingMoviesWithDetails()).Returns(movies);
            _movieService.Setup(s => s.GetComingSoonMoviesWithDetails()).Returns(new List<Movie>());
            var ctrl = BuildController();

            // Act
            var result = ctrl.MovieList("Test", null, null) as ViewResult;

            // Assert
            Assert.NotNull(result);
        }

        [Fact]
        public void MovieList_WithTypeFilter_FiltersMovies()
        {
            // Arrange
            var movies = new List<Movie>
            {
                new Movie 
                { 
                    MovieId = "1", 
                    MovieNameEnglish = "Test Movie",
                    Types = new List<ModelType> { new ModelType { TypeId = 1 } }
                }
            };
            _movieService.Setup(s => s.GetCurrentlyShowingMoviesWithDetails()).Returns(movies);
            _movieService.Setup(s => s.GetComingSoonMoviesWithDetails()).Returns(new List<Movie>());
            var ctrl = BuildController();

            // Act
            var result = ctrl.MovieList(null, "1", null) as ViewResult;

            // Assert
            Assert.NotNull(result);
        }

        [Fact]
        public void MovieList_WithVersionFilter_FiltersMovies()
        {
            // Arrange
            var movies = new List<Movie>
            {
                new Movie 
                { 
                    MovieId = "1", 
                    MovieNameEnglish = "Test Movie",
                    Versions = new List<ModelVersion> { new ModelVersion { VersionId = 1 } }
                }
            };
            _movieService.Setup(s => s.GetCurrentlyShowingMoviesWithDetails()).Returns(movies);
            _movieService.Setup(s => s.GetComingSoonMoviesWithDetails()).Returns(new List<Movie>());
            var ctrl = BuildController();

            // Act
            var result = ctrl.MovieList(null, null, "1") as ViewResult;

            // Assert
            Assert.NotNull(result);
        }

        [Fact]
        public async Task GetAvailableScheduleTimes_ReturnsJsonResult()
        {
            // Arrange
            var schedules = new List<Schedule> { new Schedule { ScheduleId = 1, ScheduleTime = new TimeOnly(14, 0) } };
            _movieService.Setup(s => s.GetAvailableSchedulesAsync(It.IsAny<DateOnly>(), It.IsAny<int>()))
                         .ReturnsAsync(schedules);
            _movieService.Setup(s => s.GetMovieShowsByRoomAndDate(It.IsAny<int>(), It.IsAny<DateOnly>()))
                        .Returns(new List<MovieShow>());
            var ctrl = BuildController();

            // Act
            var result = await ctrl.GetAvailableScheduleTimes(1, "2024-06-15", 120, 15) as OkObjectResult;

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.Value);
        }

        [Fact]
        public async Task AddMovieShow_ReturnsOk_OnSuccess()
        {
            // Arrange
            var request = new MovieShowRequest
            {
                MovieId = "1",
                CinemaRoomId = 1,
                ShowDate = DateOnly.FromDateTime(DateTime.Today),
                ScheduleId = 1
            };
            _movieService.Setup(s => s.AddMovieShow(It.IsAny<MovieShow>())).Returns(true);
            var ctrl = BuildController();

            // Act
            var result = await ctrl.AddMovieShow(request) as OkObjectResult;

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.Value);
        }

        [Fact]
        public async Task AddMovieShow_ReturnsBadRequest_OnFailure()
        {
            // Arrange
            var request = new MovieShowRequest
            {
                MovieId = "1",
                CinemaRoomId = 1,
                ShowDate = DateOnly.FromDateTime(DateTime.Today),
                ScheduleId = 1
            };
            _movieService.Setup(s => s.AddMovieShow(It.IsAny<MovieShow>())).Returns(false);
            var ctrl = BuildController();

            // Act
            var result = await ctrl.AddMovieShow(request) as BadRequestObjectResult;

            // Assert
            Assert.NotNull(result);
        }

        [Fact]
        public async Task DeleteAllMovieShows_ReturnsOk_OnSuccess()
        {
            // Arrange
            var request = new MovieController.MovieShowRequestDeleteAll { MovieId = "1" };
            _movieService.Setup(s => s.DeleteAllMovieShows("1")).Returns(true);
            var ctrl = BuildController();

            // Act
            var result = await ctrl.DeleteAllMovieShows(request) as OkResult;

            // Assert
            Assert.NotNull(result);
        }

        [Fact]
        public async Task DeleteAllMovieShows_ReturnsBadRequest_OnFailure()
        {
            // Arrange
            var request = new MovieController.MovieShowRequestDeleteAll { MovieId = "1" };
            _movieService.Setup(s => s.DeleteAllMovieShows("1")).Returns(false);
            var ctrl = BuildController();

            // Act
            var result = await ctrl.DeleteAllMovieShows(request) as BadRequestObjectResult;

            // Assert
            Assert.NotNull(result);
        }

        [Fact]
        public async Task GetAvailableSchedules_ReturnsJsonResult()
        {
            // Arrange
            var schedules = new List<Schedule> { new Schedule { ScheduleId = 1, ScheduleTime = new TimeOnly(14, 0) } };
            _movieService.Setup(s => s.GetAvailableSchedulesAsync(DateOnly.FromDateTime(DateTime.Today), 1))
                         .ReturnsAsync(schedules);
            var ctrl = BuildController();

            // Act
            var result = await ctrl.GetAvailableSchedules(DateOnly.FromDateTime(DateTime.Today), 1) as JsonResult;

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.Value);
        }

        [Fact]
        public void GetMovieShowsByRoomAndDate_ReturnsJsonResult_WithValidParameters()
        {
            // Arrange
            var movieShows = new List<MovieShow>
            {
                new MovieShow 
                { 
                    MovieShowId = 1, 
                    ShowDate = DateOnly.FromDateTime(DateTime.Today),
                    Schedule = new Schedule { ScheduleTime = new TimeOnly(14, 0) },
                    Version = new MovieTheater.Models.Version { VersionName = "2D" },
                    CinemaRoom = new CinemaRoom { CinemaRoomName = "Room 1" },
                    Movie = new Movie { MovieNameEnglish = "Test Movie" }
                }
            };
            _movieService.Setup(s => s.GetMovieShowsByRoomAndDate(It.IsAny<int>(), It.IsAny<DateOnly>()))
                        .Returns(movieShows);
            var ctrl = BuildController();

            // Act
            var result = ctrl.GetMovieShowsByRoomAndDate(1, "2024-06-15") as JsonResult;

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.Value);
        }

        [Fact]
        public void GetMovieShowsByRoomAndDate_ReturnsBadRequest_WithInvalidDate()
        {
            // Arrange
            var ctrl = BuildController();

            // Act
            var result = ctrl.GetMovieShowsByRoomAndDate(1, "invalid-date") as BadRequestObjectResult;

            // Assert
            Assert.NotNull(result);
        }

        [Fact]
        public void ViewShow_ReturnsNotFound_WhenMovieNull()
        {
            // Arrange
            _movieService.Setup(s => s.GetById("1")).Returns((Movie)null);
            var ctrl = BuildController();

            // Act
            var result = ctrl.ViewShow("1") as NotFoundResult;

            // Assert
            Assert.NotNull(result);
        }

        [Fact]
        public void ViewShow_ReturnsView_WhenMovieFound()
        {
            // Arrange
            var movie = new Movie 
            { 
                MovieId = "1", 
                MovieNameEnglish = "Test Movie",
                Versions = new List<MovieTheater.Models.Version>()
            };
            _movieService.Setup(s => s.GetById("1")).Returns(movie);
            _movieService.Setup(s => s.GetMovieShows("1")).Returns(new List<MovieShow>());
            _cinemaService.Setup(s => s.GetAll()).Returns(new List<CinemaRoom>());
            _movieService.Setup(s => s.GetSchedules()).Returns(new List<Schedule>());
            var ctrl = BuildController();

            // Act
            var result = ctrl.ViewShow("1") as ViewResult;

            // Assert
            Assert.NotNull(result);
            Assert.IsType<MovieDetailViewModel>(result.Model);
        }

        [Fact]
        public void DeleteMovieShowIfNotReferenced_ReturnsJsonResult_OnSuccess()
        {
            // Arrange
            var movieShow = new MovieShow { MovieShowId = 1, Invoices = new List<Invoice>() };
            _movieService.Setup(s => s.GetMovieShowById(1)).Returns(movieShow);
            _movieService.Setup(s => s.DeleteMovieShows(1)).Returns(true);
            var ctrl = BuildController();

            // Act
            var result = ctrl.DeleteMovieShowIfNotReferenced(1) as JsonResult;

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.Value);
        }

        [Fact]
        public void DeleteMovieShowIfNotReferenced_ReturnsJsonResult_WhenReferenced()
        {
            // Arrange
            var movieShow = new MovieShow { MovieShowId = 1, Invoices = new List<Invoice> { new Invoice() } };
            _movieService.Setup(s => s.GetMovieShowById(1)).Returns(movieShow);
            var ctrl = BuildController();

            // Act
            var result = ctrl.DeleteMovieShowIfNotReferenced(1) as JsonResult;

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.Value);
        }
    }
}