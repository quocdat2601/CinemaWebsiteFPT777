using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MovieTheater.Service;
using MovieTheater.ViewModels;
using System.Security.Claims;

namespace MovieTheater.Controllers
{
    [Authorize]
    public class MyAccountController : Controller
    {
        private readonly IAccountService _service;
        private readonly ILogger<MyAccountController> _logger;
        private readonly IVoucherService _voucherService;
        private readonly IRankService _rankService;
        private readonly IScoreService _scoreService;

        public MyAccountController(IAccountService service, ILogger<MyAccountController> logger,
            IVoucherService voucherService,
            IRankService rankService,
            IScoreService scoreService)
        {
            _service = service;
            _logger = logger;
            _rankService = rankService;
            _voucherService = voucherService;
            _scoreService = scoreService;
        }

        private string GetCurrentAccountId()
        {
            return User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        }

        /// <summary>
        /// Trang chính tài khoản người dùng, hiển thị tab mặc định là 'Profile'.
        /// </summary>
        /// <remarks>url: /MyAccount/MainPage (GET)</remarks>
        [HttpGet]
        public IActionResult MainPage(string tab = "Profile")
        {
            ViewData["ActiveTab"] = tab;
            var user = _service.GetCurrentUser();

            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            return View("~/Views/Account/MainPage.cshtml", user);
        }

        /// <summary>
        /// Load nội dung tab tương ứng trong tài khoản (Profile, Rank, Score, Voucher, History).
        /// </summary>
        /// <remarks>url: /MyAccount/LoadTab (GET)</remarks>
        [HttpGet]
        public IActionResult LoadTab(string tab)
        {
            var user = _service.GetCurrentUser();
            if (user == null) return NotFound();

            switch (tab)
            {
                case "Profile":
                    // Check and update rank when loading Profile tab
                    _service.CheckAndUpgradeRank(user.AccountId);

                    var rankInfo = _rankService.GetRankInfoForUser(user.AccountId);
                    var allRanks = _rankService.GetAllRanks();
                    var viewModel = new ProfilePageViewModel
                    {
                        Profile = user,
                        RankInfo = rankInfo,
                        AllRanks = allRanks
                    };
                    return PartialView("~/Views/Account/Tabs/Profile.cshtml", viewModel);
                case "Score":
                    var accountId = GetCurrentAccountId();
                    var currentScore = _scoreService?.GetCurrentScore(accountId) ?? 0;
                    var scoreHistory = _scoreService?.GetScoreHistory(accountId) ?? new List<ScoreHistoryViewModel>();
                    var scoreViewModel = new ScoreHistoryViewModel { CurrentScore = currentScore };
                    return PartialView("~/Views/Account/Tabs/Score.cshtml", scoreViewModel);
                case "Voucher":
                    if (user == null)
                        return NotFound();
                    var allVouchers = _voucherService.GetAll();
                    var userVouchers = allVouchers.Where(v => v.AccountId == user.AccountId).ToList();
                    return PartialView("~/Views/Account/Tabs/Voucher.cshtml", userVouchers);
                case "History":
                    // History tab is now merged in Profile tab, return a message or redirect
                    return Content("Booking history is now available in the Profile tab.");
                default:
                    return Content("Tab not found.");
            }
        }

        /// <summary>
        /// Cập nhật thông tin hồ sơ người dùng.
        /// </summary>
        /// <remarks>url: /MyAccount/Edit (POST)</remarks>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateImage(ProfilePageViewModel model)
        {
            var user = _service.GetCurrentUser();
            if (user == null)
            {
                TempData["ErrorMessage"] = "User session expired. Please log in again.";
                return RedirectToAction("Login", "Account");
            }

            ModelState.Remove("Profile.FullName");
            ModelState.Remove("Profile.DateOfBirth");
            ModelState.Remove("Profile.Gender");
            ModelState.Remove("Profile.IdentityCard");
            ModelState.Remove("Profile.Email");
            ModelState.Remove("Profile.Address");
            ModelState.Remove("Profile.PhoneNumber");

            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "An error occurred during image upload.";
                return RedirectToAction("MainPage", new { tab = "Profile" });
            }

