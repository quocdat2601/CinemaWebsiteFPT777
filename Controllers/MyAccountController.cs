using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MovieTheater.Service;
using MovieTheater.ViewModels;

namespace MovieTheater.Controllers
{
    [Authorize]
    public class MyAccountController : Controller
    {
        private readonly IAccountService _service;
        private readonly ILogger<MyAccountController> _logger;
        private readonly IJwtService _jwtService;
        private static readonly Dictionary<string, (string Otp, DateTime Expiry)> _otpStore = new();

        public MyAccountController(IAccountService service, ILogger<MyAccountController> logger, IJwtService jwtService)
        {
            _service = service;
            _logger = logger;
            _jwtService = jwtService;
        }

        [HttpGet]
        public IActionResult MainPage(string tab = "Profile")
        {
            ViewData["ActiveTab"] = tab;
            var user = _service.GetCurrentUser();

            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var model = new ProfileUpdateViewModel
            {
                AccountId = user.AccountId,
                Username = user.Username,
                FullName = user.FullName,
                DateOfBirth = user.DateOfBirth,
                Gender = user.Gender,
                IdentityCard = user.IdentityCard,
                Email = user.Email,
                Address = user.Address,
                PhoneNumber = user.PhoneNumber,
                Image = user.Image,
                IsGoogleAccount = user.IsGoogleAccount
            };

            return View("~/Views/Account/MainPage.cshtml", model);
        }

