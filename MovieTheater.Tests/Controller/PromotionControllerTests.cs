using MovieTheater.Controllers;
using MovieTheater.Models;
using MovieTheater.Service;
using MovieTheater.ViewModels;
using Moq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.SignalR;
using MovieTheater.Hubs;
using System;
using System.Threading.Tasks;
using Xunit;
using System.Threading;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using System.IO;

namespace MovieTheater.Tests.Controller
{
    public class PromotionControllerTests
    {
        private readonly Mock<IPromotionService> _mockService;
        private readonly Mock<IWebHostEnvironment> _mockEnv;
        private readonly Mock<IHubContext<DashboardHub>> _mockHub;
        private readonly PromotionController _controller;

        public PromotionControllerTests()
        {
            _mockService = new Mock<IPromotionService>();
            _mockEnv = new Mock<IWebHostEnvironment>();
            _mockHub = new Mock<IHubContext<DashboardHub>>();

            // Mock WebRootPath
            _mockEnv.Setup(e => e.WebRootPath).Returns("C:\\temp");

            // Mock SignalR
            var mockClients = new Mock<IHubClients>();
            var mockClientProxy = new Mock<IClientProxy>();
            mockClients.Setup(c => c.All).Returns(mockClientProxy.Object);
            _mockHub.Setup(h => h.Clients).Returns(mockClients.Object);
            mockClientProxy
                .Setup(proxy => proxy.SendCoreAsync(
                    It.IsAny<string>(),
                    It.IsAny<object[]>(),
                    It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            _controller = new PromotionController(_mockService.Object, _mockEnv.Object, _mockHub.Object);

            // Mock TempData
            var tempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>());
            _controller.TempData = tempData;
        }

        [Fact]
        public void Edit_Get_ReturnsView_WhenPromotionExists()
        {
            // Arrange
            var promo = new Promotion { PromotionId = 1, Title = "T", Detail = "D", DiscountLevel = 10, StartTime = System.DateTime.Now, EndTime = System.DateTime.Now.AddDays(1), IsActive = true };
            _mockService.Setup(s => s.GetById(1)).Returns(promo);

            // Act
            var result = _controller.Edit(1) as ViewResult;

            // Assert
            Assert.NotNull(result);
            Assert.IsType<PromotionViewModel>(result.Model);
        }

        [Fact]
        public void Edit_Get_ReturnsNotFound_WhenPromotionDoesNotExist()
        {
            // Arrange
            _mockService.Setup(s => s.GetById(1)).Returns((Promotion)null);

            // Act
            var result = _controller.Edit(1);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async void Edit_Post_IdMismatch_ReturnsNotFound()
        {
            // Arrange
            var vm = new PromotionViewModel { PromotionId = 2 };

            // Act
            var result = await _controller.Edit(1, vm, null);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async void Edit_Post_InvalidModel_ReturnsView()
        {
            // Arrange
            var vm = new PromotionViewModel { PromotionId = 1 };
            _controller.ModelState.AddModelError("Title", "Required");

            // Act
            var result = await _controller.Edit(1, vm, null) as ViewResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal(vm, result.Model);
        }

        [Fact]
        public async void Edit_Post_PromotionNotFound_ReturnsNotFound()
        {
            // Arrange
            var vm = new PromotionViewModel { PromotionId = 1 };
            _mockService.Setup(s => s.GetById(1)).Returns((Promotion)null);
            _controller.ModelState.Clear();

            // Act
            var result = await _controller.Edit(1, vm, null);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async void Edit_Post_ValidModel_Redirects()
        {
            // Arrange
            var promo = new Promotion { PromotionId = 1 };
            var vm = new PromotionViewModel { PromotionId = 1, Title = "T", Detail = "D", DiscountLevel = 10, StartTime = System.DateTime.Now, EndTime = System.DateTime.Now.AddDays(1), IsActive = true };
            _mockService.Setup(s => s.GetById(1)).Returns(promo);
            _mockService.Setup(s => s.Update(It.IsAny<Promotion>()));
            _mockService.Setup(s => s.Save());
            _controller.ModelState.Clear();

            // Act
            var result = await _controller.Edit(1, vm, null);

            // Assert
            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("MainPage", redirect.ActionName);
            Assert.Equal("Admin", redirect.ControllerName);
        }

        [Fact]
        public async void DeleteConfirmed_Success_Redirects()
        {
            // Arrange
            var promo = new Promotion { PromotionId = 1, Image = null };
            _mockService.Setup(s => s.GetById(1)).Returns(promo);
            _mockService.Setup(s => s.Delete(1));
            _mockService.Setup(s => s.Save());

            // Act
            var result = await _controller.DeleteConfirmed(1);

            // Assert
            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("MainPage", redirect.ActionName);
            Assert.Equal("Admin", redirect.ControllerName);
        }

        [Fact]
        public async void DeleteConfirmed_Fail_RedirectsWithToast()
        {
            // Arrange
            _mockService.Setup(s => s.GetById(999)).Returns((Promotion)null);

            // Act
            var result = await _controller.DeleteConfirmed(999);

            // Assert
            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("MainPage", redirect.ActionName);
            Assert.Equal("Admin", redirect.ControllerName);
        }

        [Fact]
        public void List_ReturnsListView_WithActivePromotions()
        {
            // Arrange
            var now = DateTime.Now;
            var promos = new[]
            {
                new Promotion { PromotionId = 1, Title = "Active1", IsActive = true, StartTime = now.AddDays(-1), EndTime = now.AddDays(1) },
                new Promotion { PromotionId = 2, Title = "Inactive", IsActive = false, StartTime = now.AddDays(-2), EndTime = now.AddDays(2) },
                new Promotion { PromotionId = 3, Title = "Expired", IsActive = true, StartTime = now.AddDays(-10), EndTime = now.AddDays(-1) },
                new Promotion { PromotionId = 4, Title = "Active2", IsActive = true, StartTime = now.AddDays(-2), EndTime = now.AddDays(2) }
            };
            _mockService.Setup(s => s.GetAll()).Returns(promos);

            // Act
            var result = _controller.List() as ViewResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal("List", result.ViewName);
            var model = Assert.IsAssignableFrom<System.Collections.Generic.List<Promotion>>(result.Model);
            Assert.Equal(2, model.Count); // Only 2 active and not expired
            Assert.Contains(model, p => p.PromotionId == 1);
            Assert.Contains(model, p => p.PromotionId == 4);
            // Check order: newest StartTime first
            Assert.True(model[0].StartTime >= model[1].StartTime);
        }

        [Fact]
        public void List_ReturnsListView_WithNoActivePromotions()
        {
            // Arrange
            var now = DateTime.Now;
            var promos = new[]
            {
                new Promotion { PromotionId = 1, Title = "Inactive", IsActive = false, StartTime = now.AddDays(-1), EndTime = now.AddDays(1) },
                new Promotion { PromotionId = 2, Title = "Expired", IsActive = true, StartTime = now.AddDays(-10), EndTime = now.AddDays(-1) }
            };
            _mockService.Setup(s => s.GetAll()).Returns(promos);

            // Act
            var result = _controller.List() as ViewResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal("List", result.ViewName);
            var model = Assert.IsAssignableFrom<System.Collections.Generic.List<Promotion>>(result.Model);
            Assert.Empty(model);
        }



        [Fact]
        public void Create_Get_ReturnsViewWithDefaultModel()
        {
            // Act
            var result = _controller.Create() as ViewResult;

            // Assert
            Assert.NotNull(result);
            var model = Assert.IsType<MovieTheater.ViewModels.PromotionViewModel>(result.Model);
            Assert.True(model.IsActive);
        }

        [Fact]
        public async Task Create_Post_InvalidModel_ReturnsView()
        {
            // Arrange
            var vm = new MovieTheater.ViewModels.PromotionViewModel();
            _controller.ModelState.AddModelError("Title", "Required");

            // Act
            var result = await _controller.Create(vm, null) as ViewResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal(vm, result.Model);
        }

        [Fact]
        public async Task Create_Post_ImageTooLarge_ReturnsView()
        {
            // Arrange
            var vm = new MovieTheater.ViewModels.PromotionViewModel { Title = "T", Detail = "D", DiscountLevel = 1, StartTime = DateTime.Now, EndTime = DateTime.Now.AddDays(1), IsActive = true };
            var file = new Mock<Microsoft.AspNetCore.Http.IFormFile>();
            file.Setup(f => f.Length).Returns(3 * 1024 * 1024L); // 3MB
            file.Setup(f => f.FileName).Returns("test.jpg");
            _controller.ModelState.Clear();
            _mockService.Setup(s => s.GetAll()).Returns(new List<Promotion>());

            // Act
            var result = await _controller.Create(vm, file.Object) as ViewResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal(vm, result.Model);
            Assert.True(_controller.ModelState.ErrorCount > 0);
        }

        [Fact]
        public async Task Create_Post_ImageWrongExtension_ReturnsView()
        {
            // Arrange
            var vm = new MovieTheater.ViewModels.PromotionViewModel { Title = "T", Detail = "D", DiscountLevel = 1, StartTime = DateTime.Now, EndTime = DateTime.Now.AddDays(1), IsActive = true };
            var file = new Mock<Microsoft.AspNetCore.Http.IFormFile>();
            file.Setup(f => f.Length).Returns(1000);
            file.Setup(f => f.FileName).Returns("test.txt");
            _controller.ModelState.Clear();
            _mockService.Setup(s => s.GetAll()).Returns(new List<Promotion>());

            // Act
            var result = await _controller.Create(vm, file.Object) as ViewResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal(vm, result.Model);
            Assert.True(_controller.ModelState.ErrorCount > 0);
        }

        [Fact]
        public async Task Create_Post_Exception_ReturnsViewWithError()
        {
            // Arrange
            var vm = new MovieTheater.ViewModels.PromotionViewModel { Title = "T", Detail = "D", DiscountLevel = 1, StartTime = DateTime.Now, EndTime = DateTime.Now.AddDays(1), IsActive = true };
            _controller.ModelState.Clear();
            _mockService.Setup(s => s.GetAll()).Throws(new Exception("DB error"));

            // Act
            var result = await _controller.Create(vm, null) as ViewResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal(vm, result.Model);
            Assert.True(_controller.ModelState.ErrorCount > 0);
        }

        [Fact]
        public async Task Create_Post_Valid_Redirects()
        {
            // Arrange
            var vm = new MovieTheater.ViewModels.PromotionViewModel { Title = "T", Detail = "D", DiscountLevel = 1, StartTime = DateTime.Now, EndTime = DateTime.Now.AddDays(1), IsActive = true };
            _controller.ModelState.Clear();
            _mockService.Setup(s => s.GetAll()).Returns(new List<Promotion>());
            _mockService.Setup(s => s.Add(It.IsAny<Promotion>()));
            _mockService.Setup(s => s.Save());

            // Act
            var result = await _controller.Create(vm, null);

            // Assert
            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("MainPage", redirect.ActionName);
            Assert.Equal("Admin", redirect.ControllerName);
        }

        [Fact]
        public async Task Create_Post_ImageFileEmpty_SkipImageUpload()
        {
            // Arrange
            var vm = new MovieTheater.ViewModels.PromotionViewModel
            {
                Title = "T", Detail = "D", DiscountLevel = 1,
                StartTime = DateTime.Now, EndTime = DateTime.Now.AddDays(1), IsActive = true
            };
            var file = new Mock<Microsoft.AspNetCore.Http.IFormFile>();
            file.Setup(f => f.Length).Returns(0L); // file rỗng
            file.Setup(f => f.FileName).Returns("test.jpg");
            _controller.ModelState.Clear();
            _mockService.Setup(s => s.GetAll()).Returns(new List<Promotion>());
            _mockService.Setup(s => s.Add(It.IsAny<Promotion>()));
            _mockService.Setup(s => s.Save());

            // Act
            var result = await _controller.Create(vm, file.Object);

            // Assert
            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("MainPage", redirect.ActionName);
            Assert.Equal("Admin", redirect.ControllerName);
        }

        [Fact]
        public async Task Create_Post_ImageFile_NoExtension_ReturnsView()
        {
            // Arrange
            var vm = new MovieTheater.ViewModels.PromotionViewModel
            {
                Title = "T", Detail = "D", DiscountLevel = 1,
                StartTime = DateTime.Now, EndTime = DateTime.Now.AddDays(1), IsActive = true
            };
            var file = new Mock<Microsoft.AspNetCore.Http.IFormFile>();
            file.Setup(f => f.Length).Returns(1000);
            file.Setup(f => f.FileName).Returns("file"); // không có extension
            _controller.ModelState.Clear();
            _mockService.Setup(s => s.GetAll()).Returns(new List<Promotion>());

            // Act
            var result = await _controller.Create(vm, file.Object) as ViewResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal(vm, result.Model);
            Assert.True(_controller.ModelState.ErrorCount > 0);
        }

        [Fact]
        public async Task Create_Post_ImageFile_EmptyExtension_ReturnsView()
        {
            // Arrange
            var vm = new MovieTheater.ViewModels.PromotionViewModel
            {
                Title = "T", Detail = "D", DiscountLevel = 1,
                StartTime = DateTime.Now, EndTime = DateTime.Now.AddDays(1), IsActive = true
            };
            var file = new Mock<Microsoft.AspNetCore.Http.IFormFile>();
            file.Setup(f => f.Length).Returns(1000);
            file.Setup(f => f.FileName).Returns("file."); // extension rỗng
            _controller.ModelState.Clear();
            _mockService.Setup(s => s.GetAll()).Returns(new List<Promotion>());

            // Act
            var result = await _controller.Create(vm, file.Object) as ViewResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal(vm, result.Model);
            Assert.True(_controller.ModelState.ErrorCount > 0);
        }

        [Fact]
        public async Task Edit_Post_Exception_ReturnsViewWithError()
        {
            // Arrange
            var promo = new Promotion { PromotionId = 1 };
            var vm = new PromotionViewModel { PromotionId = 1, Title = "T", Detail = "D", DiscountLevel = 10, StartTime = DateTime.Now, EndTime = DateTime.Now.AddDays(1), IsActive = true };
            _mockService.Setup(s => s.GetById(1)).Returns(promo);
            _mockService.Setup(s => s.Update(It.IsAny<Promotion>())).Throws(new Exception("Update error"));
            _controller.ModelState.Clear();

            // Act
            var result = await _controller.Edit(1, vm, null) as ViewResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal(vm, result.Model);
            Assert.True(_controller.ModelState.ErrorCount > 0);
        }

        [Fact]
        public async Task DeleteConfirmed_Exception_RedirectsWithToast()
        {
            // Arrange
            var promo = new Promotion { PromotionId = 1, Image = null };
            _mockService.Setup(s => s.GetById(1)).Returns(promo);
            _mockService.Setup(s => s.Delete(1)).Throws(new Exception("Delete error"));

            // Act
            var result = await _controller.DeleteConfirmed(1);

            // Assert
            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("MainPage", redirect.ActionName);
            Assert.Equal("Admin", redirect.ControllerName);
            Assert.Equal("Failed to delete promotion.", _controller.TempData["ToastMessage"]);
        }

        [Fact]
        public async Task Edit_Post_ImageFileEmpty_SkipImageUpload()
        {
            // Arrange
            var promo = new Promotion { PromotionId = 1 };
            var vm = new PromotionViewModel { PromotionId = 1, Title = "T", Detail = "D", DiscountLevel = 10, StartTime = DateTime.Now, EndTime = DateTime.Now.AddDays(1), IsActive = true };
            var file = new Mock<Microsoft.AspNetCore.Http.IFormFile>();
            file.Setup(f => f.Length).Returns(0L);
            file.Setup(f => f.FileName).Returns("test.jpg");
            _mockService.Setup(s => s.GetById(1)).Returns(promo);
            _mockService.Setup(s => s.Update(It.IsAny<Promotion>()));
            _mockService.Setup(s => s.Save());
            _controller.ModelState.Clear();

            // Act
            var result = await _controller.Edit(1, vm, file.Object);

            // Assert
            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("MainPage", redirect.ActionName);
            Assert.Equal("Admin", redirect.ControllerName);
        }

        [Fact]
        public async Task Edit_Post_ImageTooLarge_ReturnsView()
        {
            // Arrange
            var promo = new Promotion { PromotionId = 1 };
            var vm = new PromotionViewModel { PromotionId = 1, Title = "T", Detail = "D", DiscountLevel = 10, StartTime = DateTime.Now, EndTime = DateTime.Now.AddDays(1), IsActive = true };
            var file = new Mock<Microsoft.AspNetCore.Http.IFormFile>();
            file.Setup(f => f.Length).Returns(3 * 1024 * 1024L);
            file.Setup(f => f.FileName).Returns("test.jpg");
            _mockService.Setup(s => s.GetById(1)).Returns(promo);
            _controller.ModelState.Clear();

            // Act
            var result = await _controller.Edit(1, vm, file.Object) as ViewResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal(vm, result.Model);
            Assert.True(_controller.ModelState.ErrorCount > 0);
        }

        [Fact]
        public async Task Edit_Post_ImageWrongExtension_ReturnsView()
        {
            // Arrange
            var promo = new Promotion { PromotionId = 1 };
            var vm = new PromotionViewModel { PromotionId = 1, Title = "T", Detail = "D", DiscountLevel = 10, StartTime = DateTime.Now, EndTime = DateTime.Now.AddDays(1), IsActive = true };
            var file = new Mock<Microsoft.AspNetCore.Http.IFormFile>();
            file.Setup(f => f.Length).Returns(1000);
            file.Setup(f => f.FileName).Returns("test.txt");
            _mockService.Setup(s => s.GetById(1)).Returns(promo);
            _controller.ModelState.Clear();

            // Act
            var result = await _controller.Edit(1, vm, file.Object) as ViewResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal(vm, result.Model);
            Assert.True(_controller.ModelState.ErrorCount > 0);
        }

        [Fact]
        public async Task Edit_Post_ImageFile_NoExtension_ReturnsView()
        {
            // Arrange
            var promo = new Promotion { PromotionId = 1 };
            var vm = new PromotionViewModel { PromotionId = 1, Title = "T", Detail = "D", DiscountLevel = 10, StartTime = DateTime.Now, EndTime = DateTime.Now.AddDays(1), IsActive = true };
            var file = new Mock<Microsoft.AspNetCore.Http.IFormFile>();
            file.Setup(f => f.Length).Returns(1000);
            file.Setup(f => f.FileName).Returns("file");
            _mockService.Setup(s => s.GetById(1)).Returns(promo);
            _controller.ModelState.Clear();

            // Act
            var result = await _controller.Edit(1, vm, file.Object) as ViewResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal(vm, result.Model);
            Assert.True(_controller.ModelState.ErrorCount > 0);
        }

        [Fact]
        public async Task Edit_Post_ImageFile_EmptyExtension_ReturnsView()
        {
            // Arrange
            var promo = new Promotion { PromotionId = 1 };
            var vm = new PromotionViewModel { PromotionId = 1, Title = "T", Detail = "D", DiscountLevel = 10, StartTime = DateTime.Now, EndTime = DateTime.Now.AddDays(1), IsActive = true };
            var file = new Mock<Microsoft.AspNetCore.Http.IFormFile>();
            file.Setup(f => f.Length).Returns(1000);
            file.Setup(f => f.FileName).Returns("file.");
            _mockService.Setup(s => s.GetById(1)).Returns(promo);
            _controller.ModelState.Clear();

            // Act
            var result = await _controller.Edit(1, vm, file.Object) as ViewResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal(vm, result.Model);
            Assert.True(_controller.ModelState.ErrorCount > 0);
        }

        [Fact]
        public async Task Edit_Post_ImageFile_Valid_UploadsImageAndRedirects()
        {
            // Arrange
            var promo = new Promotion { PromotionId = 1, Image = "/images/promotions/old.jpg" };
            var vm = new PromotionViewModel { PromotionId = 1, Title = "T", Detail = "D", DiscountLevel = 10, StartTime = DateTime.Now, EndTime = DateTime.Now.AddDays(1), IsActive = true };
            var file = new Mock<Microsoft.AspNetCore.Http.IFormFile>();
            file.Setup(f => f.Length).Returns(1000);
            file.Setup(f => f.FileName).Returns("test.jpg");
            file.Setup(f => f.CopyToAsync(It.IsAny<System.IO.Stream>(), It.IsAny<System.Threading.CancellationToken>()))
                .Returns<System.IO.Stream, System.Threading.CancellationToken>((stream, token) =>
                {
                    stream.WriteByte(0x1);
                    return Task.CompletedTask;
                });
            _mockService.Setup(s => s.GetById(1)).Returns(promo);
            _mockService.Setup(s => s.Update(It.IsAny<Promotion>()));
            _mockService.Setup(s => s.Save());
            _controller.ModelState.Clear();

            // Act
            var result = await _controller.Edit(1, vm, file.Object);

            // Assert
            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("MainPage", redirect.ActionName);
            Assert.Equal("Admin", redirect.ControllerName);
        }

        [Fact]
        public void Delete_Get_ReturnsView_WhenPromotionExistsWithEmptyImage()
        {
            // Arrange
            var promo = new Promotion { PromotionId = 1, Image = "" };
            _mockService.Setup(s => s.GetById(1)).Returns(promo);

            // Act
            var result = _controller.Delete(1) as ViewResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal(promo, result.Model);
        }

        [Fact]
        public async Task DeleteConfirmed_ImageEmpty_SkipDeleteFile()
        {
            // Arrange
            var promo = new Promotion { PromotionId = 1, Image = "" };
            _mockService.Setup(s => s.GetById(1)).Returns(promo);
            _mockService.Setup(s => s.Delete(1));
            _mockService.Setup(s => s.Save());

            // Act
            var result = await _controller.DeleteConfirmed(1);

            // Assert
            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("MainPage", redirect.ActionName);
            Assert.Equal("Admin", redirect.ControllerName);
        }

        [Fact]
        public async Task DeleteConfirmed_ImageFileNotExists_SkipDeleteFile()
        {
            // Arrange
            var promo = new Promotion { PromotionId = 1, Image = "/images/promotions/test.jpg" };
            _mockService.Setup(s => s.GetById(1)).Returns(promo);
            _mockService.Setup(s => s.Delete(1));
            _mockService.Setup(s => s.Save());
            var webRoot = "C:\\temp";
            _mockEnv.Setup(e => e.WebRootPath).Returns(webRoot);
            var filePath = System.IO.Path.Combine(webRoot, "images", "promotions", "test.jpg");
            if (System.IO.File.Exists(filePath)) System.IO.File.Delete(filePath); // Đảm bảo file không tồn tại

            // Act
            var result = await _controller.DeleteConfirmed(1);

            // Assert
            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("MainPage", redirect.ActionName);
            Assert.Equal("Admin", redirect.ControllerName);
        }

        [Fact]
        public async Task DeleteConfirmed_ImageFileExists_DeletesFile()
        {
            // Arrange
            var promo = new Promotion { PromotionId = 1, Image = "/images/promotions/testfile.txt" };
            _mockService.Setup(s => s.GetById(1)).Returns(promo);
            _mockService.Setup(s => s.Delete(1));
            _mockService.Setup(s => s.Save());
            var webRoot = "C:\\temp";
            _mockEnv.Setup(e => e.WebRootPath).Returns(webRoot);
            var filePath = System.IO.Path.Combine(webRoot, "images", "promotions", "testfile.txt");
            System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(filePath));
            System.IO.File.WriteAllText(filePath, "test");

            // Act
            var result = await _controller.DeleteConfirmed(1);

            // Assert
            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("MainPage", redirect.ActionName);
            Assert.Equal("Admin", redirect.ControllerName);
            Assert.False(System.IO.File.Exists(filePath)); // Đã xóa file
        }

        [Fact]
        public void Index_ReturnsView()
        {
            // Act
            var result = _controller.Index() as ViewResult;

            // Assert
            Assert.NotNull(result);
        }
    }
}