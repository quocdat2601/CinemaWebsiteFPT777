using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MovieTheater.Service;

namespace MovieTheater.Controllers
{
    public class PromotionController : Controller
    {
        private readonly IPromotionService _promotionService;

        public PromotionController(IPromotionService promotionService)
        {
            _promotionService = promotionService;
        }

        // GET: PromotionController
        public ActionResult List()
        {
            return View();
        }

        // GET: PromotionController/Details/5
        public ActionResult Details(int id)
        {
            return View();
        }

        // GET: PromotionController/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: PromotionController/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(IFormCollection collection)
        {
            try
            {
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View();
            }
        }

        // GET: PromotionController/Edit/5
        public ActionResult Edit(int id)
        {
            return View();
        }

        // POST: PromotionController/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(int id, IFormCollection collection)
        {
            try
            {
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View();
            }
        }

        // GET: PromotionController/Delete/5
        public ActionResult Delete(int id)
        {
            return View();
        }

        // POST: PromotionController/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(int id, IFormCollection collection)
        {
            try
            {_promotionService.Delete(id);
                TempData["ToastMessage"] = "Promotion deleted successfully!";
                return RedirectToAction("MainPage", "Admin", new { tab = "PromotionMg" });
                /* return RedirectToAction(nameof(Index));*/
            }
            catch
            {
                TempData["ToastMessage"] = "Failed to delete promotion.";
                return RedirectToAction("MainPage", "Admin", new { tab = "PromotionMg" });
            }

          

        }
    }
}
