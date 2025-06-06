using Microsoft.AspNetCore.Mvc;
using MovieTheater.Models;
using MovieTheater.Service;
using MovieTheater.ViewModels;

namespace MovieTheater.Controllers
{
    public class PromotionController : Controller
    {
        private readonly IPromotionService _promotionService;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public PromotionController(IPromotionService promotionService, IWebHostEnvironment webHostEnvironment)
        {
            _promotionService = promotionService;
            _webHostEnvironment = webHostEnvironment;
        }

        // GET: PromotionController/List
        public ActionResult List()
        {
            var promotions = _promotionService.GetAll()
                .Where(p => p.IsActive && p.EndTime >= DateTime.Now)
                .OrderByDescending(p => p.StartTime)
                .ToList();
            return View(promotions);
        }

        // GET: PromotionController/Details/5
        public ActionResult Details(int id)
        {
            var promotion = _promotionService.GetById(id);
            if (promotion == null)
            {
                return NotFound();
            }
            return View(promotion);
        }

        // GET: PromotionController/Create
        public ActionResult Create()
        {
            return View(new PromotionViewModel
            {
                StartTime = DateTime.Now,
                EndTime = DateTime.Now.AddDays(30),
                IsActive = true
            });
        }

        // POST: PromotionController/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create(PromotionViewModel viewModel, IFormFile? imageFile)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    // Get the next available ID
                    var nextId = _promotionService.GetAll().Any() ?
                        _promotionService.GetAll().Max(p => p.PromotionId) + 1 : 1;

                    var promotion = new Promotion
                    {
                        PromotionId = nextId,
                        Title = viewModel.Title,
                        Detail = viewModel.Detail,
                        DiscountLevel = viewModel.DiscountLevel,
                        StartTime = viewModel.StartTime,
                        EndTime = viewModel.EndTime,
                        IsActive = viewModel.IsActive
                    };

                    if (imageFile != null && imageFile.Length > 0)
                    {
                        string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images", "promotions");
                        if (!Directory.Exists(uploadsFolder))
                        {
                            Directory.CreateDirectory(uploadsFolder);
                        }

                        string uniqueFileName = Guid.NewGuid().ToString() + "_" + imageFile.FileName;
                        string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                        using (var fileStream = new FileStream(filePath, FileMode.Create))
                        {
                            await imageFile.CopyToAsync(fileStream);
                        }

                        promotion.Image = "/images/promotions/" + uniqueFileName;
                    }

                    _promotionService.Add(promotion);
                    _promotionService.Save();

                    TempData["ToastMessage"] = "Promotion created successfully!";
                    return RedirectToAction("MainPage", "Admin", new { tab = "PromotionMg" });
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Error creating promotion: " + ex.Message);
                }
            }

            return View(viewModel);
        }

        // GET: PromotionController/Edit/5
        public ActionResult Edit(int id)
        {
            var promotion = _promotionService.GetById(id);
            if (promotion == null)
            {
                return NotFound();
            }

            var viewModel = new PromotionViewModel
            {
                PromotionId = promotion.PromotionId,
                Title = promotion.Title,
                Detail = promotion.Detail,
                DiscountLevel = promotion.DiscountLevel ?? 0,
                StartTime = promotion.StartTime ?? DateTime.Now,
                EndTime = promotion.EndTime ?? DateTime.Now.AddDays(30),
                Image = promotion.Image,
                IsActive = promotion.IsActive
            };

            return View(viewModel);
        }

        // POST: PromotionController/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(int id, PromotionViewModel viewModel, IFormFile? imageFile)
        {
            if (id != viewModel.PromotionId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var promotion = _promotionService.GetById(id);
                    if (promotion == null)
                    {
                        return NotFound();
                    }

                    promotion.Title = viewModel.Title;
                    promotion.Detail = viewModel.Detail;
                    promotion.DiscountLevel = viewModel.DiscountLevel;
                    promotion.StartTime = viewModel.StartTime;
                    promotion.EndTime = viewModel.EndTime;
                    promotion.IsActive = viewModel.IsActive;

                    if (imageFile != null && imageFile.Length > 0)
                    {
                        string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images", "promotions");
                        if (!Directory.Exists(uploadsFolder))
                        {
                            Directory.CreateDirectory(uploadsFolder);
                        }

                        // Delete old image if exists
                        if (!string.IsNullOrEmpty(promotion.Image))
                        {
                            string oldImagePath = Path.Combine(_webHostEnvironment.WebRootPath, promotion.Image.TrimStart('/'));
                            if (System.IO.File.Exists(oldImagePath))
                            {
                                System.IO.File.Delete(oldImagePath);
                            }
                        }

                        string uniqueFileName = Guid.NewGuid().ToString() + "_" + imageFile.FileName;
                        string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                        using (var fileStream = new FileStream(filePath, FileMode.Create))
                        {
                            await imageFile.CopyToAsync(fileStream);
                        }

                        promotion.Image = "/images/promotions/" + uniqueFileName;
                    }

                    _promotionService.Update(promotion);
                    _promotionService.Save();

                    TempData["ToastMessage"] = "Promotion updated successfully!";
                    return RedirectToAction("MainPage", "Admin", new { tab = "PromotionMg" });
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Error updating promotion: " + ex.Message);
                }
            }

            return View(viewModel);
        }

        // GET: PromotionController/Delete/5
        public ActionResult Delete(int id)
        {
            var promotion = _promotionService.GetById(id);
            if (promotion == null)
            {
                return NotFound();
            }
            return View(promotion);
        }

        // POST: PromotionController/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            try
            {
                var promotion = _promotionService.GetById(id);
                if (promotion != null && !string.IsNullOrEmpty(promotion.Image))
                {
                    string imagePath = Path.Combine(_webHostEnvironment.WebRootPath, promotion.Image.TrimStart('/'));
                    if (System.IO.File.Exists(imagePath))
                    {
                        System.IO.File.Delete(imagePath);
                    }
                }

                _promotionService.Delete(id);
                _promotionService.Save();

                TempData["ToastMessage"] = "Promotion deleted successfully!";
                return RedirectToAction("MainPage", "Admin", new { tab = "PromotionMg" });
            }
            catch
            {
                TempData["ToastMessage"] = "Failed to delete promotion.";
                return RedirectToAction("MainPage", "Admin", new { tab = "PromotionMg" });
            }
        }
    }
}
