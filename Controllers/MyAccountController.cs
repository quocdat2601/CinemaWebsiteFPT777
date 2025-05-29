using Microsoft.AspNetCore.Mvc;

namespace MovieTheater.Controllers
{
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
                    return PartialView("~/Views/Account/Tabs/Profile.cshtml");
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
