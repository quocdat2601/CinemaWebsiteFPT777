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
    }
} 