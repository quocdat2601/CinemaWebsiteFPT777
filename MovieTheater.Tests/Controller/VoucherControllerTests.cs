using Xunit;
using Moq;
using MovieTheater.Controllers;
using MovieTheater.Service;
using MovieTheater.Models;
using MovieTheater.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.SignalR;
using MovieTheater.Hubs;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;
using System.Linq;
using Newtonsoft.Json.Linq;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using System.Threading;
using System.IO;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc.Controllers;

namespace MovieTheater.Tests.Controller
{
    public class VoucherControllerTests
    {
        private readonly Mock<IVoucherService> _voucherServiceMock;
        private readonly Mock<IWebHostEnvironment> _envMock;
        private readonly Mock<IHubContext<DashboardHub>> _hubMock;
        private readonly VoucherController _controller;

        public VoucherControllerTests()
        {
            _voucherServiceMock = new Mock<IVoucherService>();
            _envMock = new Mock<IWebHostEnvironment>();
            _envMock.Setup(e => e.WebRootPath).Returns("wwwroot");
            _hubMock = new Mock<IHubContext<DashboardHub>>();
            _controller = new VoucherController(_voucherServiceMock.Object, _envMock.Object, _hubMock.Object);
        }

        private VoucherController CreateControllerWithTempData(string role = "Admin")
        {
            var hubMock = new Mock<IHubContext<DashboardHub>>();
            var clientsMock = new Mock<IHubClients>();
            var clientProxyMock = new Mock<IClientProxy>();
            clientProxyMock
                .Setup(x => x.SendCoreAsync(It.IsAny<string>(), It.IsAny<object[]>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            clientsMock.Setup(x => x.All).Returns(clientProxyMock.Object);
            hubMock.Setup(x => x.Clients).Returns(clientsMock.Object);

            var controller = new VoucherController(_voucherServiceMock.Object, _envMock.Object, hubMock.Object);
            controller.TempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>());
            
            // Set up user with role
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "test-user"),
                new Claim(ClaimTypes.Role, role)
            };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            var principal = new ClaimsPrincipal(identity);
            
            var httpContext = new DefaultHttpContext();
            httpContext.User = principal;
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext,
                ActionDescriptor = new ControllerActionDescriptor()
            };
            
