using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MovieTheater.Services;
using MovieTheater.ViewModels;

namespace MovieTheater.Controllers
{
    public class AdminController : Controller
    {
        private readonly IMovieService _movieService;
        // GET: AdminController
        [RoleAuthorize(new[] { 1 })] // Only Employee
        public IActionResult Dashboard()
        {
            return View();
        }

        // GET: AdminController/Details/5
        public ActionResult Details(int id)
        {
            return View();
        }

        // GET: AdminController/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: AdminController/Create
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

        // GET: Movie/Edit/5
        public ActionResult Edit(string id)
        {
            // 1. Fetch movie data by ID
            var movie = _movieService.GetById(id);
            if (movie == null)
                return NotFound();

            return View();
        }


        // POST: Movie/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, MovieDetailViewModel model)
        {
            if (!ModelState.IsValid)
            {
                // Re-populate dropdowns, lists, etc., if needed
                return View(model);
            }

            try
            {
                // 1. Fetch existing movie from DB
                var movie = _movieService.GetById(id);
                if (movie == null)
                    return NotFound();

                // 2. Update movie fields from model

                // update other fields...

                // 3. Handle SmallImage upload
                //if (model.SmallImage != null && model.SmallImage.Length > 0)
                //{
                //    // e.g., save to wwwroot/uploads or cloud storage
                //    var smallImageFileName = Path.GetFileName(model.SmallImage.FileName);
                //    var smallImagePath = Path.Combine("wwwroot/uploads", smallImageFileName);

                //    using (var stream = new FileStream(smallImagePath, FileMode.Create))
                //    {
                //        await model.SmallImage.CopyToAsync(stream);
                //    }

                //    // Update movie.SmallImageUrl or similar property with the path or URL
                //    movie.SmallImageUrl = "/uploads/" + smallImageFileName;
                //}

                //// 4. Handle LargeImage upload similarly
                //if (model.LargeImage != null && model.LargeImage.Length > 0)
                //{
                //    var largeImageFileName = Path.GetFileName(model.LargeImage.FileName);
                //    var largeImagePath = Path.Combine("wwwroot/uploads", largeImageFileName);

                //    using (var stream = new FileStream(largeImagePath, FileMode.Create))
                //    {
                //        await model.LargeImage.CopyToAsync(stream);
                //    }

                //    movie.LargeImageUrl = "/uploads/" + largeImageFileName;
                //}

                // 5. Save changes to DB
                _movieService.UpdateMovie(movie);

                TempData["ToastMessage"] = "Movie updated successfully!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                // Log error here
                ModelState.AddModelError("", "An error occurred while updating the movie.");
                return View(model);
            }
        }


        // GET: AdminController/Delete/5
        public ActionResult Delete(int id)
        {
            return View();
        }

        // POST: AdminController/Delete/5
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