            var registerModel = new RegisterViewModel
            {
                AccountId = user.AccountId,
                Username = user.Username,
                ImageFile = model.Profile.ImageFile,
                FullName = user.FullName,
                DateOfBirth = user.DateOfBirth,
                Gender = user.Gender,
                IdentityCard = user.IdentityCard,
                Email = user.Email,
                Address = user.Address,
                PhoneNumber = user.PhoneNumber,
                Password = user.Password,
                Image = user.Image
            };

            // If we have cropped image data, save it as a file
            if (!string.IsNullOrEmpty(model.Profile.CroppedImageData))
            {
                try
                {
                    var base64Data = model.Profile.CroppedImageData.Replace("data:image/jpeg;base64,", "")
                                                                   .Replace("data:image/png;base64,", "")
                                                                   .Replace("data:image/gif;base64,", "");
                    var imageBytes = Convert.FromBase64String(base64Data);
                    
                    var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images/avatars");
                    var uniqueFileName = Guid.NewGuid().ToString() + "_avatar.jpg";
                    var filePath = Path.Combine(uploadsFolder, uniqueFileName);
                    
                    System.IO.File.WriteAllBytes(filePath, imageBytes);
                    registerModel.Image = $"/images/avatars/{uniqueFileName}";
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error saving cropped image");
                    TempData["ErrorMessage"] = "Error processing image. Please try again.";
                    return RedirectToAction("MainPage", new { tab = "Profile" });
                }
            }

            var success = _service.Update(user.AccountId, registerModel);
            if (success)
            {
                // Refresh user claims after update
                var updatedUser = _service.GetById(user.AccountId);
                if (updatedUser != null)
                {
                    await _service.SignInUserAsync(HttpContext, updatedUser);
                }
                TempData["ToastMessage"] = "Profile image updated successfully!";
            }
            else
            {
                TempData["ErrorMessage"] = "Image update failed.";
            }
            return RedirectToAction("MainPage", new { tab = "Profile" });
        }

