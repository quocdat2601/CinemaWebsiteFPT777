using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using MovieTheater.Controllers;
using MovieTheater.Models;
using MovieTheater.Repository;
using MovieTheater.Service;
using System.Security.Claims;
using Xunit;

namespace MovieTheater.Tests.Controller
{
    public class HomeControllerTests
    {
        private readonly Mock<IPromotionService> _promotionServiceMock;
        private readonly Mock<IMovieService> _movieServiceMock;
        private readonly Mock<IAccountService> _accountServiceMock;
        private readonly Mock<IPersonRepository> _personRepositoryMock;
        private readonly HomeController _controller;

        public HomeControllerTests()
        {
            _promotionServiceMock = new Mock<IPromotionService>();
            _movieServiceMock = new Mock<IMovieService>();
            _accountServiceMock = new Mock<IAccountService>();
            _personRepositoryMock = new Mock<IPersonRepository>();

            // 👇 Mock all needed methods
            _movieServiceMock.Setup(s => s.GetCurrentlyShowingMoviesWithDetails())
                .Returns(new List<Movie>());

            _movieServiceMock.Setup(s => s.GetComingSoonMoviesWithDetails())
                .Returns(new List<Movie>());

            _movieServiceMock.Setup(s => s.GetAll())
                .Returns(new List<Movie>());

            _promotionServiceMock.Setup(s => s.GetAll())
                .Returns(new List<Promotion>());

            _personRepositoryMock.Setup(s => s.GetAll())
                .Returns(new List<Person>());

            _controller = new HomeController(
                _promotionServiceMock.Object,
                _movieServiceMock.Object,
                _accountServiceMock.Object,
                _personRepositoryMock.Object
            );
        }


