using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using MovieTheater.Models;
using MovieTheater.ViewModels;
using Microsoft.EntityFrameworkCore;

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

            // 2. Get all movies scheduled for the selected date (active in date range)
            var moviesForDate = _context.Movies
                .Where(m => m.FromDate <= selectedDateOnly && m.ToDate >= selectedDateOnly)
                .Where(m => m.ShowDates.Any(sd => sd.ShowDate1 == selectedDateOnly))
                .Include(m => m.Schedules)
                .ToList();

            // 3. Build the view model
            var movies = moviesForDate.Select(m => new MovieShowtimeInfo
            {
                MovieId = m.MovieId,
                MovieName = m.MovieNameEnglish ?? m.MovieNameVn ?? "Unknown",
                PosterUrl = m.LargeImage ?? m.SmallImage ?? "/images/default-movie.png",
                Showtimes = m.Schedules.Select(s => s.ScheduleTime).Where(t => !string.IsNullOrEmpty(t)).ToList() ?? new List<string>()
            })
            .Where(m => m.Showtimes.Any())
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

        public IActionResult SelectEmployee(DateTime? date)
        {
            // Reuse the same logic as Select action but return the employee view
            var availableDates = _context.ShowDates
                .OrderBy(d => d.ShowDate1)
                .Select(d => d.ShowDate1)
                .ToList()
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
                return View("~/Views/Showtime/SelectEmployee.cshtml", emptyModel);
            }
            var selectedDate = date ?? availableDates.First();
            var selectedDateOnly = DateOnly.FromDateTime(selectedDate);

            var moviesForDate = _context.Movies
                .Where(m => m.FromDate <= selectedDateOnly && m.ToDate >= selectedDateOnly)
                .Where(m => m.ShowDates.Any(sd => sd.ShowDate1 == selectedDateOnly))
                .Include(m => m.Schedules)
                .ToList();

            var movies = moviesForDate.Select(m => new MovieShowtimeInfo
            {
                MovieId = m.MovieId,
                MovieName = m.MovieNameVn ?? m.MovieNameEnglish ?? "Unknown",
                PosterUrl = m.LargeImage ?? m.SmallImage ?? "/images/default-movie.png",
                Showtimes = m.Schedules.Select(s => s.ScheduleTime).Where(t => !string.IsNullOrEmpty(t)).ToList() ?? new List<string>()
            })
            .Where(m => m.Showtimes.Any())
            .ToList();

            var model = new ShowtimeSelectionViewModel
            {
                AvailableDates = availableDates,
                SelectedDate = selectedDate,
                Movies = movies
            };

            return View("~/Views/Showtime/SelectEmployee.cshtml", model);
        }

        // GET: Showtime/SelectSeat
        // Placeholder for seat selection screen (to be implemented)
        public IActionResult SelectSeat(string movieId, DateTime date, string time)
        {
            // TODO: Implement seat selection logic here
            // For now, just show a placeholder message with the parameters
            return Content($"Seat selection for MovieId={movieId}, Date={date:yyyy-MM-dd}, Time={time} (to be implemented)");
        }
    }
}
