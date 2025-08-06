using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using MovieTheater.Helpers;
using MovieTheater.Hubs;
using MovieTheater.Models;
using MovieTheater.Repository;
using MovieTheater.Service;
using MovieTheater.ViewModels;
using System.Security.Claims;

namespace MovieTheater.Controllers
{//movie
    public class MovieController : Controller
    {
        private readonly IMovieService _movieService;
        private readonly ICinemaService _cinemaService;
        private readonly ILogger<MovieController> _logger;
        private readonly IHubContext<DashboardHub> _dashboardHubContext;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly IPersonRepository _personRepository;

        // Constants for string literals
        private const string ERROR_MESSAGE = "ErrorMessage";
        private const string TOAST_MESSAGE = "ToastMessage";
        private const string MAIN_PAGE = "MainPage";
        private const string ADMIN_CONTROLLER = "Admin";
        private const string EMPLOYEE_CONTROLLER = "Employee";
        private const string MOVIE_MG_TAB = "MovieMg";

        public MovieController(IMovieService movieService, ICinemaService cinemaService, ILogger<MovieController> logger, IHubContext<DashboardHub> dashboardHubContext, IWebHostEnvironment webHostEnvironment, IPersonRepository personRepository)
        {
            _movieService = movieService;
            _cinemaService = cinemaService;
            _logger = logger;
            _dashboardHubContext = dashboardHubContext;
            _webHostEnvironment = webHostEnvironment;
            _personRepository = personRepository;
        }
        public string role => User?.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value;

        /// <summary>
        /// [GET] api/movie/movielist
        /// Tìm kiếm và hiển thị danh sách phim. Nếu là Ajax request thì trả về partial view.
        /// </summary>
        [HttpGet]
        [Route("Movie/MovieList")]
        public IActionResult MovieList(string searchTerm, string typeIds, string versionIds) // NOSONAR - GET methods don't require ModelState.IsValid check
        {
            var selectedTypeIds = string.IsNullOrEmpty(typeIds) ? new List<int>() : typeIds.Split(',').Select(int.Parse).ToList();
            var selectedVersionIds = string.IsNullOrEmpty(versionIds) ? new List<int>() : versionIds.Split(',').Select(int.Parse).ToList();

            // Get ongoing and incoming movies
            var ongoingMovies = _movieService.GetCurrentlyShowingMoviesWithDetails();
            var incomingMovies = _movieService.GetComingSoonMoviesWithDetails();

            // Filter by search term first
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                ongoingMovies = ongoingMovies.Where(m =>
                    m.MovieNameEnglish?.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) == true ||
                    m.Content?.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) == true).ToList();

