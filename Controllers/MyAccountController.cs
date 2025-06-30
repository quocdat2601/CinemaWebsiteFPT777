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
        private readonly IVoucherService _voucherService;
        private static readonly Dictionary<string, (string Otp, DateTime Expiry)> _otpStore = new();
        private readonly IRankService _rankService;

        public MyAccountController(IAccountService service, ILogger<MyAccountController> logger, IJwtService jwtService, 
            IVoucherService voucherService,
            IRankService rankService)
        {
            _service = service;
            _logger = logger;
            _rankService = rankService;
            _jwtService = jwtService;
            _voucherService = voucherService;
        }

        /// <summary>
        /// [GET] api/MyAccount/MainPage
        /// Trang chính tài khoản người dùng, hiển thị tab mặc định là 'Profile'.
        /// </summary>
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
        /// [GET] api/MyAccount/LoadTab
        /// Load nội dung tab tương ứng trong tài khoản (Profile, Rank, Score, Voucher, History).
        /// </summary>
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
                case "Rank":
                    return PartialView("~/Views/Account/Tabs/Rank.cshtml");
                case "Score":
                    return PartialView("~/Views/Account/Tabs/Score.cshtml");
                case "Voucher":
                    if (user == null)
                        return NotFound();
                    var allVouchers = _voucherService.GetAll();
                    var userVouchers = allVouchers.Where(v => v.AccountId == user.AccountId).ToList();
                    return PartialView("~/Views/Account/Tabs/Voucher.cshtml", userVouchers);
                case "History":
                    return PartialView("~/Views/Account/Tabs/History.cshtml");
                default:
                    return Content("Tab not found.");
            }
        }

        /// <summary>
        /// [POST] api/MyAccount/Edit
        /// Cập nhật thông tin hồ sơ người dùng.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(ProfilePageViewModel model, string action)
        {
            var user = _service.GetCurrentUser();
            if (user == null)
            {
                TempData["ErrorMessage"] = "User session expired. Please log in again.";
                return RedirectToAction("Login", "Account");
            }

            if (action == "updateImage")
            {
                // Minimal validation for image upload
                ModelState.Remove("Profile.FullName");
                ModelState.Remove("Profile.DateOfBirth");
                ModelState.Remove("Profile.Gender");
                ModelState.Remove("Profile.IdentityCard");
                ModelState.Remove("Profile.Email");
                ModelState.Remove("Profile.Address");
                ModelState.Remove("Profile.PhoneNumber");

                if (!ModelState.IsValid)
                {
                    // If validation fails, reload the tab with the errors
                    TempData["ErrorMessage"] = "An error occurred during image upload.";
                    return RedirectToAction("MainPage", new { tab = "Profile" });
                }

                var registerModel = new RegisterViewModel
                {
                    AccountId = user.AccountId,
                    Username = user.Username,
                    // Pass the uploaded file to the service. The service now handles saving.
                    ImageFile = model.Profile.ImageFile,
                    // Pass the rest of the user's data to prevent it from being wiped out
                    FullName = user.FullName,
                    DateOfBirth = user.DateOfBirth,
                    Gender = user.Gender,
                    IdentityCard = user.IdentityCard,
                    Email = user.Email,
                    Address = user.Address,
                    PhoneNumber = user.PhoneNumber,
                    Password = user.Password,
                    Image = user.Image // Pass the current image name for deletion purposes
                };

                var success = _service.Update(user.AccountId, registerModel);
                if (success)
                {
                    TempData["ToastMessage"] = "Profile image updated successfully!";
                }
                else
                {
                    TempData["ErrorMessage"] = "Image update failed.";
                }
                return RedirectToAction("MainPage", new { tab = "Profile" });
            }
            else if (action == "editProfile")
            {
                // Standard validation for profile fields
                if (!ModelState.IsValid)
                {
                    var errors = string.Join(", ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                    TempData["ErrorMessage"] = $"Update failed: {errors}";
                    // It's better to redirect here as returning the partial view can cause issues with page state
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
                    ImageFile = null    // Ensure we do not process a file
                };

                var success = _service.Update(user.AccountId, registerModel);
                if (success)
                {
                    TempData["ToastMessage"] = "Profile updated successfully!";
                }
                else
                {
                    TempData["ErrorMessage"] = "Update failed.";
                }
                return RedirectToAction("MainPage", new { tab = "Profile" });
            }

            // Fallback for any other action
            return RedirectToAction("MainPage", new { tab = "Profile" });
        }

        /// <summary>
        /// [POST] api/MyAccount/SendOtp
        /// Gửi mã OTP đến email người dùng để xác thực thay đổi mật khẩu.
        /// </summary>
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

        /// <summary>
        /// [POST] api/MyAccount/VerifyOtp
        /// Kiểm tra mã OTP do người dùng nhập.
        /// </summary>
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
        /// [POST] api/MyAccount/ChangePasswordAsync
        /// Đổi mật khẩu người dùng sau khi xác thực OTP.
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> ChangePasswordAsync(string currentPassword, string newPassword, string confirmPassword)
        {
            var user = _service.GetCurrentUser();
            if (user == null)
            {
                return View("~/Views/Account/Tabs/ChangePassword.cshtml");
            }

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

        /// <summary>
        /// [GET] api/MyAccount/ChangePassword
        /// Trả về view đổi mật khẩu người dùng.
        /// </summary>
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

        [HttpPost]
        public async Task<IActionResult> UpdateProfile(ProfileUpdateViewModel model)
        {
            // Implementation of the method
            // This method should be implemented to handle the update of the profile
            return View("~/Views/Account/MainPage.cshtml", _service.GetCurrentUser());
        }
    }
}

