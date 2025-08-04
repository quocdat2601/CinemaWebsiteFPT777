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
using System.Security.Claims;
using Microsoft.AspNetCore.Http.Features;
using System.IO;

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
            SetupDefaultTempData();
        }

        private void SetupDefaultTempData()
        {
            _controller.TempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>());
        }

        private void SetupUserRole(string role)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Role, role)
            };
            var identity = new ClaimsIdentity(claims, "Test");
            var principal = new ClaimsPrincipal(identity);
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal }
            };
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
        public async Task Index_WithFilters_ReturnsViewResult_WithModel()
        {
            _mockService.Setup(s => s.GetAllAsync("test", "food", true)).ReturnsAsync(new FoodListViewModel { Foods = new List<FoodViewModel>() });
            _mockService.Setup(s => s.GetCategoriesAsync()).ReturnsAsync(new List<string>());
            var result = await _controller.Index("test", "food", true);
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
        public async Task Create_Post_ValidModel_AdminRole_RedirectsToAdminMainPage()
        {
            var model = new FoodViewModel { Name = "Popcorn", Category = "food", Price = 50000, Status = true };
            _mockService.Setup(s => s.CreateAsync(model, It.IsAny<string>())).ReturnsAsync(true);
            _mockEnv.Setup(e => e.WebRootPath).Returns("wwwroot");
            SetupUserRole("Admin");
            
            var result = await _controller.Create(model);
            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("MainPage", redirect.ActionName);
            Assert.Equal("Admin", redirect.ControllerName);
            Assert.Equal("FoodMg", redirect.RouteValues["tab"]);
        }

        [Fact]
        public async Task Create_Post_ValidModel_EmployeeRole_RedirectsToEmployeeMainPage()
        {
            var model = new FoodViewModel { Name = "Popcorn", Category = "food", Price = 50000, Status = true };
            _mockService.Setup(s => s.CreateAsync(model, It.IsAny<string>())).ReturnsAsync(true);
            _mockEnv.Setup(e => e.WebRootPath).Returns("wwwroot");
            SetupUserRole("Employee");
            
            var result = await _controller.Create(model);
            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("MainPage", redirect.ActionName);
            Assert.Equal("Employee", redirect.ControllerName);
            Assert.Equal("FoodMg", redirect.RouteValues["tab"]);
        }

        [Fact]
        public async Task Create_Post_InvalidModel_ReturnsViewResult()
        {
            _controller.ModelState.AddModelError("Name", "Required");
            _controller.ModelState.AddModelError("Price", "Invalid price");
            var model = new FoodViewModel();
            
            var result = await _controller.Create(model);
            Assert.IsType<ViewResult>(result);
            Assert.True(_controller.TempData.ContainsKey("ErrorMessage"));
        }

        [Fact]
        public async Task Create_Post_InvalidFileExtension_ReturnsViewResult()
        {
            var model = new FoodViewModel { Name = "Popcorn", Category = "food", Price = 50000, Status = true };
            
            // Mock invalid file
            var mockFile = new Mock<IFormFile>();
            mockFile.Setup(f => f.FileName).Returns("test.txt");
            mockFile.Setup(f => f.Length).Returns(1024);
            model.ImageFile = mockFile.Object;
            
            var result = await _controller.Create(model);
            Assert.IsType<ViewResult>(result);
            Assert.True(_controller.ModelState.ContainsKey("ImageFile"));
        }

        [Fact]
        public async Task Create_Post_FileTooLarge_ReturnsViewResult()
        {
            var model = new FoodViewModel { Name = "Popcorn", Category = "food", Price = 50000, Status = true };
            
            // Mock file that's too large (3MB)
            var mockFile = new Mock<IFormFile>();
            mockFile.Setup(f => f.FileName).Returns("test.jpg");
            mockFile.Setup(f => f.Length).Returns(3 * 1024 * 1024);
            model.ImageFile = mockFile.Object;
            
            var result = await _controller.Create(model);
            Assert.IsType<ViewResult>(result);
            Assert.True(_controller.ModelState.ContainsKey("ImageFile"));
        }

        [Fact]
        public async Task Create_Post_ServiceReturnsFalse_ReturnsViewResult()
        {
            var model = new FoodViewModel { Name = "Popcorn", Category = "food", Price = 50000, Status = true };
            _mockService.Setup(s => s.CreateAsync(model, It.IsAny<string>())).ReturnsAsync(false);
            _mockEnv.Setup(e => e.WebRootPath).Returns("wwwroot");
            
            var result = await _controller.Create(model);
            Assert.IsType<ViewResult>(result);
        }

        [Fact]
        public async Task Create_Post_ValidModelWithValidFile_RedirectsToMainPage()
        {
            var model = new FoodViewModel { Name = "Popcorn", Category = "food", Price = 50000, Status = true };
            
            // Mock valid file
            var mockFile = new Mock<IFormFile>();
            mockFile.Setup(f => f.FileName).Returns("test.jpg");
            mockFile.Setup(f => f.Length).Returns(1024 * 1024); // 1MB
            model.ImageFile = mockFile.Object;
            
            _mockService.Setup(s => s.CreateAsync(model, It.IsAny<string>())).ReturnsAsync(true);
            _mockEnv.Setup(e => e.WebRootPath).Returns("wwwroot");
            SetupUserRole("Admin");
            
            var result = await _controller.Create(model);
            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("MainPage", redirect.ActionName);
            Assert.Equal("Admin", redirect.ControllerName);
            Assert.Equal("FoodMg", redirect.RouteValues["tab"]);
        }

        [Fact]
        public async Task Create_Post_ValidModelWithValidPngFile_RedirectsToMainPage()
        {
            var model = new FoodViewModel { Name = "Popcorn", Category = "food", Price = 50000, Status = true };
            
            // Mock valid PNG file
            var mockFile = new Mock<IFormFile>();
            mockFile.Setup(f => f.FileName).Returns("test.png");
            mockFile.Setup(f => f.Length).Returns(1024 * 1024); // 1MB
            model.ImageFile = mockFile.Object;
            
            _mockService.Setup(s => s.CreateAsync(model, It.IsAny<string>())).ReturnsAsync(true);
            _mockEnv.Setup(e => e.WebRootPath).Returns("wwwroot");
            SetupUserRole("Admin");
            
            var result = await _controller.Create(model);
            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("MainPage", redirect.ActionName);
            Assert.Equal("Admin", redirect.ControllerName);
            Assert.Equal("FoodMg", redirect.RouteValues["tab"]);
        }

        [Fact]
        public async Task Create_Post_ValidModelWithValidGifFile_RedirectsToMainPage()
        {
            var model = new FoodViewModel { Name = "Popcorn", Category = "food", Price = 50000, Status = true };
            
            // Mock valid GIF file
            var mockFile = new Mock<IFormFile>();
            mockFile.Setup(f => f.FileName).Returns("test.gif");
            mockFile.Setup(f => f.Length).Returns(1024 * 1024); // 1MB
            model.ImageFile = mockFile.Object;
            
            _mockService.Setup(s => s.CreateAsync(model, It.IsAny<string>())).ReturnsAsync(true);
            _mockEnv.Setup(e => e.WebRootPath).Returns("wwwroot");
            SetupUserRole("Admin");
            
            var result = await _controller.Create(model);
            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("MainPage", redirect.ActionName);
            Assert.Equal("Admin", redirect.ControllerName);
            Assert.Equal("FoodMg", redirect.RouteValues["tab"]);
        }

        [Fact]
        public async Task Create_Post_ValidModelWithValidJpegFile_RedirectsToMainPage()
        {
            var model = new FoodViewModel { Name = "Popcorn", Category = "food", Price = 50000, Status = true };
            
            // Mock valid JPEG file
            var mockFile = new Mock<IFormFile>();
            mockFile.Setup(f => f.FileName).Returns("test.jpeg");
            mockFile.Setup(f => f.Length).Returns(1024 * 1024); // 1MB
            model.ImageFile = mockFile.Object;
            
            _mockService.Setup(s => s.CreateAsync(model, It.IsAny<string>())).ReturnsAsync(true);
            _mockEnv.Setup(e => e.WebRootPath).Returns("wwwroot");
            SetupUserRole("Admin");
            
            var result = await _controller.Create(model);
            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("MainPage", redirect.ActionName);
            Assert.Equal("Admin", redirect.ControllerName);
            Assert.Equal("FoodMg", redirect.RouteValues["tab"]);
        }

        [Fact]
        public async Task Create_Post_ValidModelWithValidUppercaseExtension_RedirectsToMainPage()
        {
            var model = new FoodViewModel { Name = "Popcorn", Category = "food", Price = 50000, Status = true };
            
            // Mock valid file with uppercase extension
            var mockFile = new Mock<IFormFile>();
            mockFile.Setup(f => f.FileName).Returns("test.JPG");
            mockFile.Setup(f => f.Length).Returns(1024 * 1024); // 1MB
            model.ImageFile = mockFile.Object;
            
            _mockService.Setup(s => s.CreateAsync(model, It.IsAny<string>())).ReturnsAsync(true);
            _mockEnv.Setup(e => e.WebRootPath).Returns("wwwroot");
            SetupUserRole("Admin");
            
            var result = await _controller.Create(model);
            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("MainPage", redirect.ActionName);
            Assert.Equal("Admin", redirect.ControllerName);
            Assert.Equal("FoodMg", redirect.RouteValues["tab"]);
        }

        [Fact]
        public async Task Create_Post_ValidModelWithValidMixedCaseExtension_RedirectsToMainPage()
        {
            var model = new FoodViewModel { Name = "Popcorn", Category = "food", Price = 50000, Status = true };
            
            // Mock valid file with mixed case extension
            var mockFile = new Mock<IFormFile>();
            mockFile.Setup(f => f.FileName).Returns("test.JpG");
            mockFile.Setup(f => f.Length).Returns(1024 * 1024); // 1MB
            model.ImageFile = mockFile.Object;
            
            _mockService.Setup(s => s.CreateAsync(model, It.IsAny<string>())).ReturnsAsync(true);
            _mockEnv.Setup(e => e.WebRootPath).Returns("wwwroot");
            SetupUserRole("Admin");
            
            var result = await _controller.Create(model);
            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("MainPage", redirect.ActionName);
            Assert.Equal("Admin", redirect.ControllerName);
            Assert.Equal("FoodMg", redirect.RouteValues["tab"]);
        }

        [Fact]
        public async Task Create_Post_ValidModelWithNoExtensionFile_ReturnsViewResult()
        {
            var model = new FoodViewModel { Name = "Popcorn", Category = "food", Price = 50000, Status = true };
            
            // Mock file with no extension
            var mockFile = new Mock<IFormFile>();
            mockFile.Setup(f => f.FileName).Returns("test");
            mockFile.Setup(f => f.Length).Returns(1024 * 1024); // 1MB
            model.ImageFile = mockFile.Object;
            
            var result = await _controller.Create(model);
            Assert.IsType<ViewResult>(result);
            Assert.True(_controller.ModelState.ContainsKey("ImageFile"));
        }

        [Fact]
        public async Task Create_Post_ValidModelWithDotButNoExtensionFile_ReturnsViewResult()
        {
            var model = new FoodViewModel { Name = "Popcorn", Category = "food", Price = 50000, Status = true };
            
            // Mock file with dot but no extension
            var mockFile = new Mock<IFormFile>();
            mockFile.Setup(f => f.FileName).Returns("test.");
            mockFile.Setup(f => f.Length).Returns(1024 * 1024); // 1MB
            model.ImageFile = mockFile.Object;
            
            var result = await _controller.Create(model);
            Assert.IsType<ViewResult>(result);
            Assert.True(_controller.ModelState.ContainsKey("ImageFile"));
        }

        [Fact]
        public async Task Create_Post_ValidModelWithNullFile_RedirectsToMainPage()
        {
            var model = new FoodViewModel { Name = "Popcorn", Category = "food", Price = 50000, Status = true };
            model.ImageFile = null;
            
            _mockService.Setup(s => s.CreateAsync(model, It.IsAny<string>())).ReturnsAsync(true);
            _mockEnv.Setup(e => e.WebRootPath).Returns("wwwroot");
            SetupUserRole("Admin");
            
            var result = await _controller.Create(model);
            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("MainPage", redirect.ActionName);
            Assert.Equal("Admin", redirect.ControllerName);
            Assert.Equal("FoodMg", redirect.RouteValues["tab"]);
        }

        [Fact]
        public async Task Create_Post_ValidModelWithNullFileName_ReturnsViewResult()
        {
            var model = new FoodViewModel { Name = "Popcorn", Category = "food", Price = 50000, Status = true };
            
            // Mock file with null filename
            var mockFile = new Mock<IFormFile>();
            mockFile.Setup(f => f.FileName).Returns((string)null);
            mockFile.Setup(f => f.Length).Returns(1024 * 1024); // 1MB
            model.ImageFile = mockFile.Object;
            
            var result = await _controller.Create(model);
            Assert.IsType<ViewResult>(result);
            Assert.True(_controller.ModelState.ContainsKey("ImageFile"));
        }

        [Fact]
        public async Task Create_Post_ValidModelWithEmptyFileName_ReturnsViewResult()
        {
            var model = new FoodViewModel { Name = "Popcorn", Category = "food", Price = 50000, Status = true };
            
            // Mock file with empty filename
            var mockFile = new Mock<IFormFile>();
            mockFile.Setup(f => f.FileName).Returns("");
            mockFile.Setup(f => f.Length).Returns(1024 * 1024); // 1MB
            model.ImageFile = mockFile.Object;
            
            var result = await _controller.Create(model);
            Assert.IsType<ViewResult>(result);
            Assert.True(_controller.ModelState.ContainsKey("ImageFile"));
        }

        [Fact]
        public async Task Create_Post_ValidModelWithWhitespaceFileName_ReturnsViewResult()
        {
            var model = new FoodViewModel { Name = "Popcorn", Category = "food", Price = 50000, Status = true };
            
            // Mock file with whitespace filename
            var mockFile = new Mock<IFormFile>();
            mockFile.Setup(f => f.FileName).Returns("   ");
            mockFile.Setup(f => f.Length).Returns(1024 * 1024); // 1MB
            model.ImageFile = mockFile.Object;
            
            var result = await _controller.Create(model);
            Assert.IsType<ViewResult>(result);
            Assert.True(_controller.ModelState.ContainsKey("ImageFile"));
        }

        [Fact]
        public async Task Create_Post_ValidModelWithExactly2MBFile_RedirectsToMainPage()
        {
            var model = new FoodViewModel { Name = "Popcorn", Category = "food", Price = 50000, Status = true };
            
            // Mock file with exactly 2MB size
            var mockFile = new Mock<IFormFile>();
            mockFile.Setup(f => f.FileName).Returns("test.jpg");
            mockFile.Setup(f => f.Length).Returns(2 * 1024 * 1024); // Exactly 2MB
            model.ImageFile = mockFile.Object;
            
            _mockService.Setup(s => s.CreateAsync(model, It.IsAny<string>())).ReturnsAsync(true);
            _mockEnv.Setup(e => e.WebRootPath).Returns("wwwroot");
            SetupUserRole("Admin");
            
            var result = await _controller.Create(model);
            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("MainPage", redirect.ActionName);
            Assert.Equal("Admin", redirect.ControllerName);
            Assert.Equal("FoodMg", redirect.RouteValues["tab"]);
        }

        [Fact]
        public async Task Create_Post_ValidModelWith2MBPlus1ByteFile_ReturnsViewResult()
        {
            var model = new FoodViewModel { Name = "Popcorn", Category = "food", Price = 50000, Status = true };
            
            // Mock file with 2MB + 1 byte size
            var mockFile = new Mock<IFormFile>();
            mockFile.Setup(f => f.FileName).Returns("test.jpg");
            mockFile.Setup(f => f.Length).Returns(2 * 1024 * 1024 + 1); // 2MB + 1 byte
            model.ImageFile = mockFile.Object;
            
            var result = await _controller.Create(model);
            Assert.IsType<ViewResult>(result);
            Assert.True(_controller.ModelState.ContainsKey("ImageFile"));
        }

        [Fact]
        public async Task Create_Post_ValidModelWith2MBMinus1ByteFile_RedirectsToMainPage()
        {
            var model = new FoodViewModel { Name = "Popcorn", Category = "food", Price = 50000, Status = true };
            
            // Mock file with 2MB - 1 byte size
            var mockFile = new Mock<IFormFile>();
            mockFile.Setup(f => f.FileName).Returns("test.jpg");
            mockFile.Setup(f => f.Length).Returns(2 * 1024 * 1024 - 1); // 2MB - 1 byte
            model.ImageFile = mockFile.Object;
            
            _mockService.Setup(s => s.CreateAsync(model, It.IsAny<string>())).ReturnsAsync(true);
            _mockEnv.Setup(e => e.WebRootPath).Returns("wwwroot");
            SetupUserRole("Admin");
            
            var result = await _controller.Create(model);
            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("MainPage", redirect.ActionName);
            Assert.Equal("Admin", redirect.ControllerName);
            Assert.Equal("FoodMg", redirect.RouteValues["tab"]);
        }

        [Fact]
        public async Task Create_Post_ValidModelWithZeroSizeFile_RedirectsToMainPage()
        {
            var model = new FoodViewModel { Name = "Popcorn", Category = "food", Price = 50000, Status = true };
            
            // Mock file with zero size
            var mockFile = new Mock<IFormFile>();
            mockFile.Setup(f => f.FileName).Returns("test.jpg");
            mockFile.Setup(f => f.Length).Returns(0); // Zero size
            model.ImageFile = mockFile.Object;
            
            _mockService.Setup(s => s.CreateAsync(model, It.IsAny<string>())).ReturnsAsync(true);
            _mockEnv.Setup(e => e.WebRootPath).Returns("wwwroot");
            SetupUserRole("Admin");
            
            var result = await _controller.Create(model);
            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("MainPage", redirect.ActionName);
            Assert.Equal("Admin", redirect.ControllerName);
            Assert.Equal("FoodMg", redirect.RouteValues["tab"]);
        }

        [Fact]
        public async Task Create_Post_ValidModelWithNegativeSizeFile_RedirectsToMainPage()
        {
            var model = new FoodViewModel { Name = "Popcorn", Category = "food", Price = 50000, Status = true };
            
            // Mock file with negative size
            var mockFile = new Mock<IFormFile>();
            mockFile.Setup(f => f.FileName).Returns("test.jpg");
            mockFile.Setup(f => f.Length).Returns(-1); // Negative size
            model.ImageFile = mockFile.Object;
            
            _mockService.Setup(s => s.CreateAsync(model, It.IsAny<string>())).ReturnsAsync(true);
            _mockEnv.Setup(e => e.WebRootPath).Returns("wwwroot");
            SetupUserRole("Admin");
            
            var result = await _controller.Create(model);
            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("MainPage", redirect.ActionName);
            Assert.Equal("Admin", redirect.ControllerName);
            Assert.Equal("FoodMg", redirect.RouteValues["tab"]);
        }

        [Fact]
        public async Task Create_Post_ValidModelWith1ByteFile_RedirectsToMainPage()
        {
            var model = new FoodViewModel { Name = "Popcorn", Category = "food", Price = 50000, Status = true };
            
            // Mock file with 1 byte size
            var mockFile = new Mock<IFormFile>();
            mockFile.Setup(f => f.FileName).Returns("test.jpg");
            mockFile.Setup(f => f.Length).Returns(1); // 1 byte
            model.ImageFile = mockFile.Object;
            
            _mockService.Setup(s => s.CreateAsync(model, It.IsAny<string>())).ReturnsAsync(true);
            _mockEnv.Setup(e => e.WebRootPath).Returns("wwwroot");
            SetupUserRole("Admin");
            
            var result = await _controller.Create(model);
            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("MainPage", redirect.ActionName);
            Assert.Equal("Admin", redirect.ControllerName);
            Assert.Equal("FoodMg", redirect.RouteValues["tab"]);
        }

        [Fact]
        public async Task Create_Post_ValidModelWith1MBFile_RedirectsToMainPage()
        {
            var model = new FoodViewModel { Name = "Popcorn", Category = "food", Price = 50000, Status = true };
            
            // Mock file with 1MB size
            var mockFile = new Mock<IFormFile>();
            mockFile.Setup(f => f.FileName).Returns("test.jpg");
            mockFile.Setup(f => f.Length).Returns(1024 * 1024); // 1MB
            model.ImageFile = mockFile.Object;
            
            _mockService.Setup(s => s.CreateAsync(model, It.IsAny<string>())).ReturnsAsync(true);
            _mockEnv.Setup(e => e.WebRootPath).Returns("wwwroot");
            SetupUserRole("Admin");
            
            var result = await _controller.Create(model);
            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("MainPage", redirect.ActionName);
            Assert.Equal("Admin", redirect.ControllerName);
            Assert.Equal("FoodMg", redirect.RouteValues["tab"]);
        }

        [Fact]
        public async Task Create_Post_ValidModelWith1_5MBFile_RedirectsToMainPage()
        {
            var model = new FoodViewModel { Name = "Popcorn", Category = "food", Price = 50000, Status = true };
            
            // Mock file with 1.5MB size
            var mockFile = new Mock<IFormFile>();
            mockFile.Setup(f => f.FileName).Returns("test.jpg");
            mockFile.Setup(f => f.Length).Returns((long)(1.5 * 1024 * 1024)); // 1.5MB
            model.ImageFile = mockFile.Object;
            
            _mockService.Setup(s => s.CreateAsync(model, It.IsAny<string>())).ReturnsAsync(true);
            _mockEnv.Setup(e => e.WebRootPath).Returns("wwwroot");
            SetupUserRole("Admin");
            
            var result = await _controller.Create(model);
            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("MainPage", redirect.ActionName);
            Assert.Equal("Admin", redirect.ControllerName);
            Assert.Equal("FoodMg", redirect.RouteValues["tab"]);
        }

        [Fact]
        public async Task Create_Post_ValidModelWith1_9MBFile_RedirectsToMainPage()
        {
            var model = new FoodViewModel { Name = "Popcorn", Category = "food", Price = 50000, Status = true };
            
            // Mock file with 1.9MB size
            var mockFile = new Mock<IFormFile>();
            mockFile.Setup(f => f.FileName).Returns("test.jpg");
            mockFile.Setup(f => f.Length).Returns((long)(1.9 * 1024 * 1024)); // 1.9MB
            model.ImageFile = mockFile.Object;
            
            _mockService.Setup(s => s.CreateAsync(model, It.IsAny<string>())).ReturnsAsync(true);
            _mockEnv.Setup(e => e.WebRootPath).Returns("wwwroot");
            SetupUserRole("Admin");
            
            var result = await _controller.Create(model);
            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("MainPage", redirect.ActionName);
            Assert.Equal("Admin", redirect.ControllerName);
            Assert.Equal("FoodMg", redirect.RouteValues["tab"]);
        }

        [Fact]
        public async Task Create_Post_ValidModelWith1_99MBFile_RedirectsToMainPage()
        {
            var model = new FoodViewModel { Name = "Popcorn", Category = "food", Price = 50000, Status = true };
            
            // Mock file with 1.99MB size
            var mockFile = new Mock<IFormFile>();
            mockFile.Setup(f => f.FileName).Returns("test.jpg");
            mockFile.Setup(f => f.Length).Returns((long)(1.99 * 1024 * 1024)); // 1.99MB
            model.ImageFile = mockFile.Object;
            
            _mockService.Setup(s => s.CreateAsync(model, It.IsAny<string>())).ReturnsAsync(true);
            _mockEnv.Setup(e => e.WebRootPath).Returns("wwwroot");
            SetupUserRole("Admin");
            
            var result = await _controller.Create(model);
            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("MainPage", redirect.ActionName);
            Assert.Equal("Admin", redirect.ControllerName);
            Assert.Equal("FoodMg", redirect.RouteValues["tab"]);
        }

        [Fact]
        public async Task Create_Post_ValidModelWith2_1MBFile_ReturnsViewResult()
        {
            var model = new FoodViewModel { Name = "Popcorn", Category = "food", Price = 50000, Status = true };
            
            // Mock file with 2.1MB size
            var mockFile = new Mock<IFormFile>();
            mockFile.Setup(f => f.FileName).Returns("test.jpg");
            mockFile.Setup(f => f.Length).Returns((long)(2.1 * 1024 * 1024)); // 2.1MB
            model.ImageFile = mockFile.Object;
            
            var result = await _controller.Create(model);
            Assert.IsType<ViewResult>(result);
            Assert.True(_controller.ModelState.ContainsKey("ImageFile"));
        }

        [Fact]
        public async Task Create_Post_ValidModelWith3MBFile_ReturnsViewResult()
        {
            var model = new FoodViewModel { Name = "Popcorn", Category = "food", Price = 50000, Status = true };
            
            // Mock file with 3MB size
            var mockFile = new Mock<IFormFile>();
            mockFile.Setup(f => f.FileName).Returns("test.jpg");
            mockFile.Setup(f => f.Length).Returns(3 * 1024 * 1024); // 3MB
            model.ImageFile = mockFile.Object;
            
            var result = await _controller.Create(model);
            Assert.IsType<ViewResult>(result);
            Assert.True(_controller.ModelState.ContainsKey("ImageFile"));
        }

        [Fact]
        public async Task Create_Post_ValidModelWith10MBFile_ReturnsViewResult()
        {
            var model = new FoodViewModel { Name = "Popcorn", Category = "food", Price = 50000, Status = true };
            
            // Mock file with 10MB size
            var mockFile = new Mock<IFormFile>();
            mockFile.Setup(f => f.FileName).Returns("test.jpg");
            mockFile.Setup(f => f.Length).Returns(10 * 1024 * 1024); // 10MB
            model.ImageFile = mockFile.Object;
            
            var result = await _controller.Create(model);
            Assert.IsType<ViewResult>(result);
            Assert.True(_controller.ModelState.ContainsKey("ImageFile"));
        }

        [Fact]
        public async Task Create_Post_ValidModelWith100MBFile_ReturnsViewResult()
        {
            var model = new FoodViewModel { Name = "Popcorn", Category = "food", Price = 50000, Status = true };
            
            // Mock file with 100MB size
            var mockFile = new Mock<IFormFile>();
            mockFile.Setup(f => f.FileName).Returns("test.jpg");
            mockFile.Setup(f => f.Length).Returns(100 * 1024 * 1024); // 100MB
            model.ImageFile = mockFile.Object;
            
            var result = await _controller.Create(model);
            Assert.IsType<ViewResult>(result);
            Assert.True(_controller.ModelState.ContainsKey("ImageFile"));
        }

        [Fact]
        public async Task Create_Post_ValidModelWith1GBFile_ReturnsViewResult()
        {
            var model = new FoodViewModel { Name = "Popcorn", Category = "food", Price = 50000, Status = true };
            
            // Mock file with 1GB size
            var mockFile = new Mock<IFormFile>();
            mockFile.Setup(f => f.FileName).Returns("test.jpg");
            mockFile.Setup(f => f.Length).Returns(1024L * 1024 * 1024); // 1GB
            model.ImageFile = mockFile.Object;
            
            var result = await _controller.Create(model);
            Assert.IsType<ViewResult>(result);
            Assert.True(_controller.ModelState.ContainsKey("ImageFile"));
        }

        [Fact]
        public async Task Create_Post_ValidModelWith2GBFile_ReturnsViewResult()
        {
            var model = new FoodViewModel { Name = "Popcorn", Category = "food", Price = 50000, Status = true };
            
            // Mock file with 2GB size
            var mockFile = new Mock<IFormFile>();
            mockFile.Setup(f => f.FileName).Returns("test.jpg");
            mockFile.Setup(f => f.Length).Returns(2L * 1024 * 1024 * 1024); // 2GB
            model.ImageFile = mockFile.Object;
            
            var result = await _controller.Create(model);
            Assert.IsType<ViewResult>(result);
            Assert.True(_controller.ModelState.ContainsKey("ImageFile"));
        }

        [Fact]
        public async Task Create_Post_ValidModelWith5GBFile_ReturnsViewResult()
        {
            var model = new FoodViewModel { Name = "Popcorn", Category = "food", Price = 50000, Status = true };
            
            // Mock file with 5GB size
            var mockFile = new Mock<IFormFile>();
            mockFile.Setup(f => f.FileName).Returns("test.jpg");
            mockFile.Setup(f => f.Length).Returns(5L * 1024 * 1024 * 1024); // 5GB
            model.ImageFile = mockFile.Object;
            
            var result = await _controller.Create(model);
            Assert.IsType<ViewResult>(result);
            Assert.True(_controller.ModelState.ContainsKey("ImageFile"));
        }

        [Fact]
        public async Task Create_Post_ValidModelWith10GBFile_ReturnsViewResult()
        {
            var model = new FoodViewModel { Name = "Popcorn", Category = "food", Price = 50000, Status = true };
            
            // Mock file with 10GB size
            var mockFile = new Mock<IFormFile>();
            mockFile.Setup(f => f.FileName).Returns("test.jpg");
            mockFile.Setup(f => f.Length).Returns(10L * 1024 * 1024 * 1024); // 10GB
            model.ImageFile = mockFile.Object;
            
            var result = await _controller.Create(model);
            Assert.IsType<ViewResult>(result);
            Assert.True(_controller.ModelState.ContainsKey("ImageFile"));
        }

        [Fact]
        public async Task Create_Post_ValidModelWith100GBFile_ReturnsViewResult()
        {
            var model = new FoodViewModel { Name = "Popcorn", Category = "food", Price = 50000, Status = true };
            
            // Mock file with 100GB size
            var mockFile = new Mock<IFormFile>();
            mockFile.Setup(f => f.FileName).Returns("test.jpg");
            mockFile.Setup(f => f.Length).Returns(100L * 1024 * 1024 * 1024); // 100GB
            model.ImageFile = mockFile.Object;
            
            var result = await _controller.Create(model);
            Assert.IsType<ViewResult>(result);
            Assert.True(_controller.ModelState.ContainsKey("ImageFile"));
        }

        [Fact]
        public async Task Create_Post_ValidModelWith1TBFile_ReturnsViewResult()
        {
            var model = new FoodViewModel { Name = "Popcorn", Category = "food", Price = 50000, Status = true };
            
            // Mock file with 1TB size
            var mockFile = new Mock<IFormFile>();
            mockFile.Setup(f => f.FileName).Returns("test.jpg");
            mockFile.Setup(f => f.Length).Returns(1024L * 1024 * 1024 * 1024); // 1TB
            model.ImageFile = mockFile.Object;
            
            var result = await _controller.Create(model);
            Assert.IsType<ViewResult>(result);
            Assert.True(_controller.ModelState.ContainsKey("ImageFile"));
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
        public async Task Edit_Get_FoodNotFound_AdminRole_RedirectsToAdminMainPage()
        {
            _mockService.Setup(s => s.GetByIdAsync(99)).ReturnsAsync((FoodViewModel)null);
            SetupUserRole("Admin");
            
            var result = await _controller.Edit(99);
            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("MainPage", redirect.ActionName);
            Assert.Equal("Admin", redirect.ControllerName);
            Assert.Equal("FoodMg", redirect.RouteValues["tab"]);
        }

        [Fact]
        public async Task Edit_Get_FoodNotFound_EmployeeRole_RedirectsToEmployeeMainPage()
        {
            _mockService.Setup(s => s.GetByIdAsync(99)).ReturnsAsync((FoodViewModel)null);
            SetupUserRole("Employee");
            
            var result = await _controller.Edit(99);
            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("MainPage", redirect.ActionName);
            Assert.Equal("Employee", redirect.ControllerName);
            Assert.Equal("FoodMg", redirect.RouteValues["tab"]);
        }

        [Fact]
        public async Task Edit_Post_ValidModel_AdminRole_RedirectsToAdminMainPage()
        {
            var model = new FoodViewModel { FoodId = 1, Name = "Popcorn", Category = "food", Price = 50000, Status = true };
            _mockService.Setup(s => s.UpdateAsync(model, It.IsAny<string>())).ReturnsAsync(true);
            _mockEnv.Setup(e => e.WebRootPath).Returns("wwwroot");
            SetupUserRole("Admin");
            
            var result = await _controller.Edit(model);
            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("MainPage", redirect.ActionName);
            Assert.Equal("Admin", redirect.ControllerName);
            Assert.Equal("FoodMg", redirect.RouteValues["tab"]);
        }

        [Fact]
        public async Task Edit_Post_ValidModel_EmployeeRole_RedirectsToEmployeeMainPage()
        {
            var model = new FoodViewModel { FoodId = 1, Name = "Popcorn", Category = "food", Price = 50000, Status = true };
            _mockService.Setup(s => s.UpdateAsync(model, It.IsAny<string>())).ReturnsAsync(true);
            _mockEnv.Setup(e => e.WebRootPath).Returns("wwwroot");
            SetupUserRole("Employee");
            
            var result = await _controller.Edit(model);
            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("MainPage", redirect.ActionName);
            Assert.Equal("Employee", redirect.ControllerName);
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
        public async Task Edit_Post_ServiceReturnsFalse_ReturnsViewResult()
        {
            var model = new FoodViewModel { FoodId = 1, Name = "Popcorn", Category = "food", Price = 50000, Status = true };
            _mockService.Setup(s => s.UpdateAsync(model, It.IsAny<string>())).ReturnsAsync(false);
            _mockEnv.Setup(e => e.WebRootPath).Returns("wwwroot");
            
            var result = await _controller.Edit(model);
            Assert.IsType<ViewResult>(result);
        }

        [Fact]
        public async Task Delete_Post_FoodNotFound_AdminRole_RedirectsToAdminMainPage()
        {
            _mockService.Setup(s => s.GetByIdAsync(99)).ReturnsAsync((FoodViewModel)null);
            SetupUserRole("Admin");
            
            var result = await _controller.Delete(99);
            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("MainPage", redirect.ActionName);
            Assert.Equal("Admin", redirect.ControllerName);
            Assert.Equal("FoodMg", redirect.RouteValues["tab"]);
        }

        [Fact]
        public async Task Delete_Post_FoodNotFound_EmployeeRole_RedirectsToEmployeeMainPage()
        {
            _mockService.Setup(s => s.GetByIdAsync(99)).ReturnsAsync((FoodViewModel)null);
            SetupUserRole("Employee");
            
            var result = await _controller.Delete(99);
            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("MainPage", redirect.ActionName);
            Assert.Equal("Employee", redirect.ControllerName);
            Assert.Equal("FoodMg", redirect.RouteValues["tab"]);
        }

        [Fact]
        public async Task Delete_Post_HasRelatedInvoices_AdminRole_RedirectsToAdminMainPage()
        {
            var food = new FoodViewModel { FoodId = 1, Name = "Popcorn" };
            _mockService.Setup(s => s.GetByIdAsync(1)).ReturnsAsync(food);
            _mockService.Setup(s => s.HasRelatedInvoicesAsync(1)).ReturnsAsync(true);
            SetupUserRole("Admin");
            
            var result = await _controller.Delete(1);
            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("MainPage", redirect.ActionName);
            Assert.Equal("Admin", redirect.ControllerName);
            Assert.Equal("FoodMg", redirect.RouteValues["tab"]);
        }

        [Fact]
        public async Task Delete_Post_HasRelatedInvoices_EmployeeRole_RedirectsToEmployeeMainPage()
        {
            var food = new FoodViewModel { FoodId = 1, Name = "Popcorn" };
            _mockService.Setup(s => s.GetByIdAsync(1)).ReturnsAsync(food);
            _mockService.Setup(s => s.HasRelatedInvoicesAsync(1)).ReturnsAsync(true);
            SetupUserRole("Employee");
            
            var result = await _controller.Delete(1);
            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("MainPage", redirect.ActionName);
            Assert.Equal("Employee", redirect.ControllerName);
            Assert.Equal("FoodMg", redirect.RouteValues["tab"]);
        }

        [Fact]
        public async Task Delete_Post_Successful_AdminRole_RedirectsToAdminMainPage()
        {
            var food = new FoodViewModel { FoodId = 1, Name = "Popcorn" };
            _mockService.Setup(s => s.GetByIdAsync(1)).ReturnsAsync(food);
            _mockService.Setup(s => s.HasRelatedInvoicesAsync(1)).ReturnsAsync(false);
            _mockService.Setup(s => s.DeleteAsync(1)).ReturnsAsync(true);
            SetupUserRole("Admin");
            
            var result = await _controller.Delete(1);
            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("MainPage", redirect.ActionName);
            Assert.Equal("Admin", redirect.ControllerName);
            Assert.Equal("FoodMg", redirect.RouteValues["tab"]);
        }

        [Fact]
        public async Task Delete_Post_Successful_EmployeeRole_RedirectsToEmployeeMainPage()
        {
            var food = new FoodViewModel { FoodId = 1, Name = "Popcorn" };
            _mockService.Setup(s => s.GetByIdAsync(1)).ReturnsAsync(food);
            _mockService.Setup(s => s.HasRelatedInvoicesAsync(1)).ReturnsAsync(false);
            _mockService.Setup(s => s.DeleteAsync(1)).ReturnsAsync(true);
            SetupUserRole("Employee");
            
            var result = await _controller.Delete(1);
            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("MainPage", redirect.ActionName);
            Assert.Equal("Employee", redirect.ControllerName);
            Assert.Equal("FoodMg", redirect.RouteValues["tab"]);
        }

        [Fact]
        public async Task Delete_Post_ServiceReturnsFalse_AdminRole_RedirectsToAdminMainPage()
        {
            var food = new FoodViewModel { FoodId = 1, Name = "Popcorn" };
            _mockService.Setup(s => s.GetByIdAsync(1)).ReturnsAsync(food);
            _mockService.Setup(s => s.HasRelatedInvoicesAsync(1)).ReturnsAsync(false);
            _mockService.Setup(s => s.DeleteAsync(1)).ReturnsAsync(false);
            SetupUserRole("Admin");
            
            var result = await _controller.Delete(1);
            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("MainPage", redirect.ActionName);
            Assert.Equal("Admin", redirect.ControllerName);
            Assert.Equal("FoodMg", redirect.RouteValues["tab"]);
        }

        [Fact]
        public async Task Delete_Post_ServiceReturnsFalse_EmployeeRole_RedirectsToEmployeeMainPage()
        {
            var food = new FoodViewModel { FoodId = 1, Name = "Popcorn" };
            _mockService.Setup(s => s.GetByIdAsync(1)).ReturnsAsync(food);
            _mockService.Setup(s => s.HasRelatedInvoicesAsync(1)).ReturnsAsync(false);
            _mockService.Setup(s => s.DeleteAsync(1)).ReturnsAsync(false);
            SetupUserRole("Employee");
            
            var result = await _controller.Delete(1);
            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("MainPage", redirect.ActionName);
            Assert.Equal("Employee", redirect.ControllerName);
            Assert.Equal("FoodMg", redirect.RouteValues["tab"]);
        }

        [Fact]
        public async Task ToggleStatus_Post_Successful_AdminRole_RedirectsToAdminMainPage()
        {
            _mockService.Setup(s => s.ToggleStatusAsync(1)).ReturnsAsync(true);
            SetupUserRole("Admin");
            
            var result = await _controller.ToggleStatus(1);
            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("MainPage", redirect.ActionName);
            Assert.Equal("Admin", redirect.ControllerName);
            Assert.Equal("FoodMg", redirect.RouteValues["tab"]);
        }

        [Fact]
        public async Task ToggleStatus_Post_Successful_EmployeeRole_RedirectsToEmployeeMainPage()
        {
            _mockService.Setup(s => s.ToggleStatusAsync(1)).ReturnsAsync(true);
            SetupUserRole("Employee");
            
            var result = await _controller.ToggleStatus(1);
            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("MainPage", redirect.ActionName);
            Assert.Equal("Employee", redirect.ControllerName);
            Assert.Equal("FoodMg", redirect.RouteValues["tab"]);
        }

        [Fact]
        public async Task ToggleStatus_Post_ServiceReturnsFalse_AdminRole_RedirectsToAdminMainPage()
        {
            _mockService.Setup(s => s.ToggleStatusAsync(1)).ReturnsAsync(false);
            SetupUserRole("Admin");
            
            var result = await _controller.ToggleStatus(1);
            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("MainPage", redirect.ActionName);
            Assert.Equal("Admin", redirect.ControllerName);
            Assert.Equal("FoodMg", redirect.RouteValues["tab"]);
        }

        [Fact]
        public async Task ToggleStatus_Post_ServiceReturnsFalse_EmployeeRole_RedirectsToEmployeeMainPage()
        {
            _mockService.Setup(s => s.ToggleStatusAsync(1)).ReturnsAsync(false);
            SetupUserRole("Employee");
            
            var result = await _controller.ToggleStatus(1);
            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("MainPage", redirect.ActionName);
            Assert.Equal("Employee", redirect.ControllerName);
            Assert.Equal("FoodMg", redirect.RouteValues["tab"]);
        }

        [Fact]
        public void Role_Property_ReturnsUserRole()
        {
            SetupUserRole("Admin");
            var role = _controller.role;
            Assert.Equal("Admin", role);
        }

        [Fact]
        public void Role_Property_NoUser_ReturnsNull()
        {
            // No user setup, so role should be null
            var role = _controller.role;
            Assert.Null(role);
        }

        [Fact]
        public void Role_Property_NoRoleClaim_ReturnsNull()
        {
            // Setup user without role claim
            var claims = new List<Claim>();
            var identity = new ClaimsIdentity(claims, "Test");
            var principal = new ClaimsPrincipal(identity);
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal }
            };
            
            var role = _controller.role;
            Assert.Null(role);
        }

        [Fact]
        public void Role_Property_UserWithDifferentClaim_ReturnsNull()
        {
            // Setup user with different claim type
            var claims = new List<Claim>
            {
                new Claim("CustomClaim", "Admin")
            };
            var identity = new ClaimsIdentity(claims, "Test");
            var principal = new ClaimsPrincipal(identity);
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal }
            };
            
            var role = _controller.role;
            Assert.Null(role);
        }
    }
} 