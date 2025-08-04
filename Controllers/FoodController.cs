using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MovieTheater.Service;
using MovieTheater.ViewModels;
using System.Security.Claims;

namespace MovieTheater.Controllers
{
    public class FoodController : Controller
    {
        private readonly IFoodService _foodService;
        private readonly IWebHostEnvironment _webHostEnvironment;
        public string role => User?.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value;

        public FoodController(IFoodService foodService, IWebHostEnvironment webHostEnvironment)
        {
            _foodService = foodService;
            _webHostEnvironment = webHostEnvironment;
        }

        public async Task<IActionResult> Index(string? searchKeyword, string? categoryFilter, bool? statusFilter)
        {
            var model = await _foodService.GetAllAsync(searchKeyword, categoryFilter, statusFilter);
            ViewBag.Categories = await _foodService.GetCategoriesAsync();
            return View(model);
        }

        [Authorize(Roles = "Admin,Employee")]
        public IActionResult Create()
        {
            return View(new FoodViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin, Employee")]
        public async Task<IActionResult> Create(FoodViewModel model)
        {

            if (!ModelState.IsValid)
            {
                var errors = string.Join(", ", ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage));
                TempData["ErrorMessage"] = $"Validation failed: {errors}";
                return View(model);
            }
            
            if (ModelState.IsValid)
            {
    
                
                // Làm sạch dữ liệu đầu vào
                model.Name = model.Name?.Trim();
                model.Description = model.Description?.Trim();
                model.Category = model.Category?.Trim();

                // Kiểm tra file upload (nếu có)
                if (model.ImageFile != null)
                {
                    var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
                    var extension = Path.GetExtension(model.ImageFile.FileName).ToLowerInvariant();
                    if (!allowedExtensions.Contains(extension) || model.ImageFile.Length > 2 * 1024 * 1024)
                    {
                        ModelState.AddModelError("ImageFile", "Chỉ cho phép file ảnh nhỏ hơn 2MB và đúng định dạng.");
                        return View(model);
                    }
                }

                var webRootPath = _webHostEnvironment.WebRootPath;
                var result = await _foodService.CreateAsync(model, webRootPath);

    

                if (result)
                {
                    TempData["SuccessMessage"] = "Food created successfully!";
                    if (role == "Admin")
                    {
                        return RedirectToAction("MainPage", "Admin", new { tab = "FoodMg" });
                    }
                    else return RedirectToAction("MainPage", "Employee", new { tab = "FoodMg" });
                }
                else
                {
                    TempData["ErrorMessage"] = "Failed to create food. Please try again.";
                }
            }

            return View(model);
        }

        [Authorize(Roles = "Admin,Employee")]
        public async Task<IActionResult> Edit(int id)
        {
            var food = await _foodService.GetByIdAsync(id);
            if (food == null)
            {
                TempData["ErrorMessage"] = "Food not found.";
                if (role == "Admin")
                {
                    return RedirectToAction("MainPage", "Admin", new { tab = "FoodMg" });
                }
                else return RedirectToAction("MainPage", "Employee", new { tab = "FoodMg" });
            }

            return View(food);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Employee")]
        public async Task<IActionResult> Edit(FoodViewModel model)
        {
            if (ModelState.IsValid)
            {
                var webRootPath = _webHostEnvironment.WebRootPath;
                var result = await _foodService.UpdateAsync(model, webRootPath);

                if (result)
                {
                    TempData["SuccessMessage"] = "Food updated successfully!";
                    if (role == "Admin")
                    {
                        return RedirectToAction("MainPage", "Admin", new { tab = "FoodMg" });
                    }
                    else return RedirectToAction("MainPage", "Employee", new { tab = "FoodMg" });
                }
                else
                {
                    TempData["ErrorMessage"] = "Failed to update food. Please try again.";
                }
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Employee")]
        public async Task<IActionResult> Delete(int id)
        {
            // Check if food has related invoices
            var food = await _foodService.GetByIdAsync(id);
            if (food == null)
            {
                TempData["ErrorMessage"] = "Food not found.";
                if (role == "Admin")
                {
                    return RedirectToAction("MainPage", "Admin", new { tab = "FoodMg" });
                }
                else return RedirectToAction("MainPage", "Employee", new { tab = "FoodMg" });
            }

            // Check if food has related invoices
            var hasInvoices = await _foodService.HasRelatedInvoicesAsync(id);
            if (hasInvoices)
            {
                TempData["ErrorMessage"] = "Cannot delete food that has been sold. Please deactivate it instead.";
                if (role == "Admin")
                {
                    return RedirectToAction("MainPage", "Admin", new { tab = "FoodMg" });
                }
                else return RedirectToAction("MainPage", "Employee", new { tab = "FoodMg" });
            }

            var result = await _foodService.DeleteAsync(id);

            if (result)
            {
                TempData["SuccessMessage"] = "Food deleted successfully!";
            }
            else
            {
                TempData["ErrorMessage"] = "Failed to delete food. Please try again.";
            }

            if (role == "Admin")
            {
                return RedirectToAction("MainPage", "Admin", new { tab = "FoodMg" });
            }
            else return RedirectToAction("MainPage", "Employee", new { tab = "FoodMg" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Employee")]
        public async Task<IActionResult> ToggleStatus(int id)
        {
            var result = await _foodService.ToggleStatusAsync(id);

            if (result)
            {
                TempData["SuccessMessage"] = "Food status updated successfully!";
            }
            else
            {
                TempData["ErrorMessage"] = "Failed to update food status. Please try again.";
            }

            if (role == "Admin")
            {
                return RedirectToAction("MainPage", "Admin", new { tab = "FoodMg" });
            }
            else return RedirectToAction("MainPage", "Employee", new { tab = "FoodMg" });
        }
    }
}