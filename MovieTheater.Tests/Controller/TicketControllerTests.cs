using Xunit;
using Moq;
using MovieTheater.Controllers;
using MovieTheater.Repository;
using MovieTheater.Service;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using MovieTheater.Models;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using System.Threading.Tasks;
using MovieTheater.Hubs;

namespace MovieTheater.Tests.Controller
{
    public class TicketControllerTests
    {
        private TicketController CreateController(
            Mock<IInvoiceRepository> mockInvoiceRepo = null,
            Mock<IAccountService> mockAccountService = null,
            Mock<IVoucherService> mockVoucherService = null,
            Mock<MovieTheaterContext> mockContext = null,
            Mock<IHubContext<DashboardHub>> mockHub = null,
            ClaimsPrincipal user = null)
        {
            mockInvoiceRepo ??= new Mock<IInvoiceRepository>();
            mockAccountService ??= new Mock<IAccountService>();
            mockVoucherService ??= new Mock<IVoucherService>();
            mockContext ??= new Mock<MovieTheaterContext>();
            mockHub ??= new Mock<IHubContext<DashboardHub>>();

            var controller = new TicketController(
                mockContext.Object,
                mockInvoiceRepo.Object,
                mockAccountService.Object,
                mockVoucherService.Object,
                mockHub.Object
            );

            if (user != null)
            {
                controller.ControllerContext = new ControllerContext()
                {
                    HttpContext = new DefaultHttpContext() { User = user }
                };
            }
            return controller;
        }

        [Fact]
        public void Test_ReturnsContentResult()
        {
            // Arrange
            var controller = CreateController();

            // Act
            var result = controller.Test();

            // Assert
            var contentResult = Assert.IsType<ContentResult>(result);
            Assert.Equal("Test OK", contentResult.Content);
        }

        [Fact]
        public async Task Index_ReturnsViewResult_WhenUserLoggedIn()
        {
            // Arrange
            var mockInvoiceRepo = new Mock<IInvoiceRepository>();
            mockInvoiceRepo.Setup(r => r.GetByAccountIdAsync(It.IsAny<string>(), It.IsAny<InvoiceStatus?>()))
                .ReturnsAsync(new List<Invoice>());
            var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
            {
                new Claim(ClaimTypes.NameIdentifier, "test-account-id")
            }, "mock"));
            var controller = CreateController(mockInvoiceRepo: mockInvoiceRepo, user: user);

            // Act
            var result = await controller.Index();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.IsAssignableFrom<IEnumerable<Invoice>>(viewResult.Model);
        }

        [Fact]
        public async Task Index_RedirectsToLogin_WhenUserNotLoggedIn()
        {
            // Arrange
            var controller = CreateController();
            controller.ControllerContext = new ControllerContext()
            {
                HttpContext = new DefaultHttpContext() { User = new ClaimsPrincipal() }
            };

            // Act
            var result = await controller.Index();

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Login", redirectResult.ActionName);
            Assert.Equal("Account", redirectResult.ControllerName);
        }
    }
} 