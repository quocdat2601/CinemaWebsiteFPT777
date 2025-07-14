using Microsoft.AspNetCore.Mvc;
using MovieTheater.Models;
using MovieTheater.Service;
using MovieTheater.ViewModels;
using Microsoft.AspNetCore.SignalR;
using MovieTheater.Hubs;

namespace MovieTheater.Controllers
{
    public class PromotionController : Controller
    {
        private readonly IPromotionService _promotionService;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly IHubContext<DashboardHub> _dashboardHubContext;

        public PromotionController(IPromotionService promotionService, IWebHostEnvironment webHostEnvironment, IHubContext<DashboardHub> dashboardHubContext)
        {
            _promotionService = promotionService;
            _webHostEnvironment = webHostEnvironment;
            _dashboardHubContext = dashboardHubContext;
        }

        /// <summary>
        /// Danh sách khuyến mãi đang hoạt động
        /// </summary>
        /// <remarks>url: /Promotion/List (GET)</remarks>
        public ActionResult List()
        {
            var promotions = _promotionService.GetAll()
                .Where(p => p.IsActive && p.EndTime >= DateTime.Now)
                .OrderByDescending(p => p.StartTime)
                .ToList();
            return View("Index", promotions);
        }

        /// <summary>
        /// Xem chi tiết khuyến mãi
        /// </summary>
        /// <remarks>url: /Promotion/Details (GET)</remarks>
        public ActionResult Details(int id)
        {
            var promotion = _promotionService.GetById(id);
            if (promotion == null)
            {
                return NotFound();
            }
            return View(promotion);
        }

        /// <summary>
        /// Trang quản lý khuyến mãi
        /// </summary>
        /// <remarks>url: /Promotion/Index (GET)</remarks>
        public ActionResult Index()
        {
            return View();
        }

        /// <summary>
        /// Trang tạo khuyến mãi mới
        /// </summary>
        /// <remarks>url: /Promotion/Create (GET)</remarks>
        public ActionResult Create()
        {
            return View(new PromotionViewModel
            {
                StartTime = DateTime.Now,
                EndTime = DateTime.Now.AddDays(30),
                IsActive = true
            });
        }

        /// <summary>
        /// Tạo khuyến mãi mới
        /// </summary>
        /// <remarks>url: /Promotion/Create (POST)</remarks>
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
                        // 1. Kiểm tra kích thước file (ví dụ: 2MB)
                        const long maxFileSize = 2 * 1024 * 1024; // 2MB
                        if (imageFile.Length > maxFileSize)
                        {
                            ModelState.AddModelError("", "File size must be less than 2MB.");
                            return View(viewModel);
                        }

                        // 2. Kiểm tra loại file (chỉ cho phép jpg, png, gif)
                        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
                        var extension = Path.GetExtension(imageFile.FileName).ToLowerInvariant();

                        // Làm sạch extension: chỉ lấy extension nếu đúng định dạng cho phép
                        if (string.IsNullOrEmpty(extension) || !allowedExtensions.Contains(extension))
                        {
                            ModelState.AddModelError("", "Only image files (jpg, jpeg, png, gif) are allowed.");
                            return View(viewModel);
                        }

                        // 3. (Tùy chọn) Kiểm tra magic number của file để xác thực là file ảnh thật sự
                        // Có thể bổ sung thêm nếu cần thiết

                        string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images", "promotions");
                        if (!Directory.Exists(uploadsFolder))
                        {
                            Directory.CreateDirectory(uploadsFolder);
                        }

                        // Đặt tên file mới hoàn toàn không liên quan tên gốc
                        string uniqueFileName = Guid.NewGuid().ToString("N") + extension;
                        string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                        // Đảm bảo filePath nằm trong uploadsFolder (tránh path traversal)
                        if (!filePath.StartsWith(uploadsFolder, StringComparison.OrdinalIgnoreCase))
                        {
                            ModelState.AddModelError("", "Invalid file path.");
                            return View(viewModel);
                        }

                        using (var fileStream = new FileStream(filePath, FileMode.Create))
                        {
                            await imageFile.CopyToAsync(fileStream);
                        }

                        promotion.Image = "/images/promotions/" + uniqueFileName;
                    }

                    _promotionService.Add(promotion);
                    _promotionService.Save();
                    await _dashboardHubContext.Clients.All.SendAsync("DashboardUpdated");
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

        /// <summary>
        /// Trang sửa khuyến mãi
        /// </summary>
        /// <remarks>url: /Promotion/Edit (GET)</remarks>
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

        /// <summary>
        /// Sửa khuyến mãi
        /// </summary>
        /// <remarks>url: /Promotion/Edit (POST)</remarks>
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
                        // 1. Kiểm tra kích thước file (ví dụ: 2MB)
                        const long maxFileSize = 2 * 1024 * 1024; // 2MB
                        if (imageFile.Length > maxFileSize)
                        {
                            ModelState.AddModelError("", "File size must be less than 2MB.");
                            return View(viewModel);
                        }

                        // 2. Kiểm tra loại file (chỉ cho phép jpg, png, gif)
                        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
                        var extension = Path.GetExtension(imageFile.FileName).ToLowerInvariant();

                        // Làm sạch extension: chỉ lấy extension nếu đúng định dạng cho phép
                        if (string.IsNullOrEmpty(extension) || !allowedExtensions.Contains(extension))
                        {
                            ModelState.AddModelError("", "Only image files (jpg, jpeg, png, gif) are allowed.");
                            return View(viewModel);
                        }

                        // Đặt tên file mới hoàn toàn không liên quan tên gốc
                        string uniqueFileName = Guid.NewGuid().ToString("N") + extension;

                        string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images", "promotions");
                        if (!Directory.Exists(uploadsFolder))
                        {
                            Directory.CreateDirectory(uploadsFolder);
                        }

                        string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                        // Đảm bảo filePath nằm trong uploadsFolder (tránh path traversal)
                        if (!filePath.StartsWith(uploadsFolder, StringComparison.OrdinalIgnoreCase))
                        {
                            ModelState.AddModelError("", "Invalid file path.");
                            return View(viewModel);
                        }

                        // Xóa ảnh cũ nếu có
                        if (!string.IsNullOrEmpty(promotion.Image))
                        {
                            string oldImagePath = Path.Combine(_webHostEnvironment.WebRootPath, promotion.Image.TrimStart('/'));
                            if (System.IO.File.Exists(oldImagePath))
                            {
                                System.IO.File.Delete(oldImagePath);
                            }
                        }

                        // uniqueFileName is generated using Guid and validated extension only.
                        // filePath is checked to be inside uploadsFolder, so this is safe from path traversal.
                        using (var fileStream = new FileStream(filePath, FileMode.Create))
                        {
                            await imageFile.CopyToAsync(fileStream);
                        }

                        promotion.Image = "/images/promotions/" + uniqueFileName;
                    }

                    _promotionService.Update(promotion);
                    _promotionService.Save();
                    await _dashboardHubContext.Clients.All.SendAsync("DashboardUpdated");
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

        /// <summary>
        /// Trang xóa khuyến mãi
        /// </summary>
        /// <remarks>url: /Promotion/Delete (GET)</remarks>
        public ActionResult Delete(int id)
        {
            var promotion = _promotionService.GetById(id);
            if (promotion == null)
            {
                return NotFound();
            }
            return View(promotion);
        }

        /// <summary>
        /// Xóa khuyến mãi
        /// </summary>
        /// <remarks>url: /Promotion/Delete (POST)</remarks>
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> DeleteConfirmed(int id)
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
                await _dashboardHubContext.Clients.All.SendAsync("DashboardUpdated");
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
