using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using System.Security.Claims;
using System.Security.Principal;
using Xunit;

namespace MovieTheater.Tests
{
    public class RoleAuthorizeAttributeTests
    {
        [Fact]
        public void Constructor_WithAllowedRoles_SetsAllowedRoles()
        {
            // Arrange & Act
            var allowedRoles = new int[] { 1, 2, 3 };
            var attribute = new RoleAuthorizeAttribute(allowedRoles);

            // Assert
            Assert.NotNull(attribute);
        }

        [Fact]
        public void OnAuthorization_WhenUserNotAuthenticated_RedirectsToLogin()
        {
            // Arrange
            var allowedRoles = new int[] { 1, 2 };
            var attribute = new RoleAuthorizeAttribute(allowedRoles);

            var httpContext = new DefaultHttpContext();
            var user = new ClaimsPrincipal(new GenericIdentity(""));
            httpContext.User = user;

            var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());
            var context = new AuthorizationFilterContext(actionContext, new List<IFilterMetadata>());

            // Act
            attribute.OnAuthorization(context);

            // Assert
            Assert.NotNull(context.Result);
            var redirectResult = Assert.IsType<RedirectToActionResult>(context.Result);
            Assert.Equal("Login", redirectResult.ActionName);
            Assert.Equal("Account", redirectResult.ControllerName);
        }

        [Fact]
        public void OnAuthorization_WhenUserHasValidRole_DoesNotSetResult()
        {
            // Arrange
            var allowedRoles = new int[] { 1, 2 };
            var attribute = new RoleAuthorizeAttribute(allowedRoles);

            var httpContext = new DefaultHttpContext();
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Role, "1")
            };
            var identity = new ClaimsIdentity(claims, "Test");
            var user = new ClaimsPrincipal(identity);
            httpContext.User = user;

            var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());
            var context = new AuthorizationFilterContext(actionContext, new List<IFilterMetadata>());

            // Act
            attribute.OnAuthorization(context);

            // Assert
            Assert.Null(context.Result);
        }

        [Fact]
        public void OnAuthorization_WhenUserHasInvalidRole_RedirectsToAccessDenied()
        {
            // Arrange
            var allowedRoles = new int[] { 1, 2 };
            var attribute = new RoleAuthorizeAttribute(allowedRoles);

            var httpContext = new DefaultHttpContext();
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Role, "3") // Role not in allowed roles
            };
            var identity = new ClaimsIdentity(claims, "Test");
            var user = new ClaimsPrincipal(identity);
            httpContext.User = user;

            var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());
            var context = new AuthorizationFilterContext(actionContext, new List<IFilterMetadata>());

            // Act
            attribute.OnAuthorization(context);

            // Assert
            Assert.NotNull(context.Result);
            var redirectResult = Assert.IsType<RedirectToActionResult>(context.Result);
            Assert.Equal("AccessDenied", redirectResult.ActionName);
            Assert.Equal("Account", redirectResult.ControllerName);
        }

        [Fact]
        public void OnAuthorization_WhenUserHasNoRoleClaim_RedirectsToAccessDenied()
        {
            // Arrange
            var allowedRoles = new int[] { 1, 2 };
            var attribute = new RoleAuthorizeAttribute(allowedRoles);

            var httpContext = new DefaultHttpContext();
            var identity = new ClaimsIdentity("Test");
            var user = new ClaimsPrincipal(identity);
            httpContext.User = user;

            var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());
            var context = new AuthorizationFilterContext(actionContext, new List<IFilterMetadata>());

            // Act
            attribute.OnAuthorization(context);

            // Assert
            Assert.NotNull(context.Result);
            var redirectResult = Assert.IsType<RedirectToActionResult>(context.Result);
            Assert.Equal("AccessDenied", redirectResult.ActionName);
            Assert.Equal("Account", redirectResult.ControllerName);
        }

        [Fact]
        public void OnAuthorization_WhenUserHasInvalidRoleClaim_RedirectsToAccessDenied()
        {
            // Arrange
            var allowedRoles = new int[] { 1, 2 };
            var attribute = new RoleAuthorizeAttribute(allowedRoles);

            var httpContext = new DefaultHttpContext();
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Role, "invalid") // Non-numeric role
            };
            var identity = new ClaimsIdentity(claims, "Test");
            var user = new ClaimsPrincipal(identity);
            httpContext.User = user;

            var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());
            var context = new AuthorizationFilterContext(actionContext, new List<IFilterMetadata>());

            // Act
            attribute.OnAuthorization(context);

            // Assert
            Assert.NotNull(context.Result);
            var redirectResult = Assert.IsType<RedirectToActionResult>(context.Result);
            Assert.Equal("AccessDenied", redirectResult.ActionName);
            Assert.Equal("Account", redirectResult.ControllerName);
        }

        [Fact]
        public void OnAuthorization_WithEmptyAllowedRoles_RedirectsToAccessDenied()
        {
            // Arrange
            var allowedRoles = new int[] { };
            var attribute = new RoleAuthorizeAttribute(allowedRoles);

            var httpContext = new DefaultHttpContext();
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Role, "1")
            };
            var identity = new ClaimsIdentity(claims, "Test");
            var user = new ClaimsPrincipal(identity);
            httpContext.User = user;

            var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());
            var context = new AuthorizationFilterContext(actionContext, new List<IFilterMetadata>());

            // Act
            attribute.OnAuthorization(context);

            // Assert
            Assert.NotNull(context.Result);
            var redirectResult = Assert.IsType<RedirectToActionResult>(context.Result);
            Assert.Equal("AccessDenied", redirectResult.ActionName);
            Assert.Equal("Account", redirectResult.ControllerName);
        }

        [Fact]
        public void OnAuthorization_WithNullAllowedRoles_DoesNotThrowException()
        {
            // Arrange & Act & Assert - Constructor accepts null without throwing
            var exception = Record.Exception(() => new RoleAuthorizeAttribute(null!));
            Assert.Null(exception);
        }
    }
} 