using Xunit;
using Moq;
using Microsoft.AspNetCore.Mvc;
using MovieTheater.Controllers;
using MovieTheater.Service;
using MovieTheater.ViewModels;
using MovieTheater.Models;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Routing;
using System.IdentityModel.Tokens.Jwt;

namespace MovieTheater.Tests.Controller
{
    public class JwtTestControllerTests
    {
        private Mock<IJwtService> _jwtServiceMock = new Mock<IJwtService>();
        private Mock<IAccountService> _accountServiceMock = new Mock<IAccountService>();

        private JwtTestController CreateController()
        {
            var controller = new JwtTestController(
                _jwtServiceMock.Object,
                _accountServiceMock.Object
            );

            // Setup HttpContext with proper services
            var httpContext = new DefaultHttpContext();
            var services = new ServiceCollection();
            services.AddSingleton<IUrlHelperFactory, UrlHelperFactory>();
            services.AddSingleton<IActionContextAccessor, ActionContextAccessor>();
            services.AddSingleton<IRouter>(new Mock<IRouter>().Object);
            var serviceProvider = services.BuildServiceProvider();
            httpContext.RequestServices = serviceProvider;

            // Setup ActionContext
            var actionContext = new ActionContext(httpContext, new Microsoft.AspNetCore.Routing.RouteData(), new Microsoft.AspNetCore.Mvc.Controllers.ControllerActionDescriptor());
            controller.ControllerContext = new ControllerContext(actionContext);

            return controller;
        }

        private void SetupCookies(JwtTestController controller, string token)
        {
            var httpContext = new DefaultHttpContext();
            
            // Create a mock for IRequestCookieCollection
            var mockCookies = new Mock<IRequestCookieCollection>();
            if (!string.IsNullOrEmpty(token))
            {
                mockCookies.Setup(c => c["JwtToken"]).Returns(token);
            }
            else
            {
                mockCookies.Setup(c => c["JwtToken"]).Returns((string)null);
            }
            
            httpContext.Request.Cookies = mockCookies.Object;
            
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            };
        }

        [Fact]
        public void Constructor_WithValidServices_CreatesController()
        {
            // Arrange & Act
            var controller = CreateController();

            // Assert
            Assert.NotNull(controller);
        }

        [Fact]
        public void TestGenerateToken_ValidCredentials_ReturnsOkWithToken()
        {
            // Arrange
            var controller = CreateController();
            var model = new LoginViewModel { Username = "testuser", Password = "testpass" };
            var account = new Account 
            { 
                AccountId = "user1", 
                Username = "testuser", 
                RoleId = 1, 
                Status = 1 
            };

            _accountServiceMock.Setup(s => s.Authenticate(model.Username, model.Password, out account)).Returns(true);
            _jwtServiceMock.Setup(s => s.GenerateToken(account)).Returns("jwt_token_123");

            // Act
            var result = controller.TestGenerateToken(model);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<dynamic>(okResult.Value);
            Assert.Equal("jwt_token_123", response.Token);
        }

        [Fact]
        public void TestGenerateToken_InvalidCredentials_ReturnsBadRequest()
        {
            // Arrange
            var controller = CreateController();
            var model = new LoginViewModel { Username = "testuser", Password = "wrongpass" };
            var account = new Account();

            _accountServiceMock.Setup(s => s.Authenticate(model.Username, model.Password, out account)).Returns(false);

            // Act
            var result = controller.TestGenerateToken(model);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Invalid credentials", badRequestResult.Value);
        }

