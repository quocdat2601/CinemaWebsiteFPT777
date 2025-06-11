using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MovieTheater.Models;
using MovieTheater.Service;
using MovieTheater.ViewModels;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;

namespace MovieTheater.Controllers
{
    public class MovieController : Controller
    {
        private readonly IMovieService _movieService;
        private readonly ICinemaService _cinemaService;
        private readonly ILogger<MovieController> _logger;

        public MovieController(IMovieService movieService, ICinemaService cinemaService, ILogger<MovieController> logger)
        {
            _movieService = movieService;
            _cinemaService = cinemaService;
            _logger = logger;
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
                AvailableTypes = movie.Types.ToList()
            };

            return View(viewModel);
        }

        // GET: MovieController/Create
        [HttpGet]
        public IActionResult Create()
        {
            var model = new MovieDetailViewModel
            {
                AvailableTypes = _movieService.GetAllTypes()
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
                return View(model);
            }

            if (model.FromDate >= model.ToDate)
            {
                TempData["ErrorMessage"] = "Invalid date range. From date must be before To date.";
                model.AvailableTypes = _movieService.GetAllTypes();
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
                Content = model.Content,
                TrailerUrl = _movieService.ConvertToEmbedUrl(model.TrailerUrl),
                LargeImage = model.LargeImage,
                SmallImage = model.SmallImage,
                Types = _movieService.GetAllTypes().Where(t => model.SelectedTypeIds.Contains(t.TypeId)).ToList()
            };

            if (_movieService.AddMovie(movie))
            {
                TempData["ToastMessage"] = "Movie created successfully!";
                string role = GetUserRole();
                if (role == "Admin")
                    return RedirectToAction("MainPage", "Admin", new { tab = "MovieMg" });
                else
                    return RedirectToAction("MainPage", "Employee", new { tab = "MovieMg" });
            }

            TempData["ErrorMessage"] = "Failed to create movie.";
            model.AvailableTypes = _movieService.GetAllTypes();
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
                Content = movie.Content,
                TrailerUrl = movie.TrailerUrl,
                LargeImage = movie.LargeImage,
                SmallImage = movie.SmallImage,
                AvailableTypes = _movieService.GetAllTypes(),
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
                return View(model);
            }

            if (model.FromDate >= model.ToDate)
            {
                TempData["ErrorMessage"] = "Invalid date range. From date must be before To date.";
                model.AvailableTypes = _movieService.GetAllTypes();
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
                Content = model.Content,
                TrailerUrl = _movieService.ConvertToEmbedUrl(model.TrailerUrl),
                LargeImage = model.LargeImage,
                SmallImage = model.SmallImage,
                Types = _movieService.GetAllTypes().Where(t => model.SelectedTypeIds.Contains(t.TypeId)).ToList()
            };

            if (_movieService.UpdateMovie(movie))
            {
                TempData["ToastMessage"] = "Movie updated successfully!";
                string role = GetUserRole();
                if (role == "Admin")
                    return RedirectToAction("MainPage", "Admin", new { tab = "MovieMg" });
                else
                    return RedirectToAction("MainPage", "Employee", new { tab = "MovieMg" });
            }

