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
                    MovieId = m.MovieId,
                    MovieNameEnglish = m.MovieNameEnglish,
                    Duration = m.Duration,
                    SmallImage = m.SmallImage,
                    Types = m.Types.ToList()
                });
            
            return View(movies);
        }

        // GET: MovieController/Detail/5
        public ActionResult Detail(string id)
        {
            var movie = _movieService.GetById(id);
            var cinemaRoom = _cinemaService.GetById(movie.CinemaRoomId);

            var viewModel = new MovieDetailViewModel
            {
                MovieNameEnglish = movie.MovieNameEnglish,
                MovieNameVn = movie.MovieNameVn,
                FromDate = movie.FromDate,
                ToDate = movie.ToDate,
                Actor = movie.Actor,
                MovieProductionCompany = movie.MovieProductionCompany,
                Director = movie.Director,
                Duration = movie.Duration,
                Version = movie.Version,
                Content = movie.Content,
                LargeImage = movie.LargeImage, 
                CinemaRoomName = cinemaRoom?.CinemaRoomName, 
                AvailableTypes = movie.Types.ToList(),
                AvailableSchedules = movie.Schedules.ToList(),
            };

            return View(viewModel);
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

        // GET: Movie/Edit/5
        public async Task<IActionResult> Edit(string id)
        {
            var movie = _movieService.GetById(id);
            if (movie == null)
                return NotFound();

            var viewModel = new MovieDetailViewModel
            {
                MovieNameEnglish = movie.MovieNameEnglish,
                MovieNameVn = movie.MovieNameVn,
                FromDate = movie.FromDate,
                ToDate = movie.ToDate,
                Actor = movie.Actor,
                MovieProductionCompany = movie.MovieProductionCompany,
                Director = movie.Director,
                Duration = movie.Duration,
                Version = movie.Version,
                Content = movie.Content,
                SelectedTypeIds = movie.Types.Select(t => t.TypeId).ToList(),
                SelectedScheduleIds = movie.Schedules.Select(s => s.ScheduleId).ToList(),
                CinemaRoomId = movie.CinemaRoomId,
                LargeImage = movie.LargeImage,
                SmallImage = movie.SmallImage,

                AvailableSchedules = await _movieService.GetSchedulesAsync(),
                AvailableTypes = await _movieService.GetTypesAsync(),
                AvailableCinemaRooms = _cinemaService.GetAll().ToList()
            };
            return View(viewModel);
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
