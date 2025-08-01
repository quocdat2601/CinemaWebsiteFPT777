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
    public class SeatTypeControllerTest
    {
        private readonly Mock<ISeatTypeService> _mockService;
        private readonly SeatTypeController _controller;

        public SeatTypeControllerTest()
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
        public void Create_Post_ValidForm_RedirectsToIndex()
        {
            var form = new FormCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>());

            var result = _controller.Create(form);

            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectResult.ActionName);
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
    }
}