            TempData["ErrorMessage"] = "Failed to update movie.";
            model.AvailableTypes = _movieService.GetAllTypes();
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
                    if (role == "Admin")
                        return RedirectToAction("MainPage", "Admin", new { tab = "MovieMg" });
                    else
                        return RedirectToAction("MainPage", "Employee", new { tab = "MovieMg" });
                }

                var movie = _movieService.GetById(id);
                if (movie == null)
                {
                    TempData["ToastMessage"] = "Movie not found.";
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
                    if (role == "Admin")
                        return RedirectToAction("MainPage", "Admin", new { tab = "MovieMg" });
                    else
                        return RedirectToAction("MainPage", "Employee", new { tab = "MovieMg" });
                }

                TempData["ToastMessage"] = "Movie deleted successfully!";
                if (role == "Admin")
                    return RedirectToAction("MainPage", "Admin", new { tab = "MovieMg" });
                else
                    return RedirectToAction("MainPage", "Employee", new { tab = "MovieMg" });
            }
            catch (Exception ex)
            {
                TempData["ToastMessage"] = $"An error occurred during deletion: {ex.Message}";
                if (role == "Admin")
                    return RedirectToAction("MainPage", "Admin", new { tab = "MovieMg" });
                else
                    return RedirectToAction("MainPage", "Employee", new { tab = "MovieMg" });
            }
        }

        [HttpGet]
        [Route("Movie/MovieShow/{id}")]
        public IActionResult MovieShow(string id)
        {
            var movie = _movieService.GetById(id);
            if (movie == null)
            {
                return NotFound();
            }

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                var movieShows = _movieService.GetMovieShows(id);
                var showDetails = movieShows.Select(ms => new
                {
                    ms.MovieShowId,
                    ms.MovieId,
                    ms.ShowDateId,
                    showDate = ms.ShowDate?.ShowDate1?.ToString("dd/MM/yyyy") ?? ms.ShowDate?.DateName,
                    ms.ScheduleId,
                    scheduleTime = ms.Schedule?.ScheduleTime,
                    ms.CinemaRoomId,
                    cinemaRoomName = ms.CinemaRoom?.CinemaRoomName
                }).ToList();

                return Json(showDetails);
            }

            var viewModel = new MovieDetailViewModel
            {
                MovieId = movie.MovieId,
                MovieNameEnglish = movie.MovieNameEnglish,
                AvailableCinemaRooms = _cinemaService.GetAll().ToList(),
                AvailableShowDates = _movieService.GetShowDates().ToList(),
                AvailableSchedules = _movieService.GetSchedules().ToList()
            };

            return View(viewModel);
        }

        [HttpPost]
        [Route("Movie/MovieShow/{id}")]
        [ValidateAntiForgeryToken]
        public IActionResult MovieShow(string id, MovieDetailViewModel model)
        {
            if (id != model.MovieId)
            {
                return BadRequest();
            }

            if (!ModelState.IsValid)
            {
                model.AvailableCinemaRooms = _cinemaService.GetAll().ToList();
                model.AvailableShowDates = _movieService.GetShowDates().ToList();
                model.AvailableSchedules = _movieService.GetSchedules().ToList();
                return View(model);
            }

            try
            {
                // The actual movie show creation is handled by the JavaScript AJAX calls
                TempData["SuccessMessage"] = "Movie shows updated successfully!";
                return View(model);
                //string role = GetUserRole();
                //if (role == "Admin")
                //    return RedirectToAction("MainPage", "Admin", new { tab = "ScheduleMg" });
                //else
                //    return RedirectToAction("MainPage", "Employee", new { tab = "ScheduleMg" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating movie shows for movie {MovieId}", id);
                TempData["ErrorMessage"] = "An error occurred while updating movie shows.";
                model.AvailableCinemaRooms = _cinemaService.GetAll().ToList();
                model.AvailableShowDates = _movieService.GetShowDates().ToList();
                model.AvailableSchedules = _movieService.GetSchedules().ToList();
                return View(model);
            }
        }

        [HttpPost]
        public IActionResult AddMovieShow([FromBody] MovieShowRequest request)
        {
            try
            {
                var movieShow = new MovieShow
                {
                    MovieId = request.MovieId,
                    ShowDateId = request.ShowDateId,
                    ScheduleId = request.ScheduleId,
                    CinemaRoomId = request.CinemaRoomId
                };

                _movieService.AddMovieShow(movieShow);
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding movie show");
                return BadRequest();
            }
        }

        [HttpGet]
        public IActionResult CheckScheduleAvailability(int showDateId, int scheduleId, int cinemaRoomId)
        {
            try
            {
                var isAvailable = _movieService.IsScheduleAvailable(showDateId, scheduleId, cinemaRoomId);
                return Json(new { isAvailable });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking schedule availability");
                return BadRequest();
            }
        }

        [HttpPost]
        public IActionResult DeleteAllMovieShows(string movieId)
        {
            try
            {
                var success = _movieService.DeleteAllMovieShows(movieId);
                if (success)
                {
                    return Ok();
                }
                return BadRequest("Failed to delete movie shows");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting movie shows for movie {MovieId}", movieId);
                return BadRequest();
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetAvailableSchedules(int showDateId, int cinemaRoomId)
        {
            try
            {
                var availableSchedules = await _movieService.GetAvailableSchedulesAsync(showDateId, cinemaRoomId);
                return Json(availableSchedules);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting available schedules");
                return BadRequest();
            }
        }
    }
}
