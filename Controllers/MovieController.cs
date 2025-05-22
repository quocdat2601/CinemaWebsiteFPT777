using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MovieTheater.Service;
using MovieTheater.Services;
using MovieTheater.ViewModels;

namespace MovieTheater.Controllers
{
    public class MovieController : Controller
    {
        private readonly IMovieService _movieService;
        private readonly ICinemaService _cinemaService;

        public MovieController(IMovieService movieService, ICinemaService cinemaService)
        {
            _movieService = movieService;
            _cinemaService = cinemaService;
        }

        // GET: MovieController
        public IActionResult MovieList()
        {
            var movies = _movieService.GetAll()
                .Select(m => new MovieViewModel
                {
                    MovieNameEnglish = m.MovieNameEnglish,
                    Duration = m.Duration,
                    SmallImage = m.SmallImage,
                    Types = m.Types.Select(t => new TypeViewModel 
                    { 
                        TypeId = t.TypeId,
                        TypeName = t.TypeName
                    }).ToList()
                });
            
            return View(movies);
        }

        // GET: MovieController/Details/5
        public ActionResult Details(int id)
        {
            return View();
        }

        // GET: MovieController/Create
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var vm = new MovieDetailViewModel
            {
                AvailableSchedules = await _movieService.GetSchedulesAsync(),
                AvailableShowDates = await _movieService.GetShowDatesAsync(),
                AvailableTypes = await _movieService.GetTypesAsync(),
                AvailableCinemaRooms = _cinemaService.GetAll().ToList()
            };
            return View(vm);
        }

        // POST: MovieController/Create
        [HttpPost]
        public async Task<IActionResult> Create(MovieDetailViewModel model)
        {
            if (!ModelState.IsValid)
            {
                model.AvailableSchedules = await _movieService.GetSchedulesAsync();
                model.AvailableShowDates = await _movieService.GetShowDatesAsync();
                model.AvailableTypes = await _movieService.GetTypesAsync();
                return View(model);
            }

            bool success = _movieService.AddMovie(model);

            if (!success)
            {
                ModelState.AddModelError("", "Failed to create movie.");
                return View(model);
            }

            TempData["ToastMessage"] = "Movie created successfully!";
            return RedirectToAction("Index");
        }



        // GET: MovieController/Edit/5
        public ActionResult Edit(int id)
        {
            return View();
        }

        // POST: MovieController/Edit/5
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

        // GET: MovieController/Delete/5
        public ActionResult Delete(int id)
        {
            return View();
        }

        // POST: MovieController/Delete/5
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
    }
}
