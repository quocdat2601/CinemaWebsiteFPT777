using Microsoft.AspNetCore.Mvc;
using MovieTheater.Models;
using MovieTheater.Service;
using System;
using Microsoft.AspNetCore.Authorization;

namespace MovieTheater.Controllers
{
    public class VoucherController : Controller
    {
        private readonly IVoucherService _voucherService;
        public VoucherController(IVoucherService voucherService)
        {
            _voucherService = voucherService;
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
                return RedirectToAction("VoucherMg");
            }
            return View(voucher);
        }
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public IActionResult AdminEdit(Voucher model, string returnUrl)
        {
            if (!ModelState.IsValid) return View(model);
            _voucherService.Update(model);
            TempData["ToastMessage"] = "Voucher updated successfully.";
            return Redirect("/Admin/MainPage?tab=VoucherMg");
        }
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public IActionResult AdminCreate()
        {
            return View();
        }
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public IActionResult AdminCreate(Voucher model, string returnUrl)
        {
            if (!ModelState.IsValid) return View(model);
            model.VoucherId = _voucherService.GenerateVoucherId();
            model.CreatedDate = DateTime.Now;
            model.RemainingValue = model.Value;
            model.IsUsed = false;
            _voucherService.Add(model);
            TempData["ToastMessage"] = "Voucher created successfully.";
            return Redirect("/Admin/MainPage?tab=VoucherMg");
        }
    }
} 