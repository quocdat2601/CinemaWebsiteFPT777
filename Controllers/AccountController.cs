using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
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
        private readonly IMemberRepository _memberRepository;
        private readonly IEmployeeService _employeeService;

        public AccountController(
            IAccountService service,
            ILogger<AccountController> logger,
            IAccountRepository accountRepository,
            IMemberRepository memberRepository,
            IJwtService jwtService, IEmployeeService employeeService)
        {
            _service = service;
            _logger = logger;
            _accountRepository = accountRepository;
            _memberRepository = memberRepository;
            _jwtService = jwtService;
            _employeeService = employeeService;
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
                return RedirectToAction("Login");

            var claims = result.Principal.Identities.FirstOrDefault()?.Claims;
            var email = claims?.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
            var name = claims?.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value;
            var givenName = claims?.FirstOrDefault(c => c.Type == ClaimTypes.GivenName)?.Value;
            var surname = claims?.FirstOrDefault(c => c.Type == ClaimTypes.Surname)?.Value;
            var picture = claims?.FirstOrDefault(c => c.Type == "picture")?.Value;

            if (string.IsNullOrEmpty(email))
            {
                TempData["ErrorMessage"] = "Google login failed. Email not provided.";
                return RedirectToAction("Login");
            }

            // Refactor: Đẩy logic tạo user/member mới vào service
            var user = _service.GetOrCreateGoogleAccount(email, name, givenName, surname, picture);

            // Log fields for debugging
            _logger.LogInformation("[GoogleLoginDebug] Email: {Email}, Address: '{Address}', DateOfBirth: '{DateOfBirth}', Gender: '{Gender}', IdentityCard: '{IdentityCard}', PhoneNumber: '{PhoneNumber}'", user.Email, user.Address, user.DateOfBirth, user.Gender, user.IdentityCard, user.PhoneNumber);

            // Refactor: Đẩy logic kiểm tra thông tin thiếu vào service
            bool missingInfo = _service.HasMissingProfileInfo(user);
            if (missingInfo)
            {
                TempData["FirstTimeLogin"] = true;
            }

            if (user.Status == 0)
            {
                TempData["ErrorMessage"] = "Account has been locked!";
                return RedirectToAction("Login");
            }

            // Refactor: Đẩy logic tạo claims, sign-in vào service
            await _service.SignInUserAsync(HttpContext, user);

            // Check and update rank
            if (user.RoleId == 3) // Member
            {
                _service.CheckAndUpgradeRank(user.AccountId);
            }

            // Check if it's the first time login and redirect to profile update
            if (TempData["FirstTimeLogin"] != null && (bool)TempData["FirstTimeLogin"])
            {
                TempData.Remove("FirstTimeLogin");
                return RedirectToAction("MainPage", "MyAccount", new { tab = "Profile" });
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

            TempData["ToastMessage"] = "Log in successful!";

            // After successful login and before redirecting, add:
            var rankUpMsg = _service.GetAndClearRankUpgradeNotification(user.AccountId);
            if (!string.IsNullOrEmpty(rankUpMsg))
            {
                TempData["ToastMessage"] += "<br/>" + rankUpMsg;
            }

            // Direct redirect like normal login
            if (user.RoleId == 1)
            {
                return RedirectToAction("MainPage", "Admin");
            }
            else if (user.RoleId == 2)
            {
                return RedirectToAction("MainPage", "Employee");
            }
            else
            {
                return RedirectToAction("Index", "Home");
            }
        }

        [HttpGet]
        public IActionResult Signup()
        {
            return View();
        }
        public IActionResult Login()
        {
            return View();
        }

        public async Task<IActionResult> Logout()
        {
            await _service.SignOutUserAsync(HttpContext);
            // Remove the JWT token cookie
            Response.Cookies.Delete("JwtToken");
            TempData["ToastMessage"] = "Log out successful!";
            return RedirectToAction("Login", "Account");
        }

        public IActionResult Profile()
        {
            if (!User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Login", "Account");
            }
            Response.Cookies.Delete("JwtToken");
            return View();
        }

        [HttpPost]
        public IActionResult Signup(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                var errors = string.Join(", ", ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage));

                _logger.LogWarning("Registration failed validation at {Time}. Errors: {Errors}",
                    DateTime.UtcNow, errors);

                TempData["ErrorMessage"] = $"{errors}";
                return View(model);
            }

            try
            {
                var success = _service.Register(model);

                if (!success)
                {
                    _logger.LogWarning("Registration failed for username: {Username} at {Time}. Reason: Username already exists",
                        model.Username, DateTime.UtcNow);

                    TempData["ErrorMessage"] = "Registration failed - Username already exists";
                    return View(model);
                }

                _logger.LogInformation("New account registered: {Username} at {Time}",
                    model.Username, DateTime.UtcNow);

                TempData["ToastMessage"] = "Sign up successful! Redirecting to log in..";
                return RedirectToAction("Signup");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception occurred during registration for {Username} at {Time}",
                    model.Username, DateTime.UtcNow);

                TempData["ErrorMessage"] = $"Error during registration: {ex.Message}";
                if (ex.InnerException != null)
                {
                    TempData["ErrorMessage"] += $" Inner error: {ex.InnerException.Message}";
                }

                return View(model);
            }
        }

        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                // Collect all validation errors
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();
                
                TempData["ErrorMessage"] = string.Join(", ", errors);
                return RedirectToAction("Login");
            }

            if (!_service.Authenticate(model.Username, model.Password, out var user))
            {
                TempData["ErrorMessage"] = "Invalid username or password!";
                return RedirectToAction("Login");
            }

            if (user.Status == 0)
            {
                TempData["ErrorMessage"] = "Account has been locked!";
                return RedirectToAction("Login");
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

            TempData["ToastMessage"] = "Log in successful!";

            if (user.RoleId == 1)
            {
                return RedirectToAction("MainPage", "Admin");
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
                    return RedirectToAction("MainPage", "Employee");
                } else
                {
                    TempData["ErrorMessage"] = "Employee account has been locked!";
                    return RedirectToAction("Login");
                }
            }
            else
            {
                return RedirectToAction("Index", "Home");
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
            return RedirectToAction("MainPage", "MyAccount");
        }

        [HttpPost]
        public IActionResult History(DateTime fromDate, DateTime toDate, int? status)
        {
            // This action is obsolete since history is now in the profile tab
            return RedirectToAction("MainPage", "MyAccount");
        }
    }
}
