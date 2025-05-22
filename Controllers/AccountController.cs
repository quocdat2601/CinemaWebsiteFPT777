using Microsoft.AspNetCore.Mvc;
using MovieTheater.Service;
using MovieTheater.ViewModels;
using Microsoft.Extensions.Logging;

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
            HttpContext.Session.SetInt32("Role", user.RoleId);

            TempData["ToastMessage"] = "Login successful!";

            if (user.RoleId == 1)
            {
                return RedirectToAction("Dashboard", "Admin");
            } else
            if (user.RoleId == 2)
            {
                return RedirectToAction("Index", "Employee");
            } else
                return RedirectToAction("MovieList", "Movie");
        }

        public IActionResult AccessDenied()
        {
            return View();
        }

    }
}
