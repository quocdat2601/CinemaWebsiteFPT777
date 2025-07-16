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
