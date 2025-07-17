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
            var promo = new Promotion { PromotionId = 1, Title = "T", Detail = "D", DiscountLevel = 10, StartTime = System.DateTime.Now, EndTime = System.DateTime.Now.AddDays(1), IsActive = true };
            _mockService.Setup(s => s.GetById(1)).Returns(promo);

            var result = _controller.Edit(1) as ViewResult;
            Assert.NotNull(result);
            Assert.IsType<PromotionViewModel>(result.Model);
        }

        [Fact]
        public void Edit_Get_ReturnsNotFound_WhenPromotionDoesNotExist()
        {
            _mockService.Setup(s => s.GetById(1)).Returns((Promotion)null);
            var result = _controller.Edit(1);
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async void Edit_Post_IdMismatch_ReturnsNotFound()
        {
            var vm = new PromotionViewModel { PromotionId = 2 };
            var result = await _controller.Edit(1, vm, null);
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async void Edit_Post_InvalidModel_ReturnsView()
        {
            var vm = new PromotionViewModel { PromotionId = 1 };
            _controller.ModelState.AddModelError("Title", "Required");
            var result = await _controller.Edit(1, vm, null) as ViewResult;
            Assert.NotNull(result);
            Assert.Equal(vm, result.Model);
        }

        [Fact]
        public async void Edit_Post_PromotionNotFound_ReturnsNotFound()
        {
            var vm = new PromotionViewModel { PromotionId = 1 };
            _mockService.Setup(s => s.GetById(1)).Returns((Promotion)null);
            _controller.ModelState.Clear();
            var result = await _controller.Edit(1, vm, null);
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async void Edit_Post_ValidModel_Redirects()
        {
            var promo = new Promotion { PromotionId = 1 };
            var vm = new PromotionViewModel { PromotionId = 1, Title = "T", Detail = "D", DiscountLevel = 10, StartTime = System.DateTime.Now, EndTime = System.DateTime.Now.AddDays(1), IsActive = true };
            _mockService.Setup(s => s.GetById(1)).Returns(promo);
            _mockService.Setup(s => s.Update(It.IsAny<Promotion>()));
            _mockService.Setup(s => s.Save());
            _controller.ModelState.Clear();
            var result = await _controller.Edit(1, vm, null);
            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("MainPage", redirect.ActionName);
            Assert.Equal("Admin", redirect.ControllerName);
        }

        [Fact]
        public async void DeleteConfirmed_Success_Redirects()
        {
            var promo = new Promotion { PromotionId = 1, Image = null };
            _mockService.Setup(s => s.GetById(1)).Returns(promo);
            _mockService.Setup(s => s.Delete(1));
            _mockService.Setup(s => s.Save());
            var result = await _controller.DeleteConfirmed(1);
            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("MainPage", redirect.ActionName);
            Assert.Equal("Admin", redirect.ControllerName);
        }

        [Fact]
        public async void DeleteConfirmed_Fail_RedirectsWithToast()
        {
            _mockService.Setup(s => s.GetById(999)).Returns((Promotion)null);
            var result = await _controller.DeleteConfirmed(999);
            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("MainPage", redirect.ActionName);
            Assert.Equal("Admin", redirect.ControllerName);
        }
    }
}