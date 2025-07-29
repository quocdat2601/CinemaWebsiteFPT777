using Microsoft.AspNetCore.Mvc;
using MovieTheater.Models;
using MovieTheater.Service;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using AspNetCoreGeneratedDocument;

namespace MovieTheater.Controllers
{
    public class CinemaController : Controller
    {
        private readonly ICinemaService _cinemaService;
        private readonly IMovieService _movieService;
        private readonly ITicketService _ticketService;
        public CinemaController(ICinemaService cinemaService, IMovieService movieService, ITicketService ticketService)
        {
            _cinemaService = cinemaService;
            _movieService = movieService;
            _ticketService = ticketService;
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
            try
            {
                if (!ModelState.IsValid)
                {
                    var versions = _movieService.GetAllVersions();
                    ViewBag.Versions = versions;
                    ViewBag.CurrentVersionId = VersionId;
                    
                    // Add specific validation errors to help debug
                    foreach (var modelState in ModelState.Values)
                    {
                        foreach (var error in modelState.Errors)
                        {
                            ModelState.AddModelError("", error.ErrorMessage);
                        }
                    }
                    
                    return View(cinemaRoom);
                }
                
                cinemaRoom.VersionId = VersionId;
                bool success = _cinemaService.Update(cinemaRoom);
                
                if (!success)
                {
                    TempData["ErrorMessage"] = "Failed to update showroom. Please check your input and try again.";
                    var versions = _movieService.GetAllVersions();
                    ViewBag.Versions = versions;
                    ViewBag.CurrentVersionId = VersionId;
                    return View(cinemaRoom);
                }
                
                TempData["ToastMessage"] = "Showroom updated successfully!";
                return RedirectToAction("MainPage", "Admin", new { tab = "ShowroomMg" });
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"An error occurred while updating the showroom: {ex.Message}";
                var versions = _movieService.GetAllVersions();
                ViewBag.Versions = versions;
                ViewBag.CurrentVersionId = VersionId;
                return View(cinemaRoom);
            }
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
        [Route("Cinema/Disable")]

        public IActionResult Disable(int id)
        {
            var cinemaRoom = _cinemaService.GetById(id); 
            if (cinemaRoom == null)
            {
                return NotFound(); 
            }
            return View(cinemaRoom);
        }

        [HttpPost]
        [Route("Cinema/Disable")]
        [ValidateAntiForgeryToken]
        public IActionResult Disable(CinemaRoom cinemaRoom)
        {
            try
            {
                // Just disable the room, don't delete anything
                bool success = _cinemaService.Disable(cinemaRoom);
                _cinemaService.SaveAsync();
                if (!success)
                {
                    TempData["ErrorMessage"] = "Failed to update showroom status.";
                    return RedirectToAction("MainPage", "Admin", new { tab = "ShowroomMg" });
                }
                TempData["ToastMessage"] = "Showroom disabled successfully!";
                return RedirectToAction("MainPage", "Admin", new { tab = "ShowroomMg" });
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"An error occurred while disabling the showroom: {ex.Message}";
                return RedirectToAction("MainPage", "Admin", new { tab = "ShowroomMg" });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Enable(CinemaRoom cinemaRoom)
        {
            try
            {
                bool success = _cinemaService.Enable(cinemaRoom);
                _cinemaService.SaveAsync();
                if (!success)
                {
                    TempData["ErrorMessage"] = "Failed to update showroom status.";
                    return RedirectToAction("MainPage", "Admin", new { tab = "ShowroomMg" });
                }
                TempData["ToastMessage"] = "Showroom enabled successfully!";
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

        [HttpGet]
        public IActionResult GetDetailedMovieShowsByCinemaRoom(int cinemaRoomId)
        {
            var shows = _movieService.GetMovieShow()
                .Where(ms => ms.CinemaRoomId == cinemaRoomId)
                .Select(ms => new {
                    movieShowId = ms.MovieShowId,
                    movieId = ms.MovieId,
                    movieName = ms.Movie?.MovieNameEnglish ?? "Unknown Movie",
                    duration = ms.Movie?.Duration ?? 0,
                    showDate = ms.ShowDate.ToString("dd/MM/yyyy"),
                    scheduleTime = ms.Schedule?.ScheduleTime?.ToString("HH:mm") ?? "N/A",
                    versionName = ms.Version?.VersionName ?? "N/A",
                    startTime = ms.Schedule?.ScheduleTime,
                    endTime = ms.Schedule?.ScheduleTime?.AddMinutes(ms.Movie?.Duration ?? 0),
                    bookingCount = ms.Invoices.Count(i => !i.Cancel)
                })
                .OrderBy(s => s.showDate)
                .ThenBy(s => s.scheduleTime)
                .ToList();
            
            return Json(shows);
        }

        [HttpGet]
        public IActionResult GetInvoicesByMovieShow(int movieShowId)
        {
            var invoices = _movieService.GetInvoicesByMovieShow(movieShowId)
                .Select(i => new {
                    invoiceId = i.InvoiceId,
                    accountId = i.AccountId,
                    accountName = i.Account != null ? i.Account.FullName : null,
                    seat = i.Seat
                }).ToList();
            return Json(invoices);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> RefundByMovieShow(int movieShowId)
        {
            // Get all non-cancelled invoices for this show
            var invoices = _movieService.GetInvoicesByMovieShow(movieShowId)
                .Select(i => i.InvoiceId)
                .ToList();

            var results = new List<object>();
            foreach (var id in invoices)
            {
                var (success, messages) = await _ticketService.CancelTicketByAdminAsync(id);
                results.Add(new { invoiceId = id, success, messages });
            }
            return Json(new { success = true, results });
        }
    }
}
