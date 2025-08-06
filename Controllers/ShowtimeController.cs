using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MovieTheater.Repository;
using MovieTheater.ViewModels;

namespace MovieTheater.Controllers
{
    [Authorize(Roles = "Admin,Employee")]
    public class ShowtimeController : Controller
    {
        private readonly IMovieRepository _movieRepository;
        public ShowtimeController(IMovieRepository movieRepository)
        {
            _movieRepository = movieRepository;
        }

        /// <summary>
        /// Danh sách suất chiếu
        /// </summary>
        /// <remarks>url: /Showtime/List (GET)</remarks>
        public IActionResult List()
        {
            return View();
        }

        /// <summary>
        /// Xem chi tiết suất chiếu
        /// </summary>
        /// <remarks>url: /Showtime/Details (GET)</remarks>
        public ActionResult Details(int id)
        {
            return View();
        }

        /// <summary>
        /// Trang tạo suất chiếu mới
        /// </summary>
        /// <remarks>url: /Showtime/Create (GET)</remarks>
        public ActionResult Create()
        {
            return View();
        }

        /// <summary>
        /// Tạo suất chiếu mới
        /// </summary>
        /// <remarks>url: /Showtime/Create (POST)</remarks>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(IFormCollection collection)
        {
            if (!ModelState.IsValid)
            {
                return View();
            }
            try
            {
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View();
            }
        }

        /// <summary>
        /// Trang sửa suất chiếu
        /// </summary>
        /// <remarks>url: /Showtime/Edit (GET)</remarks>
        public ActionResult Edit(int id)
        {
            return View();
        }

        /// <summary>
        /// Sửa suất chiếu
        /// </summary>
        /// <remarks>url: /Showtime/Edit (POST)</remarks>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(int id, IFormCollection collection)
        {
            if (!ModelState.IsValid)
            {
                return View();
            }
            try
            {
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View();
            }
        }

        /// <summary>
        /// Trang xóa suất chiếu
        /// </summary>
        /// <remarks>url: /Showtime/Delete (GET)</remarks>
        public ActionResult Delete(int id)
        {
            return View();
        }

        /// <summary>
        /// Xóa suất chiếu
        /// </summary>
        /// <remarks>url: /Showtime/Delete (POST)</remarks>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(int id, IFormCollection collection)
        {
            if (!ModelState.IsValid)
            {
                return View();
            }
            try
            {
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View();
            }
        }

        /// <summary>
        /// Chọn suất chiếu theo ngày
        /// </summary>
        /// <remarks>url: /Showtime/Select (GET)</remarks>
        public IActionResult Select(string date, string returnUrl)
        {
            var today = DateOnly.FromDateTime(DateTime.Today);

            // Only dates today or in the future
            var availableDates = _movieRepository.GetMovieShow()
                .Where(ms => ms.ShowDate >= today)
                .Where(ms => ms.CinemaRoom.StatusId != 3 ||
                    (ms.CinemaRoom.UnavailableEndDate.HasValue &&
                     ms.ShowDate > DateOnly.FromDateTime(ms.CinemaRoom.UnavailableEndDate.Value)))
                .Select(ms => ms.ShowDate)
                .Distinct()
                .OrderBy(d => d)
                .ToList();

            // Parse the date from dd/MM/yyyy format
            DateOnly selectedDateOnly;
            if (!string.IsNullOrEmpty(date))
            {
                try
                {
                    selectedDateOnly = DateOnly.ParseExact(date, "dd/MM/yyyy");
                    // Ensure the selected date is today or in the future
                    if (selectedDateOnly < today)
                    {
                        selectedDateOnly = today;
                    }
                }
                catch
                {
                    selectedDateOnly = today;
                }
            }
            else
            {
                selectedDateOnly = today;
            }

            // Only get MovieShows for the selected date (which is always today or future)
            // Also filter by cinema room status to only show movies from active rooms
            var movieShowsForDate = _movieRepository.GetMovieShow()
                .Where(ms => ms.ShowDate == selectedDateOnly)
                .Where(ms => ms.CinemaRoom.StatusId != 3 ||
                    (ms.CinemaRoom.UnavailableEndDate.HasValue &&
                     ms.ShowDate > DateOnly.FromDateTime(ms.CinemaRoom.UnavailableEndDate.Value))) // Only include movies from active cinema rooms or after disable period
                .ToList();

            // 3. Group by movie and version, then build the view model
            var movies = movieShowsForDate
                .GroupBy(ms => ms.Movie) // Group by the related Movie entity
                .Where(g => g.Key != null) // Ensure the Movie is not null after Include
                .Select(g => new MovieShowtimeInfo
                {
                    MovieId = g.Key.MovieId,
                    MovieName = g.Key.MovieNameEnglish ?? "Unknown",
                    PosterUrl = g.Key.LargeImage ?? g.Key.SmallImage ?? "/images/default-movie.png",
                    VersionShowtimes = g.Where(ms => ms.Schedule != null && ms.Version != null)
                                        .GroupBy(ms => new { ms.VersionId, ms.Version.VersionName })
                                        .Select(versionGroup => new VersionShowtimeInfo
                                        {
                                            VersionId = versionGroup.Key.VersionId,
                                            VersionName = versionGroup.Key.VersionName,
                                            Showtimes = versionGroup
                                                .Select(ms => ms.Schedule.ScheduleTime.HasValue ? ms.Schedule.ScheduleTime.Value.ToString("HH:mm") : null)
                                                .Where(t => !string.IsNullOrEmpty(t))
                                                .OrderBy(t => t)
                                                .ToList()
                                        })
                                        .Where(v => v.Showtimes.Any())
                                        .OrderBy(v => v.VersionName)
                                        .ToList()
                })
                .Where(m => m.VersionShowtimes.Any()) // Only include movies with showtimes
                .ToList();

            var model = new ShowtimeSelectionViewModel
            {
                AvailableDates = availableDates,
                SelectedDate = selectedDateOnly,
                Movies = movies,
                ReturnUrl = returnUrl
            };

            return View("~/Views/Showtime/Select.cshtml", model);
        }
    }
}
