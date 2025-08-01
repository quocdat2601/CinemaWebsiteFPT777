using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using MovieTheater.Hubs;
using MovieTheater.Models;
using MovieTheater.Service;
using MovieTheater.ViewModels;
using System.Data;
using System.Security.Claims;
using MovieTheater.Helpers;

namespace MovieTheater.Controllers
{
    public class VoucherController : Controller
    {
        private readonly IVoucherService _voucherService;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly IHubContext<DashboardHub> _dashboardHubContext;
        
        // Constants for string literals
        private const string TOAST_MESSAGE = "ToastMessage";
        private const string ERROR_MESSAGE = "ErrorMessage";
        private const string MAIN_PAGE = "MainPage";
        private const string ADMIN_CONTROLLER = "Admin";
        private const string EMPLOYEE_CONTROLLER = "Employee";
        private const string VOUCHER_MG_TAB = "VoucherMg";
        private const string INDEX_ACTION = "Index";
        
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
        [Authorize]
        public IActionResult Index()
        {
            var accountId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(accountId))
                return RedirectToAction("Login", "Account");

            var vouchers = _voucherService.GetAvailableVouchers(accountId);
            return View(vouchers);
        }

        /// <summary>
        /// Trang quản lý voucher (admin)
        /// </summary>
        /// <remarks>url: /Voucher/AdminIndex (GET)</remarks>
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public IActionResult AdminIndex(string keyword = "", string statusFilter = "", string expiryFilter = "")
        {
            var vouchers = _voucherService.GetAll();

            if (!string.IsNullOrEmpty(keyword))
            {
                vouchers = vouchers.Where(v => v.Code.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
                                               v.Account?.FullName.Contains(keyword, StringComparison.OrdinalIgnoreCase) == true).ToList();
            }

            if (!string.IsNullOrEmpty(statusFilter))
            {
                bool isUsed = statusFilter.ToLower() == "used";
                vouchers = vouchers.Where(v => v.IsUsed == isUsed).ToList();
            }

            if (!string.IsNullOrEmpty(expiryFilter))
            {
                DateTime now = DateTime.Now;
                switch (expiryFilter.ToLower())
                {
                    case "expired":
                        vouchers = vouchers.Where(v => v.ExpiryDate < now).ToList();
                        break;
                    case "expiring_soon":
                        vouchers = vouchers.Where(v => v.ExpiryDate >= now && v.ExpiryDate <= now.AddDays(7)).ToList();
                        break;
                    case "valid":
                        vouchers = vouchers.Where(v => v.ExpiryDate > now.AddDays(7)).ToList();
                        break;
                }
            }

            return View(vouchers);
        }

        /// <summary>
        /// Xem chi tiết voucher
        /// </summary>
        /// <remarks>url: /Voucher/Details/{id} (GET)</remarks>
        [Authorize]
        public IActionResult Details(string id)
        {
            var voucher = _voucherService.GetById(id);
            if (voucher == null)
                return NotFound();

            return View(voucher);
        }

        /// <summary>
        /// Lấy chi tiết voucher (admin, ajax)
        /// </summary>
        /// <remarks>url: /Voucher/GetVoucherDetails/{id} (GET)</remarks>
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public IActionResult GetVoucherDetails(string id)
        {
            var voucher = _voucherService.GetById(id);
            if (voucher == null)
                return Json(new { success = false, message = "Voucher not found." });

            var result = new
            {
                success = true,
                voucher = new
                {
                    id = voucher.VoucherId,
                    code = voucher.Code,
                    value = voucher.Value,
                    expiryDate = voucher.ExpiryDate.ToString("yyyy-MM-dd"),
                    isUsed = voucher.IsUsed,
                    account = voucher.Account != null ? new
                    {
                        accountId = voucher.Account.AccountId,
                        fullName = voucher.Account.FullName,
                        email = voucher.Account.Email,
                        phoneNumber = voucher.Account.PhoneNumber
                    } : null,
                    image = voucher.Image
                }
            };

            return Json(result);
        }

        /// <summary>
        /// Trang tạo voucher mới
        /// </summary>
        /// <remarks>url: /Voucher/Create (GET)</remarks>
        [Authorize(Roles = "Admin")]
        public IActionResult Create()
        {
            return View();
        }

        /// <summary>
        /// Tạo voucher mới
        /// </summary>
        /// <remarks>url: /Voucher/Create (POST)</remarks>
        [Authorize(Roles = "Admin")]
        public IActionResult Create(Voucher voucher)
        {
            if (ModelState.IsValid)
            {
                _voucherService.Add(voucher);
                return RedirectToAction(nameof(Index));
            }
            return View(voucher);
        }

        /// <summary>
        /// Trang sửa voucher
        /// </summary>
        /// <remarks>url: /Voucher/Edit/{id} (GET)</remarks>
        [Authorize(Roles = "Admin")]
        public IActionResult Edit(string id)
        {
            var voucher = _voucherService.GetById(id);
            if (voucher == null)
                return NotFound();

            return View(voucher);
        }

        /// <summary>
        /// Sửa voucher
        /// </summary>
        /// <remarks>url: /Voucher/Edit/{id} (POST)</remarks>
        [Authorize(Roles = "Admin")]
        public IActionResult Edit(Voucher voucher)
        {
            if (ModelState.IsValid)
            {
                _voucherService.Update(voucher);
                return RedirectToAction(nameof(Index));
            }
            return View(voucher);
        }

