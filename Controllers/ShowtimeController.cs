using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MovieTheater.Models;
using MovieTheater.ViewModels;

namespace MovieTheater.Controllers
{
    public class ShowtimeController : Controller
    {
        private readonly MovieTheaterContext _context;
        public ShowtimeController(MovieTheaterContext context)
        {
            _context = context;
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
            // 1. Get all available screening dates as DateTime
            var availableDates = _context.ShowDates
                .OrderBy(d => d.ShowDate1)
                .Select(d => d.ShowDate1)
                .ToList() // materialize as List<DateOnly?>
                .Where(d => d.HasValue)
                .Select(d => d.Value.ToDateTime(TimeOnly.MinValue))
                .ToList();

            if (!availableDates.Any())
            {
                var emptyModel = new ShowtimeSelectionViewModel
                {
                    AvailableDates = new List<DateTime>(),
                    SelectedDate = DateTime.Today,
                    Movies = new List<MovieShowtimeInfo>()
                };
                return View("~/Views/Showtime/Select.cshtml", emptyModel);
            }

            // Parse the date from dd/MM/yyyy format
            DateTime selectedDate;
            if (!string.IsNullOrEmpty(date))
            {
                if (!DateTime.TryParseExact(date, "dd/MM/yyyy", null, System.Globalization.DateTimeStyles.None, out selectedDate))
                {
                    // If parsing fails, use the first available date
                    selectedDate = availableDates.First();
                }
            }
            else
            {
                // If no date provided, use the first available date
                selectedDate = availableDates.First();
            }

            // Ensure the selected date is one of the available dates
            if (!availableDates.Any(d => d.Date == selectedDate.Date))
            {
                selectedDate = availableDates.First();
            }

            var selectedDateOnly = DateOnly.FromDateTime(selectedDate);

            // Find the ShowDateId for the selected date
            var selectedShowDate = _context.ShowDates.FirstOrDefault(sd => sd.ShowDate1 == selectedDateOnly);

            if (selectedShowDate == null)
            {
                // Handle case where no show date found
                var emptyModel = new ShowtimeSelectionViewModel
                {
                    AvailableDates = availableDates,
                    SelectedDate = selectedDate,
                    Movies = new List<MovieShowtimeInfo>()
                };
                return View("~/Views/Showtime/Select.cshtml", emptyModel);
            }

            // 2. Get all MovieShow entries for the selected date
            var movieShowsForDate = _context.MovieShows
                .Where(ms => ms.ShowDateId == selectedShowDate.ShowDateId)
                .Include(ms => ms.Movie)
                .Include(ms => ms.Schedule)
                .ToList();

            // 3. Group by movie and build the view model
            var movies = movieShowsForDate
                .GroupBy(ms => ms.Movie) // Group by the related Movie entity
                .Where(g => g.Key != null) // Ensure the Movie is not null after Include
                .Select(g => new MovieShowtimeInfo
                {
                    MovieId = g.Key.MovieId,
                    MovieName = g.Key.MovieNameEnglish ?? g.Key.MovieNameVn ?? "Unknown",
                    PosterUrl = g.Key.LargeImage ?? g.Key.SmallImage ?? "/images/default-movie.png",
                    Showtimes = g.Where(ms => ms.Schedule != null) // Filter out entries with null Schedule
                                     .Select(ms => ms.Schedule.ScheduleTime)
                                     .Where(t => !string.IsNullOrEmpty(t))
                                     .OrderBy(t => t) // Optional: Order showtimes
                                     .ToList() ?? new List<string>()
                })
                .Where(m => m.Showtimes.Any()) // Only include movies with showtimes
                .ToList();

            var model = new ShowtimeSelectionViewModel
            {
                AvailableDates = availableDates,
                SelectedDate = selectedDate,
                Movies = movies,
                ReturnUrl = returnUrl
            };

            return View("~/Views/Showtime/Select.cshtml", model);
        }
    }
}