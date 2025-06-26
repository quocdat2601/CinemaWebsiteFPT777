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

       public IActionResult Select(string date, string returnUrl)
       {
           // 1. Get all available screening dates from MovieShows
           var availableDates = _context.MovieShows
               .Select(ms => ms.ShowDate)
               .Distinct()
               .OrderBy(d => d)
               .ToList(); // List<DateOnly>

           if (!availableDates.Any())
           {
               var emptyModel = new ShowtimeSelectionViewModel
               {
                   AvailableDates = new List<DateOnly>(),
                   SelectedDate = DateOnly.FromDateTime(DateTime.Today),
                   Movies = new List<MovieShowtimeInfo>()
               };
               return View("~/Views/Showtime/Select.cshtml", emptyModel);
           }

           // Parse the date from dd/MM/yyyy format
           DateOnly selectedDateOnly;
           if (!string.IsNullOrEmpty(date))
           {
               try
               {
                   selectedDateOnly = DateOnly.ParseExact(date, "dd/MM/yyyy");
               }
               catch
               {
                   selectedDateOnly = DateOnly.FromDateTime(DateTime.Today);
               }
           }
           else
           {
               selectedDateOnly = DateOnly.FromDateTime(DateTime.Today);
           }

           if (string.IsNullOrEmpty(date))
            {
                selectedDateOnly = DateOnly.FromDateTime(DateTime.Today);
            }

           // 2. Get all MovieShow entries for the selected date
           var movieShowsForDate = _context.MovieShows
               .Where(ms => ms.ShowDate == selectedDateOnly)
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
                   Showtimes = g.Where(ms => ms.Schedule != null)
                                    .Select(ms => ms.Schedule.ScheduleTime.HasValue ? ms.Schedule.ScheduleTime.Value.ToString("HH:mm") : null)
                                    .Where(t => !string.IsNullOrEmpty(t))
                                    .OrderBy(t => t)
                                    .ToList() ?? new List<string>()
               })
               .Where(m => m.Showtimes.Any()) // Only include movies with showtimes
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
