using Microsoft.AspNetCore.Mvc;
using MovieTheater.Service;
using MovieTheater.ViewModels;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;

namespace MovieTheater.Controllers
{
    public class AccountController : Controller
    {
        private readonly IAccountService _service;
        private readonly ILogger<AccountController> _logger;

        public AccountController(IAccountService service, ILogger<AccountController> logger)
        {
            _service = service;
            _logger = logger;
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

        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
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
        public IActionResult Login(LoginViewModel model)
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

            HttpContext.Session.SetString("UserId", user.AccountId);
            HttpContext.Session.SetString("UserName", user.Username);
            HttpContext.Session.SetInt32("Role", user.RoleId ?? 0);
            HttpContext.Session.SetInt32("Status", user.Status ?? 0);


            if (user.Status  == 0)
            {
                TempData["ErrorMessage"] = "Account has been locked!";
                HttpContext.Session.Clear();
                return View(model);
            }

            // --- Begin Authentication Claims and Sign In ---
            // Create claims for the user
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.AccountId), // Crucially, add the AccountId here
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Role, user.RoleId.ToString()),
                // Add other claims if needed (e.g., user.Status)
            };

            var claimsIdentity = new ClaimsIdentity(
                claims,
                CookieAuthenticationDefaults.AuthenticationScheme); // Use your configured authentication scheme

            var authProperties = new AuthenticationProperties
            {
                // Set properties like IsPersistent, ExpiresUtc, etc. if needed
            };

            // Sign in the user
            HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme, // Use your configured authentication scheme
                new ClaimsPrincipal(claimsIdentity),
                authProperties);
            // --- End Authentication Claims and Sign In ---

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
