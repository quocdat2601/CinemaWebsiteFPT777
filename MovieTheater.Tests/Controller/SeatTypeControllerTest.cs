using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using MovieTheater.Controllers;
using MovieTheater.Models;
using MovieTheater.Service;
using NPOI.SS.Formula.Functions;
using System.Collections.Generic;
using Xunit;

namespace MovieTheater.Tests.Controller
{
    public class SeatTypeControllerTests
    {
        private readonly Mock<ISeatTypeService> _mockService;
        private readonly SeatTypeController _controller;

        public SeatTypeControllerTests()
        {
            _mockService = new Mock<ISeatTypeService>();
            _controller = new SeatTypeController(_mockService.Object);
        }

        [Fact]
        public void Delete_Get_ReturnsViewResult()
        {
            var result = _controller.Delete(1);

            Assert.IsType<ViewResult>(result);
        }

        [Fact]
        public void Edit_InvalidModelState_ReturnsBadRequest()
        {
            _controller.ModelState.AddModelError("SeatTypeName", "Required");

            var seatTypes = new List<SeatType>
            {
                new SeatType { SeatTypeId = 1 }
            };

            var result = _controller.Edit(seatTypes);

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public void Edit_NullList_ReturnsBadRequest()
        {
            var result = _controller.Edit(null);

            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("No seat types provided", badRequest.Value);
        }

        [Fact]
        public void Edit_EmptyList_ReturnsBadRequest()
        {
            var result = _controller.Edit(new List<SeatType>());

            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("No seat types provided", badRequest.Value);
        }

        [Fact]
        public void Edit_ValidList_RedirectsToAdminMainPage()
        {
            var seatTypes = new List<SeatType>
            {
                new SeatType { SeatTypeId = 1, TypeName = "VIP", PricePercent = 100 }
            };

            var result = _controller.Edit(seatTypes);

            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("MainPage", redirect.ActionName);
            Assert.Equal("Admin", redirect.ControllerName);
            Assert.Equal("VersionMg", redirect.RouteValues["tab"]);
        }

        [Fact]
        public void Edit_ValidListWithMultipleSeatTypes_UpdatesAllSeatTypes()
        {
            // Arrange
            var seatTypes = new List<SeatType>
            {
                new SeatType { SeatTypeId = 1, TypeName = "VIP", PricePercent = 150 },
                new SeatType { SeatTypeId = 2, TypeName = "Standard", PricePercent = 100 },
                new SeatType { SeatTypeId = 3, TypeName = "Premium", PricePercent = 200 }
            };

            // Act
            var result = _controller.Edit(seatTypes);

            // Assert
            _mockService.Verify(x => x.Update(It.IsAny<SeatType>()), Times.Exactly(3));
            _mockService.Verify(x => x.Save(), Times.Once);
            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("MainPage", redirect.ActionName);
        }

        [Fact]
        public void Edit_ValidListWithZeroSeatTypeId_SkipsUpdate()
        {
            // Arrange
            var seatTypes = new List<SeatType>
            {
                new SeatType { SeatTypeId = 0, TypeName = "New", PricePercent = 100 },
                new SeatType { SeatTypeId = 1, TypeName = "VIP", PricePercent = 150 }
            };

            // Act
            var result = _controller.Edit(seatTypes);

            // Assert
            _mockService.Verify(x => x.Update(It.Is<SeatType>(s => s.SeatTypeId == 1)), Times.Once);
            _mockService.Verify(x => x.Update(It.Is<SeatType>(s => s.SeatTypeId == 0)), Times.Never);
            _mockService.Verify(x => x.Save(), Times.Once);
        }

        [Fact]
        public void Edit_ServiceThrowsException_ReturnsBadRequest()
        {
            // Arrange
            var seatTypes = new List<SeatType>
            {
                new SeatType { SeatTypeId = 1, TypeName = "VIP", PricePercent = 100 }
            };
            _mockService.Setup(x => x.Update(It.IsAny<SeatType>()))
                       .Throws(new Exception("Database error"));

            // Act
            var result = _controller.Edit(seatTypes);

            // Assert
            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Contains("Error updating seat types", badRequest.Value.ToString());
            Assert.Contains("Database error", badRequest.Value.ToString());
        }

        [Fact]
        public void Edit_SaveThrowsException_ReturnsBadRequest()
        {
            // Arrange
            var seatTypes = new List<SeatType>
            {
                new SeatType { SeatTypeId = 1, TypeName = "VIP", PricePercent = 100 }
            };
            _mockService.Setup(x => x.Save())
                       .Throws(new Exception("Save failed"));

            // Act
            var result = _controller.Edit(seatTypes);

            // Assert
            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Contains("Error updating seat types", badRequest.Value.ToString());
            Assert.Contains("Save failed", badRequest.Value.ToString());
        }

        [Fact]
        public void Create_Post_ValidForm_RedirectsToIndex()
        {
            var form = new FormCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>());

            var result = _controller.Create(form);

            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectResult.ActionName);
        }

        [Fact]
        public void Create_Post_InvalidModelState_ReturnsView()
        {
            // Arrange
            _controller.ModelState.AddModelError("Error", "Test error");
            var form = new FormCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>());

            // Act
            var result = _controller.Create(form);

            // Assert
            Assert.IsType<ViewResult>(result);
        }



        [Fact]
        public void Create_Get_ReturnsViewResult()
        {
            var result = _controller.Create();

            Assert.IsType<ViewResult>(result);
        }

        [Fact]
        public void Details_WithValidId_ReturnsViewResult()
        {
            // Act
            var result = _controller.Details(1);

            // Assert
            Assert.IsType<ViewResult>(result);
        }

        [Fact]
        public void Details_WithZeroId_ReturnsViewResult()
        {
            // Act
            var result = _controller.Details(0);

            // Assert
            Assert.IsType<ViewResult>(result);
        }

        [Fact]
        public void Details_WithNegativeId_ReturnsViewResult()
        {
            // Act
            var result = _controller.Details(-1);

            // Assert
            Assert.IsType<ViewResult>(result);
        }

        [Fact]
        public void Index_ReturnsViewResult()
        {
            // Act
            var result = _controller.Index();

            // Assert
            Assert.IsType<ViewResult>(result);
        }

        [Fact]
        public void Delete_Post_ReturnsRedirectToIndex()
        {
            var form = new FormCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>());

            var result = _controller.Delete(1, form);

            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirect.ActionName);
        }

        [Fact]
        public void Delete_Post_WithZeroId_ReturnsRedirectToIndex()
        {
            var form = new FormCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>());

            var result = _controller.Delete(0, form);

            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirect.ActionName);
        }

        [Fact]
        public void Delete_Post_WithNegativeId_ReturnsRedirectToIndex()
        {
            var form = new FormCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>());

            var result = _controller.Delete(-1, form);

            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirect.ActionName);
        }
    }
}
