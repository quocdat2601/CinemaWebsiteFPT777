using AspNetCoreGeneratedDocument;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MovieTheater.Models;
using MovieTheater.Service;
using System.Data;
using System.Security.Claims;

namespace MovieTheater.Controllers
{
    [Authorize(Roles = "Admin,Employee")]
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
        public string role => User?.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value;

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
            if (role == "Admin")
                return RedirectToAction("MainPage", "Admin", new { tab = "ShowroomMg" });
            else
                return RedirectToAction("MainPage", "Employee", new { tab = "ShowroomMg" });
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
                if (role == "Admin")
                    return RedirectToAction("MainPage", "Admin", new { tab = "ShowroomMg" });
                else
                    return RedirectToAction("MainPage", "Employee", new { tab = "ShowroomMg" });
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
                    if (role == "Admin")
                        return RedirectToAction("MainPage", "Admin", new { tab = "ShowroomMg" });
                    else
                        return RedirectToAction("MainPage", "Employee", new { tab = "ShowroomMg" });
                }

                bool success = await _cinemaService.DeleteAsync(id);

                if (!success)
                {
                    TempData["ErrorMessage"] = "Failed to delete showroom.";
                    if (role == "Admin")
                        return RedirectToAction("MainPage", "Admin", new { tab = "ShowroomMg" });
                    else
                        return RedirectToAction("MainPage", "Employee", new { tab = "ShowroomMg" });
                }

                TempData["ToastMessage"] = "Showroom deleted successfully!";
                if (role == "Admin")
                    return RedirectToAction("MainPage", "Admin", new { tab = "ShowroomMg" });
                else
                    return RedirectToAction("MainPage", "Employee", new { tab = "ShowroomMg" });
            }
            catch (Exception ex)
            {
                TempData["ToastMessage"] = $"An error occurred during deletion: {ex.Message}";
                if (role == "Admin")
                    return RedirectToAction("MainPage", "Admin", new { tab = "ShowroomMg" });
                else
                    return RedirectToAction("MainPage", "Employee", new { tab = "ShowroomMg" });
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
        public async Task<IActionResult> Disable(CinemaRoom cinemaRoom, string removedShowIds)
        {
            try
            {
                // 1. Get all conflicted shows that will be affected by the disable period
                var conflictedShows = GetConflictedShows(cinemaRoom);
                
                // 2. Automatically refund any invoices in conflicted shows
                var refundedShows = new List<string>();
                foreach (var show in conflictedShows)
                {
                    var invoices = _movieService.GetInvoicesByMovieShow(show.MovieShowId)
                        .Where(i => !i.Cancel) // Only refund active invoices
                        .ToList();
                    
                    if (invoices.Any())
                    {
                        foreach (var invoice in invoices)
                        {
                            var (refundSuccess, refundMessages) = await _ticketService.CancelTicketByAdminAsync(invoice.InvoiceId, role);
                            if (refundSuccess)
                            {
                                refundedShows.Add($"Show {show.MovieShowId} - {invoice.InvoiceId}");
                            }
                        }
                    }
                }

                // 3. Delete removed shows with no bookings
                var deletedShows = new List<int>();
                var undeletableShows = new List<int>();
                if (!string.IsNullOrEmpty(removedShowIds))
                {
                    var ids = removedShowIds.Split(',').Where(s => !string.IsNullOrWhiteSpace(s)).Select(int.Parse).ToList();
                    foreach (var id in ids)
                    {
                        var show = _movieService.GetMovieShowById(id);
                        if (show != null && (show.Invoices == null || !show.Invoices.Any(i => !i.Cancel)))
                        {
                            _movieService.DeleteMovieShows(id);
                            deletedShows.Add(id);
                        }
                        else
                        {
                            undeletableShows.Add(id);
                        }
                    }
                }
                
                // 4. Disable the room
                bool disableSuccess = await _cinemaService.Disable(cinemaRoom);
                if (!disableSuccess)
                {
                    TempData["ErrorMessage"] = "Failed to update showroom status.";
                    if (role == "Admin")
                        return RedirectToAction("MainPage", "Admin", new { tab = "ShowroomMg" });
                    else
                        return RedirectToAction("MainPage", "Employee", new { tab = "ShowroomMg" });
                }
                
                // 5. Feedback
                var feedbackMessages = new List<string>();
                if (refundedShows.Count > 0)
                {
                    feedbackMessages.Add($"Refunded {refundedShows.Count} ticket(s) from conflicted shows.");
                }
                if (undeletableShows.Count > 0)
                {
                    feedbackMessages.Add($"Some shows could not be deleted because they have bookings.");
                }
                if (deletedShows.Count > 0)
                {
                    feedbackMessages.Add($"Deleted {deletedShows.Count} show(s) with no bookings.");
                }
                
                if (feedbackMessages.Any())
                {
                    TempData["ToastMessage"] = string.Join(" ", feedbackMessages);
                }
                else
                {
                    TempData["ToastMessage"] = "Showroom disabled successfully!";
                }

                if (role == "Admin")
                    return RedirectToAction("MainPage", "Admin", new { tab = "ShowroomMg" });
                else
                    return RedirectToAction("MainPage", "Employee", new { tab = "ShowroomMg" });
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"An error occurred while disabling the showroom: {ex.Message}";
                if (role == "Admin")
                    return RedirectToAction("MainPage", "Admin", new { tab = "ShowroomMg" });
                else
                    return RedirectToAction("MainPage", "Employee", new { tab = "ShowroomMg" });
            }
        }

        private List<MovieShow> GetConflictedShows(CinemaRoom cinemaRoom)
        {
            if (!cinemaRoom.UnavailableStartDate.HasValue || !cinemaRoom.UnavailableEndDate.HasValue)
                return new List<MovieShow>();

            var startDate = cinemaRoom.UnavailableStartDate.Value;
            var endDate = cinemaRoom.UnavailableEndDate.Value;
            
            var allShows = _movieService.GetMovieShow()
                .Where(ms => ms.CinemaRoomId == cinemaRoom.CinemaRoomId)
                .ToList();

            var conflictedShows = new List<MovieShow>();
            
            foreach (var show in allShows)
            {
                // Convert DateOnly to DateTime for comparison
                var showDate = show.ShowDate.ToDateTime(TimeOnly.MinValue);
                
                // Check if show date falls within unavailable period
                if (showDate.Date >= startDate.Date && showDate.Date <= endDate.Date)
                {
                    // Check if show time overlaps with unavailable period
                    if (show.Schedule?.ScheduleTime.HasValue == true)
                    {
                        var showStartTime = showDate.Date.Add(show.Schedule.ScheduleTime.Value.ToTimeSpan());
                        var showEndTime = showStartTime.AddMinutes(show.Movie?.Duration ?? 0);
                        
                        if (showStartTime < endDate && showEndTime > startDate)
                        {
                            conflictedShows.Add(show);
                        }
                    }
                }
            }
            
            return conflictedShows;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Enable(CinemaRoom cinemaRoom)
        {
            try
            {
                bool success = await _cinemaService.Enable(cinemaRoom);
                if (!success)
                {
                    TempData["ErrorMessage"] = "Failed to update showroom status.";
                    if (role == "Admin")
                        return RedirectToAction("MainPage", "Admin", new { tab = "ShowroomMg" });
                    else
                        return RedirectToAction("MainPage", "Employee", new { tab = "ShowroomMg" });
                }
                TempData["ToastMessage"] = "Showroom enabled successfully!";
                if (role == "Admin")
                    return RedirectToAction("MainPage", "Admin", new { tab = "ShowroomMg" });
                else
                    return RedirectToAction("MainPage", "Employee", new { tab = "ShowroomMg" });
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"An error occurred while activating the showroom: {ex.Message}";
                if (role == "Admin")
                    return RedirectToAction("MainPage", "Admin", new { tab = "ShowroomMg" });
                else
                    return RedirectToAction("MainPage", "Employee", new { tab = "ShowroomMg" });
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
            var today = DateOnly.FromDateTime(DateTime.Today);
            
            var shows = _movieService.GetMovieShow()
                .Where(ms => ms.CinemaRoomId == cinemaRoomId && ms.ShowDate >= today)
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
            var today = DateOnly.FromDateTime(DateTime.Today);
            
            var shows = _movieService.GetMovieShow()
                .Where(ms => ms.CinemaRoomId == cinemaRoomId && ms.ShowDate >= today)
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
                    bookingCount = ms.Invoices.Count(i => i.Status == InvoiceStatus.Completed && i.Cancel == false)
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
                    seat = i.Seat,
                    status = i.Cancel ? "Cancelled" : "Active",
                    totalMoney = i.TotalMoney
                }).ToList();
            return Json(invoices);
        }

        [HttpPost]
        [Authorize(Roles = "Admin, Employee")]
        public async Task<IActionResult> RefundByMovieShow(int movieShowId)
        {
            // Get all non-cancelled invoices for this show
            var invoices = _movieService.GetInvoicesByMovieShow(movieShowId)
                .Where(i => !i.Cancel) // Only not-cancelled invoices
                .Select(i => i.InvoiceId)
                .ToList();

            var results = new List<object>();
            foreach (var id in invoices)
            {
                var (success, messages) = await _ticketService.CancelTicketByAdminAsync(id, role);
                results.Add(new { invoiceId = id, success, messages });
            }
            return Json(new { success = true, results });
        }
    }
}
