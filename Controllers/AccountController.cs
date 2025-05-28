using Microsoft.AspNetCore.Mvc;
using MovieTheater.Service;
using MovieTheater.ViewModels;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Authorization;

namespace MovieTheater.Controllers
{
    public class AccountController : Controller
    {
        private readonly IAccountService _service;
        private readonly ILogger<AccountController> _logger;
        private readonly IJwtService _jwtService;

        public AccountController(IAccountService service, ILogger<AccountController> logger, IJwtService jwtService)
        {
            _service = service;
            _logger = logger;
            _jwtService = jwtService;
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

        [Authorize]
        public IActionResult Logout()
        {
            // Remove the JWT token cookie
            Response.Cookies.Delete("JwtToken");
            return RedirectToAction("Login", "Account");
        }

        [Authorize]
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

                TempData["ToastMessage"] = "Sign up successful! Please log in.";
                return RedirectToAction("Signup");
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

            if (user.Status == 0)
            {
                TempData["ErrorMessage"] = "Account has been locked!";
                return View(model);
            }

            // Generate JWT token
            var token = _jwtService.GenerateToken(user);

            // Store the token in a cookie
            Response.Cookies.Append("JwtToken", token, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Expires = DateTime.Now.AddMinutes(60)
            });

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
