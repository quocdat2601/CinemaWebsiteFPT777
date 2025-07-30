using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using MovieTheater.Hubs;
using MovieTheater.Models;
using MovieTheater.Service;
using MovieTheater.ViewModels;
using System.Data;
using System.Security.Claims;

namespace MovieTheater.Controllers
{
    public class VoucherController : Controller
    {
        private readonly IVoucherService _voucherService;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly IHubContext<DashboardHub> _dashboardHubContext;
        public string role => User?.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value;

        public VoucherController(IVoucherService voucherService, IWebHostEnvironment webHostEnvironment, IHubContext<DashboardHub> dashboardHubContext)
        {
            _voucherService = voucherService;
            _webHostEnvironment = webHostEnvironment;
            _dashboardHubContext = dashboardHubContext;            
        }

        /// <summary>
        /// Trang danh sách voucher
        /// </summary>
        /// <remarks>url: /Voucher/Index (GET)</remarks>
        public IActionResult Index()
        {
            var vouchers = _voucherService.GetAll();
            return View(vouchers);
        }

        /// <summary>
        /// Trang quản lý voucher cho admin
        /// </summary>
        /// <remarks>url: /Voucher/AdminIndex (GET)</remarks>
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public IActionResult AdminIndex(string keyword = "", string statusFilter = "", string expiryFilter = "")
        {
            var filter = new MovieTheater.Service.VoucherFilterModel
            {
                Keyword = keyword,
                StatusFilter = statusFilter,
                ExpiryFilter = expiryFilter
            };
            var vouchers = _voucherService.GetFilteredVouchers(filter);
            ViewBag.Keyword = keyword;
            ViewBag.StatusFilter = statusFilter;
            ViewBag.ExpiryFilter = expiryFilter;
            return View("VoucherMg", vouchers);
        }

        /// <summary>
        /// Xem chi tiết voucher
        /// </summary>
        /// <remarks>url: /Voucher/Details (GET)</remarks>
        public IActionResult Details(string id)
        {
            var voucher = _voucherService.GetById(id);
            if (voucher == null) return NotFound();
            return View(voucher);
        }

        /// <summary>
        /// Lấy chi tiết voucher (admin, ajax)
        /// </summary>
        /// <remarks>url: /Voucher/GetVoucherDetails (GET)</remarks>
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public IActionResult GetVoucherDetails(string id)
        {
            var voucher = _voucherService.GetById(id);
            if (voucher == null)
            {
                return Json(new { success = false, message = "Voucher not found" });
            }

            var now = DateTime.Now;
            var isExpired = voucher.ExpiryDate <= now;
            var isUsed = voucher.IsUsed.HasValue && voucher.IsUsed.Value;
            var daysUntilExpiry = (voucher.ExpiryDate - now).Days;

            var details = new
            {
                success = true,
                voucher = new
                {
                    id = voucher.VoucherId,
                    code = voucher.Code,
                    accountId = voucher.AccountId,
                    value = voucher.Value,
                    createdDate = voucher.CreatedDate.ToString("dd/MM/yyyy HH:mm"),
                    expiryDate = voucher.ExpiryDate.ToString("dd/MM/yyyy HH:mm"),
                    isUsed = voucher.IsUsed,
                    image = voucher.Image,
                    status = isUsed ? "Used" : isExpired ? "Expired" : "Active",
                    daysUntilExpiry = daysUntilExpiry,
                    isExpiringSoon = daysUntilExpiry <= 7 && daysUntilExpiry > 0
                }
            };

            return Json(details);
        }

        /// <summary>
        /// Trang tạo voucher mới
        /// </summary>
        /// <remarks>url: /Voucher/Create (GET)</remarks>
        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        /// <summary>
        /// Tạo voucher mới
        /// </summary>
        /// <remarks>url: /Voucher/Create (POST)</remarks>
        [HttpPost]
        public IActionResult Create(Voucher voucher)
        {
            if (!ModelState.IsValid) return View(voucher);
            voucher.VoucherId = _voucherService.GenerateVoucherId();
            voucher.Code = "VOUCHER";
            voucher.CreatedDate = DateTime.Now;
            voucher.ExpiryDate = DateTime.Now.AddDays(30);
            voucher.Value = voucher.Value;
            voucher.IsUsed = false;
            voucher.Image = "/voucher-img/voucher.jpg";
            _voucherService.Add(voucher);
            return RedirectToAction("Index");
        }

        /// <summary>
        /// Trang sửa voucher
        /// </summary>
        /// <remarks>url: /Voucher/Edit (GET)</remarks>
        [HttpGet]
        public IActionResult Edit(string id)
        {
            var voucher = _voucherService.GetById(id);
            if (voucher == null) return NotFound();
            return View(voucher);
        }

