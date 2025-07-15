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
using System.Linq;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace MovieTheater.Tests.Controller
{
    public class TicketControllerTests
    {
        private readonly Mock<ITicketService> _ticketServiceMock = new();

        private TicketController CreateController(ClaimsPrincipal user = null)
        {
            var controller = new TicketController(_ticketServiceMock.Object);
            if (user != null)
            {
                controller.ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext { User = user }
                };
            }
            return controller;
        }

        private ClaimsPrincipal CreateUser(string accountId = "acc1")
        {
            var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, accountId) };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            return new ClaimsPrincipal(identity);
        }

        [Fact]
        public async Task Index_ReturnsViewWithBookings()
        {
            // Arrange
            var user = CreateUser();
            var bookings = new List<object> { new { InvoiceId = "inv1" } };
            _ticketServiceMock.Setup(s => s.GetUserTicketsAsync("acc1", null)).ReturnsAsync(bookings);
            var controller = CreateController(user);
            // Act
            var result = await controller.Index();
            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Equal(bookings, viewResult.Model);
        }

        [Fact]
        public async Task Booked_ReturnsViewWithCompletedBookings()
        {
            var user = CreateUser();
            var bookings = new List<object> { new { InvoiceId = "inv1" } };
            _ticketServiceMock.Setup(s => s.GetUserTicketsAsync("acc1", 1)).ReturnsAsync(bookings);
            var controller = CreateController(user);
            var result = await controller.Booked();
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Equal("Index", viewResult.ViewName);
            Assert.Equal(bookings, viewResult.Model);
        }

        [Fact]
        public async Task Canceled_ReturnsViewWithCanceledBookings()
        {
            var user = CreateUser();
            var bookings = new List<object> { new { InvoiceId = "inv1" } };
            _ticketServiceMock.Setup(s => s.GetUserTicketsAsync("acc1", 0)).ReturnsAsync(bookings);
            var controller = CreateController(user);
            var result = await controller.Canceled();
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Equal("Index", viewResult.ViewName);
            Assert.Equal(bookings, viewResult.Model);
        }

        [Fact]
        public async Task Details_ReturnsViewWithBookingDetails()
        {
            var user = CreateUser();
            var bookingDetails = new { Booking = new { InvoiceId = "inv1" } };
            _ticketServiceMock.Setup(s => s.GetTicketDetailsAsync("inv1", "acc1")).ReturnsAsync(bookingDetails);
            var controller = CreateController(user);
            var result = await controller.Details("inv1");
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Equal(bookingDetails, viewResult.Model);
        }

        [Fact]
        public async Task Cancel_RedirectsToIndexOnSuccess()
        {
            var user = CreateUser();
            _ticketServiceMock.Setup(s => s.CancelTicketAsync("inv1", "acc1")).ReturnsAsync((true, new List<string> { "Success!" }));
            var controller = CreateController(user);
            controller.TempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>());
            var result = await controller.Cancel("inv1", null);
            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirect.ActionName);
        }

        [Fact]
        public async Task HistoryPartial_ReturnsJsonWithData()
        {
            var user = CreateUser();
            var data = new List<object> { new { InvoiceId = "inv1" } };
            _ticketServiceMock.Setup(s => s.GetHistoryPartialAsync("acc1", null, null, "all")).ReturnsAsync(data);
            var controller = CreateController(user);
            var result = await controller.HistoryPartial(null, null, "all");
            var json = Assert.IsType<JsonResult>(result);
            Assert.True((bool)json.Value.GetType().GetProperty("success").GetValue(json.Value));
            Assert.Equal(data, json.Value.GetType().GetProperty("data").GetValue(json.Value));
        }

        [Fact]
        public async Task CancelByAdmin_RedirectsToTicketInfoOnSuccess()
        {
            _ticketServiceMock.Setup(s => s.CancelTicketByAdminAsync("inv1")).ReturnsAsync((true, new List<string> { "Success!" }));
            var controller = CreateController();
            controller.TempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>());
            var result = await controller.CancelByAdmin("inv1", null);
            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("TicketInfo", redirect.ActionName);
        }
    }
}