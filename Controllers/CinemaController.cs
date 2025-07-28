using Microsoft.AspNetCore.Mvc;
using MovieTheater.Models;
using MovieTheater.Service;

namespace MovieTheater.Controllers
{
    public class CinemaController : Controller
    {
        private readonly ICinemaService _cinemaService;
        private readonly IMovieService _movieService;
        public CinemaController(ICinemaService cinemaService, IMovieService movieService)
        {
            _cinemaService = cinemaService;
            _movieService = movieService;
        }

        // GET: CinemaController
        [HttpGet]
        public ActionResult Index()
        {
            return View();
        }

        // GET: CinemaController/Details/5
        [HttpGet]
        public ActionResult Details(int id)
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(CinemaRoom cinemaRoom, int VersionId)
        {
            if (!ModelState.IsValid)
            {
                return RedirectToAction("MainPage", "Admin", new { tab = "ShowroomMg" });
            }
            cinemaRoom.VersionId = VersionId;
            _cinemaService.Add(cinemaRoom);
            TempData["ToastMessage"] = "Showroom created successfully!";
            return RedirectToAction("MainPage", "Admin", new { tab = "ShowroomMg" });
        }


        // GET: CinemaController/Edit/5
        [HttpGet]
        public IActionResult Edit(int id)
        {
            var showroom = _cinemaService.GetById(id);
            if (showroom == null)
                return NotFound();
            var versions = _movieService.GetAllVersions();
            ViewBag.Versions = versions;
            ViewBag.CurrentVersionId = showroom.VersionId ?? 0;
            return View(showroom);
        }

        // POST: CinemaController/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(CinemaRoom cinemaRoom, int VersionId)
        {
            if (!ModelState.IsValid)
            {
                var versions = _movieService.GetAllVersions();
                ViewBag.Versions = versions;
                ViewBag.CurrentVersionId = VersionId;
                return View(cinemaRoom);
            }
            cinemaRoom.VersionId = VersionId;
            bool success = _cinemaService.Update(cinemaRoom);
            if (!success)
            {
                TempData["ErrorMessage"] = "Showroom updated unsuccessfully!";
                ModelState.AddModelError("", "Failed to update movie.");
                return RedirectToAction("MainPage", "Admin", new { tab = "ShowroomMg" });
            }
            TempData["ToastMessage"] = "Showroom updated successfully!";
            return RedirectToAction("MainPage", "Admin", new { tab = "ShowroomMg" });
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
                    TempData["ToastMessage"] = "Showroom not found.";
                    return RedirectToAction("MainPage", "Admin", new { tab = "ShowroomMg" });
                }

                bool success = await _cinemaService.DeleteAsync(id);

                if (!success)
                {
                    TempData["ErrorMessage"] = "Failed to delete showroom.";
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

        [HttpGet]
        public IActionResult Active(int id)
        {
            var cinemaRoom = _cinemaService.GetById(id); 
            if (cinemaRoom == null)
            {
                return NotFound(); 
            }
            return View(cinemaRoom);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Active(CinemaRoom cinemaRoom)
        {
            try
            {
                bool success = _cinemaService.Active(cinemaRoom);
                _cinemaService.SaveAsync();
                if (!success)
                {
                    TempData["ErrorMessage"] = "Failed to update showroom status.";
                    return RedirectToAction("MainPage", "Admin", new { tab = "ShowroomMg" });
                }
                TempData["ToastMessage"] = "Showroom status updated successfully!";
                return RedirectToAction("MainPage", "Admin", new { tab = "ShowroomMg" });
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"An error occurred while activating the showroom: {ex.Message}";
                return RedirectToAction("MainPage", "Admin", new { tab = "ShowroomMg" });
            }
        }

        [HttpGet]
        public IActionResult GetRoomsByVersion(int versionId)
        {
            var rooms = _cinemaService.GetRoomsByVersion(versionId)
                .Select(r => new { r.CinemaRoomId, r.CinemaRoomName })
                .ToList();
            return Json(rooms);
        }

        [HttpGet]
        public IActionResult GetMovieShowsByCinemaRoomGrouped(int cinemaRoomId)
        {
            var shows = _movieService.GetMovieShow()
                .Where(ms => ms.CinemaRoomId == cinemaRoomId)
                .GroupBy(ms => ms.ShowDate)
                .OrderBy(g => g.Key)
                .Select(g => new {
                    date = g.Key.ToString("dd/MM/yyyy"),
                    times = g.OrderBy(ms => ms.Schedule?.ScheduleTime).Select(ms => ms.Schedule?.ScheduleTime?.ToString("HH:mm")).ToList()
                }).ToList();
            return Json(shows);
        }
    }
}
