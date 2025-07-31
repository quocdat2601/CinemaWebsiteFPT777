using Microsoft.AspNetCore.Mvc;
using MovieTheater.Models;
using MovieTheater.Service;
using MovieTheater.ViewModels;
using Microsoft.AspNetCore.SignalR;
using MovieTheater.Hubs;
using Microsoft.AspNetCore.Authorization;

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
            return View("List", promotions);
        }

        /// <summary>
        /// Trang quản lý khuyến mãi
        /// </summary>
        /// <remarks>url: /Promotion/Index (GET)</remarks>
        [Authorize(Roles = "Admin")]
        public ActionResult Index()
        {
            return View();
        }

        /// <summary>
        /// Trang tạo khuyến mãi mới
        /// </summary>
        /// <remarks>url: /Promotion/Create (GET)</remarks>
        [Authorize(Roles = "Admin")]
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
        [Authorize(Roles = "Admin")]
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

                    // Add PromotionCondition for TargetField if provided
                    if (!string.IsNullOrEmpty(viewModel.TargetField) && !string.IsNullOrEmpty(viewModel.TargetFieldColumn))
                    {
                        string? targetValueString = null;
                        if (!string.IsNullOrEmpty(viewModel.TargetValue))
                        {
                            // Check if the selected column is a date type
                            var dateColumns = new[] { "DateOfBirth", "RegisterDate", "BookingDate", "ScheduleShow", "FromDate", "ToDate", "EndTime", "StartTime", "ShowDate1" };
                            if (dateColumns.Contains(viewModel.TargetFieldColumn))
                            {
                                // Use the string as entered (already formatted in JS)
                                targetValueString = viewModel.TargetValue;
                            }
                            else
                            {
                                targetValueString = viewModel.TargetValue;
                            }
                        }
                        var promotionCondition = new PromotionCondition
                        {
                            TargetEntity = viewModel.TargetField,
                            TargetField = viewModel.TargetFieldColumn,
                            Operator = viewModel.Operator,
                            TargetValue = targetValueString,
                            PromotionId = nextId
                        };
                        promotion.PromotionConditions.Add(promotionCondition);
                    }

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
        [Authorize(Roles = "Admin")]
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
                IsActive = promotion.IsActive,
                Conditions = promotion.PromotionConditions.Select(pc => new PromotionConditionEditViewModel
                {
                    ConditionId = pc.ConditionId,
                    TargetEntity = pc.TargetEntity,
                    TargetField = pc.TargetField,
                    Operator = pc.Operator,
                    TargetValue = pc.TargetValue
                }).ToList()
            };

            // Map first PromotionCondition if exists (for editing fields)
            var condition = promotion.PromotionConditions.FirstOrDefault();
            if (condition != null)
            {
                viewModel.TargetField = condition.TargetEntity;
                viewModel.TargetFieldColumn = condition.TargetField;
                viewModel.Operator = condition.Operator;
                viewModel.TargetValue = condition.TargetValue;
            }

            return View(viewModel);
        }

        /// <summary>
        /// Sửa khuyến mãi
        /// </summary>
        /// <remarks>url: /Promotion/Edit (POST)</remarks>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
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

                    // Update or add PromotionCondition
                    var dateColumns = new[] { "DateOfBirth", "RegisterDate", "BookingDate", "ScheduleShow", "FromDate", "ToDate", "EndTime", "StartTime", "ShowDate1" };
                    string? targetValueString = null;
                    if (!string.IsNullOrEmpty(viewModel.TargetValue))
                    {
                        if (dateColumns.Contains(viewModel.TargetFieldColumn))
                        {
                            targetValueString = viewModel.TargetValue;
                        }
                        else
                        {
                            targetValueString = viewModel.TargetValue;
                        }
                    }
                    var condition = promotion.PromotionConditions.FirstOrDefault();
                    if (condition != null)
                    {
                        condition.TargetEntity = viewModel.TargetField;
                        condition.TargetField = viewModel.TargetFieldColumn;
                        condition.Operator = viewModel.Operator;
                        condition.TargetValue = targetValueString;
                    }
                    else if (!string.IsNullOrEmpty(viewModel.TargetField) && !string.IsNullOrEmpty(viewModel.TargetFieldColumn))
                    {
                        var newCondition = new PromotionCondition
                        {
                            TargetEntity = viewModel.TargetField,
                            TargetField = viewModel.TargetFieldColumn,
                            Operator = viewModel.Operator,
                            TargetValue = targetValueString,
                            PromotionId = promotion.PromotionId
                        };
                        promotion.PromotionConditions.Add(newCondition);
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
        [Authorize(Roles = "Admin")]
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
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> DeleteConfirmed(int id)
        {
            try
            {
                var promotion = _promotionService.GetById(id);
                if (promotion == null)
                {
                    TempData["ToastMessage"] = "Failed to delete promotion.";
                    return RedirectToAction("MainPage", "Admin", new { tab = "PromotionMg" });
                }
                if (!string.IsNullOrEmpty(promotion.Image))
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