        /// <summary>
        /// Sửa voucher
        /// </summary>
        /// <remarks>url: /Voucher/Edit (POST)</remarks>
        [HttpPost]
        public IActionResult Edit(Voucher voucher)
        {
            if (!ModelState.IsValid) return View(voucher);
            _voucherService.Update(voucher);
            return RedirectToAction("Index");
        }

        /// <summary>
        /// Trang xóa voucher
        /// </summary>
        /// <remarks>url: /Voucher/Delete (GET)</remarks>
        [HttpGet]
        public IActionResult Delete(string id)
        {
            var voucher = _voucherService.GetById(id);
            if (voucher == null) return NotFound();
            return View(voucher);
        }

        /// <summary>
        /// Xóa voucher
        /// </summary>
        /// <remarks>url: /Voucher/Delete (POST)</remarks>
        [HttpPost, ActionName("Delete")]
        public IActionResult DeleteConfirmed(string id)
        {
            _voucherService.Delete(id);
            return RedirectToAction("Index");
        }

        /// <summary>
        /// Xóa voucher (admin)
        /// </summary>
        /// <remarks>url: /Voucher/AdminDelete (POST)</remarks>
        [HttpPost]
        [Authorize(Roles = "Admin, Employee")]
        public async Task<IActionResult> AdminDelete(string id, string returnUrl)
        {
            var voucher = _voucherService.GetById(id);
            if (voucher == null)
            {
                TempData["ToastMessage"] = "Voucher not found.";
                return Redirect("/Admin/MainPage?tab=VoucherMg");
            }
            _voucherService.Delete(id);
            await _dashboardHubContext.Clients.All.SendAsync("DashboardUpdated");
            TempData["ToastMessage"] = "Voucher deleted successfully.";
            if (role == "Admin")
                return RedirectToAction("MainPage", "Admin", new { tab = "VoucherMg" });
            else
                return RedirectToAction("MainPage", "Employee", new { tab = "VoucherMg" });
        }

        /// <summary>
        /// Trang sửa voucher (admin)
        /// </summary>
        /// <remarks>url: /Voucher/AdminEdit (GET)</remarks>
        [HttpGet]
        [Authorize(Roles = "Admin, Employee")]
        public IActionResult AdminEdit(string id)
        {
            var voucher = _voucherService.GetById(id);
            if (voucher == null)
            {
                TempData["ToastMessage"] = "Voucher not found.";
                return Redirect("/Admin/MainPage?tab=VoucherMg");
            }

            // Check if voucher can be edited
            var now = DateTime.Now;
            var isExpired = voucher.ExpiryDate <= now;
            var isUsed = voucher.IsUsed.HasValue && voucher.IsUsed.Value;
            if (isUsed || isExpired)
            {
                TempData["ToastMessage"] = "Cannot edit used or expired vouchers.";
                if (role == "Admin")
                    return RedirectToAction("MainPage", "Admin", new { tab = "VoucherMg" });
                else
                    return RedirectToAction("MainPage", "Employee", new { tab = "VoucherMg" });
            }

            var viewModel = new VoucherViewModel
            {
                VoucherId = voucher.VoucherId,
                AccountId = voucher.AccountId,
                Code = voucher.Code,
                Value = voucher.Value,
                CreatedDate = voucher.CreatedDate,
                ExpiryDate = voucher.ExpiryDate,
                IsUsed = voucher.IsUsed ?? false,
                Image = voucher.Image
            };

            return View(viewModel);
        }

