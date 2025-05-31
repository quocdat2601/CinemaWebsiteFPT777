using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
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
        public IActionResult MainPage(string tab = "Information")
        {
            ViewData["ActiveTab"] = tab;
            return View("~/Views/Account/MainPage.cshtml");
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
                    return PartialView("~/Views/Account/Tabs/Profile.cshtml", user);
                case "Information":
                    return PartialView("~/Views/Account/Tabs/Information.cshtml");
                case "Rank":
                    return PartialView("~/Views/Account/Tabs/Rank.cshtml");
                case "Score":
                    return PartialView("~/Views/Account/Tabs/Score.cshtml");
                case "Voucher":
                    return PartialView("~/Views/Account/Tabs/Voucher.cshtml");
                case "History":
                    return PartialView("~/Views/Account/Tabs/History.cshtml");
                default:
                    return Content("Tab not found.");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(ProfileViewModel model)
        {
            ModelState.Remove("Password");
            ModelState.Remove("AccountId");
            //var demouser = _service.GetById("AC007");
            var user = _service.GetCurrentUser();
            var timestamp = DateTime.UtcNow;

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Profile update failed: Invalid model state. Data: {@Model}", model);
                return Json(new { success = false, error = " Invalid model state" });
            }

            try
            {
                var success = _service.UpdateAccount(user.AccountId, model);

                if (!success)
                {
                    _logger.LogWarning("Failed to update profile. AccountId: {AccountId}, Time: {Time}", user.AccountId, timestamp);
                    string errorMessage = "Update failed";
                    return Json(new { success = false, error = errorMessage });
                }

                // Cập nhật thành công
                _logger.LogInformation("Profile updated successfully. AccountId: {AccountId}, Time: {Time}", user.AccountId, timestamp);
                string successMessage = "Profile updated successfully!";
                return Json(new { success = true, reloadTab = "Profile", toast = successMessage });

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception during profile update. AccountId: {AccountId}, Time: {Time}", user.AccountId, DateTime.UtcNow);
                ModelState.AddModelError("", $"Error during update: {ex.Message}");
                return Json(new { success = false, error = ex.Message });
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
            var expiry = DateTime.UtcNow.AddMinutes(5);

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
        public IActionResult ChangePassword(string newPassword, string confirmPassword)
        {
            // Get current user from JWT claims
            var user = _service.GetCurrentUser();
            if (user == null)
                return Json(new { success = false, error = "User not found." });

            if (string.IsNullOrEmpty(newPassword) || newPassword != confirmPassword)
                return Json(new { success = false, error = "Passwords do not match." });

            // Check if new password is the same as the old password
            if (user.Password == newPassword)
                return Json(new { success = false, error = "New password must be different from the old password." });

            // Update password in DB via service
            var result = _service.UpdatePasswordByUsername(user.Username, newPassword);
            if (!result)
                return Json(new { success = false, error = "Failed to update password." });

            // Clear OTP from database/cache
            _service.ClearOtp(user.AccountId);

            // Always return JSON on success
            return Json(new { success = true, message = "Password updated successfully!" });
        }

        [HttpGet]
        public IActionResult ChangePassword()
        {
            var user = _service.GetCurrentUser();
            if (user == null)
                return RedirectToAction("Login", "Account");

            var viewModel = new ProfileViewModel
            {
                Username = user.Username,
                Email = user.Email
            };

            // Return the view from the new location
            return View("~/Views/Account/Tabs/ChangePassword.cshtml", viewModel);
        }
    }
}
