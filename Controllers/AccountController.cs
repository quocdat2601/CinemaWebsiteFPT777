using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using MovieTheater.Service;
using MovieTheater.ViewModels;
using Microsoft.Extensions.Logging;
using MovieTheater.Models;
using Microsoft.AspNetCore.Authentication.Google;
using System.Security.Claims;
using MovieTheater.Repository;

namespace MovieTheater.Controllers
{
    public class AccountController : Controller
    {
        private readonly IAccountRepository _accountRepository;
        private readonly MovieTheaterContext _context;
        private readonly IAccountService _service;
        private readonly ILogger<AccountController> _logger;
        private readonly IMemberRepository _memberRepository;

        public AccountController(
       MovieTheaterContext context,
       IAccountService service,
       ILogger<AccountController> logger, IAccountRepository accountRepository, IMemberRepository memberRepository)
        {
            _context = context;
            _service = service;
            _logger = logger;
            _accountRepository = accountRepository;
            _memberRepository = memberRepository;
        }

        [HttpGet]
        public IActionResult ScoreHistory()
        {
            return View();
        }

        [HttpPost]
        public IActionResult ScoreHistory(DateTime fromDate, DateTime toDate, string historyType)
        {
            var accountId = HttpContext.Session.GetString("UserId");

            if (string.IsNullOrEmpty(accountId))
            {
                return RedirectToAction("Login", "Account");
            }

            var query = _context.Invoice
                .Where(i => i.AccountId == accountId &&
                            i.BookingDate >= fromDate &&
                            i.BookingDate <= toDate);

            if (historyType == "add")
            {
                query = query.Where(i => i.AddScore > 0);
            }
            else if (historyType == "use")
            {
                query = query.Where(i => i.UseScore > 0);
            }

            var result = query.Select(i => new ScoreHistoryViewModel
            {
                DateCreated = i.BookingDate ?? DateTime.MinValue,
                MovieName = i.MovieName ?? "N/A",
                Score = historyType == "add" ? (i.AddScore ?? 0) : (i.UseScore ?? 0)
            }).ToList();

            if (!result.Any())
            {
                ViewBag.Message = "No score history found for the selected period.";
            }

            return View(result);
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

            if (string.IsNullOrEmpty(email))
            {
                TempData["ErrorMessage"] = "Google login failed. Email not provided.";
                return RedirectToAction("Login");
            }

            var user = _accountRepository.GetAccountByEmail(email);

            if (user == null)
            {
                user = new Account
                {
                    Email = email,
                    FullName = name ?? "Google User",
                    Username = email,
                    RoleId = 3,
                    Status = 1,
                    RegisterDate = DateOnly.FromDateTime(DateTime.Now)
                };
                _accountRepository.Add(user);
                _accountRepository.Save();
                _memberRepository.Add(new Member
                {
                    Score = 0,
                    AccountId = user.AccountId
                });
                _memberRepository.Save();
            }

            if (user.Status == 0)
            {
                TempData["ErrorMessage"] = "Account has been locked!";
                HttpContext.Session.Clear();
                return RedirectToAction("Login");
            }

            HttpContext.Session.SetString("UserId", user.AccountId);
            HttpContext.Session.SetString("UserName", user.Username);
            HttpContext.Session.SetInt32("Role", user.RoleId ?? 0);
            HttpContext.Session.SetInt32("Status", user.Status ?? 0);

            // Sign in using cookie
            string roleName = user.RoleId switch
            {
                1 => "Admin",
                2 => "Employee",
                3 => "Customer",
                _ => "Guest"
            };

            var appClaims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.AccountId),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Role, roleName),
            };

            var identity = new ClaimsIdentity(appClaims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);


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
                return RedirectToAction("MovieList", "Movie");
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

        public IActionResult MainPage()
        {
            return View();
        }

        public async Task<IActionResult> Logout()
        {
            HttpContext.Session.Clear();
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login", "Account");
        }


        public IActionResult Profile()
        {
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
                TempData["ErrorMessage"] = $"Validation failed: {errors}";
                return View(model);
            }

            try
            {
                var success = _service.Register(model);

                if (!success)
                {
                    TempData["ErrorMessage"] = "Registration failed - Username already exists";
                    return View(model);
                }

                //TempData["SuccessMessage"] = "Sign up successful! Please log in.";
                TempData["ToastMessage"] = "Sign up successful! Please log in.";
                return RedirectToAction("Signup");

                //return RedirectToAction("Login", "Account");

            }
            catch (Exception ex)
            {
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
                TempData["ErrorMessage"] = "Invalid login data.";
                return View(model);
            }

            if (!_service.Authenticate(model.Username, model.Password, out var user))
            {
                TempData["ErrorMessage"] = "Invalid username or password.";
                return View(model);
            }

            HttpContext.Session.SetInt32("Status", user.Status ?? 0);


            if (user.Status == 0)
            {
                TempData["ErrorMessage"] = "Account has been locked!";
                return View(model);
            }

            // Build claims only if user is valid and unlocked
            string roleName = user.RoleId switch
            {
                1 => "Admin",
                2 => "Employee",
                3 => "Customer",
                _ => "Guest"
            };

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.AccountId),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Role, roleName),
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

            // Redirect by role
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
                return RedirectToAction("MovieList", "Movie");
            }

        }

        public IActionResult AccessDenied()
        {
            return View();
        }

    }
}
