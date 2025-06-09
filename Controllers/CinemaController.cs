using Microsoft.AspNetCore.Mvc;
using MovieTheater.Models;
using MovieTheater.Service;

namespace MovieTheater.Controllers
{
    public class CinemaController : Controller
    {
        private readonly ICinemaService _cinemaService;
        public CinemaController(ICinemaService cinemaService)
        {
            _cinemaService = cinemaService;
        }

        // GET: CinemaController
        public ActionResult Index()
        {
            return View();
        }

        // GET: CinemaController/Details/5
        public ActionResult Details(int id)
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(CinemaRoom cinemaRoom)
        {
            if (!ModelState.IsValid)
            {
                return RedirectToAction("MainPage", "Admin", new { tab = "ShowroomMg" });
            }
            _cinemaService.Add(cinemaRoom);
            return RedirectToAction("MainPage", "Admin", new { tab = "ShowroomMg" });
        }


        // GET: CinemaController/Edit/5
        [HttpGet]
        public IActionResult Edit(int id)
        {
            var showroom = _cinemaService.GetById(id);
            if (showroom == null)
                return NotFound();

            return View(showroom);
        }

        // POST: CinemaController/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(int id, CinemaRoom cinemaRoom)
        {
            if (!ModelState.IsValid)
            {
                return View(cinemaRoom);
            }

            bool success = _cinemaService.Update(id, cinemaRoom);

            if (!success)
            {
                ModelState.AddModelError("", "Failed to update movie.");
                return RedirectToAction("MainPage", "Admin", new { tab = "ShowroomMg" });
            }
            return RedirectToAction("MainPage", "Admin", new { tab = "ShowroomMg" });
        }

        // GET: CinemaController/Delete/5
        public ActionResult Delete(int id)
        {
            return View();
        }

        // POST: CinemaController/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id, IFormCollection collection)
        {
            try
            {
                var cinema = _cinemaService.GetById(id);
                if (cinema == null)
                {
                    TempData["ToastMessage"] = "Cinema not found.";
                    return RedirectToAction("MainPage", "Admin", new { tab = "ShowroomMg" });
                }

                bool success = await _cinemaService.DeleteAsync(id);

                if (!success)
                {
                    TempData["ToastMessage"] = "Failed to delete showroom.";
                    return RedirectToAction("MainPage", "Admin", new { tab = "ShowroomMg" });
                }

                TempData["ToastMessage"] = "Showroom deleted successfully!";
                return RedirectToAction("MainPage", "Admin", new { tab = "ShowroomMg" });
            }
            catch (Exception ex)
            {
                TempData["ToastMessage"] = $"An error occurred during deletion: {ex.Message}";
                return RedirectToAction("MainPage", "Admin", new { tab = "ShowroomMg" });
            }
        }
    }
}
