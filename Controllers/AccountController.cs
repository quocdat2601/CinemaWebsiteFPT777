using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MovieTheater.Models;
using MovieTheater.Repository;
using MovieTheater.Service;
using MovieTheater.ViewModels;
using System.Security.Claims;

namespace MovieTheater.Controllers
{
    public class AccountController : Controller
    {
        private readonly IAccountRepository _accountRepository;
        private readonly IAccountService _service;
        private readonly ILogger<AccountController> _logger;
        private readonly IJwtService _jwtService;
        private readonly IEmployeeService _employeeService;

        // Constants for string literals
        private const string LOGIN_ACTION = "Login";
        private const string ERROR_MESSAGE = "ErrorMessage";
        private const string FIRST_TIME_LOGIN = "FirstTimeLogin";
        private const string MAIN_PAGE = "MainPage";
        private const string TOAST_MESSAGE = "ToastMessage";
        private const string INDEX_ACTION = "Index";
        private const string HOME_CONTROLLER = "Home";
        private const string ADMIN_CONTROLLER = "Admin";
        private const string EMPLOYEE_CONTROLLER = "Employee";
        private const string MY_ACCOUNT_CONTROLLER = "MyAccount";
        private const string PROFILE_TAB = "Profile";

        public AccountController(
            IAccountService service,
            ILogger<AccountController> logger,
            IAccountRepository accountRepository,
            IMemberRepository memberRepository,
            IJwtService jwtService, 
            IEmployeeService employeeService)
        {
            _service = service;
            _logger = logger;
            _accountRepository = accountRepository;
            _jwtService = jwtService;
        }

        [HttpGet]
        public IActionResult ExternalLogin()
        {
            var redirectUrl = Url.Action("ExternalLoginCallback", "Account");
            var properties = new AuthenticationProperties { RedirectUri = redirectUrl };
            return Challenge(properties, GoogleDefaults.AuthenticationScheme);
        }

        public async Task<IActionResult> ExternalLoginCallback()
        {
            var result = await HttpContext.AuthenticateAsync(GoogleDefaults.AuthenticationScheme);

            if (!result.Succeeded || result.Principal == null)
                return RedirectToAction(LOGIN_ACTION);

            var claims = result.Principal.Identities.FirstOrDefault()?.Claims;
            var email = claims?.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
            var name = claims?.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value;
            var givenName = claims?.FirstOrDefault(c => c.Type == ClaimTypes.GivenName)?.Value;
            var surname = claims?.FirstOrDefault(c => c.Type == ClaimTypes.Surname)?.Value;
            var picture = claims?.FirstOrDefault(c => c.Type == "picture")?.Value;

            if (string.IsNullOrEmpty(email))
            {
                TempData[ERROR_MESSAGE] = "Google login failed. Email not provided.";
                return RedirectToAction(LOGIN_ACTION);
            }

            // Refactor: Đẩy logic tạo user/member mới vào service
            var user = _service.GetOrCreateGoogleAccount(email, name, givenName, surname, picture);

            // Refactor: Đẩy logic kiểm tra thông tin thiếu vào service
            bool missingInfo = _service.HasMissingProfileInfo(user);
            if (missingInfo)
            {
                TempData[FIRST_TIME_LOGIN] = true;
            }

            if (user.Status == 0)
            {
                TempData[ERROR_MESSAGE] = "Account has been locked!";
                return RedirectToAction(LOGIN_ACTION);
            }

            // Refactor: Đẩy logic tạo claims, sign-in vào service
            await _service.SignInUserAsync(HttpContext, user);

            // Check and update rank
            if (user.RoleId == 3) // Member
            {
                _service.CheckAndUpgradeRank(user.AccountId);
            }

            // Check if it's the first time login and redirect to profile update
            if (TempData[FIRST_TIME_LOGIN] != null && (bool)TempData[FIRST_TIME_LOGIN])
            {
                TempData.Remove(FIRST_TIME_LOGIN);
                return RedirectToAction(MAIN_PAGE, MY_ACCOUNT_CONTROLLER, new { tab = PROFILE_TAB });
            }

            // Generate JWT token for Google login
            var token = _jwtService.GenerateToken(user);

            // Store the token in a cookie
            Response.Cookies.Append("JwtToken", token, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Expires = DateTime.Now.AddMinutes(60)
            });

