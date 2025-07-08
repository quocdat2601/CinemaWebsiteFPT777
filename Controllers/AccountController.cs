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
        private readonly MovieTheaterContext _context;
        private readonly IAccountService _service;
        private readonly ILogger<AccountController> _logger;
        private readonly IJwtService _jwtService;
        private readonly IMemberRepository _memberRepository;

        public AccountController(
       MovieTheaterContext context,
       IAccountService service,
       ILogger<AccountController> logger, IAccountRepository accountRepository, IMemberRepository memberRepository, IJwtService jwtService)
        {
            _service = service;
            _logger = logger;
            _accountRepository = accountRepository;
            _memberRepository = memberRepository;
            _jwtService = jwtService;
            _context = context;
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

            var user = _accountRepository.GetAccountByEmail(email);

            if (user == null)
            {
                // Set initial rank (Bronze)
                var bronzeRank = _context.Ranks.OrderBy(r => r.RequiredPoints).FirstOrDefault();
                user = new Account
                {
                    Email = email,
                    FullName = name ?? $"{givenName} {surname}".Trim() ?? "Google User",
                    Username = email,
                    RoleId = 3,
                    Status = 1,
                    RegisterDate = DateOnly.FromDateTime(DateTime.Now),
                    Image = !string.IsNullOrEmpty(picture) ? picture : "/image/profile.jpg",
                    Password = null, // Set Password to null for Google login
                    RankId = bronzeRank?.RankId // Always set the lowest rank if available
                };

                _accountRepository.Add(user);
                _accountRepository.Save();
                _memberRepository.Add(new Member
                {
                    Score = 0,
                    TotalPoints = 0,
                    AccountId = user.AccountId
                });
                _memberRepository.Save();
            }

            // After adding new user (if any)
            user = _accountRepository.GetAccountByEmail(email); // get the latest user

            // Log fields for debugging
            _logger.LogInformation("[GoogleLoginDebug] Email: {Email}, Address: '{Address}', DateOfBirth: '{DateOfBirth}', Gender: '{Gender}', IdentityCard: '{IdentityCard}', PhoneNumber: '{PhoneNumber}'", user.Email, user.Address, user.DateOfBirth, user.Gender, user.IdentityCard, user.PhoneNumber);
            // Check for missing information
            bool missingInfo =
                !user.DateOfBirth.HasValue || user.DateOfBirth.Value == DateOnly.MinValue ||
                string.IsNullOrWhiteSpace(user.Gender) ||
                string.IsNullOrWhiteSpace(user.IdentityCard) ||
                string.IsNullOrWhiteSpace(user.Address) ||
                string.IsNullOrWhiteSpace(user.PhoneNumber);

            if (missingInfo)
            {
                TempData["FirstTimeLogin"] = true;
            }

            if (user.Status == 0)
            {
                TempData["ErrorMessage"] = "Account has been locked!";
                return RedirectToAction("Login");
            }

            string roleName = user.RoleId switch
            {
                1 => "Admin",
                2 => "Employee",
                3 => "Member",
                _ => "Guest"
            };

            var appClaims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.AccountId),
                new Claim(ClaimTypes.Name, user.FullName),
                new Claim(ClaimTypes.Role, roleName),
                new Claim("Status", user.Status.ToString()),
                new Claim("Email", user.Email),
            };

            var identity = new ClaimsIdentity(appClaims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            // Sign in with cookie authentication - match normal login flow
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

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
                //return RedirectToAction("MainPage","MyAccount", new { tab = "Profile" });
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
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
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
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var username = User.FindFirst(ClaimTypes.Name)?.Value;
            var role = User.FindFirst(ClaimTypes.Role)?.Value;
            var status = User.FindFirst("Status")?.Value;

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
                TempData["ErrorMessage"] = "Please fill all required fields!";
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

            string roleName = user.RoleId switch
            {
                1 => "Admin",
                2 => "Employee",
                3 => "Member",
                _ => "Guest"
            };

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.AccountId),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Role, roleName),
                new Claim("Status", user.Status.ToString())
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

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
                return RedirectToAction("MainPage", "Employee");
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
            var accountId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(accountId))
            {
                return RedirectToAction("Login");
            }

            var bookings = _context.Invoices
                .Where(i => i.AccountId == accountId)
                .OrderByDescending(i => i.BookingDate)
                .ToList();

            return View("~/Views/Account/Tabs/History.cshtml", bookings);
        }

        [HttpPost]
        public IActionResult History(DateTime fromDate, DateTime toDate, int? status)
        {
            var accountId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(accountId))
            {
                return RedirectToAction("Login");
            }

            var query = _context.Invoices
                .Where(i => i.AccountId == accountId &&
                            i.BookingDate >= fromDate &&
                            i.BookingDate <= toDate);

            if (status.HasValue)
            {
                var statusEnum = (MovieTheater.Models.InvoiceStatus?)status;
                query = query.Where(i => i.Status == statusEnum);
            }

            var bookings = query
                .OrderByDescending(i => i.BookingDate)
                .ToList();

            return View("~/Views/Account/Tabs/History.cshtml", bookings);
        }
    }
}
