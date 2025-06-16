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

        // GET: ShowtimeController
        public IActionResult List()
        {
            return View();
        }

        // GET: ShowtimeController/Details/5
        public ActionResult Details(int id)
        {
            return View();
        }

        // GET: ShowtimeController/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: ShowtimeController/Create
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

        // GET: ShowtimeController/Edit/5
        public ActionResult Edit(int id)
        {
            return View();
        }

        // POST: ShowtimeController/Edit/5
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

        // GET: ShowtimeController/Delete/5
        public ActionResult Delete(int id)
        {
            return View();
        }

        // POST: ShowtimeController/Delete/5
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

        public IActionResult Select(DateTime? date, string returnUrl)
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
                    SelectedDate = date ?? DateTime.Today,
                    Movies = new List<MovieShowtimeInfo>()
                };
                return View("~/Views/Showtime/Select.cshtml", emptyModel);
            }
            var selectedDate = date ?? availableDates.First();
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