            TempData[TOAST_MESSAGE] = "Log in successful!";

            // After successful login and before redirecting, add:
            var rankUpMsg = _service.GetAndClearRankUpgradeNotification(user.AccountId);
            if (!string.IsNullOrEmpty(rankUpMsg))
            {
                TempData[TOAST_MESSAGE] += "<br/>" + rankUpMsg;
            }

            // Direct redirect like normal login
            if (user.RoleId == 1)
            {
                return RedirectToAction(MAIN_PAGE, ADMIN_CONTROLLER);
            }
            else if (user.RoleId == 2)
            {
                return RedirectToAction(MAIN_PAGE, EMPLOYEE_CONTROLLER);
            }
            else
            {
                return RedirectToAction(INDEX_ACTION, HOME_CONTROLLER);
            }
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        public async Task<IActionResult> Logout()
        {
            await _service.SignOutUserAsync(HttpContext);
            // Remove the JWT token cookie
            Response.Cookies.Delete("JwtToken");
            TempData[TOAST_MESSAGE] = "Log out successful!";
            return RedirectToAction(LOGIN_ACTION, "Account");
        }

        public IActionResult Profile()
        {
            if (!User.Identity.IsAuthenticated)
            {
                return RedirectToAction(LOGIN_ACTION, "Account");
            }
            Response.Cookies.Delete("JwtToken");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Signup(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                var errors = string.Join(", ", ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage));

                _logger.LogWarning("Registration failed validation at {Time}. Errors: {Errors}",
                    DateTime.UtcNow, errors);

                TempData[ERROR_MESSAGE] = $"{errors}";
                return RedirectToAction(LOGIN_ACTION);
            }

            try
            {
                var success = _service.Register(model);

                if (!success)
                {
                    _logger.LogWarning("Registration failed at {Time}. Reason: Username already exists",
                        DateTime.UtcNow);

                    TempData[ERROR_MESSAGE] = "Registration failed - Username already exists";
                    return RedirectToAction(LOGIN_ACTION);
                }

                _logger.LogInformation("New account registered at {Time}",
                    DateTime.UtcNow);

                TempData[TOAST_MESSAGE] = "Sign up successful! Redirecting to log in..";
                return RedirectToAction(LOGIN_ACTION);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception occurred during registration at {Time}",
                    DateTime.UtcNow);

                TempData[ERROR_MESSAGE] = $"Error during registration: {ex.Message}";
                if (ex.InnerException != null)
                {
                    TempData[ERROR_MESSAGE] += $" Inner error: {ex.InnerException.Message}";
                }

                return RedirectToAction(LOGIN_ACTION);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                // Collect all validation errors
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();
                
                TempData[ERROR_MESSAGE] = string.Join(", ", errors);
                return RedirectToAction(LOGIN_ACTION);
            }

            if (!_service.Authenticate(model.Username, model.Password, out var user))
            {
                TempData[ERROR_MESSAGE] = "Invalid username or password!";
                return RedirectToAction(LOGIN_ACTION);
            }

            if (user.Status == 0)
            {
                TempData[ERROR_MESSAGE] = "Account has been locked!";
                return RedirectToAction(LOGIN_ACTION);
            }

            await _service.SignInUserAsync(HttpContext, user);

            // Generate JWT token
            var token = _jwtService.GenerateToken(user);