            return controller;
        }

        [Fact]
        public void Index_ReturnsViewWithAllVouchers()
        {
            // Arrange
            var vouchers = new List<Voucher> { new Voucher { VoucherId = "V1" } };
            _voucherServiceMock.Setup(s => s.GetAvailableVouchers("test-user")).Returns(vouchers);
            var controller = CreateControllerWithTempData();
            
            // Mock User claims
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "test-user")
            };
            var identity = new ClaimsIdentity(claims, "Test");
            var principal = new ClaimsPrincipal(identity);
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal }
            };
            
            // Act
            var result = controller.Index();
            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Equal(vouchers, viewResult.Model);
        }

        [Fact]
        public void AdminIndex_ReturnsViewWithFilteredVouchers()
        {
            // Arrange
            var vouchers = new List<Voucher> { 
                new Voucher { 
                    VoucherId = "V1",
                    Code = "TESTCODE",
                    IsUsed = false,
                    ExpiryDate = DateTime.Now.AddDays(5), // Expiring soon
                    Account = new Account { 
                        AccountId = "A1",
                        FullName = "Test User",
                        Email = "test@example.com",
                        PhoneNumber = "1234567890"
                    }
                } 
            };
            _voucherServiceMock.Setup(s => s.GetAll()).Returns(vouchers);
            // Act
            var result = _controller.AdminIndex("TEST", "active", "expiring_soon");
            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Null(viewResult.ViewName); // Default view name is null
            Assert.Equal(vouchers, viewResult.Model);
        }

        [Fact]
        public void Details_ReturnsViewResult_WithVoucher()
        {
            // Arrange
            var voucherId = "testId";
            var voucher = new Voucher { VoucherId = voucherId };
            _voucherServiceMock.Setup(s => s.GetById(voucherId)).Returns(voucher);
            // Act
            var result = _controller.Details(voucherId);
            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Equal(voucher, viewResult.Model);
        }

        [Fact]
        public void Details_ReturnsNotFound_WhenVoucherNull()
        {
            // Arrange
            _voucherServiceMock.Setup(s => s.GetById("notfound")).Returns((Voucher)null);
            // Act
            var result = _controller.Details("notfound");
            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public void GetVoucherDetails_ReturnsJson_WhenVoucherFound()
        {
            // Arrange
            var voucherId = "V1";
            var voucher = new Voucher
            {
                VoucherId = voucherId,
                Code = "C1",
                AccountId = "A1",
                Value = 100,
                CreatedDate = DateTime.Now.AddDays(-1),
                ExpiryDate = DateTime.Now.AddDays(10),
                IsUsed = false,
                Image = "img.jpg"
            };
            _voucherServiceMock.Setup(s => s.GetById(voucherId)).Returns(voucher);
            // Act
            var result = _controller.GetVoucherDetails(voucherId);
            var json = Assert.IsType<JsonResult>(result);
            // Assert
            var jObj = JObject.FromObject(json.Value);
            Assert.True(jObj["success"].Value<bool>());
            Assert.Equal(voucherId, jObj["voucher"]["id"].ToString());
        }

        [Fact]
        public void GetVoucherDetails_ReturnsJsonError_WhenVoucherNull()
        {
            // Arrange
            _voucherServiceMock.Setup(s => s.GetById("notfound")).Returns((Voucher)null);
            // Act
            var result = _controller.GetVoucherDetails("notfound");
            var json = Assert.IsType<JsonResult>(result);
            // Assert
            var jObj = JObject.FromObject(json.Value);
            Assert.False(jObj["success"].Value<bool>());
        }

        [Fact]
        public void Create_Get_ReturnsView()
        {
            // Act
            var result = _controller.Create();
            // Assert
            Assert.IsType<ViewResult>(result);
        }

        [Fact]
        public void Create_Post_ValidModel_RedirectsToIndex()
        {
            // Arrange
            var voucher = new Voucher { Value = 100 };
            _voucherServiceMock.Setup(s => s.GenerateVoucherId()).Returns("V1");
            // Act
            var result = _controller.Create(voucher);
            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectResult.ActionName);
        }

        [Fact]
        public void Create_Post_InvalidModel_ReturnsView()
        {
            // Arrange
            _controller.ModelState.AddModelError("err", "err");
            var voucher = new Voucher();
            // Act
            var result = _controller.Create(voucher);
            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Equal(voucher, viewResult.Model);
        }

        [Fact]
        public void Edit_Get_ReturnsView_WithVoucher()
        {
            var voucherId = "V1";
            var voucher = new Voucher { VoucherId = voucherId };
            _voucherServiceMock.Setup(s => s.GetById(voucherId)).Returns(voucher);
            var result = _controller.Edit(voucherId);
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Equal(voucher, viewResult.Model);
        }

        [Fact]
        public void Edit_Get_ReturnsNotFound_WhenVoucherNull()
        {
            _voucherServiceMock.Setup(s => s.GetById("notfound")).Returns((Voucher)null);
            var result = _controller.Edit("notfound");
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public void Edit_Post_ValidModel_RedirectsToIndex()
        {
            var voucher = new Voucher { VoucherId = "V1" };
            var result = _controller.Edit(voucher);
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectResult.ActionName);
        }

        [Fact]
        public void Edit_Post_InvalidModel_ReturnsView()
        {
            _controller.ModelState.AddModelError("err", "err");
            var voucher = new Voucher();
            var result = _controller.Edit(voucher);
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Equal(voucher, viewResult.Model);
        }

        [Fact]
        public void Delete_Get_ReturnsView_WithVoucher()
        {
            var voucherId = "V1";
            var voucher = new Voucher { VoucherId = voucherId };
            _voucherServiceMock.Setup(s => s.GetById(voucherId)).Returns(voucher);
            var result = _controller.Delete(voucherId);
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Equal(voucher, viewResult.Model);
        }

        [Fact]
        public void Delete_Get_ReturnsNotFound_WhenVoucherNull()
        {
            _voucherServiceMock.Setup(s => s.GetById("notfound")).Returns((Voucher)null);
            var result = _controller.Delete("notfound");
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public void DeleteConfirmed_DeletesVoucher_AndRedirects()
        {
            var voucherId = "V1";
            var result = _controller.DeleteConfirmed(voucherId);
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectResult.ActionName);
        }

        [Fact]
        public async Task AdminDelete_ReturnsRedirect_WhenVoucherNotFound()
        {
            _voucherServiceMock.Setup(s => s.GetById("notfound")).Returns((Voucher)null);
            var controller = CreateControllerWithTempData();
            var result = await controller.AdminDelete("notfound", null);
            var redirect = Assert.IsType<RedirectResult>(result);
            Assert.Contains("VoucherMg", redirect.Url);
            Assert.Equal("Voucher not found.", controller.TempData["ToastMessage"]);
        }

        [Fact]
        public async Task AdminDelete_DeletesVoucher_AndRedirects()
        {
            var voucher = new Voucher { VoucherId = "V1" };
            _voucherServiceMock.Setup(s => s.GetById("V1")).Returns(voucher);
            var controller = CreateControllerWithTempData("Admin");
            var result = await controller.AdminDelete("V1", null);
            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("MainPage", redirect.ActionName);
            Assert.Equal("Admin", redirect.ControllerName);
            Assert.Equal("Voucher deleted successfully.", controller.TempData["ToastMessage"]);
        }

        [Fact]
        public void AdminEdit_Get_ReturnsRedirect_WhenVoucherNotFound()
        {
            _voucherServiceMock.Setup(s => s.GetById("notfound")).Returns((Voucher)null);
            var controller = CreateControllerWithTempData();
            var result = controller.AdminEdit("notfound");
            var redirect = Assert.IsType<RedirectResult>(result);
            Assert.Contains("Admin/MainPage", redirect.Url);
            Assert.Equal("Voucher not found.", controller.TempData["ToastMessage"]);
        }

        [Fact]
        public void AdminEdit_Get_ReturnsRedirect_WhenVoucherUsedOrExpired()
        {
            var voucher = new Voucher { VoucherId = "V1", IsUsed = true, ExpiryDate = DateTime.Now.AddDays(10) };
            _voucherServiceMock.Setup(s => s.GetById("V1")).Returns(voucher);
            var controller = CreateControllerWithTempData("Admin");
            var result = controller.AdminEdit("V1");
            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("MainPage", redirect.ActionName);
            Assert.Equal("Admin", redirect.ControllerName);
            Assert.Equal("Cannot edit used or expired vouchers.", controller.TempData["ToastMessage"]);
        }

        [Fact]
        public async Task AdminEdit_Post_ReturnsRedirect_WhenVoucherNotFound()
        {
            _voucherServiceMock.Setup(s => s.GetById("notfound")).Returns((Voucher)null);
            var controller = CreateControllerWithTempData();
            var viewModel = new VoucherViewModel { VoucherId = "notfound" };
            var result = await controller.AdminEdit(viewModel, null);
            var redirect = Assert.IsType<RedirectResult>(result);
            Assert.Contains("Admin/MainPage", redirect.Url);
            Assert.Equal("Voucher not found.", controller.TempData["ToastMessage"]);
        }

        [Fact]
        public async Task AdminEdit_Post_ReturnsRedirect_WhenVoucherUsedOrExpired()
        {
            var voucher = new Voucher { VoucherId = "V1", IsUsed = true, ExpiryDate = DateTime.Now.AddDays(10) };
            _voucherServiceMock.Setup(s => s.GetById("V1")).Returns(voucher);
            var controller = CreateControllerWithTempData("Admin");
            var viewModel = new VoucherViewModel { VoucherId = "V1" };
            var result = await controller.AdminEdit(viewModel, null);
            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("MainPage", redirect.ActionName);
            Assert.Equal("Admin", redirect.ControllerName);
            Assert.Equal("Cannot edit used or expired vouchers.", controller.TempData["ToastMessage"]);
        }

        [Fact]
        public async Task AdminEdit_Post_UpdatesImage_WhenImageFileProvided()
        {
            var voucher = new Voucher { VoucherId = "V1", IsUsed = false, ExpiryDate = DateTime.Now.AddDays(10) };
            _voucherServiceMock.Setup(s => s.GetById("V1")).Returns(voucher);

            var controller = CreateControllerWithTempData("Admin");
            var viewModel = new VoucherViewModel
            {
                VoucherId = "V1",
                AccountId = "A1",
                Code = "C1",
                Value = 100,
                CreatedDate = DateTime.Now,
                ExpiryDate = DateTime.Now.AddDays(10),
                IsUsed = false
            };

            var fileMock = new Mock<IFormFile>();
            fileMock.Setup(f => f.Length).Returns(1);
            fileMock.Setup(f => f.FileName).Returns("test.jpg");
            fileMock.Setup(f => f.CopyToAsync(It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            var result = await controller.AdminEdit(viewModel, fileMock.Object);
            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("MainPage", redirect.ActionName);
            Assert.Equal("Admin", redirect.ControllerName);
        }

        [Fact]
        public async Task AdminEdit_Post_ReturnsView_WhenExceptionThrown()
        {
            var voucher = new Voucher { VoucherId = "V1", IsUsed = false, ExpiryDate = DateTime.Now.AddDays(10) };
            _voucherServiceMock.Setup(s => s.GetById("V1")).Returns(voucher);
            _voucherServiceMock.Setup(s => s.Update(It.IsAny<Voucher>())).Throws(new Exception("Update failed"));

            var controller = CreateControllerWithTempData();
            var viewModel = new VoucherViewModel
            {
                VoucherId = "V1",
                AccountId = "A1",
                Code = "C1",
                Value = 100,
                CreatedDate = DateTime.Now,
                ExpiryDate = DateTime.Now.AddDays(10),
                IsUsed = false
            };

            var result = await controller.AdminEdit(viewModel, null);
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Equal(viewModel, viewResult.Model);
            Assert.Contains("Error updating voucher", controller.ModelState[string.Empty].Errors[0].ErrorMessage);
        }

        [Fact]
        public void AdminCreate_Get_ReturnsView()
        {
            var result = _controller.AdminCreate();
            Assert.IsType<ViewResult>(result);
        }

        [Fact]
        public async Task AdminCreate_Post_InvalidModel_ReturnsView()
        {
            var controller = CreateControllerWithTempData();
            controller.ModelState.AddModelError("err", "err");
            var viewModel = new VoucherViewModel();
            var result = await controller.AdminCreate(viewModel, null);
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Equal(viewModel, viewResult.Model);
        }

        [Fact]
        public async Task AdminCreate_Post_UploadsImage_WhenImageFileProvided()
        {
            var controller = CreateControllerWithTempData();
            var viewModel = new VoucherViewModel
            {
                AccountId = "A1",
                Code = "C1",
                Value = 100,
                CreatedDate = DateTime.Now,
                ExpiryDate = DateTime.Now.AddDays(10),
                IsUsed = false
            };
            _voucherServiceMock.Setup(s => s.GenerateVoucherId()).Returns("V1");

            var fileMock = new Mock<IFormFile>();
            fileMock.Setup(f => f.Length).Returns(1);
            fileMock.Setup(f => f.FileName).Returns("test.jpg");
            fileMock.Setup(f => f.CopyToAsync(It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            var result = await controller.AdminCreate(viewModel, fileMock.Object);
            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("MainPage", redirect.ActionName);
        }

        [Fact]
        public async Task AdminCreate_Post_ReturnsView_WhenExceptionThrown()
        {
            var controller = CreateControllerWithTempData();
            var viewModel = new VoucherViewModel
            {
                AccountId = "A1",
                Code = "C1",
                Value = 100,
                CreatedDate = DateTime.Now,
                ExpiryDate = DateTime.Now.AddDays(10),
                IsUsed = false
            };
            _voucherServiceMock.Setup(s => s.GenerateVoucherId()).Returns("V1");
            _voucherServiceMock.Setup(s => s.Add(It.IsAny<Voucher>())).Throws(new Exception("Create failed"));

            var result = await controller.AdminCreate(viewModel, null);
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Equal(viewModel, viewResult.Model);
        }

        [Fact]
        public void GetAllMembers_ReturnsJson()
        {
            var members = new List<Member>
            {
                new Member
                {
                    MemberId = "M1",
                    Score = 10,
                    Account = new Account { AccountId = "A1", FullName = "Test", IdentityCard = "123", Email = "a@b.com", PhoneNumber = "123456" }
                }
            };
            _voucherServiceMock.Setup(s => s.GetAllMembers()).Returns(members);
            var result = _controller.GetAllMembers();
            var json = Assert.IsType<JsonResult>(result);
            Assert.NotNull(json.Value);
        }

        [Fact]
        public void GetAvailableVouchers_ReturnsEmptyList_WhenAccountIdNull()
        {
            var controller = CreateControllerWithTempData();
            // Gán User giả lập để tránh NullReferenceException
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = new System.Security.Claims.ClaimsPrincipal() }
            };
            var result = controller.GetAvailableVouchers(null);
            var json = Assert.IsType<JsonResult>(result);
            var list = Assert.IsType<List<object>>(json.Value);
            Assert.Empty(list);
        }

        [Fact]
        public void GetAvailableVouchers_ReturnsEmptyList_WhenAccountIdFromUserIsNull()
        {
            var controller = CreateControllerWithTempData();
            var userMock = new Mock<System.Security.Claims.ClaimsPrincipal>();
            userMock.Setup(u => u.FindFirst(It.IsAny<string>())).Returns((System.Security.Claims.Claim)null);
            controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = userMock.Object } };
            var result = controller.GetAvailableVouchers("");
            var json = Assert.IsType<JsonResult>(result);
            var list = Assert.IsType<List<object>>(json.Value);
            Assert.Empty(list);
        }

        [Fact]
        public void GetAvailableVouchers_ReturnsVouchers_WhenAccountIdFromUser()
        {
            var vouchers = new List<Voucher>
            {
                new Voucher { VoucherId = "V1", Code = "C1", Value = 100, ExpiryDate = DateTime.Now.AddDays(5), Image = "img.png" }
            };
            _voucherServiceMock.Setup(s => s.GetAvailableVouchers("A1")).Returns(vouchers);
            var controller = CreateControllerWithTempData();
            var claims = new List<System.Security.Claims.Claim> { new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.NameIdentifier, "A1") };
            var identity = new System.Security.Claims.ClaimsIdentity(claims, "TestAuth");
            var user = new System.Security.Claims.ClaimsPrincipal(identity);
            controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = user } };
            var result = controller.GetAvailableVouchers("");
            var json = Assert.IsType<JsonResult>(result);
            Assert.NotNull(json.Value);
        }

        [Fact]
        public void GetAllMembers_ReturnsEmptyList_WhenNoMembers()
        {
            _voucherServiceMock.Setup(s => s.GetAllMembers()).Returns(new List<Member>());
            var result = _controller.GetAllMembers();
            var json = Assert.IsType<JsonResult>(result);
            Assert.NotNull(json.Value);
        }

        [Fact]
        public void GetVoucherDetails_ReturnsJson_StatusUsed()
        {
            var voucherId = "V1";
            var voucher = new Voucher
            {
                VoucherId = voucherId,
                Code = "C1",
                AccountId = "A1",
                Value = 100,
                CreatedDate = DateTime.Now.AddDays(-10),
                ExpiryDate = DateTime.Now.AddDays(10),
                IsUsed = true,
                Image = "img.jpg"
            };
            _voucherServiceMock.Setup(s => s.GetById(voucherId)).Returns(voucher);
            var result = _controller.GetVoucherDetails(voucherId);
            var json = Assert.IsType<JsonResult>(result);
            var jObj = Newtonsoft.Json.Linq.JObject.FromObject(json.Value);
            Assert.True(jObj["success"].Value<bool>());
            Assert.Equal(voucherId, jObj["voucher"]["id"].ToString());
            Assert.True(jObj["voucher"]["isUsed"].Value<bool>());
        }

        [Fact]
        public void GetVoucherDetails_ReturnsJson_StatusExpired()
        {
            var voucherId = "V2";
            var voucher = new Voucher
            {
                VoucherId = voucherId,
                Code = "C2",
                AccountId = "A2",
                Value = 100,
                CreatedDate = DateTime.Now.AddDays(-10),
                ExpiryDate = DateTime.Now.AddDays(-1),
                IsUsed = false,
                Image = "img.jpg"
            };
            _voucherServiceMock.Setup(s => s.GetById(voucherId)).Returns(voucher);
            var result = _controller.GetVoucherDetails(voucherId);
            var json = Assert.IsType<JsonResult>(result);
            var jObj = Newtonsoft.Json.Linq.JObject.FromObject(json.Value);
            Assert.True(jObj["success"].Value<bool>());
            Assert.Equal(voucherId, jObj["voucher"]["id"].ToString());
            Assert.False(jObj["voucher"]["isUsed"].Value<bool>());
        }

        [Fact]
        public void GetVoucherDetails_ReturnsJson_StatusActive_ExpiringSoon()
        {
            var voucherId = "V3";
            var voucher = new Voucher
            {
                VoucherId = voucherId,
                Code = "C3",
                AccountId = "A3",
                Value = 100,
                CreatedDate = DateTime.Now.AddDays(-1),
                ExpiryDate = DateTime.Now.AddDays(3),
                IsUsed = false,
                Image = "img.jpg"
            };
            _voucherServiceMock.Setup(s => s.GetById(voucherId)).Returns(voucher);
            var result = _controller.GetVoucherDetails(voucherId);
            var json = Assert.IsType<JsonResult>(result);
            var jObj = Newtonsoft.Json.Linq.JObject.FromObject(json.Value);
            Assert.True(jObj["success"].Value<bool>());
            Assert.Equal(voucherId, jObj["voucher"]["id"].ToString());
            Assert.False(jObj["voucher"]["isUsed"].Value<bool>());
        }

        [Fact]
        public void AdminEdit_Get_ReturnsRedirect_WhenVoucherExpired()
        {
            var voucher = new Voucher { VoucherId = "V1", IsUsed = false, ExpiryDate = DateTime.Now.AddDays(-1) };
            _voucherServiceMock.Setup(s => s.GetById("V1")).Returns(voucher);
            var controller = CreateControllerWithTempData("Admin");
            var result = controller.AdminEdit("V1");
            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("MainPage", redirect.ActionName);
            Assert.Equal("Admin", redirect.ControllerName);
            Assert.Equal("Cannot edit used or expired vouchers.", controller.TempData["ToastMessage"]);
        }

        [Fact]
        public async Task AdminEdit_Post_DeletesOldImage_WhenImageFileProvided()
        {
            var voucher = new Voucher { VoucherId = "V1", IsUsed = false, ExpiryDate = DateTime.Now.AddDays(10), Image = "/images/vouchers/old.jpg" };
            _voucherServiceMock.Setup(s => s.GetById("V1")).Returns(voucher);
            var controller = CreateControllerWithTempData("Admin");
            var viewModel = new VoucherViewModel
            {
                VoucherId = "V1",
                AccountId = "A1",
                Code = "C1",
                Value = 100,
                CreatedDate = DateTime.Now,
                ExpiryDate = DateTime.Now.AddDays(10),
                IsUsed = false
            };
            var fileMock = new Mock<IFormFile>();
            fileMock.Setup(f => f.Length).Returns(1);
            fileMock.Setup(f => f.FileName).Returns("test.jpg");
            fileMock.Setup(f => f.CopyToAsync(It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            // Tạo file giả lập để test xóa
            var uploadsFolder = Path.Combine("wwwroot", "images", "vouchers");
            Directory.CreateDirectory(uploadsFolder);
            var oldImagePath = Path.Combine("wwwroot", "images", "vouchers", "old.jpg");
            File.WriteAllText(oldImagePath, "test");
            var result = await controller.AdminEdit(viewModel, fileMock.Object);
            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("MainPage", redirect.ActionName);
            Assert.Equal("Admin", redirect.ControllerName);
            // File cũ đã bị xóa
            Assert.False(File.Exists(oldImagePath));
        }

        [Fact]
        public async Task AdminEdit_Post_ValidModel_ImageFileNull()
        {
            var voucher = new Voucher { VoucherId = "V1", IsUsed = false, ExpiryDate = DateTime.Now.AddDays(10) };
            _voucherServiceMock.Setup(s => s.GetById("V1")).Returns(voucher);
            var controller = CreateControllerWithTempData("Admin");
            var viewModel = new VoucherViewModel
            {
                VoucherId = "V1",
                AccountId = "A1",
                Code = "C1",
                Value = 100,
                CreatedDate = DateTime.Now,
                ExpiryDate = DateTime.Now.AddDays(10),
                IsUsed = false
            };
            var result = await controller.AdminEdit(viewModel, null);
            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("MainPage", redirect.ActionName);
            Assert.Equal("Admin", redirect.ControllerName);
        }

        [Fact]
        public async Task AdminCreate_Post_ValidModel_ImageFileNull()
        {
            var controller = CreateControllerWithTempData("Admin");
            var viewModel = new VoucherViewModel
            {
                AccountId = "A1",
                Code = "C1",
                Value = 100,
                CreatedDate = DateTime.Now,
                ExpiryDate = DateTime.Now.AddDays(10),
                IsUsed = false
            };
            _voucherServiceMock.Setup(s => s.GenerateVoucherId()).Returns("V1");
            var result = await controller.AdminCreate(viewModel, null);
            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("MainPage", redirect.ActionName);
        }

        [Fact]
        public void DeleteConfirmed_CallsDeleteAndRedirects()
        {
            var voucherId = "V1";
            var voucher = new Voucher { VoucherId = voucherId };
            _voucherServiceMock.Setup(s => s.GetById(voucherId)).Returns(voucher);
            var controller = CreateControllerWithTempData();
            var result = controller.DeleteConfirmed(voucherId);
            _voucherServiceMock.Verify(s => s.Delete(voucherId), Times.Once);
            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirect.ActionName);
        }
    }
} 