        [Fact]
        public void TestValidateToken_ValidToken_ReturnsOkWithUserInfo()
        {
            // Arrange
            var controller = CreateController();
            var token = "valid_jwt_token";
            
            // Setup cookies
            SetupCookies(controller, token);

            // Setup user claims
            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, "user123"),
                new Claim(ClaimTypes.Role, "Admin")
            };
            var identity = new ClaimsIdentity(claims, "Test");
            var principal = new ClaimsPrincipal(identity);
            
            controller.ControllerContext.HttpContext.User = principal;

            _jwtServiceMock.Setup(s => s.ValidateToken(token)).Returns(true);

            // Act
            var result = controller.TestValidateToken();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<dynamic>(okResult.Value);
            Assert.Equal("user123", response.UserId);
            Assert.Equal("Admin", response.Role);
            Assert.True(response.IsValid);
        }

        [Fact]
        public void TestValidateToken_NoTokenInCookies_ReturnsBadRequest()
        {
            // Arrange
            var controller = CreateController();
            
            // Setup empty cookies
            SetupCookies(controller, null);

            // Act
            var result = controller.TestValidateToken();

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("No token found", badRequestResult.Value);
        }

        [Fact]
        public void TestValidateToken_InvalidToken_ReturnsBadRequest()
        {
            // Arrange
            var controller = CreateController();
            var token = "invalid_jwt_token";
            
            // Setup cookies
            SetupCookies(controller, token);

            _jwtServiceMock.Setup(s => s.ValidateToken(token)).Returns(false);

            // Act
            var result = controller.TestValidateToken();

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Invalid token", badRequestResult.Value);
        }

        [Fact]
        public void TestValidateToken_ValidTokenButNoUserClaims_ReturnsOkWithNullValues()
        {
            // Arrange
            var controller = CreateController();
            var token = "valid_jwt_token";
            
            // Setup cookies
            SetupCookies(controller, token);

            // Setup empty user claims
            var claims = new List<Claim>();
            var identity = new ClaimsIdentity(claims, "Test");
            var principal = new ClaimsPrincipal(identity);
            
            controller.ControllerContext.HttpContext.User = principal;

            _jwtServiceMock.Setup(s => s.ValidateToken(token)).Returns(true);

            // Act
            var result = controller.TestValidateToken();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<dynamic>(okResult.Value);
            Assert.Null(response.UserId);
            Assert.Null(response.Role);
            Assert.True(response.IsValid);
        }

        [Fact]
        public void TestValidateToken_ValidTokenWithPartialClaims_ReturnsOkWithAvailableClaims()
        {
            // Arrange
            var controller = CreateController();
            var token = "valid_jwt_token";
            
            // Setup cookies
            SetupCookies(controller, token);

            // Setup partial user claims (only UserId, no Role)
            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, "user456")
            };
            var identity = new ClaimsIdentity(claims, "Test");
            var principal = new ClaimsPrincipal(identity);
            
            controller.ControllerContext.HttpContext.User = principal;

            _jwtServiceMock.Setup(s => s.ValidateToken(token)).Returns(true);

            // Act
            var result = controller.TestValidateToken();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<dynamic>(okResult.Value);
            Assert.Equal("user456", response.UserId);
            Assert.Null(response.Role);
            Assert.True(response.IsValid);
        }

        [Fact]
        public void TestValidateToken_ValidTokenWithRoleOnly_ReturnsOkWithAvailableClaims()
        {
            // Arrange
            var controller = CreateController();
            var token = "valid_jwt_token";
            
            // Setup cookies
            SetupCookies(controller, token);

            // Setup partial user claims (only Role, no UserId)
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Role, "User")
            };
            var identity = new ClaimsIdentity(claims, "Test");
            var principal = new ClaimsPrincipal(identity);
            
            controller.ControllerContext.HttpContext.User = principal;

            _jwtServiceMock.Setup(s => s.ValidateToken(token)).Returns(true);

            // Act
            var result = controller.TestValidateToken();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = Assert.IsType<dynamic>(okResult.Value);
            Assert.Null(response.UserId);
            Assert.Equal("User", response.Role);
            Assert.True(response.IsValid);
        }

        [Fact]
        public void TestGenerateToken_NullModel_ReturnsBadRequest()
        {
            // Arrange
            var controller = CreateController();
            LoginViewModel model = null;

            // Act
            var result = controller.TestGenerateToken(model);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Invalid credentials", badRequestResult.Value);
        }

        [Fact]
        public void TestGenerateToken_EmptyCredentials_ReturnsBadRequest()
        {
            // Arrange
            var controller = CreateController();
            var model = new LoginViewModel { Username = "", Password = "" };
            var account = new Account();

            _accountServiceMock.Setup(s => s.Authenticate(model.Username, model.Password, out account)).Returns(false);

            // Act
            var result = controller.TestGenerateToken(model);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Invalid credentials", badRequestResult.Value);
        }
    }
} 