        [HttpGet]
        public IActionResult LoadTab(string tab)
        {
            var user = _service.GetCurrentUser();

            //TEST HARD-CODE
            //var user = _service.GetDemoUser();
            switch (tab)
            {
                case "Profile":
                    if (user == null)
                        return NotFound();
                    var profileModel = new ProfileUpdateViewModel
                    {
                        AccountId = user.AccountId,
                        FullName = user.FullName,
                        DateOfBirth = user.DateOfBirth,
                        Gender = user.Gender,
                        IdentityCard = user.IdentityCard,
                        Email = user.Email,
                        Address = user.Address,
                        PhoneNumber = user.PhoneNumber,
                        Image = user.Image,
                        IsGoogleAccount = user.IsGoogleAccount
                    };
                    return PartialView("~/Views/Account/Tabs/Profile.cshtml", profileModel);
                case "Rank":
                    return PartialView("~/Views/Account/Tabs/Rank.cshtml");
                case "Score":
                    return PartialView("~/Views/Account/Tabs/Score.cshtml");
                case "Voucher":
                    return PartialView("~/Views/Account/Tabs/Voucher.cshtml");
                case "History":
                    return PartialView("~/Views/Account/Tabs/History.cshtml");
                //case "Password":
                //    return PartialView("~/Views/Account/Tabs/ChangePassword.cshtml");
                default:
                    return Content("Tab not found.");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(ProfileUpdateViewModel model)
        {
            var user = _service.GetCurrentUser();
            var timestamp = DateTime.UtcNow;

            if (user == null)
            {
                TempData["ErrorMessage"] = "User not found";
                return PartialView("~/Views/Account/Tabs/Profile.cshtml", model);
            }

            if (!ModelState.IsValid)
            {
                var errors = string.Join(", ", ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage));

                _logger.LogWarning("Update failed validation at {Time}. Errors: {Errors}",
                    DateTime.UtcNow, errors);

                TempData["ErrorMessage"] = $"{errors}";
                return PartialView("~/Views/Account/Tabs/Profile.cshtml", model);
            }

            try
            {
                var registerModel = new RegisterViewModel
                {
                    AccountId = model.AccountId,
                    Username = user.Username,
                    Password = user.Password,
                    FullName = model.FullName,
                    DateOfBirth = model.DateOfBirth,
                    Gender = model.Gender,
                    IdentityCard = model.IdentityCard,
                    Email = model.Email,
                    Address = model.Address,
                    PhoneNumber = model.PhoneNumber,
                    Image = model.Image,
                    ImageFile = model.ImageFile
                };

                var success = _service.Update(user.AccountId, registerModel);

                if (!success)
                {
                    _logger.LogWarning("Failed to update profile. AccountId: {AccountId}, Time: {Time}", user.AccountId, timestamp);
                    TempData["ErrorMessage"] = "Update failed";
                    return PartialView("~/Views/Account/Tabs/Profile.cshtml", model);
                }

                TempData["ToastMessage"] = "Profile updated successfully!";
                return RedirectToAction("MainPage", new { tab = "Profile" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception during profile update. AccountId: {AccountId}, Time: {Time}", user.AccountId, DateTime.UtcNow);
                TempData["ErrorMessage"] = ex.Message;
                return PartialView("~/Views/Account/Tabs/Profile.cshtml", model);
            }
        }

        // --- OTP Password Change Endpoints ---

        [HttpPost]
        public IActionResult SendOtp()
        {
            var user = _service.GetCurrentUser();
            if (user == null || string.IsNullOrEmpty(user.Email))
                return Json(new { success = false, error = "User email not found." });

            _logger.LogInformation($"[SendOtp] accountId={user.AccountId}");

            var otp = new Random().Next(100000, 999999).ToString();
            var expiry = DateTime.UtcNow.AddMinutes(10);

            var otpStored = _service.StoreOtp(user.AccountId, otp, expiry);
            if (!otpStored)
                return Json(new { success = false, error = "Failed to store OTP. Please try again later." });

            var emailSent = _service.SendOtpEmail(user.Email, otp);
            if (!emailSent)
                return Json(new { success = false, error = "Failed to send OTP email. Please try again later." });

            return Json(new { success = true, message = "OTP sent to your email." });
        }

        [HttpPost]
        public IActionResult VerifyOtp([FromBody] VerifyOtpViewModel model)
        {
            var user = _service.GetCurrentUser();
            if (user == null)
                return Json(new { success = false, error = "User not found." });

            _logger.LogInformation($"[VerifyOtp] accountId={user.AccountId}");

            var receivedOtp = model?.Otp?.Trim();
            var otpValid = _service.VerifyOtp(user.AccountId, receivedOtp);
            if (!otpValid)
                return Json(new { success = false, error = "Invalid or expired OTP." });

            return Json(new { success = true });
        }

        [HttpPost]
        public async Task<IActionResult> ChangePasswordAsync(string currentPassword, string newPassword, string confirmPassword)
        {
            // Get current user from JWT claims
            var user = _service.GetCurrentUser();
            if (user == null)
            {
                return View("~/Views/Account/Tabs/ChangePassword.cshtml");
            }

            // Verify current password
            if (string.IsNullOrEmpty(currentPassword))
            {
                TempData["ErrorMessage"] = "Current password cannot be null";
                return View("~/Views/Account/Tabs/ChangePassword.cshtml");
            }

            if (!_service.VerifyCurrentPassword(user.Username, currentPassword))
            {
                TempData["ErrorMessage"] = "Invalid current password";
                return View("~/Views/Account/Tabs/ChangePassword.cshtml");
            }

            if (string.IsNullOrEmpty(newPassword) || newPassword != confirmPassword)
            {
                TempData["ErrorMessage"] = "Invalid new password";
                return View("~/Views/Account/Tabs/ChangePassword.cshtml");
            }

            if (currentPassword == newPassword)
            {
                TempData["ErrorMessage"] = "New password cannot be the same as current password";
                return View("~/Views/Account/Tabs/ChangePassword.cshtml");
            }

            // Update password in DB via service
            var result = _service.UpdatePasswordByUsername(user.Username, newPassword);
            _service.ClearOtp(user.AccountId);

            if (!result)
            {
                TempData["ErrorMessage"] = "Failed to update password";
                return View("~/Views/Account/Tabs/ChangePassword.cshtml");
            }

            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            Response.Cookies.Delete("JwtToken");
            TempData["ToastMessage"] = "Password updated successfully! Please log back in.";
            return RedirectToAction("Login", "Account");

        }

        [HttpGet]
        public IActionResult ChangePassword()
        {
            var user = _service.GetCurrentUser();
            if (user == null)
                return RedirectToAction("Login", "Account");

            var viewModel = new RegisterViewModel
            {
                Username = user.Username,
                Email = user.Email
            };

            // Return the view from the new location
            return View("~/Views/Account/Tabs/ChangePassword.cshtml", viewModel);
        }
    }
}
