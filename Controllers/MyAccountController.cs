using Microsoft.AspNetCore.Mvc;
using MovieTheater.Service;
using MovieTheater.Services;
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
            switch (tab)
            {
                case "Profile":
                    return PartialView("~/Views/Account/Tabs/Profile.cshtml");
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

        //public IActionResult Edit(string id)
        //{
        //    var account = _service.GetById(id);
        //    if (account == null)
        //        return NotFound();

        //    var viewModel = new RegisterViewModel
        //    {
        //        Username = account.Account.Username,
        //        FullName = account.Account.FullName,
        //        DateOfBirth = (DateOnly)account.Account.DateOfBirth,
        //        Gender = account.Account.Gender,
        //        IdentityCard =  account .Account.IdentityCard,
        //        Email =     account.Account.Email,
        //        Address = account.Account.Address,
        //        PhoneNumber = account.Account.PhoneNumber,
        //        Image = account.Account.Image,
        //        Password = null,
        //        ConfirmPassword = null
        //    };

        //    return View(viewModel);
        //}

        //// POST: EmployeeController/Edit/5
        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //public async Task<ActionResult> EditAsync(string id, RegisterViewModel model)
        //{
        //    if (!ModelState.IsValid)
        //    {
        //        return View(model);
        //    }
        //    try
        //    {
        //        if (model.ImageFile != null && model.ImageFile.Length > 0)
        //        {
        //            var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "image");
        //            Directory.CreateDirectory(uploadsFolder);

        //            var uniqueFileName = Guid.NewGuid().ToString() + Path.GetExtension(model.ImageFile.FileName);
        //            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

        //            using (var stream = new FileStream(filePath, FileMode.Create))
        //            {
        //                await model.ImageFile.CopyToAsync(stream);
        //            }
        //            model.Image = "/image/" + uniqueFileName;
        //        }
        //        else
        //        {
        //            var existingEmployee = _service.GetById(id);
        //            if (existingEmployee != null)
        //            {
        //                model.Image = existingEmployee.Account.Image;
        //            }
        //        }

        //        var success = _service.Update(id, model);

        //        if (!success)
        //        {
        //            TempData["ErrorMessage"] = "Update failed - Username already exists";
        //            return View(model);
        //        }

        //        TempData["ToastMessage"] = "Employee Updated Successfully!";
        //        return RedirectToAction("MainPage", "Admin", new { tab = "EmployeeMg" });
        //    }
        //    catch (Exception ex)
        //    {
        //        TempData["ErrorMessage"] = $"Error during update: {ex.Message}";
        //        return View(model);
        //    }
        //}
    }
}
