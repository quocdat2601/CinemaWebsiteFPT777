using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MovieTheater.Models;
using MovieTheater.Service;
using MovieTheater.ViewModels;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

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
        private string GetUserRole()
        {
            return User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value;
        }

        // GET: MovieController
        public IActionResult MovieList(string searchTerm)
        {
            var movies = _movieService.SearchMovies(searchTerm)
                .Select(m => new MovieViewModel
                {
                    MovieId = m.MovieId,
                    MovieNameEnglish = m.MovieNameEnglish,
                    Duration = m.Duration,
                    SmallImage = m.SmallImage,
                    Types = m.Types.ToList()
                })
                .ToList();

            ViewBag.SearchTerm = searchTerm;

            // Kiểm tra nếu là Ajax thì chỉ render partial
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return PartialView("_MovieGrid", movies);
            }

            return View(movies);
        }

        // GET: MovieController/Detail/5
        public ActionResult Detail(string id)
        {
            var movie = _movieService.GetById(id);
            var cinemaRoom = _cinemaService.GetById(movie.CinemaRoomId);

            var viewModel = new MovieDetailViewModel
            {
                MovieId = movie.MovieId,
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
                TrailerUrl = _movieService.ConvertToEmbedUrl(movie.TrailerUrl),
                LargeImage = movie.LargeImage,
                CinemaRoomName = cinemaRoom?.CinemaRoomName,
                AvailableTypes = movie.Types.ToList(),
                AvailableSchedules = movie.MovieShows.Select(ms => ms.Schedule).Where(s => s != null).ToList(),
                AvailableShowDates = _movieService.GetMovieShows(id).Select(ms => ms.ShowDate).Where(sd => sd != null).ToList()
            }; 

            return View(viewModel);
        }


        // GET: MovieController/Create
        [HttpGet]
        public IActionResult Create()
        {
            var model = new MovieDetailViewModel
            {
                AvailableTypes = _movieService.GetAllTypes(),
                AvailableCinemaRooms = _movieService.GetAllCinemaRooms(),
                AvailableShowDates = _movieService.GetAllShowDates(),
                AvailableSchedules = _movieService.GetAllSchedules()
            };
            return View(model);
        }

        // POST: MovieController/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(MovieDetailViewModel model)
        {
            if (!ModelState.IsValid)
            {
                model.AvailableTypes = _movieService.GetAllTypes();
                model.AvailableCinemaRooms = _movieService.GetAllCinemaRooms();
                model.AvailableShowDates = _movieService.GetAllShowDates();
                model.AvailableSchedules = _movieService.GetAllSchedules();
                return View(model);
            }

            if (model.FromDate >= model.ToDate)
            {
                TempData["ErrorMessage"] = "Invalid date range. From date must be before To date.";
                model.AvailableTypes = _movieService.GetAllTypes();
                model.AvailableCinemaRooms = _movieService.GetAllCinemaRooms();
                model.AvailableShowDates = _movieService.GetAllShowDates();
                model.AvailableSchedules = _movieService.GetAllSchedules();
                return View(model);
            }

            var movie = new Movie
            {
                MovieId = model.MovieId,
                MovieNameEnglish = model.MovieNameEnglish,
                MovieNameVn = model.MovieNameVn,
                Actor = model.Actor,
                Director = model.Director,
                Duration = model.Duration,
                Version = model.Version,
                FromDate = model.FromDate,
                ToDate = model.ToDate,
                MovieProductionCompany = model.MovieProductionCompany,
                CinemaRoomId = model.CinemaRoomId,
                Content = model.Content,
                TrailerUrl = _movieService.ConvertToEmbedUrl(model.TrailerUrl),
                LargeImage = model.LargeImage,
                SmallImage = model.SmallImage,
                Types = _movieService.GetAllTypes().Where(t => model.SelectedTypeIds.Contains(t.TypeId)).ToList()
            };

            if (_movieService.AddMovie(movie, model.SelectedShowDateIds, model.SelectedScheduleIds))
            {
                TempData["ToastMessage"] = "Movie created successfully!";
                //return RedirectToAction("MainPage", "Admin", new { tab = "MovieMg" });
                string role = GetUserRole();
                if (role == "Admin")
                    return RedirectToAction("MainPage", "Admin", new { tab = "MovieMg" });
                else
                    return RedirectToAction("MainPage", "Employee", new { tab = "MovieMg" });

            }

            TempData["ErrorMessage"] = "Failed to create movie. Some schedules may be unavailable.";
            model.AvailableTypes = _movieService.GetAllTypes();
            model.AvailableCinemaRooms = _movieService.GetAllCinemaRooms();
            model.AvailableShowDates = _movieService.GetAllShowDates();
            model.AvailableSchedules = _movieService.GetAllSchedules();
            return View(model);
        }

        // GET: Movie/Edit/5
        [HttpGet]
        [Route("Movie/Edit/{id}")]
        public IActionResult Edit(string id)
        {
            var movie = _movieService.GetById(id);
            if (movie == null)
            {
                return NotFound();
            }

            var model = new MovieDetailViewModel
            {
                MovieId = movie.MovieId,
                MovieNameEnglish = movie.MovieNameEnglish,
                MovieNameVn = movie.MovieNameVn,
                Actor = movie.Actor,
                Director = movie.Director,
                Duration = movie.Duration,
                Version = movie.Version,
                FromDate = movie.FromDate,
                ToDate = movie.ToDate,
                MovieProductionCompany = movie.MovieProductionCompany,
                CinemaRoomId = movie.CinemaRoomId,
                Content = movie.Content,
                TrailerUrl = movie.TrailerUrl,
                LargeImage = movie.LargeImage,
                SmallImage = movie.SmallImage,
                AvailableTypes = _movieService.GetAllTypes(),
                AvailableCinemaRooms = _movieService.GetAllCinemaRooms(),
                AvailableShowDates = _movieService.GetAllShowDates(),
                AvailableSchedules = _movieService.GetAllSchedules(),
                SelectedShowDateIds = _movieService.GetMovieShows(id).Select(ms => ms.ShowDateId ?? 0).Where(id => id != 0).ToList(),
                SelectedScheduleIds = _movieService.GetMovieShows(id).Select(ms => ms.ScheduleId ?? 0).Where(id => id != 0).ToList(),
                SelectedTypeIds = movie.Types.Select(t => t.TypeId).ToList()
            };

            return View(model);
        }

        // POST: MovieController/Edit/5
        [HttpPost]
        [Route("Movie/Edit/{id}")]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(string id, MovieDetailViewModel model)
        {
            if (id != model.MovieId)
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                model.AvailableTypes = _movieService.GetAllTypes();
                model.AvailableCinemaRooms = _movieService.GetAllCinemaRooms();
                model.AvailableShowDates = _movieService.GetAllShowDates();
                model.AvailableSchedules = _movieService.GetAllSchedules();
                return View(model);
            }

            if (model.FromDate >= model.ToDate)
            {
                TempData["ErrorMessage"] = "Invalid date range. From date must be before To date.";
                model.AvailableTypes = _movieService.GetAllTypes();
                model.AvailableCinemaRooms = _movieService.GetAllCinemaRooms();
                model.AvailableShowDates = _movieService.GetAllShowDates();
                model.AvailableSchedules = _movieService.GetAllSchedules();
                return View(model);
            }

            var movie = new Movie
            {
                MovieId = model.MovieId,
                MovieNameEnglish = model.MovieNameEnglish,
                MovieNameVn = model.MovieNameVn,
                Actor = model.Actor,
                Director = model.Director,
                Duration = model.Duration,
                Version = model.Version,
                FromDate = model.FromDate,
                ToDate = model.ToDate,
                MovieProductionCompany = model.MovieProductionCompany,
                CinemaRoomId = model.CinemaRoomId,
                Content = model.Content,
                TrailerUrl = _movieService.ConvertToEmbedUrl(model.TrailerUrl),
                LargeImage = model.LargeImage,
                SmallImage = model.SmallImage,
                Types = _movieService.GetAllTypes().Where(t => model.SelectedTypeIds.Contains(t.TypeId)).ToList()
            };

            if (_movieService.UpdateMovie(movie, model.SelectedShowDateIds, new List<int>() /* TODO: Provide actual scheduleIds */))
            {
                TempData["ToastMessage"] = "Movie updated successfully!";
            string role = GetUserRole();
            if (role == "Admin")
                return RedirectToAction("MainPage", "Admin", new { tab = "MovieMg" });
            else
                return RedirectToAction("MainPage", "Employee", new { tab = "MovieMg" });
            }

            TempData["ErrorMessage"] = "Failed to update movie. Some schedules may be unavailable.";
            model.AvailableTypes = _movieService.GetAllTypes();
            model.AvailableCinemaRooms = _movieService.GetAllCinemaRooms();
            model.AvailableShowDates = _movieService.GetAllShowDates();
            model.AvailableSchedules = _movieService.GetAllSchedules();
            return View(model);
        }
                
        // POST: Movie/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Delete(string id, IFormCollection collection)
        {
            string role = GetUserRole();
            try
            {
                if (string.IsNullOrEmpty(id))
                {
                    TempData["ToastMessage"] = "Invalid movie ID.";
                    //return RedirectToAction("MainPage", "Admin", new { tab = "MovieMg" });
                    if (role == "Admin")
                        return RedirectToAction("MainPage", "Admin", new { tab = "MovieMg" });
                    else
                        return RedirectToAction("MainPage", "Employee", new { tab = "MovieMg" });
                }

                var movie = _movieService.GetById(id);
                if (movie == null)
                {
                    TempData["ToastMessage"] = "Movie not found.";
                    //return RedirectToAction("MainPage", "Admin", new { tab = "MovieMg" });
                    if (role == "Admin")
                        return RedirectToAction("MainPage", "Admin", new { tab = "MovieMg" });
                    else
                        return RedirectToAction("MainPage", "Employee", new { tab = "MovieMg" });
                }

                movie.Types?.Clear();
                
                bool success = _movieService.DeleteMovie(id);

                if (!success)
                {
                    TempData["ToastMessage"] = "Failed to delete movie.";
                    //return RedirectToAction("MainPage", "Admin", new { tab = "MovieMg" });
                    if (role == "Admin")
                        return RedirectToAction("MainPage", "Admin", new { tab = "MovieMg" });
                    else
                        return RedirectToAction("MainPage", "Employee", new { tab = "MovieMg" });
                }

                TempData["ToastMessage"] = "Movie deleted successfully!";
                //return RedirectToAction("MainPage", "Admin", new { tab = "MovieMg" });
                if (role == "Admin")
                    return RedirectToAction("MainPage", "Admin", new { tab = "MovieMg" });
                else
                    return RedirectToAction("MainPage", "Employee", new { tab = "MovieMg" });
            }
            catch (Exception ex)
            {
                TempData["ToastMessage"] = $"An error occurred during deletion: {ex.Message}";
                //return RedirectToAction("MainPage", "Admin", new { tab = "MovieMg" });
                if (role == "Admin")
                    return RedirectToAction("MainPage", "Admin", new { tab = "MovieMg" });
                else
                    return RedirectToAction("MainPage", "Employee", new { tab = "MovieMg" });
            }
        }

    }
}