        /// <summary>
        /// Trang xóa voucher
        /// </summary>
        /// <remarks>url: /Voucher/Delete/{id} (GET)</remarks>
        [Authorize(Roles = "Admin")]
        public IActionResult Delete(string id)
        {
            var voucher = _voucherService.GetById(id);
            if (voucher == null)
                return NotFound();

            return View(voucher);
        }

        /// <summary>
        /// Xóa voucher
        /// </summary>
        /// <remarks>url: /Voucher/Delete/{id} (POST)</remarks>
        [Authorize(Roles = "Admin")]
        public IActionResult DeleteConfirmed(string id)
        {
            var voucher = _voucherService.GetById(id);
            if (voucher != null)
            {
                _voucherService.Delete(id);
            }
            return RedirectToAction(nameof(Index));
        }

        /// <summary>
        /// Xóa voucher (admin)
        /// </summary>
        /// <remarks>url: /Voucher/AdminDelete (POST)</remarks>
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AdminDelete(string id, string returnUrl)
        {
            var voucher = _voucherService.GetById(id);
            if (voucher == null)
            {
                TempData[TOAST_MESSAGE] = "Voucher not found.";
                return Redirect($"/{ADMIN_CONTROLLER}/{MAIN_PAGE}?tab={VOUCHER_MG_TAB}");
            }
            _voucherService.Delete(id);
            await _dashboardHubContext.Clients.All.SendAsync("DashboardUpdated");
            TempData[TOAST_MESSAGE] = "Voucher deleted successfully.";
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
        [Authorize(Roles = "Admin")]
        public IActionResult AdminEdit(string id)
        {
            var voucher = _voucherService.GetById(id);
            if (voucher == null)
            {
                TempData[TOAST_MESSAGE] = "Voucher not found.";
                return Redirect($"/{ADMIN_CONTROLLER}/{MAIN_PAGE}?tab={VOUCHER_MG_TAB}");
            }

            // Check if voucher can be edited
            var now = DateTime.Now;
            var isExpired = voucher.ExpiryDate <= now;
            var isUsed = voucher.IsUsed.HasValue && voucher.IsUsed.Value;
            if (isUsed || isExpired)
            {
                TempData[TOAST_MESSAGE] = "Cannot edit used or expired vouchers.";
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
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AdminEdit(VoucherViewModel viewModel, IFormFile? imageFile)
        {
            if (!ModelState.IsValid) return View(viewModel);
            try
            {
                var voucher = _voucherService.GetById(viewModel.VoucherId);
                if (voucher == null)
                {
                    TempData[TOAST_MESSAGE] = "Voucher not found.";
                    return Redirect($"/{ADMIN_CONTROLLER}/{MAIN_PAGE}?tab={VOUCHER_MG_TAB}");
                }

                // Check if voucher can be edited
                var now = DateTime.Now;
                var isExpired = voucher.ExpiryDate <= now;
                var isUsed = voucher.IsUsed.HasValue && voucher.IsUsed.Value;

                if (isUsed || isExpired)
                {
                    TempData[TOAST_MESSAGE] = "Cannot edit used or expired vouchers.";
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

                    string sanitizedFileName = PathSecurityHelper.SanitizeFileName(imageFile.FileName);
                    string uniqueFileName = Guid.NewGuid().ToString() + "_" + sanitizedFileName;
                    
                    string? secureFilePath = PathSecurityHelper.CreateSecureFilePath(uploadsFolder, uniqueFileName);
                    if (secureFilePath == null)
                    {
                        TempData[ERROR_MESSAGE] = "Invalid file path detected.";
                        return View(viewModel);
                    }
                    
                    using (var fileStream = new FileStream(secureFilePath, FileMode.Create))
                    {
                        await imageFile.CopyToAsync(fileStream);
                    }

                    voucher.Image = "/images/vouchers/" + uniqueFileName;
                }

                _voucherService.Update(voucher);
                await _dashboardHubContext.Clients.All.SendAsync("DashboardUpdated");
                TempData[TOAST_MESSAGE] = "Voucher updated successfully.";
                if (role == "Admin")
                    return RedirectToAction("MainPage", "Admin", new { tab = "VoucherMg" });
                else
                    return RedirectToAction("MainPage", "Employee", new { tab = "VoucherMg" });
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "[Voucher AdminCreate] Exception: {Message}", ex.Message);
                ModelState.AddModelError("", "Error updating voucher: " + ex.Message);
                return View(viewModel);
            }
        }

        /// <summary>
        /// Trang tạo voucher (admin)
        /// </summary>
        /// <remarks>url: /Voucher/AdminCreate (GET)</remarks>
        [HttpGet]
        [Authorize(Roles = "Admin")]
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
        [Authorize(Roles = "Admin")]
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

                        string sanitizedFileName = PathSecurityHelper.SanitizeFileName(imageFile.FileName);
                        string uniqueFileName = Guid.NewGuid().ToString() + "_" + sanitizedFileName;
                        
                        string? secureFilePath = PathSecurityHelper.CreateSecureFilePath(uploadsFolder, uniqueFileName);
                        if (secureFilePath == null)
                        {
                            TempData[ERROR_MESSAGE] = "Invalid file path detected.";
                            return View(viewModel);
                        }
                        
                        using (var fileStream = new FileStream(secureFilePath, FileMode.Create))
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
                    TempData[TOAST_MESSAGE] = "Voucher created successfully!";
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
        [Authorize(Roles = "Admin")]
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