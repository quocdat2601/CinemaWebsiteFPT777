using Microsoft.AspNetCore.Mvc;
using MovieTheater.Models;
using MovieTheater.Service;
using MovieTheater.ViewModels;
using System;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using System.IO;
using System.Threading.Tasks;
using System.Linq;
using Serilog;
using System.Collections.Generic;

namespace MovieTheater.Controllers
{
    public class VoucherController : Controller
    {
        private readonly IVoucherService _voucherService;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public VoucherController(IVoucherService voucherService, IWebHostEnvironment webHostEnvironment)
        {
            _voucherService = voucherService;
            _webHostEnvironment = webHostEnvironment;
        }

        public IActionResult Index()
        {
            var vouchers = _voucherService.GetAll();
            return View(vouchers);
        }

        public IActionResult Details(string id)
        {
            var voucher = _voucherService.GetById(id);
            if (voucher == null) return NotFound();
            return View(voucher);
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Create(Voucher voucher)
        {
            if (!ModelState.IsValid) return View(voucher);
            voucher.VoucherId = _voucherService.GenerateVoucherId();
            voucher.Code = "VOUCHER";
            voucher.CreatedDate = DateTime.Now;
            voucher.ExpiryDate = DateTime.Now.AddDays(30);
            voucher.RemainingValue = voucher.Value;
            voucher.IsUsed = false;
            voucher.Image = "/voucher-img/voucher.jpg";
            _voucherService.Add(voucher);
            return RedirectToAction("Index");
        }

        [HttpGet]
        public IActionResult Edit(string id)
        {
            var voucher = _voucherService.GetById(id);
            if (voucher == null) return NotFound();
            return View(voucher);
        }

        [HttpPost]
        public IActionResult Edit(Voucher voucher)
        {
            if (!ModelState.IsValid) return View(voucher);
            _voucherService.Update(voucher);
            return RedirectToAction("Index");
        }

        [HttpGet]
        public IActionResult Delete(string id)
        {
            var voucher = _voucherService.GetById(id);
            if (voucher == null) return NotFound();
            return View(voucher);
        }

        [HttpPost, ActionName("Delete")]
        public IActionResult DeleteConfirmed(string id)
        {
            _voucherService.Delete(id);
            return RedirectToAction("Index");
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public IActionResult AdminDelete(string id, string returnUrl)
        {
            var voucher = _voucherService.GetById(id);
            if (voucher == null)
            {
                TempData["ToastMessage"] = "Voucher not found.";
                return Redirect("/Admin/MainPage?tab=VoucherMg");
            }
            _voucherService.Delete(id);
            TempData["ToastMessage"] = "Voucher deleted successfully.";
            return Redirect("/Admin/MainPage?tab=VoucherMg");
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public IActionResult AdminEdit(string id)
        {
            var voucher = _voucherService.GetById(id);
            if (voucher == null)
            {
                TempData["ToastMessage"] = "Voucher not found.";
                return Redirect("/Admin/MainPage?tab=VoucherMg");
            }

            var viewModel = new VoucherViewModel
            {
                VoucherId = voucher.VoucherId,
                AccountId = voucher.AccountId,
                Code = voucher.Code,
                Value = voucher.Value,
                RemainingValue = voucher.RemainingValue,
                CreatedDate = voucher.CreatedDate,
                ExpiryDate = voucher.ExpiryDate,
                IsUsed = voucher.IsUsed ?? false,
                Image = voucher.Image
            };

            return View(viewModel);
        }

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
                    TempData["ToastMessage"] = "Voucher not found.";
                    return Redirect("/Admin/MainPage?tab=VoucherMg");
                }

                voucher.AccountId = viewModel.AccountId;
                voucher.Code = viewModel.Code;
                voucher.Value = viewModel.Value;
                voucher.RemainingValue = viewModel.RemainingValue;
                voucher.CreatedDate = viewModel.CreatedDate;
                voucher.ExpiryDate = viewModel.ExpiryDate;
                voucher.IsUsed = viewModel.IsUsed;

                if (imageFile != null && imageFile.Length > 0)
                {
                    string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "voucher-img");
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

                    voucher.Image = "/voucher-img/" + uniqueFileName;
                }

                _voucherService.Update(voucher);
                TempData["ToastMessage"] = "Voucher updated successfully.";
                return Redirect("/Admin/MainPage?tab=VoucherMg");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Error updating voucher: " + ex.Message);
                return View(viewModel);
            }
        }

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
                        RemainingValue = viewModel.RemainingValue,
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
                    TempData["ToastMessage"] = "Voucher created successfully!";
                    return RedirectToAction("MainPage", "Admin", new { tab = "VoucherMg" });
                }
                catch (Exception ex)
                {
                    Serilog.Log.Error(ex, "[Voucher AdminCreate] Exception: {Message}", ex.Message);
                    ModelState.AddModelError("", "Error creating voucher: " + ex.Message);
                }
            }
            return View(viewModel);
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public IActionResult GetAllMembers()
        {
            var members = _voucherService.GetAllMembers()
                .Select(m => new {
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
                .Select(v => new {
                    id = v.VoucherId,
                    code = v.Code,
                    remainingValue = v.RemainingValue,
                    expirationDate = v.ExpiryDate.ToString("yyyy-MM-dd"),
                    image = v.Image
                }).ToList();

            return Json(vouchers);
        }
    }
} 