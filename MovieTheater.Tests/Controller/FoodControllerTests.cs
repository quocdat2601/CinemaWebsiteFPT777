using Xunit;
using Moq;
using MovieTheater.Controllers;
using MovieTheater.Service;
using MovieTheater.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace MovieTheater.Tests.Controller
{
    public class FoodControllerTests
    {
        private readonly Mock<IFoodService> _mockService;
        private readonly Mock<IWebHostEnvironment> _mockEnv;
        private readonly FoodController _controller;

        public FoodControllerTests()
        {
            _mockService = new Mock<IFoodService>();
            _mockEnv = new Mock<IWebHostEnvironment>();
            _controller = new FoodController(_mockService.Object, _mockEnv.Object);
        }

        [Fact]
        public async Task Index_ReturnsViewResult_WithModel()
        {
            _mockService.Setup(s => s.GetAllAsync(null, null, null)).ReturnsAsync(new FoodListViewModel { Foods = new List<FoodViewModel>() });
            _mockService.Setup(s => s.GetCategoriesAsync()).ReturnsAsync(new List<string>());
            var result = await _controller.Index(null, null, null);
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.IsType<FoodListViewModel>(viewResult.Model);
        }

        [Fact]
        public void Create_Get_ReturnsViewResult()
        {
            var result = _controller.Create();
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.IsType<FoodViewModel>(viewResult.Model);
        }

        [Fact]
        public async Task Create_Post_ValidModel_RedirectsToMainPage()
        {
            var model = new FoodViewModel { Name = "Popcorn", Category = "food", Price = 50000, Status = true };
            _mockService.Setup(s => s.CreateAsync(model, It.IsAny<string>())).ReturnsAsync(true);
            _mockEnv.Setup(e => e.WebRootPath).Returns("wwwroot");
            // Setup TempData để tránh lỗi null
            _controller.TempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>());
            var result = await _controller.Create(model);
            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("MainPage", redirect.ActionName);
            Assert.Equal("Admin", redirect.ControllerName);
            Assert.Equal("FoodMg", redirect.RouteValues["tab"]);
        }

        [Fact]
        public async Task Create_Post_InvalidModel_ReturnsViewResult()
        {
            _controller.ModelState.AddModelError("Name", "Required");
            var model = new FoodViewModel();
            var result = await _controller.Create(model);
            Assert.IsType<ViewResult>(result);
        }

        [Fact]
        public async Task Edit_Get_FoodExists_ReturnsViewResult()
        {
            _mockService.Setup(s => s.GetByIdAsync(1)).ReturnsAsync(new FoodViewModel { FoodId = 1 });
            var result = await _controller.Edit(1);
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.IsType<FoodViewModel>(viewResult.Model);
        }

        [Fact]
        public async Task Edit_Get_FoodNotFound_RedirectsToMainPage()
        {
            _mockService.Setup(s => s.GetByIdAsync(99)).ReturnsAsync((FoodViewModel)null);
            // Setup TempData để tránh lỗi null
            _controller.TempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>());
            var result = await _controller.Edit(99);
            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("MainPage", redirect.ActionName);
            Assert.Equal("Admin", redirect.ControllerName);
            Assert.Equal("FoodMg", redirect.RouteValues["tab"]);
        }

        [Fact]
        public async Task Edit_Post_ValidModel_RedirectsToMainPage()
        {
            var model = new FoodViewModel { FoodId = 1, Name = "Popcorn", Category = "food", Price = 50000, Status = true };
            _mockService.Setup(s => s.UpdateAsync(model, It.IsAny<string>())).ReturnsAsync(true);
            _mockEnv.Setup(e => e.WebRootPath).Returns("wwwroot");
            // Setup TempData để tránh lỗi null
            _controller.TempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>());
            var result = await _controller.Edit(model);
            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("MainPage", redirect.ActionName);
            Assert.Equal("Admin", redirect.ControllerName);
            Assert.Equal("FoodMg", redirect.RouteValues["tab"]);
        }

        [Fact]
        public async Task Edit_Post_InvalidModel_ReturnsViewResult()
        {
            _controller.ModelState.AddModelError("Name", "Required");
            var model = new FoodViewModel();
            var result = await _controller.Edit(model);
            Assert.IsType<ViewResult>(result);
        }

        [Fact]
        public async Task Delete_Post_RedirectsToMainPage()
        {
            _mockService.Setup(s => s.DeleteAsync(1)).ReturnsAsync(true);
            // Setup TempData để tránh lỗi null
            _controller.TempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>());
            var result = await _controller.Delete(1);
            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("MainPage", redirect.ActionName);
            Assert.Equal("Admin", redirect.ControllerName);
            Assert.Equal("FoodMg", redirect.RouteValues["tab"]);
        }

        [Fact]
        public async Task ToggleStatus_Post_RedirectsToMainPage()
        {
            _mockService.Setup(s => s.ToggleStatusAsync(1)).ReturnsAsync(true);
            // Setup TempData để tránh lỗi null
            _controller.TempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>());
            var result = await _controller.ToggleStatus(1);
            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("MainPage", redirect.ActionName);
            Assert.Equal("Admin", redirect.ControllerName);
            Assert.Equal("FoodMg", redirect.RouteValues["tab"]);
        }
    }
} 