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

            if (model.SmallImageFile != null && model.SmallImageFile.Length > 0)
            {
                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "image");
                Directory.CreateDirectory(uploadsFolder); 

                var uniqueFileName = Guid.NewGuid().ToString() + Path.GetExtension(model.SmallImageFile.FileName);
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await model.SmallImageFile.CopyToAsync(stream);
                }
                model.SmallImage = "/image/" + uniqueFileName;
            }
            if (model.LargeImageFile != null && model.LargeImageFile.Length > 0)
            {
                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "image");
                Directory.CreateDirectory(uploadsFolder);

                var uniqueFileName2 = Guid.NewGuid().ToString() + Path.GetExtension(model.LargeImageFile.FileName);
                var filePath = Path.Combine(uploadsFolder, uniqueFileName2);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await model.LargeImageFile.CopyToAsync(stream);
                }
                model.LargeImage = "/image/" + uniqueFileName2;
            }

            bool success = _movieService.AddMovie(model);

            if (!success)
            {
                ModelState.AddModelError("", "Failed to create movie.");
                return View(model);
            }

            TempData["ToastMessage"] = "Movie created successfully!";
            return RedirectToAction("MainPage", "Admin", new { tab = "MovieMg" });
        }

        // GET: Movie/Edit/5
        public async Task<IActionResult> Edit(string id)
        {
            var movie = _movieService.GetById(id);
            if (movie == null)
                return NotFound();

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
        public async Task<IActionResult> Edit(string id, MovieDetailViewModel model)
        {
            if (!ModelState.IsValid)
            {
                model.AvailableSchedules = await _movieService.GetSchedulesAsync();
                model.AvailableShowDates = await _movieService.GetShowDatesAsync();
                model.AvailableTypes = await _movieService.GetTypesAsync();
                return View(model);
            }

            // Get the existing movie to preserve images
            var existingMovie = _movieService.GetById(id);
            if (existingMovie != null)
            {
                // Preserve existing images if no new ones are uploaded
                if (string.IsNullOrEmpty(model.SmallImage))
                    model.SmallImage = existingMovie.SmallImage;
                if (string.IsNullOrEmpty(model.LargeImage))
                    model.LargeImage = existingMovie.LargeImage;
            }

            // Handle new SmallImageFile upload
            if (model.SmallImageFile != null && model.SmallImageFile.Length > 0)
            {
                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "image");
                Directory.CreateDirectory(uploadsFolder);

                var uniqueFileName = Guid.NewGuid().ToString() + Path.GetExtension(model.SmallImageFile.FileName);
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await model.SmallImageFile.CopyToAsync(stream);
                }
                model.SmallImage = "/image/" + uniqueFileName;
            }

            // Handle new LargeImageFile upload
            if (model.LargeImageFile != null && model.LargeImageFile.Length > 0)
            {
                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "image");
                Directory.CreateDirectory(uploadsFolder);

                var uniqueFileName2 = Guid.NewGuid().ToString() + Path.GetExtension(model.LargeImageFile.FileName);
                var filePath = Path.Combine(uploadsFolder, uniqueFileName2);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await model.LargeImageFile.CopyToAsync(stream);
                }
                model.LargeImage = "/image/" + uniqueFileName2;
            }

            // Perform the update
            bool success = _movieService.UpdateMovie(id, model);

            if (!success)
            {
                ModelState.AddModelError("", "Failed to update movie.");
                return View(model);
            }

            TempData["ToastMessage"] = "Movie updated successfully!";
            return RedirectToAction("MainPage", "Admin", new { tab = "MovieMg" });
        }

        // GET: Movie/Delete/5
        [HttpGet]
        public IActionResult Delete(string id)
        {
            var movie = _movieService.GetById(id);
            if (movie == null)
            {
                TempData["ToastMessage"] = "Movie not found.";
                return RedirectToAction("MainPage", "Admin", new { tab = "MovieMg" });
            }

            return View(movie);
        }
        
        // POST: Movie/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Delete(string id, IFormCollection collection)
        {
            try
            {
                if (string.IsNullOrEmpty(id))
                {
                    TempData["ToastMessage"] = "Invalid movie ID.";
                    return RedirectToAction("MainPage", "Admin", new { tab = "MovieMg" });
                }

                var movie = _movieService.GetById(id);
                if (movie == null)
                {
                    TempData["ToastMessage"] = "Movie not found.";
                    return RedirectToAction("MainPage", "Admin", new { tab = "MovieMg" });
                }

                // Force delete by clearing all relationships first
                movie.Schedules?.Clear();
                movie.Types?.Clear();
                movie.ShowDates?.Clear();
                
                bool success = _movieService.DeleteMovie(id);

                if (!success)
                {
                    TempData["ToastMessage"] = "Failed to delete movie.";
                    return RedirectToAction("MainPage", "Admin", new { tab = "MovieMg" });
                }

                TempData["ToastMessage"] = "Movie deleted successfully!";
                return RedirectToAction("MainPage", "Admin", new { tab = "MovieMg" });
            }
            catch (Exception ex)
            {
                TempData["ToastMessage"] = $"An error occurred during deletion: {ex.Message}";
                return RedirectToAction("MainPage", "Admin", new { tab = "MovieMg" });
            }
        }

    }
}