        /// <summary>
        /// Sửa voucher (admin)
        /// </summary>
        /// <remarks>url: /Voucher/AdminEdit (POST)</remarks>
        [HttpPost]
        [Authorize(Roles = "Admin, Employee")]
        public async Task<IActionResult> AdminEdit(VoucherViewModel viewModel, IFormFile? imageFile)
        {
            if (!ModelState.IsValid) return View(viewModel);
            try
            {
                var voucher = _voucherService.GetById(viewModel.VoucherId);
                if (voucher == null)
                {
                    TempData["ToastMessage"] = "Voucher not found.";
                    return Redirect("/Admin/MainPage?tab=VoucherMg");
                }

                // Check if voucher can be edited
                var now = DateTime.Now;
                var isExpired = voucher.ExpiryDate <= now;
                var isUsed = voucher.IsUsed.HasValue && voucher.IsUsed.Value;

                if (isUsed || isExpired)
                {
                    TempData["ToastMessage"] = "Cannot edit used or expired vouchers.";
                    if (role == "Admin")
                        return RedirectToAction("MainPage", "Admin", new { tab = "VoucherMg" });
                    else
                        return RedirectToAction("MainPage", "Employee", new { tab = "VoucherMg" });
                }

                voucher.AccountId = viewModel.AccountId;
                voucher.Code = viewModel.Code;
                voucher.Value = viewModel.Value;
                voucher.CreatedDate = viewModel.CreatedDate;
                voucher.ExpiryDate = viewModel.ExpiryDate;
                voucher.IsUsed = viewModel.IsUsed;

                if (imageFile != null && imageFile.Length > 0)
                {
                    string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images", "vouchers");
                    if (!Directory.Exists(uploadsFolder))
                    {
                        Directory.CreateDirectory(uploadsFolder);
                    }

                    // Delete old image if exists
                    if (!string.IsNullOrEmpty(voucher.Image))
                    {
                        string oldImagePath = Path.Combine(_webHostEnvironment.WebRootPath, voucher.Image.TrimStart('/'));
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

                    voucher.Image = "/images/vouchers/" + uniqueFileName;
                }

                _voucherService.Update(voucher);
                await _dashboardHubContext.Clients.All.SendAsync("DashboardUpdated");
                TempData["ToastMessage"] = "Voucher updated successfully.";
                if (role == "Admin")
                    return RedirectToAction("MainPage", "Admin", new { tab = "VoucherMg" });
                else
                    return RedirectToAction("MainPage", "Employee", new { tab = "VoucherMg" });
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Error updating voucher: " + ex.Message);
                return View(viewModel);
            }
        }

        /// <summary>
        /// Trang tạo voucher (admin)
        /// </summary>
        /// <remarks>url: /Voucher/AdminCreate (GET)</remarks>
        [HttpGet]
        [Authorize(Roles = "Admin, Employee")]
        public IActionResult AdminCreate()
        {
            return View(new VoucherViewModel
            {
                CreatedDate = DateTime.Now,
                ExpiryDate = DateTime.Now.AddDays(30),
                IsUsed = false
            });
        }

        /// <summary>
        /// Tạo voucher (admin)
        /// </summary>
        /// <remarks>url: /Voucher/AdminCreate (POST)</remarks>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin, Employee")]
        public async Task<IActionResult> AdminCreate(VoucherViewModel viewModel, IFormFile? imageFile)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var voucher = new Voucher
                    {
                        VoucherId = _voucherService.GenerateVoucherId(),
                        AccountId = viewModel.AccountId,
                        Code = viewModel.Code,
                        Value = viewModel.Value,
                        CreatedDate = viewModel.CreatedDate,
                        ExpiryDate = viewModel.ExpiryDate,
                        IsUsed = viewModel.IsUsed
                    };

                    if (imageFile != null && imageFile.Length > 0)
                    {
                        string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images", "vouchers");
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

                        voucher.Image = "/images/vouchers/" + uniqueFileName;
                    }
                    else
                    {
                        voucher.Image = "/images/vouchers/voucher.jpg";
                    }

                    _voucherService.Add(voucher);
                    await _dashboardHubContext.Clients.All.SendAsync("DashboardUpdated");
                    TempData["ToastMessage"] = "Voucher created successfully!";
                    if (role == "Admin")
                        return RedirectToAction("MainPage", "Admin", new { tab = "VoucherMg" });
                    else
                        return RedirectToAction("MainPage", "Employee", new { tab = "VoucherMg" });
                }
                catch (Exception ex)
                {
                    Serilog.Log.Error(ex, "[Voucher AdminCreate] Exception: {Message}", ex.Message);
                    ModelState.AddModelError("", "Error creating voucher: " + ex.Message);
                }
            }
            return View(viewModel);
        }

        /// <summary>
        /// Lấy danh sách member (admin, ajax)
        /// </summary>
        /// <remarks>url: /Voucher/GetAllMembers (GET)</remarks>
        [HttpGet]
        [Authorize(Roles = "Admin, Employee")]
        public IActionResult GetAllMembers()
        {
            var members = _voucherService.GetAllMembers()
                .Select(m => new
                {
                    memberId = m.MemberId,
                    score = m.Score,
                    account = new
                    {
                        accountId = m.Account?.AccountId,
                        fullName = m.Account?.FullName,
                        identityCard = m.Account?.IdentityCard,
                        email = m.Account?.Email,
                        phoneNumber = m.Account?.PhoneNumber
                    }
                }).ToList();
            return Json(members);
        }

        /// <summary>
        /// Lấy voucher khả dụng cho user
        /// </summary>
        /// <remarks>url: /Voucher/GetAvailableVouchers (GET)</remarks>
        [HttpGet]
        [Authorize]
        public IActionResult GetAvailableVouchers(string? accountId = null)
        {
            if (string.IsNullOrEmpty(accountId))
            {
                accountId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            }
            if (string.IsNullOrEmpty(accountId))
                return Json(new List<object>());

            var vouchers = _voucherService.GetAvailableVouchers(accountId)
                .Select(v => new
                {
                    id = v.VoucherId,
                    code = v.Code,
                    value = v.Value,
                    expirationDate = v.ExpiryDate.ToString("yyyy-MM-dd"),
                    image = v.Image
                }).ToList();

            return Json(vouchers);
        }
    }
}