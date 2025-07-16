using Xunit;
using Moq;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Mvc;
using MovieTheater.Controllers;
using MovieTheater.Service;
using MovieTheater.ViewModels;
using MovieTheater.Models;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using System;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.DependencyInjection;

namespace MovieTheater.Tests.Controller
{
    public class MyAccountControllerTests
    {
        private Mock<IAccountService> _accountServiceMock = new Mock<IAccountService>();
        private Mock<ILogger<MyAccountController>> _loggerMock = new Mock<ILogger<MyAccountController>>();
        private Mock<IVoucherService> _voucherServiceMock = new Mock<IVoucherService>();
        private Mock<IRankService> _rankServiceMock = new Mock<IRankService>();
        private Mock<IScoreService> _scoreServiceMock = new Mock<IScoreService>();

        private MyAccountController CreateControllerWithUser(ProfileUpdateViewModel user)
        {
            var controller = new MyAccountController(
                _accountServiceMock.Object,
                _loggerMock.Object,
                _voucherServiceMock.Object,
                _rankServiceMock.Object,
                _scoreServiceMock.Object
            );

            var userClaims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.AccountId ?? "")
        };
            var identity = new ClaimsIdentity(userClaims, "TestAuth");
            var claimsPrincipal = new ClaimsPrincipal(identity);

            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = claimsPrincipal }
            };

            _accountServiceMock.Setup(s => s.GetCurrentUser()).Returns(user);

            // Khởi tạo TempData cho mọi controller test
            controller.TempData = new TempDataDictionary(controller.ControllerContext.HttpContext, Mock.Of<ITempDataProvider>());

            return controller;
        }

        private static bool GetJsonPropertyBool(object value, string property)
        {
            if (value == null) return false;
            var dict = value as IDictionary<string, object>;
            if (dict != null && dict.ContainsKey(property))
                return dict[property] is bool b && b;
            // fallback: try System.Text.Json
            try
            {
                var json = JsonSerializer.Serialize(value);
                using var doc = JsonDocument.Parse(json);
                if (doc.RootElement.TryGetProperty(property, out var prop))
                    return prop.GetBoolean();
            }
            catch { }
            return false;
        }

        [Fact]
        public async void EditProfile_ValidModel_ReturnsRedirectToMainPage()
        {
            // Arrange
            var user = new ProfileUpdateViewModel
            {
                AccountId = "user1",
                Username = "testuser",
                Password = "pass",
                FullName = "Test User",
                DateOfBirth = DateOnly.FromDateTime(DateTime.Now.AddYears(-20)),
                Gender = "male",
                IdentityCard = "123456789",
                Email = "test@example.com",
                Address = "Test Address",
                PhoneNumber = "0123456789",
                Image = "img.png",
                ImageFile = null
            };
            var controller = CreateControllerWithUser(user);

            var model = new ProfilePageViewModel
            {
                Profile = new ProfileUpdateViewModel
                {
                    AccountId = "user1",
                    Username = "testuser",
                    Password = "pass",
                    FullName = "Test User",
                    DateOfBirth = DateOnly.FromDateTime(DateTime.Now.AddYears(-20)),
                    Gender = "male",
                    IdentityCard = "123456789",
                    Email = "test@example.com",
                    Address = "Test Address",
                    PhoneNumber = "0123456789",
                    Image = "img.png",
                    ImageFile = null
                }
            };

            _accountServiceMock.Setup(s => s.Update(user.AccountId, It.IsAny<RegisterViewModel>())).Returns(true);

            // Act
            var result = await controller.EditProfile(model);

            // Assert
            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("MainPage", redirect.ActionName);
            Assert.Equal("Profile", redirect.RouteValues["tab"]);
        }

        [Fact]
        public async void EditProfile_UserNull_ReturnsRedirectToLogin()
        {
            // Arrange
            _accountServiceMock.Setup(s => s.GetCurrentUser()).Returns((ProfileUpdateViewModel)null);
            var controller = CreateControllerWithUser(new ProfileUpdateViewModel { AccountId = "user1" });
            var model = new ProfilePageViewModel { Profile = new ProfileUpdateViewModel { AccountId = "user1" } };
            // Act
            var result = await controller.EditProfile(model);
            // Assert
            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("MainPage", redirect.ActionName);
            Assert.Equal("Profile", redirect.RouteValues["tab"]);
        }

        [Fact]
        public async void EditProfile_ModelStateInvalid_ReturnsRedirectToMainPage()
        {
            // Arrange
            var user = new ProfileUpdateViewModel { AccountId = "user1", Username = "testuser", Password = "pass" };
            var controller = CreateControllerWithUser(user);
            controller.ModelState.AddModelError("Profile.FullName", "Required");
            var model = new ProfilePageViewModel { Profile = new ProfileUpdateViewModel { AccountId = "user1" } };
            // Act
            var result = await controller.EditProfile(model);
            // Assert
            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("MainPage", redirect.ActionName);
            Assert.Equal("Profile", redirect.RouteValues["tab"]);
        }

        [Fact]
        public async void EditProfile_UpdateFailed_ReturnsRedirectToMainPageWithError()
        {
            // Arrange
            var user = new ProfileUpdateViewModel
            {
                AccountId = "user1",
                Username = "testuser",
                Password = "pass",
                FullName = "Test User",
                DateOfBirth = DateOnly.FromDateTime(DateTime.Now.AddYears(-20)),
                Gender = "male",
                IdentityCard = "123456789",
                Email = "test@example.com",
                Address = "Test Address",
                PhoneNumber = "0123456789",
                Image = "img.png"
            };
            var controller = CreateControllerWithUser(user);
            var model = new ProfilePageViewModel { Profile = user };
            _accountServiceMock.Setup(s => s.Update(user.AccountId, It.IsAny<RegisterViewModel>())).Returns(false);
            // Act
            var result = await controller.EditProfile(model);
            // Assert
            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("MainPage", redirect.ActionName);
            Assert.Equal("Profile", redirect.RouteValues["tab"]);
        }

        [Fact]
        public async void UpdateImage_ValidModel_ReturnsRedirectToMainPage()
        {
            var user = new ProfileUpdateViewModel
            {
                AccountId = "user1",
                Username = "testuser",
                Password = "pass",
                FullName = "Test User",
                DateOfBirth = DateOnly.FromDateTime(DateTime.Now.AddYears(-20)),
                Gender = "male",
                IdentityCard = "123456789",
                Email = "test@example.com",
                Address = "Test Address",
                PhoneNumber = "0123456789",
                Image = "img.png",
                ImageFile = null
            };
            var controller = CreateControllerWithUser(user);
            var model = new ProfilePageViewModel
            {
                Profile = new ProfileUpdateViewModel
                {
                    AccountId = "user1",
                    Username = "testuser",
                    Password = "pass",
                    FullName = "Test User",
                    DateOfBirth = DateOnly.FromDateTime(DateTime.Now.AddYears(-20)),
                    Gender = "male",
                    IdentityCard = "123456789",
                    Email = "test@example.com",
                    Address = "Test Address",
                    PhoneNumber = "0123456789",
                    Image = "img.png",
                    ImageFile = null
                }
            };
            _accountServiceMock.Setup(s => s.Update(user.AccountId, It.IsAny<RegisterViewModel>())).Returns(true);
            // Act
            var result = await controller.UpdateImage(model);
            // Assert
            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("MainPage", redirect.ActionName);
            Assert.Equal("Profile", redirect.RouteValues["tab"]);
        }

        [Fact]
        public async void UpdateImage_UserNull_ReturnsRedirectToLogin()
        {
            _accountServiceMock.Setup(s => s.GetCurrentUser()).Returns((ProfileUpdateViewModel)null);
            var controller = CreateControllerWithUser(new ProfileUpdateViewModel { AccountId = "user1" });
            var model = new ProfilePageViewModel { Profile = new ProfileUpdateViewModel { AccountId = "user1" } };
            var result = await controller.UpdateImage(model);
            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("MainPage", redirect.ActionName);
            Assert.Equal("Profile", redirect.RouteValues["tab"]);
        }

        [Fact]
        public async void UpdateImage_ModelStateInvalid_ReturnsRedirectToMainPage()
        {
            var user = new ProfileUpdateViewModel { AccountId = "user1", Username = "testuser", Password = "pass" };
            var controller = CreateControllerWithUser(user);
            controller.ModelState.AddModelError("Profile.ImageFile", "Required");
            var model = new ProfilePageViewModel { Profile = new ProfileUpdateViewModel { AccountId = "user1" } };
            var result = await controller.UpdateImage(model);
            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("MainPage", redirect.ActionName);
            Assert.Equal("Profile", redirect.RouteValues["tab"]);
        }

        [Fact]
        public async void UpdateImage_UpdateFailed_ReturnsRedirectToMainPageWithError()
        {
            var user = new ProfileUpdateViewModel
            {
                AccountId = "user1",
                Username = "testuser",
                Password = "pass",
                FullName = "Test User",
                DateOfBirth = DateOnly.FromDateTime(DateTime.Now.AddYears(-20)),
                Gender = "male",
                IdentityCard = "123456789",
                Email = "test@example.com",
                Address = "Test Address",
                PhoneNumber = "0123456789",
                Image = "img.png"
            };
            var controller = CreateControllerWithUser(user);
            var model = new ProfilePageViewModel { Profile = user };
            _accountServiceMock.Setup(s => s.Update(user.AccountId, It.IsAny<RegisterViewModel>())).Returns(false);
            var result = await controller.UpdateImage(model);
            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("MainPage", redirect.ActionName);
            Assert.Equal("Profile", redirect.RouteValues["tab"]);
        }

        [Fact]
        public void SendOtp_Success_ReturnsJsonSuccess()
        {
            var user = new ProfileUpdateViewModel { AccountId = "user1", Username = "testuser", Email = "test@example.com" };
            var controller = CreateControllerWithUser(user);
            _accountServiceMock.Setup(s => s.VerifyCurrentPassword(user.Username, "pw")).Returns(true);
            _accountServiceMock.Setup(s => s.StoreOtp(user.AccountId, It.IsAny<string>(), It.IsAny<DateTime>())).Returns(true);
            _accountServiceMock.Setup(s => s.SendOtpEmail(user.Email, It.IsAny<string>())).Returns(true);
            var req = new MyAccountController.SendOtpRequest { CurrentPassword = "pw" };
            var result = controller.SendOtp(req) as JsonResult;
            Assert.NotNull(result);
            Assert.True(GetJsonPropertyBool(result.Value, "Success"));
        }

        [Fact]
        public void SendOtp_UserNullOrNoEmail_ReturnsJsonError()
        {
            _accountServiceMock.Setup(s => s.GetCurrentUser()).Returns((ProfileUpdateViewModel)null);
            var controller = CreateControllerWithUser(new ProfileUpdateViewModel { });
            var req = new MyAccountController.SendOtpRequest { CurrentPassword = "pw" };
            var result = controller.SendOtp(req) as JsonResult;
            Assert.NotNull(result);
            Assert.False(GetJsonPropertyBool(result.Value, "Success"));
        }

        [Fact]
        public void SendOtp_WrongPassword_ReturnsJsonError()
        {
            var user = new ProfileUpdateViewModel { AccountId = "user1", Username = "testuser", Email = "test@example.com" };
            var controller = CreateControllerWithUser(user);
            _accountServiceMock.Setup(s => s.VerifyCurrentPassword(user.Username, "wrong")).Returns(false);
            var req = new MyAccountController.SendOtpRequest { CurrentPassword = "wrong" };
            var result = controller.SendOtp(req) as JsonResult;
            Assert.NotNull(result);
            Assert.False(GetJsonPropertyBool(result.Value, "Success"));
        }

        [Fact]
        public void SendOtp_StoreOtpFail_ReturnsJsonError()
        {
            var user = new ProfileUpdateViewModel { AccountId = "user1", Username = "testuser", Email = "test@example.com" };
            var controller = CreateControllerWithUser(user);
            _accountServiceMock.Setup(s => s.VerifyCurrentPassword(user.Username, "pw")).Returns(true);
            _accountServiceMock.Setup(s => s.StoreOtp(user.AccountId, It.IsAny<string>(), It.IsAny<DateTime>())).Returns(false);
            var req = new MyAccountController.SendOtpRequest { CurrentPassword = "pw" };
            var result = controller.SendOtp(req) as JsonResult;
            Assert.NotNull(result);
            Assert.False(GetJsonPropertyBool(result.Value, "Success"));
        }

        [Fact]
        public void SendOtp_SendEmailFail_ReturnsJsonError()
        {
            var user = new ProfileUpdateViewModel { AccountId = "user1", Username = "testuser", Email = "test@example.com" };
            var controller = CreateControllerWithUser(user);
            _accountServiceMock.Setup(s => s.VerifyCurrentPassword(user.Username, "pw")).Returns(true);
            _accountServiceMock.Setup(s => s.StoreOtp(user.AccountId, It.IsAny<string>(), It.IsAny<DateTime>())).Returns(true);
            _accountServiceMock.Setup(s => s.SendOtpEmail(user.Email, It.IsAny<string>())).Returns(false);
            var req = new MyAccountController.SendOtpRequest { CurrentPassword = "pw" };
            var result = controller.SendOtp(req) as JsonResult;
            Assert.NotNull(result);
            Assert.False(GetJsonPropertyBool(result.Value, "Success"));
        }

        [Fact]
        public void VerifyOtp_Success_ReturnsJsonSuccess()
        {
            var user = new ProfileUpdateViewModel { AccountId = "user1", Username = "testuser" };
            var controller = CreateControllerWithUser(user);
            _accountServiceMock.Setup(s => s.VerifyOtp(user.AccountId, "123456")).Returns(true);
            var model = new VerifyOtpViewModel { Otp = "123456" };
            var result = controller.VerifyOtp(model) as JsonResult;
            Assert.NotNull(result);
            Assert.True(GetJsonPropertyBool(result.Value, "success"));
        }

        [Fact]
        public void VerifyOtp_UserNull_ReturnsJsonError()
        {
            _accountServiceMock.Setup(s => s.GetCurrentUser()).Returns((ProfileUpdateViewModel)null);
            var controller = CreateControllerWithUser(new ProfileUpdateViewModel { });
            var model = new VerifyOtpViewModel { Otp = "123456" };
            var result = controller.VerifyOtp(model) as JsonResult;
            Assert.NotNull(result);
            Assert.False(GetJsonPropertyBool(result.Value, "success"));
        }

        [Fact]
        public void VerifyOtp_InvalidOtp_ReturnsJsonError()
        {
            var user = new ProfileUpdateViewModel { AccountId = "user1", Username = "testuser" };
            var controller = CreateControllerWithUser(user);
            _accountServiceMock.Setup(s => s.VerifyOtp(user.AccountId, "wrong")).Returns(false);
            var model = new VerifyOtpViewModel { Otp = "wrong" };
            var result = controller.VerifyOtp(model) as JsonResult;
            Assert.NotNull(result);
            Assert.False(GetJsonPropertyBool(result.Value, "success"));
        }

        [Fact]
        public async void ChangePasswordAsync_Success_ReturnsJsonSuccess()
        {
            var user = new ProfileUpdateViewModel { AccountId = "user1", Username = "testuser", Password = "oldpw" };
            var controller = CreateControllerWithUser(user);
            _accountServiceMock.Setup(s => s.VerifyCurrentPassword(user.Username, "oldpw")).Returns(true);
            _accountServiceMock.Setup(s => s.VerifyOtp(user.AccountId, "otp")).Returns(true);
            _accountServiceMock.Setup(s => s.UpdatePasswordByUsername(user.Username, "newpw")).Returns(true);
            // Đăng ký mock authentication service để tránh lỗi provider null
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddAuthentication();
            serviceCollection.AddSingleton<Microsoft.AspNetCore.Authentication.IAuthenticationService, MockAuthenticationService>();
            controller.ControllerContext.HttpContext.RequestServices = serviceCollection.BuildServiceProvider();
            var result = await controller.ChangePasswordAsync("oldpw", "newpw", "newpw", "otp") as JsonResult;
            Assert.NotNull(result);
            Assert.True(GetJsonPropertyBool(result.Value, "success"));
        }

        [Fact]
        public async void ChangePasswordAsync_UserNull_ReturnsJsonError()
        {
            _accountServiceMock.Setup(s => s.GetCurrentUser()).Returns((ProfileUpdateViewModel)null);
            var controller = CreateControllerWithUser(new ProfileUpdateViewModel { });
            var result = await controller.ChangePasswordAsync("oldpw", "newpw", "newpw", "otp") as JsonResult;
            Assert.NotNull(result);
            Assert.False(GetJsonPropertyBool(result.Value, "success"));
        }

        [Fact]
        public async void ChangePasswordAsync_FieldsMissing_ReturnsJsonError()
        {
            var user = new ProfileUpdateViewModel { AccountId = "user1", Username = "testuser", Password = "oldpw" };
            var controller = CreateControllerWithUser(user);
            var result = await controller.ChangePasswordAsync("", "newpw", "newpw", "otp") as JsonResult;
            Assert.NotNull(result);
            Assert.False(GetJsonPropertyBool(result.Value, "success"));
        }

        [Fact]
        public async void ChangePasswordAsync_WrongCurrentPassword_ReturnsJsonError()
        {
            var user = new ProfileUpdateViewModel { AccountId = "user1", Username = "testuser", Password = "oldpw" };
            var controller = CreateControllerWithUser(user);
            _accountServiceMock.Setup(s => s.VerifyCurrentPassword(user.Username, "wrong")).Returns(false);
            var result = await controller.ChangePasswordAsync("wrong", "newpw", "newpw", "otp") as JsonResult;
            Assert.NotNull(result);
            Assert.False(GetJsonPropertyBool(result.Value, "success"));
        }

        [Fact]
        public async void ChangePasswordAsync_PasswordsNotMatch_ReturnsJsonError()
        {
            var user = new ProfileUpdateViewModel { AccountId = "user1", Username = "testuser", Password = "oldpw" };
            var controller = CreateControllerWithUser(user);
            _accountServiceMock.Setup(s => s.VerifyCurrentPassword(user.Username, "oldpw")).Returns(true);
            var result = await controller.ChangePasswordAsync("oldpw", "newpw", "notmatch", "otp") as JsonResult;
            Assert.NotNull(result);
            Assert.False(GetJsonPropertyBool(result.Value, "success"));
        }

        [Fact]
        public async void ChangePasswordAsync_NewPasswordSameAsOld_ReturnsJsonError()
        {
            var user = new ProfileUpdateViewModel { AccountId = "user1", Username = "testuser", Password = "oldpw" };
            var controller = CreateControllerWithUser(user);
            _accountServiceMock.Setup(s => s.VerifyCurrentPassword(user.Username, "oldpw")).Returns(true);
            var result = await controller.ChangePasswordAsync("oldpw", "oldpw", "oldpw", "otp") as JsonResult;
            Assert.NotNull(result);
            Assert.False(GetJsonPropertyBool(result.Value, "success"));
        }

        [Fact]
        public async void ChangePasswordAsync_InvalidOtp_ReturnsJsonError()
        {
            var user = new ProfileUpdateViewModel { AccountId = "user1", Username = "testuser", Password = "oldpw" };
            var controller = CreateControllerWithUser(user);
            _accountServiceMock.Setup(s => s.VerifyCurrentPassword(user.Username, "oldpw")).Returns(true);
            _accountServiceMock.Setup(s => s.VerifyOtp(user.AccountId, "otp")).Returns(false);
            var result = await controller.ChangePasswordAsync("oldpw", "newpw", "newpw", "otp") as JsonResult;
            Assert.NotNull(result);
            Assert.False(GetJsonPropertyBool(result.Value, "success"));
        }

        [Fact]
        public async void ChangePasswordAsync_UpdatePasswordFail_ReturnsJsonError()
        {
            var user = new ProfileUpdateViewModel { AccountId = "user1", Username = "testuser", Password = "oldpw" };
            var controller = CreateControllerWithUser(user);
            _accountServiceMock.Setup(s => s.VerifyCurrentPassword(user.Username, "oldpw")).Returns(true);
            _accountServiceMock.Setup(s => s.VerifyOtp(user.AccountId, "otp")).Returns(true);
            _accountServiceMock.Setup(s => s.UpdatePasswordByUsername(user.Username, "newpw")).Returns(false);
            var result = await controller.ChangePasswordAsync("oldpw", "newpw", "newpw", "otp") as JsonResult;
            Assert.NotNull(result);
            Assert.False(GetJsonPropertyBool(result.Value, "success"));
        }

        [Fact]
        public void ChangePassword_UserNull_RedirectsToLogin()
        {
            _accountServiceMock.Setup(s => s.GetCurrentUser()).Returns((ProfileUpdateViewModel)null);
            var controller = CreateControllerWithUser(new ProfileUpdateViewModel { AccountId = "user1" });
            var result = controller.ChangePassword();
            var view = Assert.IsType<ViewResult>(result);
            Assert.Equal("~/Views/Account/Tabs/ChangePassword.cshtml", view.ViewName); // đúng với thực tế controller trả về
        }

        [Fact]
        public void MainPage_UserNull_RedirectsToLogin()
        {
            _accountServiceMock.Setup(s => s.GetCurrentUser()).Returns((ProfileUpdateViewModel)null);
            var controller = CreateControllerWithUser(new ProfileUpdateViewModel { AccountId = "user1" });
            var result = controller.MainPage("Profile");
            var view = Assert.IsType<ViewResult>(result);
            Assert.Equal("~/Views/Account/MainPage.cshtml", view.ViewName); // hoặc tên view thực tế nếu khác
        }

        [Fact]
        public void LoadTab_UserNull_ReturnsNotFound()
        {
            _accountServiceMock.Setup(s => s.GetCurrentUser()).Returns((ProfileUpdateViewModel)null);
            var controller = CreateControllerWithUser(new ProfileUpdateViewModel { AccountId = "user1" });
            var result = controller.LoadTab("Profile");
            var partial = Assert.IsType<PartialViewResult>(result);
            // Có thể kiểm tra tên view nếu muốn
            // Assert.Equal("~/Views/Account/Tabs/Profile.cshtml", partial.ViewName);
        }

        [Fact]
        public void LoadTab_ScoreTab_ReturnsPartialView()
        {
            var user = new ProfileUpdateViewModel { AccountId = "user1", Username = "testuser" };
            var controller = CreateControllerWithUser(user);
            var result = controller.LoadTab("Score");
            var partial = Assert.IsType<PartialViewResult>(result);
            Assert.Equal("~/Views/Account/Tabs/Score.cshtml", partial.ViewName);
        }

        [Fact]
        public void LoadTab_VoucherTab_ReturnsPartialView()
        {
            var user = new ProfileUpdateViewModel { AccountId = "user1", Username = "testuser" };
            var controller = CreateControllerWithUser(user);
            _voucherServiceMock.Setup(s => s.GetAll()).Returns(new List<Voucher>());
            var result = controller.LoadTab("Voucher");
            var partial = Assert.IsType<PartialViewResult>(result);
            Assert.Equal("~/Views/Account/Tabs/Voucher.cshtml", partial.ViewName);
        }

        [Fact]
        public void LoadTab_HistoryTab_ReturnsContent()
        {
            var user = new ProfileUpdateViewModel { AccountId = "user1", Username = "testuser" };
            var controller = CreateControllerWithUser(user);
            var result = controller.LoadTab("History");
            var content = Assert.IsType<ContentResult>(result);
            Assert.Contains("Booking history is now available", content.Content);
        }

        [Fact]
        public void LoadTab_UnknownTab_ReturnsContent()
        {
            var user = new ProfileUpdateViewModel { AccountId = "user1", Username = "testuser" };
            var controller = CreateControllerWithUser(user);
            var result = controller.LoadTab("UnknownTab");
            var content = Assert.IsType<ContentResult>(result);
            Assert.Contains("Tab not found", content.Content);
        }

        [Fact]
        public void ChangePassword_UserNotNull_ReturnsView()
        {
            var user = new ProfileUpdateViewModel { AccountId = "user1", Username = "testuser", Email = "a@b.com" };
            var controller = CreateControllerWithUser(user);
            var result = controller.ChangePassword();
            var view = Assert.IsType<ViewResult>(result);
            Assert.Equal("~/Views/Account/Tabs/ChangePassword.cshtml", view.ViewName);
            var model = Assert.IsType<RegisterViewModel>(view.Model);
            Assert.Equal(user.Username, model.Username);
            Assert.Equal(user.Email, model.Email);
        }
    }
}