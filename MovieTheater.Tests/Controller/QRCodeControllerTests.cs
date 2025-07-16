using Xunit;
using Moq;
using MovieTheater.Controllers;
using MovieTheater.Service;
using MovieTheater.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using TicketVerificationResultViewModel = MovieTheater.ViewModels.TicketVerificationResultViewModel;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json.Linq;
using static MovieTheater.Controllers.QRCodeController;

namespace MovieTheater.Tests.Controller
{
    public class QRCodeControllerTests
    {
        private readonly Mock<ITicketVerificationService> _mockTicketService;
        private readonly Mock<ILogger<QRCodeController>> _mockLogger;
        private readonly QRCodeController _controller;

        public QRCodeControllerTests()
        {
            _mockTicketService = new Mock<ITicketVerificationService>();
            _mockLogger = new Mock<ILogger<QRCodeController>>();
            _controller = new QRCodeController(null, null, null, null, null, _mockLogger.Object, _mockTicketService.Object);

            // Thiết lập fake user cho controller
            var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
            {
                new Claim(ClaimTypes.NameIdentifier, "test-staff-id")
            }, "mock"));
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user }
            };
        }

        [Fact]
        public void VerifyTicket_Valid_ReturnsSuccess()
        {
            // Arrange
            var vm = new TicketVerificationResultViewModel { IsSuccess = true, Message = "OK" };
            _mockTicketService.Setup(s => s.VerifyTicket("INV123")).Returns(vm);
            // Act
            var result = _controller.VerifyTicket(new VerifyTicketRequest { InvoiceId = "INV123" }) as JsonResult;
            // Assert
            Assert.NotNull(result);
            var json = JObject.FromObject(result.Value);
            Assert.True((bool)json["success"]);
            Assert.Equal("OK", (string)json["message"]);
        }

        [Fact]
        public void VerifyTicket_Invalid_ReturnsFail()
        {
            // Arrange
            var vm = new TicketVerificationResultViewModel { IsSuccess = false, Message = "Fail" };
            _mockTicketService.Setup(s => s.VerifyTicket("INV404")).Returns(vm);
            // Act
            var result = _controller.VerifyTicket(new VerifyTicketRequest { InvoiceId = "INV404" }) as JsonResult;
            // Assert
            Assert.NotNull(result);
            var json = JObject.FromObject(result.Value);
            Assert.False((bool)json["success"]);
            Assert.Equal("Fail", (string)json["message"]);
        }

        [Fact]
        public void GetTicketInfo_Valid_ReturnsSuccess()
        {
            // Arrange
            var vm = new TicketVerificationResultViewModel { IsSuccess = true };
            _mockTicketService.Setup(s => s.GetTicketInfo("INV123")).Returns(vm);
            // Act
            var result = _controller.GetTicketInfo("INV123") as JsonResult;
            // Assert
            Assert.NotNull(result);
            var json = JObject.FromObject(result.Value);
            Assert.True((bool)json["success"]);
        }

        [Fact]
        public void GetTicketInfo_Invalid_ReturnsFail()
        {
            // Arrange
            var vm = new TicketVerificationResultViewModel { IsSuccess = false, Message = "Not found" };
            _mockTicketService.Setup(s => s.GetTicketInfo("INV404")).Returns(vm);
            // Act
            var result = _controller.GetTicketInfo("INV404") as JsonResult;
            // Assert
            Assert.NotNull(result);
            var json = JObject.FromObject(result.Value);
            Assert.False((bool)json["success"]);
            Assert.Equal("Not found", (string)json["message"]);
        }

        [Fact]
        public void ConfirmCheckIn_Valid_ReturnsSuccess()
        {
            // Arrange
            var vm = new TicketVerificationResultViewModel { IsSuccess = true, Message = "Checked in" };
            _mockTicketService.Setup(s => s.ConfirmCheckIn("INV123", It.IsAny<string>())).Returns(vm);
            // Act
            var result = _controller.ConfirmCheckIn(new ConfirmCheckInRequest { InvoiceId = "INV123" }) as JsonResult;
            // Assert
            Assert.NotNull(result);
            var json = JObject.FromObject(result.Value);
            Assert.True((bool)json["success"]);
            Assert.Equal("Checked in", (string)json["message"]);
        }

        [Fact]
        public void ConfirmCheckIn_Invalid_ReturnsFail()
        {
            // Arrange
            var vm = new TicketVerificationResultViewModel { IsSuccess = false, Message = "Already checked in" };
            _mockTicketService.Setup(s => s.ConfirmCheckIn("INV123", It.IsAny<string>())).Returns(vm);
            // Act
            var result = _controller.ConfirmCheckIn(new ConfirmCheckInRequest { InvoiceId = "INV123" }) as JsonResult;
            // Assert
            Assert.NotNull(result);
            var json = JObject.FromObject(result.Value);
            Assert.False((bool)json["success"]);
            Assert.Equal("Already checked in", (string)json["message"]);
        }
    }
} 