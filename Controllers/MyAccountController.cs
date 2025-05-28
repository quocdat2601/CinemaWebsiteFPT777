using Microsoft.AspNetCore.Mvc;
using MovieTheater.Service;
using MovieTheater.ViewModels;

namespace MovieTheater.Controllers
{
    public class MyAccountController : Controller
    {
        private readonly IAccountService _service;
        private readonly ILogger<AccountController> _logger;

        public MyAccountController(IAccountService service, ILogger<AccountController> logger)
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

        // GET: EmployeeController/Edit/5
        //public IActionResult Edit(string id)
        //{
        //    var user = _service.GetById(id);
        //    if (user == null)
        //        return NotFound();

        //    var viewModel = new ProfileViewModel
        //    {
        //        Username = user.Username,
        //        FullName = user.FullName,
        //        DateOfBirth = (DateOnly)user.DateOfBirth,
        //        Gender = user.Gender,
        //        IdentityCard = user.IdentityCard,
        //        Email = user.Email,
        //        Address = user.Address,
        //        PhoneNumber = user.PhoneNumber,
        //    };

        //    return View(viewModel);
        //}

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(ProfileViewModel model)
        {
            ModelState.Remove("Password"); // hoặc chỉ khi bạn không yêu cầu nhập lại password

            if (!ModelState.IsValid)
            {

                return PartialView("~/Views/Account/Tabs/Profile.cshtml", model);
            }

            try
            {
                var demouser = _service.GetById("AC007");
                var success = _service.Update1(demouser.AccountId, model);

                if (!success)
                {
                    string errorMessage = "Update failed";
                    return Json(new { success = false, error = errorMessage });
                }

                // Cập nhật thành công
                string successMessage = "Profile updated successfully!";
                return Json(new { success = true, reloadTab = "Profile", toast = successMessage });

            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Error during update: {ex.Message}");
                return Json(new { success = false, error = ex.Message });
            }
        }


    }
}