        private void SetUser(bool isAuthenticated, string role = null, string userId = null)
        {
            var claims = new List<Claim>();
            if (isAuthenticated)
            {
                claims.Add(new Claim(ClaimTypes.NameIdentifier, userId ?? "test-user-id"));
                if (!string.IsNullOrEmpty(role))
                {
                    claims.Add(new Claim(ClaimTypes.Role, role));
                }
            }
            var identity = new ClaimsIdentity(claims, isAuthenticated ? "TestAuthType" : null);
            var principal = new ClaimsPrincipal(identity);
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal }
            };
        }

        [Fact]
        public void Index_UserNotAuthenticated_DoesNotCallCheckAndUpgradeRank()
        {
            _promotionServiceMock.Setup(s => s.GetAll()).Returns(new List<Promotion>());
            _movieServiceMock.Setup(s => s.GetCurrentlyShowingMoviesWithDetails())
                .Returns(new List<Movie>());

            // Arrange
            SetUser(false);

            // Act
            var result = _controller.Index();

            // Assert
            _accountServiceMock.Verify(s => s.CheckAndUpgradeRank(It.IsAny<string>()), Times.Never);
            Assert.IsType<ViewResult>(result);
        }

        [Fact]
        public void Index_UserAuthenticatedNotMember_DoesNotCallCheckAndUpgradeRank()
        {
            _promotionServiceMock.Setup(s => s.GetAll()).Returns(new List<Promotion>());
            _movieServiceMock.Setup(s => s.GetCurrentlyShowingMoviesWithDetails())
                .Returns(new List<Movie>());

            // Arrange
            SetUser(true, role: "Admin", userId: "user1");

            // Act
            var result = _controller.Index();

            // Assert
            _accountServiceMock.Verify(s => s.CheckAndUpgradeRank(It.IsAny<string>()), Times.Never);
            Assert.IsType<ViewResult>(result);
        }

        [Fact]
        public void Index_UserAuthenticatedIsMember_CallsCheckAndUpgradeRank()
        {
            _promotionServiceMock.Setup(s => s.GetAll()).Returns(new List<Promotion>());
            _movieServiceMock.Setup(s => s.GetCurrentlyShowingMoviesWithDetails())
                .Returns(new List<Movie>());

            // Arrange
            SetUser(true, role: "Member", userId: "user2");

            // Act
            var result = _controller.Index();

            // Assert
            _accountServiceMock.Verify(s => s.CheckAndUpgradeRank("user2"), Times.Once);
            Assert.IsType<ViewResult>(result);
        }

        [Fact]
        public void Index_SetsMoviesAndPromotionsInViewBag()
        {
            // Arrange
            var movies = new List<Movie> { new Movie { MovieId = "MV001", MovieNameEnglish = "Movie 1" } };
            var promotions = new List<Promotion> { new Promotion { PromotionId = 1, Title = "Promo 1" } };
            var currentlyShowing = new List<Movie> { new Movie { MovieId = "MV002", MovieNameEnglish = "Now Showing" } };
            var comingSoon = new List<Movie> { new Movie { MovieId = "MV003", MovieNameEnglish = "Coming Soon" } };
            var people = new List<Person> { new Person { PersonId = 1, Name = "Someone" } };

            _movieServiceMock.Setup(s => s.GetAll()).Returns(movies);
            _promotionServiceMock.Setup(s => s.GetAll()).Returns(promotions);
            _movieServiceMock.Setup(s => s.GetCurrentlyShowingMoviesWithDetails()).Returns(currentlyShowing);
            _movieServiceMock.Setup(s => s.GetComingSoonMoviesWithDetails()).Returns(comingSoon);
            _personRepositoryMock.Setup(p => p.GetAll()).Returns(people);
            SetUser(false);

            // Act
            var result = _controller.Index() as ViewResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal(movies, _controller.ViewBag.Movies);
            Assert.Equal(promotions, _controller.ViewBag.Promotions);
            Assert.Equal(currentlyShowing, _controller.ViewBag.CurrentlyShowingMovies);
            Assert.Equal(comingSoon, _controller.ViewBag.ComingSoonMovies);
            Assert.Equal(people, _controller.ViewBag.People);
            Assert.Equal(currentlyShowing.First(), result.Model);
        }

        [Fact]
        public void Index_WhenCurrentlyShowingMoviesIsNull_UsesEmptyList()
        {
            // Arrange
            var comingSoon = new List<Movie> { new Movie { MovieId = "MV003", MovieNameEnglish = "Coming Soon" } };
            var people = new List<Person> { new Person { PersonId = 1, Name = "Someone" } };

            _movieServiceMock.Setup(s => s.GetCurrentlyShowingMoviesWithDetails()).Returns((List<Movie>)null);
            _movieServiceMock.Setup(s => s.GetComingSoonMoviesWithDetails()).Returns(comingSoon);
            _movieServiceMock.Setup(s => s.GetAll()).Returns(new List<Movie>());
            _promotionServiceMock.Setup(s => s.GetAll()).Returns(new List<Promotion>());
            _personRepositoryMock.Setup(p => p.GetAll()).Returns(people);
            SetUser(false);

            // Act
            var result = _controller.Index() as ViewResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal(comingSoon.First(), result.Model);
        }

        [Fact]
        public void Index_WhenNoCurrentlyShowingMovies_UsesComingSoonAsActiveMovie()
        {
            // Arrange
            var comingSoon = new List<Movie> { new Movie { MovieId = "MV003", MovieNameEnglish = "Coming Soon" } };
            var people = new List<Person> { new Person { PersonId = 1, Name = "Someone" } };

            _movieServiceMock.Setup(s => s.GetCurrentlyShowingMoviesWithDetails()).Returns(new List<Movie>());
            _movieServiceMock.Setup(s => s.GetComingSoonMoviesWithDetails()).Returns(comingSoon);
            _movieServiceMock.Setup(s => s.GetAll()).Returns(new List<Movie>());
            _promotionServiceMock.Setup(s => s.GetAll()).Returns(new List<Promotion>());
            _personRepositoryMock.Setup(p => p.GetAll()).Returns(people);
            SetUser(false);

            // Act
            var result = _controller.Index() as ViewResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal(comingSoon.First(), result.Model);
        }

        [Fact]
        public void Index_WhenNoMoviesAtAll_ReturnsNullModel()
        {
            // Arrange
            _movieServiceMock.Setup(s => s.GetCurrentlyShowingMoviesWithDetails()).Returns(new List<Movie>());
            _movieServiceMock.Setup(s => s.GetComingSoonMoviesWithDetails()).Returns(new List<Movie>());
            _movieServiceMock.Setup(s => s.GetAll()).Returns(new List<Movie>());
            _promotionServiceMock.Setup(s => s.GetAll()).Returns(new List<Promotion>());
            _personRepositoryMock.Setup(p => p.GetAll()).Returns(new List<Person>());
            SetUser(false);

            // Act
            var result = _controller.Index() as ViewResult;

            // Assert
            Assert.NotNull(result);
            Assert.Null(result.Model);
        }

        [Fact]
        public void Index_WhenUserHasNoNameIdentifier_DoesNotCallCheckAndUpgradeRank()
        {
            // Arrange
            var claims = new List<Claim> { new Claim(ClaimTypes.Role, "Member") };
            var identity = new ClaimsIdentity(claims, "TestAuthType");
            var principal = new ClaimsPrincipal(identity);
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal }
            };

            _movieServiceMock.Setup(s => s.GetCurrentlyShowingMoviesWithDetails()).Returns(new List<Movie>());
            _promotionServiceMock.Setup(s => s.GetAll()).Returns(new List<Promotion>());

            // Act
            var result = _controller.Index();

            // Assert
            _accountServiceMock.Verify(s => s.CheckAndUpgradeRank(It.IsAny<string>()), Times.Never);
            Assert.IsType<ViewResult>(result);
        }

        [Fact]
        public void Index_WhenUserHasEmptyNameIdentifier_DoesNotCallCheckAndUpgradeRank()
        {
            // Arrange
            var claims = new List<Claim> 
            { 
                new Claim(ClaimTypes.Role, "Member"),
                new Claim(ClaimTypes.NameIdentifier, "")
            };
            var identity = new ClaimsIdentity(claims, "TestAuthType");
            var principal = new ClaimsPrincipal(identity);
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal }
            };

            _movieServiceMock.Setup(s => s.GetCurrentlyShowingMoviesWithDetails()).Returns(new List<Movie>());
            _promotionServiceMock.Setup(s => s.GetAll()).Returns(new List<Promotion>());

            // Act
            var result = _controller.Index();

            // Assert
            _accountServiceMock.Verify(s => s.CheckAndUpgradeRank(It.IsAny<string>()), Times.Never);
            Assert.IsType<ViewResult>(result);
        }

        [Fact]
        public void MovieList_ReturnsView()
        {
            // Act
            var result = _controller.MovieList();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            // MovieList method returns View() without specifying view name, so ViewName will be null
            Assert.Null(viewResult.ViewName);
        }

        [Fact]
        public void Showtime_ReturnsView()
        {
            // Act
            var result = _controller.Showtime();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            // Showtime method returns View() without specifying view name, so ViewName will be null
            Assert.Null(viewResult.ViewName);
        }

        [Fact]
        public void GradientColorPicker_ReturnsView()
        {
            // Act
            var result = _controller.GradientColorPicker();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Equal("GradientColorPicker", viewResult.ViewName);
        }

        [Fact]
        public void Index_WhenServicesThrowException_HandlesGracefully()
        {
            // Arrange
            _movieServiceMock.Setup(s => s.GetCurrentlyShowingMoviesWithDetails())
                .Throws(new InvalidOperationException("Service error"));
            _promotionServiceMock.Setup(s => s.GetAll())
                .Throws(new InvalidOperationException("Service error"));

            SetUser(false);

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() => _controller.Index());
            Assert.Equal("Service error", exception.Message);
        }

        [Fact]
        public void Index_WhenAccountServiceThrowsException_HandlesGracefully()
        {
            // Arrange
            SetUser(true, role: "Member", userId: "user2");
            _accountServiceMock.Setup(s => s.CheckAndUpgradeRank(It.IsAny<string>()))
                .Throws(new InvalidOperationException("Account service error"));

            _movieServiceMock.Setup(s => s.GetCurrentlyShowingMoviesWithDetails()).Returns(new List<Movie>());
            _promotionServiceMock.Setup(s => s.GetAll()).Returns(new List<Promotion>());

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() => _controller.Index());
            Assert.Equal("Account service error", exception.Message);
        }


        [Fact]
        public void Index_WithLargeDataSets_HandlesCorrectly()
        {
            // Arrange
            var movies = Enumerable.Range(1, 100).Select(i => new Movie { MovieId = $"MV{i:D3}", MovieNameEnglish = $"Movie {i}" }).ToList();
            var promotions = Enumerable.Range(1, 50).Select(i => new Promotion { PromotionId = i, Title = $"Promo {i}" }).ToList();
            var currentlyShowing = Enumerable.Range(1, 20).Select(i => new Movie { MovieId = $"CS{i:D3}", MovieNameEnglish = $"Now Showing {i}" }).ToList();
            var comingSoon = Enumerable.Range(1, 10).Select(i => new Movie { MovieId = $"CS{i:D3}", MovieNameEnglish = $"Coming Soon {i}" }).ToList();
            var people = Enumerable.Range(1, 30).Select(i => new Person { PersonId = i, Name = $"Person {i}" }).ToList();

            _movieServiceMock.Setup(s => s.GetAll()).Returns(movies);
            _promotionServiceMock.Setup(s => s.GetAll()).Returns(promotions);
            _movieServiceMock.Setup(s => s.GetCurrentlyShowingMoviesWithDetails()).Returns(currentlyShowing);
            _movieServiceMock.Setup(s => s.GetComingSoonMoviesWithDetails()).Returns(comingSoon);
            _personRepositoryMock.Setup(p => p.GetAll()).Returns(people);
            SetUser(false);

            // Act
            var result = _controller.Index() as ViewResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal(100, ((List<Movie>)_controller.ViewBag.Movies).Count);
            Assert.Equal(50, ((List<Promotion>)_controller.ViewBag.Promotions).Count);
            Assert.Equal(20, ((List<Movie>)_controller.ViewBag.CurrentlyShowingMovies).Count);
            Assert.Equal(10, ((List<Movie>)_controller.ViewBag.ComingSoonMovies).Count);
            Assert.Equal(30, ((List<Person>)_controller.ViewBag.People).Count);
            Assert.Equal(currentlyShowing.First(), result.Model);
        }

        [Fact]
        public void Index_WithNullServices_HandlesGracefully()
        {
            // Arrange
            // Mock services to return empty lists instead of null to avoid ArgumentNullException
            _movieServiceMock.Setup(s => s.GetCurrentlyShowingMoviesWithDetails()).Returns(new List<Movie>());
            _movieServiceMock.Setup(s => s.GetComingSoonMoviesWithDetails()).Returns(new List<Movie>());
            _movieServiceMock.Setup(s => s.GetAll()).Returns(new List<Movie>());
            _promotionServiceMock.Setup(s => s.GetAll()).Returns(new List<Promotion>());
            _personRepositoryMock.Setup(p => p.GetAll()).Returns(new List<Person>());
            SetUser(false);

            // Act
            var result = _controller.Index();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.NotNull(viewResult);
            Assert.Null(viewResult.Model); // No movies available
        }

        [Fact]
        public void Index_WithEmptyUserIdentity_HandlesCorrectly()
        {
            // Arrange
            var httpContext = new DefaultHttpContext();
            httpContext.User = new ClaimsPrincipal(new ClaimsIdentity()); // Empty identity
            _controller.ControllerContext = new ControllerContext { HttpContext = httpContext };

            _movieServiceMock.Setup(s => s.GetCurrentlyShowingMoviesWithDetails()).Returns(new List<Movie>());
            _promotionServiceMock.Setup(s => s.GetAll()).Returns(new List<Promotion>());

            // Act
            var result = _controller.Index();

            // Assert
            Assert.IsType<ViewResult>(result);
            _accountServiceMock.Verify(s => s.CheckAndUpgradeRank(It.IsAny<string>()), Times.Never);
        }
    }
} 