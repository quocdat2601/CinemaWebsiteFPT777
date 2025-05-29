using Microsoft.AspNetCore.Mvc;
using MovieTheater.Service;
using MovieTheater.ViewModels;

namespace MovieTheater.Controllers
{
    public class MyAccountController : Controller
    {
        private readonly IAccountService _service;
        private readonly ILogger<AccountController> _logger;

        public MyAccountController(IAccountService service, ILogger<AccountController> logger)
        {
            _service = service;
            _logger = logger;
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
            ModelState.Remove("Password"); // hoặc chỉ khi bạn không yêu cầu nhập lại password

            if (!ModelState.IsValid)
            {

                return PartialView("~/Views/Account/Tabs/Profile.cshtml", model);
            }

            try
            {
                var demouser = _service.GetById("AC007");
                var success = _service.Update1(demouser.AccountId, model);

                if (!success)
                {
                    string errorMessage = "Update failed";
                    return Json(new { success = false, error = errorMessage });
                }

                // Cập nhật thành công
                string successMessage = "Profile updated successfully!";
                return Json(new { success = true, reloadTab = "Profile", toast = successMessage });

            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Error during update: {ex.Message}");
                return Json(new { success = false, error = ex.Message });
            }
        }

        // --- OTP Password Change Endpoints ---

        [HttpPost]
        public IActionResult SendOtp()
        {
            // Get current user's email (replace with actual user retrieval)
            var user = _service.GetCurrentUser();
            if (user == null || string.IsNullOrEmpty(user.Email))
                return Json(new { success = false, error = "User email not found." });

            // Generate OTP
            var otp = new Random().Next(100000, 999999).ToString();
            var expiry = DateTime.UtcNow.AddMinutes(5);

            // Store OTP and expiry in session
            HttpContext.Session.SetString("PasswordOtp", otp);
            HttpContext.Session.SetString("PasswordOtpExpiry", expiry.ToString("o"));

            // Send OTP to user's email via service
            var emailSent = _service.SendOtpEmail(user.Email, otp);
            if (!emailSent)
                return Json(new { success = false, error = "Failed to send OTP email. Please try again later." });

            return Json(new { success = true, message = "OTP sent to your email." });
        }

        [HttpPost]
        public IActionResult VerifyOtp([FromBody] VerifyOtpViewModel model)
        {
            // Trim whitespace from the received OTP from the ViewModel
            var receivedOtp = model?.Otp?.Trim();

            var storedOtp = HttpContext.Session.GetString("PasswordOtp");
            var expiryStr = HttpContext.Session.GetString("PasswordOtpExpiry");

            if (string.IsNullOrEmpty(storedOtp) || string.IsNullOrEmpty(expiryStr))
                return Json(new { success = false, error = "OTP not found. Please request a new one." });

            if (!DateTime.TryParse(expiryStr, null, System.Globalization.DateTimeStyles.RoundtripKind, out var expiry))
                return Json(new { success = false, error = "OTP expiry error." });

            if (DateTime.UtcNow > expiry)
                return Json(new { success = false, error = "OTP expired. Please request a new one." });

            if (receivedOtp != storedOtp)
            {
                return Json(new { success = false, error = "Incorrect OTP." });
            }

            // Mark OTP as verified (could set a flag in session)
            HttpContext.Session.SetString("PasswordOtpVerified", "true");
            return Json(new { success = true });
        }

        [HttpPost]
        public IActionResult ChangePassword(string newPassword, string confirmPassword)
        {
            var otpVerified = HttpContext.Session.GetString("PasswordOtpVerified");
            if (otpVerified != "true")
                return Json(new { success = false, error = "OTP not verified." });

            if (string.IsNullOrEmpty(newPassword) || newPassword != confirmPassword)
                return Json(new { success = false, error = "Passwords do not match." });

            // Get current user (replace with actual user retrieval)
            var user = _service.GetCurrentUser();
            if (user == null)
                return Json(new { success = false, error = "User not found." });

            // Update password in DB via service using username
            var result = _service.UpdatePasswordByUsername(user.Username, newPassword);

            if (!result)
                return Json(new { success = false, error = "Failed to update password." });

            // Clear OTP session
            HttpContext.Session.Remove("PasswordOtp");
            HttpContext.Session.Remove("PasswordOtpExpiry");
            HttpContext.Session.Remove("PasswordOtpVerified");

            // Redirect to MainPage with toast message as query parameter
            return RedirectToAction("MainPage", "MyAccount", new { tab = "Profile", toast = "Password updated successfully!" });
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

            // Return full View
            return View(viewModel);
        }
    }
}