                incomingMovies = incomingMovies.Where(m =>
                    m.MovieNameEnglish?.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) == true ||
                    m.Content?.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) == true).ToList();
            }

            // Separate ongoing and incoming movies after filtering
            var ongoingMoviesFiltered = ongoingMovies
                .Where(m => (selectedTypeIds.Count == 0 || (m.Types ?? new List<Models.Type>()).Any(t => selectedTypeIds.Contains(t.TypeId))) &&
                            (selectedVersionIds.Count == 0 || (m.Versions ?? new List<Models.Version>()).Any(v => selectedVersionIds.Contains(v.VersionId))))
                .Select(m => new MovieViewModel
                {
                    MovieId = m.MovieId,
                    MovieNameEnglish = m.MovieNameEnglish,
                    Duration = m.Duration,
                    SmallImage = m.SmallImage,
                    Types = m.Types.ToList(),
                    Versions = m.Versions.ToList(),
                    IsOngoing = true
                })
                .ToList();

            var incomingMoviesFiltered = incomingMovies
                .Where(m => (selectedTypeIds.Count == 0 || (m.Types ?? new List<Models.Type>()).Any(t => selectedTypeIds.Contains(t.TypeId))) &&
                            (selectedVersionIds.Count == 0 || (m.Versions ?? new List<Models.Version>()).Any(v => selectedVersionIds.Contains(v.VersionId))))
                .Select(m => new MovieViewModel
                {
                    MovieId = m.MovieId,
                    MovieNameEnglish = m.MovieNameEnglish,
                    Duration = m.Duration,
                    SmallImage = m.SmallImage,
                    Types = m.Types.ToList(),
                    Versions = m.Versions.ToList(),
                    IsOngoing = false
                })
                .ToList();

            var movies = ongoingMoviesFiltered.Concat(incomingMoviesFiltered).ToList();

            ViewBag.AllTypes = _movieService.GetAllTypes();
            ViewBag.AllVersions = _movieService.GetAllVersions();
            ViewBag.SelectedTypeIds = selectedTypeIds;
            ViewBag.SelectedVersionIds = selectedVersionIds;
            ViewBag.SearchTerm = searchTerm;

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return PartialView("_MovieFilterAndGrid", movies);
            }

            return View(movies);
        }

        /// <summary>
        /// [GET] api/movie/detail/{id}
        /// Hiển thị thông tin chi tiết của một bộ phim.
        /// </summary>
        [HttpGet]
        [Route("Movie/Detail/{id}")]
        public ActionResult Detail(string id) // NOSONAR - GET methods don't require ModelState.IsValid check
        {
            var movie = _movieService.GetById(id);
            if (movie == null)
            {
                return NotFound();
            }
            var actors = movie.People.Where(p => p.IsDirector == false).ToList();
            var directors = movie.People.Where(p => p.IsDirector == true).ToList();

            var viewModel = new MovieDetailViewModel
            {
                MovieId = movie.MovieId,
                MovieNameEnglish = movie.MovieNameEnglish,
                FromDate = movie.FromDate,
                ToDate = movie.ToDate,
                MovieProductionCompany = movie.MovieProductionCompany,
                Duration = movie.Duration,
                Content = movie.Content,
                TrailerUrl = _movieService.ConvertToEmbedUrl(movie.TrailerUrl),
                SmallImage = movie.SmallImage,
                AvailableTypes = movie.Types.ToList(),
                AvailableVersions = movie.Versions.ToList(),
                People = movie.People.ToList()
            };

            // Lấy phim cùng thể loại cho phần "You may also like"
            var similarMovies = _movieService.GetMoviesBySameGenre(id, 4);
            ViewBag.SimilarMovies = similarMovies;

            return View(viewModel);
        }

        /// <summary>
        /// [GET] api/movie/create
        /// Trả về form tạo mới phim.
        /// </summary>
        [HttpGet]
        [Route("Movie/Create")]
        [Authorize(Roles = "Admin,Employee")]
        public IActionResult Create() // NOSONAR - GET methods don't require ModelState.IsValid check
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
        [Route("Movie/Create")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Employee")]
        public async Task<IActionResult> Create(MovieDetailViewModel model)
        {
            if (!ModelState.IsValid)
            {
                model.AvailableTypes = _movieService.GetAllTypes();
                model.AvailableVersions = _movieService.GetAllVersions();
                return View(model);
            }

            if (model.FromDate >= model.ToDate)
            {
                TempData[ERROR_MESSAGE] = "Invalid date range. From date must be before To date.";
                model.AvailableTypes = _movieService.GetAllTypes();
                model.AvailableVersions = _movieService.GetAllVersions();
                return View(model);
            }

            // Handle image uploads (LargeImageFile, SmallImageFile)
            string largeImagePath = null;
            string smallImagePath = null;
            string logoPath = null;

            string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images", "movies");
            if (!Directory.Exists(uploadsFolder))
                Directory.CreateDirectory(uploadsFolder);

            if (model.LargeImageFile != null && model.LargeImageFile.Length > 0)
            {
                string sanitizedFileName = PathSecurityHelper.SanitizeFileName(model.LargeImageFile.FileName);
                string uniqueFileName = Guid.NewGuid().ToString() + "_" + sanitizedFileName;

                string? secureFilePath = PathSecurityHelper.CreateSecureFilePath(uploadsFolder, uniqueFileName);
                if (secureFilePath == null)
                {
                    TempData[ERROR_MESSAGE] = "Invalid file path detected.";
                    model.AvailableTypes = _movieService.GetAllTypes();
                    model.AvailableVersions = _movieService.GetAllVersions();
                    return View(model);
                }

                using (var fileStream = new FileStream(secureFilePath, FileMode.Create))
                {
                    await model.LargeImageFile.CopyToAsync(fileStream);
                }
                largeImagePath = "/images/movies/" + uniqueFileName;
            }
            else
            {
                largeImagePath = "/images/movies/default-movie.jpg";
            }

            if (model.SmallImageFile != null && model.SmallImageFile.Length > 0)
            {
                string sanitizedFileName = PathSecurityHelper.SanitizeFileName(model.SmallImageFile.FileName);
                string uniqueFileName = Guid.NewGuid().ToString() + "_" + sanitizedFileName;

                string? secureFilePath = PathSecurityHelper.CreateSecureFilePath(uploadsFolder, uniqueFileName);
                if (secureFilePath == null)
                {
                    TempData[ERROR_MESSAGE] = "Invalid file path detected.";
                    model.AvailableTypes = _movieService.GetAllTypes();
                    model.AvailableVersions = _movieService.GetAllVersions();
                    return View(model);
                }

                using (var fileStream = new FileStream(secureFilePath, FileMode.Create))
                {
                    await model.SmallImageFile.CopyToAsync(fileStream);
                }
                smallImagePath = "/images/movies/" + uniqueFileName;
            }
            else
            {
                smallImagePath = "/images/movies/default-movie.jpg";
            }

            if (model.LogoFile != null && model.LogoFile.Length > 0)
            {
                string sanitizedFileName = PathSecurityHelper.SanitizeFileName(model.LogoFile.FileName);
                string uniqueFileName = Guid.NewGuid().ToString() + "_" + sanitizedFileName;

                string? secureFilePath = PathSecurityHelper.CreateSecureFilePath(uploadsFolder, uniqueFileName);
                if (secureFilePath == null)
                {
                    TempData[ERROR_MESSAGE] = "Invalid file path detected.";
                    model.AvailableTypes = _movieService.GetAllTypes();
                    model.AvailableVersions = _movieService.GetAllVersions();
                    return View(model);
                }

                using (var fileStream = new FileStream(secureFilePath, FileMode.Create))
                {
                    await model.LogoFile.CopyToAsync(fileStream);
                }
                logoPath = "/images/movies/" + uniqueFileName;
            }
            else
            {
                logoPath = "/images/movies/default-movie.jpg";
            }

            var selectedVersions = _movieService.GetAllVersions().Where(v => model.SelectedVersionIds.Contains(v.VersionId)).ToList();

            // Parse selected actor and director IDs
            var selectedActorIds = new List<int>();
            var selectedDirectorIds = new List<int>();

            if (!string.IsNullOrEmpty(model.SelectedActorIds))
            {
                selectedActorIds = model.SelectedActorIds.Split(',')
                    .Where(id => !string.IsNullOrEmpty(id))
                    .Select(int.Parse)
                    .ToList();
            }

            if (!string.IsNullOrEmpty(model.SelectedDirectorIds))
            {
                selectedDirectorIds = model.SelectedDirectorIds.Split(',')
                    .Where(id => !string.IsNullOrEmpty(id))
                    .Select(int.Parse)
                    .ToList();
            }

            // Get selected actors and directors
            var selectedActors = _personRepository.GetActors().Where(a => selectedActorIds.Contains(a.PersonId)).ToList();
            var selectedDirectors = _personRepository.GetDirectors().Where(d => selectedDirectorIds.Contains(d.PersonId)).ToList();

            var movie = new Movie
            {
                MovieId = model.MovieId,
                MovieNameEnglish = model.MovieNameEnglish,
                Duration = model.Duration,
                FromDate = model.FromDate,
                ToDate = model.ToDate,
                MovieProductionCompany = model.MovieProductionCompany,
                Content = model.Content,
                TrailerUrl = _movieService.ConvertToEmbedUrl(model.TrailerUrl),
                LargeImage = largeImagePath,
                SmallImage = smallImagePath,
                LogoImage = logoPath,
                Types = _movieService.GetAllTypes().Where(t => model.SelectedTypeIds.Contains(t.TypeId)).ToList(),
                Versions = _movieService.GetAllVersions().Where(v => model.SelectedVersionIds.Contains(v.VersionId)).ToList(),
                People = selectedActors.Concat(selectedDirectors).ToList()
            };

            if (_movieService.AddMovie(movie))
            {
                TempData[TOAST_MESSAGE] = "Movie created successfully!";
                await _dashboardHubContext.Clients.All.SendAsync("DashboardUpdated");
                if (role == "Admin")
                    return RedirectToAction(MAIN_PAGE, ADMIN_CONTROLLER, new { tab = MOVIE_MG_TAB });
                else
                    return RedirectToAction(MAIN_PAGE, EMPLOYEE_CONTROLLER, new { tab = MOVIE_MG_TAB });
            }

            TempData[ERROR_MESSAGE] = "Failed to create movie.";
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
        [Authorize(Roles = "Admin,Employee")]
        public IActionResult Edit(string id) // NOSONAR - GET methods don't require ModelState.IsValid check
        {
            var movie = _movieService.GetById(id);
            if (movie == null)
            {
                return NotFound();
            }

            // Get actors and directors from the movie's People collection
            var actors = movie.People.Where(p => p.IsDirector == false).ToList();
            var directors = movie.People.Where(p => p.IsDirector == true).ToList();

            var model = new MovieDetailViewModel
            {
                MovieId = movie.MovieId,
                MovieNameEnglish = movie.MovieNameEnglish,
                Duration = movie.Duration,
                FromDate = movie.FromDate,
                ToDate = movie.ToDate,
                MovieProductionCompany = movie.MovieProductionCompany,
                Content = movie.Content,
                TrailerUrl = movie.TrailerUrl,
                LargeImage = movie.LargeImage,
                SmallImage = movie.SmallImage,
                Logo = movie.LogoImage,
                AvailableTypes = _movieService.GetAllTypes(),
                AvailableVersions = _movieService.GetAllVersions(),
                SelectedTypeIds = movie.Types.Select(t => t.TypeId).ToList(),
                SelectedVersionIds = movie.Versions.Select(v => v.VersionId).ToList(),
                SelectedActorIds = string.Join(",", actors.Select(a => a.PersonId)),
                SelectedDirectorIds = string.Join(",", directors.Select(d => d.PersonId))
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
        [Authorize(Roles = "Admin,Employee")]
        public async Task<IActionResult> Edit(string id, MovieDetailViewModel model)
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
                TempData[ERROR_MESSAGE] = "Invalid date range. From date must be before To date.";
                model.AvailableTypes = _movieService.GetAllTypes();
                return View(model);
            }

            var existingMovie = _movieService.GetById(id);
            if (existingMovie == null)
            {
                return NotFound();
            }

            // Handle image uploads
            string largeImagePath = existingMovie.LargeImage ?? "/images/movies/default-movie.jpg";
            string smallImagePath = existingMovie.SmallImage ?? "/images/movies/default-movie.jpg";
            string logoPath = existingMovie.LogoImage ?? "/images/movies/default-movie.jpg";
            string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images", "movies");
            if (!Directory.Exists(uploadsFolder))
                Directory.CreateDirectory(uploadsFolder);

            // Process large image upload
            if (model.LargeImageFile != null && model.LargeImageFile.Length > 0)
            {
                // Delete old image if exists and not default
                if (!string.IsNullOrEmpty(existingMovie.LargeImage) && !existingMovie.LargeImage.Contains("default-movie.jpg"))
                {
                    string oldImagePath = Path.Combine(_webHostEnvironment.WebRootPath, existingMovie.LargeImage.TrimStart('/').Replace("/", Path.DirectorySeparatorChar.ToString()));
                    if (System.IO.File.Exists(oldImagePath))
                        System.IO.File.Delete(oldImagePath);
                }
                string sanitizedFileName = PathSecurityHelper.SanitizeFileName(model.LargeImageFile.FileName);
                string uniqueFileName = Guid.NewGuid().ToString() + "_" + sanitizedFileName;

                string? secureFilePath = PathSecurityHelper.CreateSecureFilePath(uploadsFolder, uniqueFileName);
                if (secureFilePath == null)
                {
                    TempData[ERROR_MESSAGE] = "Invalid file path detected.";
                    model.AvailableTypes = _movieService.GetAllTypes();
                    return View(model);
                }

                using (var fileStream = new FileStream(secureFilePath, FileMode.Create))
                {
                    await model.LargeImageFile.CopyToAsync(fileStream);
                }
                largeImagePath = "/images/movies/" + uniqueFileName;
            }

            // Process small image upload
            if (model.SmallImageFile != null && model.SmallImageFile.Length > 0)
            {
                if (!string.IsNullOrEmpty(existingMovie.SmallImage) && !existingMovie.SmallImage.Contains("default-movie.jpg"))
                {
                    string oldImagePath = Path.Combine(_webHostEnvironment.WebRootPath, existingMovie.SmallImage.TrimStart('/').Replace("/", Path.DirectorySeparatorChar.ToString()));
                    if (System.IO.File.Exists(oldImagePath))
                        System.IO.File.Delete(oldImagePath);
                }
                string sanitizedFileName = PathSecurityHelper.SanitizeFileName(model.SmallImageFile.FileName);
                string uniqueFileName = Guid.NewGuid().ToString() + "_" + sanitizedFileName;

                string? secureFilePath = PathSecurityHelper.CreateSecureFilePath(uploadsFolder, uniqueFileName);
                if (secureFilePath == null)
                {
                    TempData[ERROR_MESSAGE] = "Invalid file path detected.";
                    model.AvailableTypes = _movieService.GetAllTypes();
                    return View(model);
                }

                using (var fileStream = new FileStream(secureFilePath, FileMode.Create))
                {
                    await model.SmallImageFile.CopyToAsync(fileStream);
                }
                smallImagePath = "/images/movies/" + uniqueFileName;
            }

            if (model.LogoFile != null && model.LogoFile.Length > 0)
            {
                if (!string.IsNullOrEmpty(existingMovie.LogoImage) && !existingMovie.LogoImage.Contains("default-movie.jpg"))
                {
                    string oldImagePath = Path.Combine(_webHostEnvironment.WebRootPath, existingMovie.LogoImage.TrimStart('/').Replace("/", Path.DirectorySeparatorChar.ToString()));
                    if (System.IO.File.Exists(oldImagePath))
                        System.IO.File.Delete(oldImagePath);
                }
                string sanitizedFileName = PathSecurityHelper.SanitizeFileName(model.LogoFile.FileName);
                string uniqueFileName = Guid.NewGuid().ToString() + "_" + sanitizedFileName;

                string? secureFilePath = PathSecurityHelper.CreateSecureFilePath(uploadsFolder, uniqueFileName);
                if (secureFilePath == null)
                {
                    TempData[ERROR_MESSAGE] = "Invalid file path detected.";
                    model.AvailableTypes = _movieService.GetAllTypes();
                    return View(model);
                }

                using (var fileStream = new FileStream(secureFilePath, FileMode.Create))
                {
                    await model.LogoFile.CopyToAsync(fileStream);
                }
                logoPath = "/images/movies/" + uniqueFileName;
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
                            TempData[ERROR_MESSAGE] = $"Duration update causes conflict on {show.ShowDate:dd/MM/yyyy} in room {show.CinemaRoom.CinemaRoomName} with another movie show ({other.Movie.MovieNameEnglish}) between {otherStart:HH\\:mm} and {otherEnd:HH\\:mm}. Consider updating movie show before changing duration.";
                            model.AvailableTypes = _movieService.GetAllTypes();
                            return View(model);
                        }
                    }
                }
            }

            // Parse selected actor and director IDs
            var selectedActorIds = new List<int>();
            var selectedDirectorIds = new List<int>();

            if (!string.IsNullOrEmpty(model.SelectedActorIds))
            {
                selectedActorIds = model.SelectedActorIds.Split(',')
                    .Where(id => !string.IsNullOrEmpty(id))
                    .Select(int.Parse)
                    .ToList();
            }

            if (!string.IsNullOrEmpty(model.SelectedDirectorIds))
            {
                selectedDirectorIds = model.SelectedDirectorIds.Split(',')
                    .Where(id => !string.IsNullOrEmpty(id))
                    .Select(int.Parse)
                    .ToList();
            }

            // Get selected actors and directors
            var selectedActors = _personRepository.GetActors().Where(a => selectedActorIds.Contains(a.PersonId)).ToList();
            var selectedDirectors = _personRepository.GetDirectors().Where(d => selectedDirectorIds.Contains(d.PersonId)).ToList();

            var movie = new Movie
            {
                MovieId = model.MovieId,
                MovieNameEnglish = model.MovieNameEnglish,
                Duration = model.Duration,
                FromDate = model.FromDate,
                ToDate = model.ToDate,
                MovieProductionCompany = model.MovieProductionCompany,
                Content = model.Content,
                TrailerUrl = _movieService.ConvertToEmbedUrl(model.TrailerUrl),
                LargeImage = largeImagePath,
                SmallImage = smallImagePath,
                LogoImage = logoPath,
                Types = _movieService.GetAllTypes().Where(t => model.SelectedTypeIds.Contains(t.TypeId)).ToList(),
                Versions = _movieService.GetAllVersions().Where(v => model.SelectedVersionIds.Contains(v.VersionId)).ToList(),
                People = selectedActors.Concat(selectedDirectors).ToList()
            };

            if (_movieService.UpdateMovie(movie))
            {
                TempData[TOAST_MESSAGE] = "Movie updated successfully!";
                await _dashboardHubContext.Clients.All.SendAsync("DashboardUpdated");
                if (role == "Admin")
                    return RedirectToAction(MAIN_PAGE, ADMIN_CONTROLLER, new { tab = MOVIE_MG_TAB });
                else
                    return RedirectToAction(MAIN_PAGE, EMPLOYEE_CONTROLLER, new { tab = MOVIE_MG_TAB });
            }

            TempData[ERROR_MESSAGE] = "Failed to update movie.";
            model.AvailableTypes = _movieService.GetAllTypes();
            model.AvailableVersions = _movieService.GetAllVersions();
            return View(model);
        }

        /// <summary>
        /// [POST] api/movie/delete/{id}
        /// Xóa phim khỏi hệ thống dựa trên ID. Role xác định route sau khi xóa.
        /// </summary>
        [HttpPost]
        [Route("Movie/Delete/{id}")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Employee")]
        public IActionResult Delete(string id, IFormCollection collection)
        {
            try
            {
                if (string.IsNullOrEmpty(id))
                {
                    TempData[TOAST_MESSAGE] = "Invalid movie ID.";
                    if (role == "Admin")
                        return RedirectToAction(MAIN_PAGE, ADMIN_CONTROLLER, new { tab = MOVIE_MG_TAB });
                    else
                        return RedirectToAction(MAIN_PAGE, EMPLOYEE_CONTROLLER, new { tab = MOVIE_MG_TAB });
                }

                var movie = _movieService.GetById(id);
                if (movie == null)
                {
                    TempData[TOAST_MESSAGE] = "Movie not found.";
                    if (role == "Admin")
                        return RedirectToAction(MAIN_PAGE, ADMIN_CONTROLLER, new { tab = MOVIE_MG_TAB });
                    else
                        return RedirectToAction(MAIN_PAGE, EMPLOYEE_CONTROLLER, new { tab = MOVIE_MG_TAB });
                }

                movie.Types?.Clear();
                movie.Versions?.Clear();

                bool success = _movieService.DeleteMovie(id);

                if (!success)
                {
                    TempData[TOAST_MESSAGE] = "Failed to delete movie.";
                    if (role == "Admin")
                        return RedirectToAction(MAIN_PAGE, ADMIN_CONTROLLER, new { tab = MOVIE_MG_TAB });
                    else
                        return RedirectToAction(MAIN_PAGE, EMPLOYEE_CONTROLLER, new { tab = MOVIE_MG_TAB });
                }

                TempData[TOAST_MESSAGE] = "Movie deleted successfully!";
                _dashboardHubContext.Clients.All.SendAsync("DashboardUpdated").GetAwaiter().GetResult();
                if (role == "Admin")
                    return RedirectToAction(MAIN_PAGE, ADMIN_CONTROLLER, new { tab = MOVIE_MG_TAB });
                else
                    return RedirectToAction(MAIN_PAGE, EMPLOYEE_CONTROLLER, new { tab = MOVIE_MG_TAB });
            }
            catch (Exception ex)
            {
                TempData[TOAST_MESSAGE] = $"An error occurred during deletion: {ex.Message}";
                if (role == "Admin")
                    return RedirectToAction(MAIN_PAGE, ADMIN_CONTROLLER, new { tab = MOVIE_MG_TAB });
                else
                    return RedirectToAction(MAIN_PAGE, EMPLOYEE_CONTROLLER, new { tab = MOVIE_MG_TAB });
            }
        }

        [HttpGet]
        public IActionResult GetMovieShows(string movieId) // NOSONAR - GET methods don't require ModelState.IsValid check
        {
            var movieShows = _movieService.GetMovieShowsByMovieId(movieId)
                .Select(ms => new
                {
                    ms.MovieShowId,
                    ms.MovieId,
                    showDate = ms.ShowDate.ToString("yyyy-MM-dd"),
                    scheduleTime = ms.Schedule.ScheduleTime.Value.ToString("HH:mm"),
                    versionName = ms.Version?.VersionName,
                    versionId = ms.VersionId,
                    CinemaRoomStatus = ms.CinemaRoom?.StatusId ?? 1, // Include room status for backward compatibility
                    IsAvailable = true // Since GetMovieShowsByMovieId already filters, all returned shows are available
                }).ToList();
            return Json(movieShows);
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
                    showDate = ms.ShowDate.ToString("dd/MM/yyyy"),
                    ms.ScheduleId,
                    scheduleTime = ms.Schedule?.ScheduleTime.HasValue == true ? ms.Schedule.ScheduleTime.Value.ToString("HH:mm") : null,
                    ms.CinemaRoomId,
                    cinemaRoomName = ms.CinemaRoom?.CinemaRoomName,
                    cinemaRoomStatus = ms.CinemaRoom?.StatusId ?? 1, // Include room status
                    ms.VersionId,
                    versionName = ms.Version?.VersionName
                }).ToList();

                return Json(showDetails);
            }

            var movieShows = _movieService.GetMovieShows(id);

            var viewModel = new MovieDetailViewModel
            {
                MovieId = movie.MovieId,
                MovieNameEnglish = movie.MovieNameEnglish,
                Duration = movie.Duration,
                AvailableCinemaRooms = _cinemaService.GetAll()
                    .Where(r => r.StatusId == 1)
                    .ToList(),
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
                model.AvailableCinemaRooms = _cinemaService.GetAll()
                    .Where(r => r.StatusId == 1)
                    .ToList();
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
                model.AvailableCinemaRooms = _cinemaService.GetAll()
                    .Where(r => r.StatusId == 1)
                    .ToList();
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
            if (filteredSchedules.Any())
            {
                nextAvailableTime = filteredSchedules.First().ScheduleTime?.ToString("HH:mm") ?? "N/A";
            }

            var scheduleVms = filteredSchedules.Select(s => new
            {
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
                CinemaRoomId = request.CinemaRoomId,
                VersionId = request.VersionId
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
        public async Task<IActionResult> DeleteAllMovieShows([FromBody] MovieShowRequestDeleteAll request)
        {
            if (request == null || string.IsNullOrEmpty(request.MovieId))
                return BadRequest("Missing movieId");
            var success = _movieService.DeleteAllMovieShows(request.MovieId);
            if (success)
            {
                return Ok();
            }
            return BadRequest("Could not delete all movie shows.");
        }

        public class MovieShowRequestDeleteAll
        {
            public string MovieId { get; set; }
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

        [HttpGet]
        public IActionResult GetMovieShowsByRoomAndDate(int cinemaRoomId, string showDate) // NOSONAR - GET methods don't require ModelState.IsValid check
        {
            if (!DateOnly.TryParse(showDate, out var parsedDate))
                return BadRequest("Invalid date format.");

            var shows = _movieService.GetMovieShowsByRoomAndDate(cinemaRoomId, parsedDate)
                .Select(ms => new
                {
                    scheduleText = ms.Schedule?.ScheduleTime?.ToString("HH:mm"),
                    roomId = ms.CinemaRoomId,
                    dateId = ms.ShowDate.ToString("yyyy-MM-dd"),
                }).ToList();

            return Json(shows);
        }

        [HttpGet]
        public IActionResult GetMovieShowsByMovieVersionDate(string movieId, int versionId, string showDate) // NOSONAR - GET methods don't require ModelState.IsValid check
        {
            if (!DateOnly.TryParse(showDate, out var parsedDate))
                return BadRequest("Invalid date format.");

            var shows = _movieService.GetMovieShowsByMovieVersionDate(movieId, versionId, parsedDate)
                .Select(ms => new
                {
                    scheduleText = ms.Schedule?.ScheduleTime?.ToString("HH:mm"),
                    roomId = ms.CinemaRoomId,
                    dateId = ms.ShowDate.ToString("yyyy-MM-dd"),
                    versionId = ms.VersionId,
                }).ToList();

            return Json(shows);
        }

        [HttpGet]
        [Route("Movie/ViewShow/{id}")]
        public IActionResult ViewShow(string id)
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
                    showDate = ms.ShowDate.ToString("dd/MM/yyyy"),
                    ms.ScheduleId,
                    scheduleTime = ms.Schedule?.ScheduleTime.HasValue == true ? ms.Schedule.ScheduleTime.Value.ToString("HH:mm") : null,
                    ms.CinemaRoomId,
                    cinemaRoomName = ms.CinemaRoom?.CinemaRoomName,
                    ms.VersionId,
                    versionName = ms.Version?.VersionName
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
        public IActionResult DeleteMovieShowIfNotReferenced([FromBody] int movieShowId)
        {
            // Find the show
            var show = _movieService.GetMovieShowById(movieShowId);
            if (show == null)
                return Json(new { success = false, message = "Show not found." });

            // Check for references in Invoice
            bool isReferenced = show.Invoices != null && show.Invoices.Any();
            if (isReferenced)
                return Json(new { success = false, message = "Show is referenced by invoices." });

            // Delete the show
            bool deleted = _movieService.DeleteMovieShows(movieShowId);
            return Json(new { success = deleted, message = deleted ? "Show deleted." : "Failed to delete show." });
        }

        [HttpGet]
        public IActionResult GetDirectors() // NOSONAR - GET methods don't require ModelState.IsValid check
        {
            var directors = _personRepository.GetDirectors();
            return Json(directors.Select(d => new
            {
                id = d.PersonId,
                name = d.Name,
                image = d.Image
            }));
        }

        [HttpGet]
        public IActionResult GetActors() // NOSONAR - GET methods don't require ModelState.IsValid check
        {
            var actors = _personRepository.GetActors();
            return Json(actors.Select(a => new
            {
                id = a.PersonId,
                name = a.Name,
                image = a.Image
            }));
        }

        [HttpGet]
        public IActionResult GetAllMovies() // NOSONAR - GET methods don't require ModelState.IsValid check
        {
            var movies = _movieService.GetCurrentlyShowingMoviesWithDetails()
                .Select(m => new
                {
                    movieId = m.MovieId,
                    movieNameEnglish = m.MovieNameEnglish,
                    duration = m.Duration,
                    fromDate = m.FromDate,
                    toDate = m.ToDate
                }).ToList();
            return Json(movies);
        }

        [HttpGet]
        public IActionResult GetVersionsByMovie(string movieId) // NOSONAR - GET methods don't require ModelState.IsValid check
        {
            var movie = _movieService.GetById(movieId);
            if (movie == null)
                return NotFound();

            var versions = movie.Versions.Select(v => new
            {
                versionId = v.VersionId,
                versionName = v.VersionName
            }).ToList();
            return Json(versions);
        }

        [HttpGet]
        public IActionResult GetAvailableMovies() // NOSONAR - GET methods don't require ModelState.IsValid check
        {
            var today = DateOnly.FromDateTime(DateTime.Today);
            var movies = _movieService.GetCurrentlyShowingMoviesWithDetails()
                .Where(m => m.ToDate.HasValue && m.ToDate.Value > today)
                .Select(m => new
                {
                    movieId = m.MovieId,
                    movieNameEnglish = m.MovieNameEnglish,
                    duration = m.Duration,
                    fromDate = m.FromDate,
                    toDate = m.ToDate,
                    versions = m.Versions.Select(v => new
                    {
                        versionId = v.VersionId,
                        versionName = v.VersionName
                    }).ToList()
                }).ToList();
            return Json(movies);
        }

        [HttpGet]
        public IActionResult GetAvailableDatesForRoom(int cinemaRoomId, string movieId)
        {

            var movie = _movieService.GetById(movieId);
            if (movie == null)
            {
                return NotFound();
            }

            var room = _cinemaService.GetById(cinemaRoomId);
            if (room == null)
            {
                _logger.LogWarning($"Room not found: {cinemaRoomId}");
                return NotFound();
            }

            var today = DateOnly.FromDateTime(DateTime.Today);
            var availableDates = new List<object>();

            // Generate dates from movie's fromDate to toDate
            if (movie.FromDate.HasValue && movie.ToDate.HasValue)
            {
                for (var date = movie.FromDate.Value; date <= movie.ToDate.Value; date = date.AddDays(1))
                {
                    // Skip past dates
                    if (date < today)
                        continue;

                    // Check if room is available on this date
                    bool isRoomAvailable = true;

                    // If room is disabled (StatusId = 3), check the unavailable period
                    if (room.StatusId == 3 && room.UnavailableStartDate.HasValue && room.UnavailableEndDate.HasValue)
                    {
                        var unavailableStart = DateOnly.FromDateTime(room.UnavailableStartDate.Value);
                        var unavailableEnd = DateOnly.FromDateTime(room.UnavailableEndDate.Value);

                        // If date falls within unavailable period, skip it
                        if (date >= unavailableStart && date <= unavailableEnd)
                        {
                            isRoomAvailable = false;
                        }
                    }

                    if (isRoomAvailable)
                    {
                        availableDates.Add(new
                        {
                            value = date.ToString("yyyy-MM-dd"),
                            text = date.ToString("dd/MM/yyyy")
                        });
                    }
                }
            }

            return Json(new { availableDates });
        }
    }
}