        /// <summary>
        /// Cập nhật thông tin hồ sơ người dùng.
        /// </summary>
        /// <remarks>url: /MyAccount/Edit (POST)</remarks>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditProfile(ProfilePageViewModel model)
        {
            var user = _service.GetCurrentUser();
            if (user == null)
            {
                TempData["ErrorMessage"] = "User session expired. Please log in again.";
                return RedirectToAction("Login", "Account");
            }

            // Remove validation for fields that might be empty during update
            ModelState.Remove("Profile.FullName");
            ModelState.Remove("Profile.DateOfBirth");
            ModelState.Remove("Profile.Gender");
            ModelState.Remove("Profile.IdentityCard");
            ModelState.Remove("Profile.Email");
            ModelState.Remove("Profile.Address");
            ModelState.Remove("Profile.PhoneNumber");
            
            if (!ModelState.IsValid)
            {
                var errors = string.Join(", ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                TempData["ErrorMessage"] = $"Update failed: {errors}";
                return RedirectToAction("MainPage", new { tab = "Profile" });
            }

            var registerModel = new RegisterViewModel
            {
                AccountId = model.Profile.AccountId,
                Username = user.Username, // Username is not editable here
                Password = user.Password, // Password is not changed here
                FullName = model.Profile.FullName,
                DateOfBirth = model.Profile.DateOfBirth,
                Gender = model.Profile.Gender,
                IdentityCard = model.Profile.IdentityCard,
                Email = model.Profile.Email, // Email is not editable here
                Address = model.Profile.Address,
                PhoneNumber = model.Profile.PhoneNumber,
                Image = user.Image, // Preserve the existing image
                ImageFile = model.Profile.ImageFile // Allow image upload
            };

            var success = _service.Update(user.AccountId, registerModel);
            if (success)
            {
                // Refresh user claims after update
                var updatedUser = _service.GetById(user.AccountId);
                if (updatedUser != null)
                {
                    await _service.SignInUserAsync(HttpContext, updatedUser);
                }
                TempData["ToastMessage"] = "Profile updated successfully!";
            }
            else
            {
                TempData["ErrorMessage"] = "Update failed.";
            }
            return RedirectToAction("MainPage", new { tab = "Profile" });
        }

        public class SendOtpRequest
        {
            public string CurrentPassword { get; set; }
        }

        public class OtpResponse
        {
            public bool Success { get; set; }
            public string Message { get; set; }
            public string Error { get; set; }
        }

        /// <summary>
        /// Gửi mã OTP đến email người dùng để xác thực thay đổi mật khẩu.
        /// </summary>
        /// <remarks>url: /MyAccount/SendOtp (POST)</remarks>
        [HttpPost]
        public IActionResult SendOtp([FromBody] SendOtpRequest req)
        {
            var user = _service.GetCurrentUser();
            if (user == null || string.IsNullOrEmpty(user.Email))
                return Json(new OtpResponse { Success = false, Error = "User email not found." });

            if (string.IsNullOrEmpty(req.CurrentPassword) || !_service.VerifyCurrentPassword(user.Username, req.CurrentPassword))
            {
                return Json(new OtpResponse { Success = false, Error = "Current password is incorrect." });
            }

            _logger.LogInformation($"[SendOtp] accountId={user.AccountId}");

            var otp = new Random().Next(100000, 999999).ToString();
            var expiry = DateTime.UtcNow.AddMinutes(10);

            var otpStored = _service.StoreOtp(user.AccountId, otp, expiry);
            if (!otpStored)
                return Json(new OtpResponse { Success = false, Error = "Failed to store OTP. Please try again later." });

            var emailSent = _service.SendOtpEmail(user.Email, otp);
            if (!emailSent)
                return Json(new OtpResponse { Success = false, Error = "Failed to send OTP email. Please try again later." });

            return Json(new OtpResponse { Success = true, Message = "OTP sent to your email." });
        }

        /// <summary>
        /// Kiểm tra mã OTP do người dùng nhập.
        /// </summary>
        /// <remarks>url: /MyAccount/VerifyOtp (POST)</remarks>
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

        /// <summary>
        /// Đổi mật khẩu người dùng sau khi xác thực OTP.
        /// </summary>
        /// <remarks>url: /MyAccount/ChangePasswordAsync (POST)</remarks>
        [HttpPost]
        public async Task<IActionResult> ChangePasswordAsync(string currentPassword, string newPassword, string confirmPassword, string otp)
        {
            var user = _service.GetCurrentUser();
            if (user == null)
                return Json(new { success = false, error = "User session expired. Please log in again." });

            if (string.IsNullOrEmpty(currentPassword) || string.IsNullOrEmpty(newPassword) || string.IsNullOrEmpty(confirmPassword))
                return Json(new { success = false, error = "All fields are required." });

            if (!_service.VerifyCurrentPassword(user.Username, currentPassword))
                return Json(new { success = false, error = "Current password is incorrect." });

            if (newPassword != confirmPassword)
                return Json(new { success = false, error = "Passwords do not match." });

            if (currentPassword == newPassword)
                return Json(new { success = false, error = "New password must be different from current password." });

            // Check OTP
            if (!_service.VerifyOtp(user.AccountId, otp))
                return Json(new { success = false, error = "Invalid or expired OTP." });

            var result = _service.UpdatePasswordByUsername(user.Username, newPassword);
            _service.ClearOtp(user.AccountId);

            if (!result)
                return Json(new { success = false, error = "Failed to update password." });

            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            Response.Cookies.Delete("JwtToken");
            return Json(new { success = true });
        }

        /// <summary>
        /// Trang đổi mật khẩu (hiển thị form)
        /// </summary>
        /// <remarks>url: /MyAccount/ChangePassword (GET)</remarks>
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

            return View("~/Views/Account/Tabs/ChangePassword.cshtml", viewModel);
        }

        /// <summary>
        /// Cập nhật thông tin profile (AJAX)
        /// </summary>
        /// <remarks>url: /MyAccount/UpdateProfile (POST)</remarks>
        [HttpPost]
        public async Task<IActionResult> UpdateProfile(ProfileUpdateViewModel model)
        {
            // Implementation of the method
            // This method should be implemented to handle the update of the profile
            return View("~/Views/Account/MainPage.cshtml", _service.GetCurrentUser());
        }
    }
}

