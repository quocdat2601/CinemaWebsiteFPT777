using Microsoft.AspNetCore.Mvc;
using MovieTheater.Service;
using MovieTheater.ViewModels;
using Microsoft.AspNetCore.Authorization;

namespace MovieTheater.Controllers
{
    public class FoodController : Controller
    {
        private readonly IFoodService _foodService;
        private readonly IWebHostEnvironment _webHostEnvironment;

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

        public IActionResult Create()
        {
            return View(new FoodViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create(FoodViewModel model)
        {
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
                    return RedirectToAction("MainPage", "Admin", new { tab = "FoodMg" });
                }
                else
                {
                    TempData["ErrorMessage"] = "Failed to create food. Please try again.";
                }
            }

            return View(model);
        }

        public async Task<IActionResult> Edit(int id)
        {
            var food = await _foodService.GetByIdAsync(id);
            if (food == null)
            {
                TempData["ErrorMessage"] = "Food not found.";
                return RedirectToAction("MainPage", "Admin", new { tab = "FoodMg" });
            }

            return View(food);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(FoodViewModel model)
        {
            if (ModelState.IsValid)
            {
                var webRootPath = _webHostEnvironment.WebRootPath;
                var result = await _foodService.UpdateAsync(model, webRootPath);

                if (result)
                {
                    TempData["SuccessMessage"] = "Food updated successfully!";
                    return RedirectToAction("MainPage", "Admin", new { tab = "FoodMg" });
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
        public async Task<IActionResult> Delete(int id)
        {
            var result = await _foodService.DeleteAsync(id);

            if (result)
            {
                TempData["SuccessMessage"] = "Food deleted successfully!";
            }
            else
            {
                TempData["ErrorMessage"] = "Failed to delete food. Please try again.";
            }

            return RedirectToAction("MainPage", "Admin", new { tab = "FoodMg" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
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

            return RedirectToAction("MainPage", "Admin", new { tab = "FoodMg" });
        }
    }
}