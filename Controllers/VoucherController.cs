using Microsoft.AspNetCore.Mvc;
using MovieTheater.Models;
using MovieTheater.Service;
using System;

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
    }
} 