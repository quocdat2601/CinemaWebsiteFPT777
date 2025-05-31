using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace MovieTheater.Controllers
{
    [Authorize]
    public class MyAccountController : Controller
    {
        [HttpGet]
        public IActionResult MainPage(string tab = "Profile")
        {
            ViewData["ActiveTab"] = tab;
            return View();
        }

        [HttpGet]
        public IActionResult LoadTab(string tab)
        {
            switch (tab)
            {
                case "Profile":
                    var user = HttpContext.User;
                    var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                    var username = user.Identity?.Name;
                    var email = user.FindFirst(ClaimTypes.Email)?.Value;
                    var role = user.FindFirst(ClaimTypes.Role)?.Value;

                    var userInfo = new
                    {
                        UserId = userId,
                        Username = username,
                        Email = email,
                        Role = role
                    };

                    return PartialView("~/Views/Account/Tabs/Profile.cshtml", userInfo);

                case "Password":
                    return PartialView("~/Views/Account/Tabs/Password.cshtml");
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
    }
}
