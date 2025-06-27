using Microsoft.AspNetCore.Mvc;
using MovieTheater.Models;
using MovieTheater.Service;
using MovieTheater.ViewModels;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using MovieTheater.Repository;

namespace MovieTheater.Controllers
{
    public class MovieController : Controller
    {
        private readonly IMovieService _movieService;
        private readonly ICinemaService _cinemaService;
        private readonly ILogger<MovieController> _logger;
        private readonly IScheduleRepository _scheduleRepository;

        public MovieController(IMovieService movieService, ICinemaService cinemaService, ILogger<MovieController> logger, IScheduleRepository scheduleRepository)
        {
            _movieService = movieService;
            _cinemaService = cinemaService;
            _logger = logger;
            _scheduleRepository = scheduleRepository;
        }

        /// <summary>
        /// Lấy role người dùng hiện tại từ JWT Claims.
        /// </summary>
        private string GetUserRole()
        {
            return User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value;
        }

        /// <summary>
        /// [GET] api/movie/movielist
        /// Tìm kiếm và hiển thị danh sách phim. Nếu là Ajax request thì trả về partial view.
        /// </summary>
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

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return PartialView("_MovieGrid", movies);
            }

            return View(movies);
        }

        /// <summary>
        /// [GET] api/movie/detail/{id}
        /// Hiển thị thông tin chi tiết của một bộ phim.
        /// </summary>
        public ActionResult Detail(string id)
        {
            var movie = _movieService.GetById(id);
            if (movie == null)
            {
                return NotFound();
            }

            CinemaRoom cinemaRoom = null;
            if (movie.CinemaRoomId != null)
            {
                cinemaRoom = _cinemaService.GetById(movie.CinemaRoomId);
            }

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
                Content = movie.Content,
                TrailerUrl = _movieService.ConvertToEmbedUrl(movie.TrailerUrl),
                LargeImage = movie.LargeImage,
                AvailableTypes = movie.Types.ToList(),
                AvailableVersions = movie.Versions.ToList()
            };

            return View(viewModel);
        }

        /// <summary>
        /// [GET] api/movie/create
        /// Trả về form tạo mới phim.
        /// </summary>
        [HttpGet]
        public IActionResult Create()
        {
            var model = new MovieDetailViewModel
            {
                AvailableTypes = _movieService.GetAllTypes(),
                AvailableVersions = _movieService.GetAllVersions()
            };
            return View(model);
        }

        /// <summary>
        /// [POST] api/movie/create
        /// Tạo mới một bộ phim kèm theo các lịch chiếu và ngày chiếu.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(MovieDetailViewModel model)
        {
            if (!ModelState.IsValid)
            {
                model.AvailableTypes = _movieService.GetAllTypes();
                model.AvailableVersions = _movieService.GetAllVersions();
                return View(model);
            }

            if (model.FromDate >= model.ToDate)
            {
                TempData["ErrorMessage"] = "Invalid date range. From date must be before To date.";
                model.AvailableTypes = _movieService.GetAllTypes();
                model.AvailableVersions = _movieService.GetAllVersions();
                return View(model);
            }

            var selectedVersions = _movieService.GetAllVersions().Where(v => model.SelectedVersionIds.Contains(v.VersionId)).ToList();

            var movie = new Movie
            {
                MovieId = model.MovieId,
                MovieNameEnglish = model.MovieNameEnglish,
                MovieNameVn = model.MovieNameVn,
                Actor = model.Actor,
                Director = model.Director,
                Duration = model.Duration,
                FromDate = model.FromDate,
                ToDate = model.ToDate,
                MovieProductionCompany = model.MovieProductionCompany,
                Content = model.Content,
                TrailerUrl = _movieService.ConvertToEmbedUrl(model.TrailerUrl),
                LargeImage = model.LargeImage,
                SmallImage = model.SmallImage,
                Types = _movieService.GetAllTypes().Where(t => model.SelectedTypeIds.Contains(t.TypeId)).ToList(),
                Versions = _movieService.GetAllVersions().Where(v => model.SelectedVersionIds.Contains(v.VersionId)).ToList(),
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
            model.AvailableVersions = _movieService.GetAllVersions();
            return View(model);
        }

        /// <summary>
        /// [GET] api/movie/edit/{id}
        /// Trả về form cập nhật phim.
        /// </summary>
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
                FromDate = movie.FromDate,
                ToDate = movie.ToDate,
                MovieProductionCompany = movie.MovieProductionCompany,
                Content = movie.Content,
                TrailerUrl = movie.TrailerUrl,
                LargeImage = movie.LargeImage,
                SmallImage = movie.SmallImage,
                AvailableTypes = _movieService.GetAllTypes(),
                AvailableVersions = _movieService.GetAllVersions(),
                SelectedTypeIds = movie.Types.Select(t => t.TypeId).ToList(),
                SelectedVersionIds = movie.Versions.Select(v => v.VersionId).ToList()
            };

            return View(model);
        }

        /// <summary>
        /// [POST] api/movie/edit/{id}
        /// Cập nhật thông tin phim dựa trên ID. Kiểm tra validation và cập nhật dữ liệu.
        /// </summary>
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


            var existingMovie = _movieService.GetById(id);
            if (existingMovie == null)
            {
                return NotFound();
            }

            existingMovie.Types = _movieService.GetAllTypes().Where(t => model.SelectedTypeIds.Contains(t.TypeId)).ToList();
            existingMovie.Versions = _movieService.GetAllVersions().Where(v => model.SelectedVersionIds.Contains(v.VersionId)).ToList();

            // Conflict check only if duration has changed
            if (existingMovie.Duration != model.Duration)
            {
                const int CLEANING_TIME_MINUTES = 15;
                var movieShows = _movieService.GetMovieShows(id);
                var allMovieShows = _movieService.GetMovieShow();

                foreach (var show in movieShows)
                {
                    var start = show.Schedule?.ScheduleTime;
                    if (start == null) continue;
                    var newEnd = start.Value.AddMinutes((model.Duration ?? 0) + CLEANING_TIME_MINUTES);

                    // Get all other shows in the same room and date (any movie, not just this one)
                    var otherShows = allMovieShows
                        .Where(ms => ms.CinemaRoomId == show.CinemaRoomId
                                  && ms.ShowDate == show.ShowDate
                                  && ms.MovieShowId != show.MovieShowId
                                  && ms.Schedule != null
                                  && ms.Movie != null)
                        .ToList();

                    foreach (var other in otherShows)
                    {
                        var otherStart = other.Schedule.ScheduleTime;
                        var otherEnd = otherStart?.AddMinutes((other.Movie.Duration ?? 0) + CLEANING_TIME_MINUTES);
                        if (otherStart == null || otherEnd == null) continue;

                        // Check for overlap (including buffer)
                        if (start < otherEnd && newEnd > otherStart)
                        {
                            TempData["ErrorMessage"] = $"Duration update causes conflict on {show.ShowDate:dd/MM/yyyy} in room {show.CinemaRoom.CinemaRoomName} with another movie show ({other.Movie.MovieNameEnglish}) between {otherStart:HH\\:mm} and {otherEnd:HH\\:mm}. Consider updating movie show before changing duration.";
                            model.AvailableTypes = _movieService.GetAllTypes();
                            return View(model);
                        }
                    }
                }
            }

            var movie = new Movie
            {
                MovieId = model.MovieId,
                MovieNameEnglish = model.MovieNameEnglish,
                MovieNameVn = model.MovieNameVn,
                Actor = model.Actor,
                Director = model.Director,
                Duration = model.Duration,
                FromDate = model.FromDate,
                ToDate = model.ToDate,
                MovieProductionCompany = model.MovieProductionCompany,
                Content = model.Content,
                TrailerUrl = _movieService.ConvertToEmbedUrl(model.TrailerUrl),
                LargeImage = model.LargeImage,
                SmallImage = model.SmallImage,
                Types = _movieService.GetAllTypes().Where(t => model.SelectedTypeIds.Contains(t.TypeId)).ToList(),
                Versions = _movieService.GetAllVersions().Where(v => model.SelectedVersionIds.Contains(v.VersionId)).ToList(),

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
            model.AvailableVersions = _movieService.GetAllVersions();
            return View(model);
        }

        /// <summary>
        /// [POST] api/movie/delete/{id}
        /// Xóa phim khỏi hệ thống dựa trên ID. Role xác định route sau khi xóa.
        /// </summary>
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
                movie.Versions?.Clear();

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

            var showDates = new List<DateOnly>();
            if (movie.FromDate.HasValue && movie.ToDate.HasValue)
            {
                for (var date = movie.FromDate.Value; date <= movie.ToDate.Value; date = date.AddDays(1))
                {
                    showDates.Add(date);
                }
            }

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                var movieShowsJson = _movieService.GetMovieShows(id);
                var showDetails = movieShowsJson.Select(ms => new
                {
                    ms.MovieShowId,
                    ms.MovieId,
                    ms.ShowDate,
                    showDate = ms.ShowDate.ToString("dd/MM/yyyy"),
                    ms.ScheduleId,
                    scheduleTime = ms.Schedule?.ScheduleTime,
                    ms.CinemaRoomId,
                    cinemaRoomName = ms.CinemaRoom?.CinemaRoomName
                }).ToList();

                return Json(showDetails);
            }

            var movieShows = _movieService.GetMovieShows(id);

            var viewModel = new MovieDetailViewModel
            {
                MovieId = movie.MovieId,
                MovieNameEnglish = movie.MovieNameEnglish,
                Duration = movie.Duration,
                AvailableCinemaRooms = _cinemaService.GetAll().ToList(),
                AvailableShowDates = showDates,
                AvailableSchedules = _movieService.GetSchedules().ToList(),
                CurrentMovieShows = movieShows,
                AvailableVersions = movie.Versions.ToList()
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
                model.AvailableShowDates = _movieService.GetShowDates(model.MovieId).ToList();
                model.AvailableSchedules = _movieService.GetSchedules().ToList();
                return View(model);
            }

            try
            {
                // The actual movie show creation is handled by the JavaScript AJAX calls
                TempData["SuccessMessage"] = "Movie shows updated successfully!";
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating movie shows for movie {MovieId}", id);
                TempData["ErrorMessage"] = "An error occurred while updating movie shows.";
                model.AvailableCinemaRooms = _cinemaService.GetAll().ToList();
                model.AvailableShowDates = _movieService.GetShowDates(model.MovieId).ToList();
                model.AvailableSchedules = _movieService.GetSchedules().ToList();
                return View(model);
            }
        }

        [HttpGet]
        [Route("Movie/GetAvailableScheduleTimes")]
        public async Task<IActionResult> GetAvailableScheduleTimes(int cinemaRoomId, string showDate, int movieDurationMinutes, int cleaningTimeMinutes)
        {
            if (!DateOnly.TryParse(showDate, out var parsedDate))
            {
                return BadRequest("Invalid date format.");
            }

            var availableSchedules = await _movieService.GetAvailableSchedulesAsync(parsedDate, cinemaRoomId);
            
            var lastShowInRoom = _movieService.GetMovieShowsByRoomAndDate(cinemaRoomId, parsedDate)
                .OrderByDescending(ms => ms.Schedule.ScheduleTime)
                .FirstOrDefault();

            string lastShowEndTime = "N/A";
            TimeOnly? lastEndTimeForFiltering = null;

            if (lastShowInRoom != null && lastShowInRoom.Schedule != null && lastShowInRoom.Schedule.ScheduleTime.HasValue)
            {
                var startTime = lastShowInRoom.Schedule.ScheduleTime.Value;
                var movie = _movieService.GetById(lastShowInRoom.MovieId);
                if (movie != null && movie.Duration.HasValue)
                {
                    var endTime = startTime.AddMinutes(movie.Duration.Value + cleaningTimeMinutes);
                    lastShowEndTime = endTime.ToString("HH:mm");
                    lastEndTimeForFiltering = endTime;
                }
            }
            
            var filteredSchedules = availableSchedules;
            if (lastEndTimeForFiltering.HasValue)
            {
                filteredSchedules = availableSchedules.Where(s => s.ScheduleTime.HasValue && s.ScheduleTime.Value > lastEndTimeForFiltering.Value).ToList();
            }

            var nextAvailableTime = "N/A";
            if(filteredSchedules.Any()){
                nextAvailableTime = filteredSchedules.First().ScheduleTime?.ToString("HH:mm") ?? "N/A";
            }

            var scheduleVms = filteredSchedules.Select(s => new {
                scheduleId = s.ScheduleId,
                scheduleTime = s.ScheduleTime?.ToString("HH:mm")
            }).ToList();

            return Ok(new { schedules = scheduleVms, lastShowEndTime = lastShowEndTime, nextAvailableTime = nextAvailableTime });
        }

        [HttpPost]
        public async Task<IActionResult> AddMovieShow([FromBody] MovieShowRequest request)
        {
            if (request == null)
            {
                return BadRequest("Invalid request data.");
            }

            var movieShow = new MovieShow
            {
                MovieId = request.MovieId,
                ShowDate = request.ShowDate,
                ScheduleId = request.ScheduleId,
                CinemaRoomId = request.CinemaRoomId
            };

            var added = _movieService.AddMovieShow(movieShow);

            if (added)
            {
                return Ok(new { success = true, message = "Movie show added successfully." });
            }
            else
            {
                return BadRequest(new { success = false, message = "Failed to add movie show. The schedule might be unavailable." });
            }
        }
        
        [HttpPost]
        public async Task<IActionResult> DeleteAllMovieShows(string movieId)
        {
            var success = _movieService.DeleteAllMovieShows(movieId);
            if (success)
            {
                return Ok();
            }
            return BadRequest("Could not delete all movie shows.");
        }

        [HttpGet]
        public async Task<IActionResult> GetAvailableSchedules(DateOnly showDate, int cinemaRoomId)
        {
            try
            {
                var availableSchedules = await _movieService.GetAvailableSchedulesAsync(showDate, cinemaRoomId);
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