            Response.Cookies.Append("JwtToken", token, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Expires = DateTime.Now.AddMinutes(60)
            });

            TempData[TOAST_MESSAGE] = "Log in successful!";

            if (user.RoleId == 1)
            {
                return RedirectToAction(MAIN_PAGE, ADMIN_CONTROLLER);
            }
            else if (user.RoleId == 2)
            {
                var account = _accountRepository.GetById(user.AccountId);
                var employee = account?.Employees
                    .FirstOrDefault(e => e.AccountId == account.AccountId);
                
                if (employee == null)
                {
                    TempData["ErrorMessage"] = "Employee account not found!";
                    return RedirectToAction("Login");
                }
                
                if (employee.Status)
                {
                    return RedirectToAction(MAIN_PAGE, EMPLOYEE_CONTROLLER);
                } else
                {
                    TempData[ERROR_MESSAGE] = "Account has been locked!";
                    return RedirectToAction(LOGIN_ACTION);
                }
            }
            else
            {
                return RedirectToAction(INDEX_ACTION, HOME_CONTROLLER);
            }
        }

        public IActionResult AccessDenied()
        {
            return View();
        }

        [HttpGet]
        public IActionResult History()
        {
            // This action is obsolete since history is now in the profile tab
            return RedirectToAction(MAIN_PAGE, MY_ACCOUNT_CONTROLLER);
        }

        [HttpPost]
        public IActionResult History(DateTime fromDate, DateTime toDate, int? status)
        {
            // This action is obsolete since history is now in the profile tab
            return RedirectToAction(MAIN_PAGE, MY_ACCOUNT_CONTROLLER);
        }

        // --- Forget Password Actions ---
        [HttpGet]
        public IActionResult ForgetPassword()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ForgetPassword(ForgetPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var account = _service.GetAccountByEmail(model.Email);
            if (account == null)
            {
                ModelState.AddModelError("Email", "Email không tồn tại trong hệ thống.");
                return View(model);
            }

            var success = _service.SendForgetPasswordOtp(model.Email);
            if (success)
            {
                TempData["SuccessMessage"] = "Mã OTP đã được gửi đến email của bạn. Vui lòng kiểm tra hộp thư và thư mục spam.";
                TempData["Email"] = model.Email;
                return RedirectToAction("ResetPassword");
            }
            else
            {
                ModelState.AddModelError("", "Không thể gửi mã OTP. Vui lòng thử lại sau.");
                return View(model);
            }
        }

        [HttpGet]
        public IActionResult ResetPassword()
        {
            var email = TempData["Email"] as string;
            if (string.IsNullOrEmpty(email))
            {
                return RedirectToAction("ForgetPassword");
            }

            var model = new ResetPasswordViewModel
            {
                Email = email
            };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ResetPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Verify OTP
            if (!_service.VerifyForgetPasswordOtp(model.Email, model.Otp))
            {
                ModelState.AddModelError("Otp", "Mã OTP không đúng hoặc đã hết hạn.");
                return View(model);
            }

            // Reset password
            var success = _service.ResetPassword(model.Email, model.NewPassword);
            if (success)
            {
                TempData["SuccessMessage"] = "Mật khẩu đã được đặt lại thành công. Vui lòng đăng nhập với mật khẩu mới.";
                return RedirectToAction("Login");
            }
            else
            {
                ModelState.AddModelError("", "Không thể đặt lại mật khẩu. Vui lòng thử lại sau.");
                return View(model);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public IActionResult ToggleStatus(string id)
        {
            try
            {
                if (string.IsNullOrEmpty(id))
                {
                    TempData["ErrorMessage"] = "Invalid Account ID.";
                    return RedirectToAction("MainPage", "Admin", new { tab = "MemberMg" });
                }

                var account = _service.GetById(id);
                if (account == null)
                {
                    TempData["ErrorMessage"] = "Member not found.";
                    return RedirectToAction("MainPage", "Admin", new { tab = "MemberMg" });
                }

                _service.ToggleStatus(id);
                TempData["ToastMessage"] = "Member status updated successfully!";
                return RedirectToAction("MainPage", "Admin", new { tab = "MemberMg" });
            }
            catch (ArgumentException ex)
            {
                TempData["ErrorMessage"] = ex.Message;
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"An unexpected error occurred: {ex.Message}";
            }
            return RedirectToAction("MainPage", "Admin", new { tab = "MemberMg" });
        }
    }
}
