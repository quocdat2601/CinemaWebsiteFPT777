using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using MovieTheater.Service;
using MovieTheater.ViewModels;

namespace MovieTheater.Controllers
{
    [Authorize]
    public class MyAccountController : Controller
    {
        private readonly IAccountService _service;
        private readonly ILogger<MyAccountController> _logger;

        public MyAccountController(IAccountService service, ILogger<MyAccountController> logger)
        {
            _service = service;
            _logger = logger;
        }

        [HttpGet]
        public IActionResult MainPage(string tab = "Information")
        {
            ViewData["ActiveTab"] = tab;
            return View();
        }

        [HttpGet]
        public IActionResult LoadTab(string tab)
        {
            //var user = _service.GetCurrentUser();

            //TEST HARD-CODE
            var user = _service.GetDemoUser();
            switch (tab)
            {
                case "Profile":
                    if (user == null)
                        return NotFound();
                    return PartialView("~/Views/Account/Tabs/Profile.cshtml", user);
                case "Information":
                    return PartialView("~/Views/Account/Tabs/Information.cshtml");
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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(ProfileViewModel model)
        {
            ModelState.Remove("Password"); // hoặc chỉ khi bạn không yêu cầu nhập lại password
            var demouser = _service.GetById("AC007");
            var timestamp = DateTime.UtcNow;

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Profile update failed: Invalid model state. Data: {@Model}", model);
                return Json(new { success = false, error = " Invalid model state" });
            }

            try
            {
                var success = _service.Update1(demouser.AccountId, model);

                if (!success)
                {
                    _logger.LogWarning("Failed to update profile. AccountId: {AccountId}, Time: {Time}", demouser.AccountId, timestamp);
                    string errorMessage = "Update failed";
                    return Json(new { success = false, error = errorMessage });
                }

                // Cập nhật thành công
                _logger.LogInformation("Profile updated successfully. AccountId: {AccountId}, Time: {Time}", demouser.AccountId, timestamp);
                string successMessage = "Profile updated successfully!";
                return Json(new { success = true, reloadTab = "Profile", toast = successMessage });

            }
            catch (Exception ex)
            {
                //HARD-CODE DEMO GHI LOG
                _logger.LogError(ex, "Exception during profile update. AccountId: {AccountId}, Time: {Time}", demouser.AccountId, DateTime.UtcNow);
                ModelState.AddModelError("", $"Error during update: {ex.Message}");
                return Json(new { success = false, error = ex.Message });
            }
        }


    }
}
