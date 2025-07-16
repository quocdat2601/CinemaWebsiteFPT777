using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using MovieTheater.Controllers;
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
        private readonly HomeController _controller;

        public HomeControllerTests()
        {
            _promotionServiceMock = new Mock<IPromotionService>();
            _movieServiceMock = new Mock<IMovieService>();
            _accountServiceMock = new Mock<IAccountService>();
            _controller = new HomeController(_promotionServiceMock.Object, _movieServiceMock.Object, _accountServiceMock.Object);
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
            var movies = new List<MovieTheater.Models.Movie> { new MovieTheater.Models.Movie { MovieId = "m1" } };
            var promotions = new List<MovieTheater.Models.Promotion> { new MovieTheater.Models.Promotion { PromotionId = 1 } };
            _movieServiceMock.Setup(s => s.GetAll()).Returns(movies);
            _promotionServiceMock.Setup(s => s.GetAll()).Returns(promotions);
            SetUser(false);

            // Act
            var result = _controller.Index() as ViewResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal(movies, _controller.ViewBag.Movies);
            Assert.Equal(promotions, _controller.ViewBag.Promotions);
        }
    }
} 