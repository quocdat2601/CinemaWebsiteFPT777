using Microsoft.AspNetCore.Mvc;
using MovieTheater.Repository;
using Version = MovieTheater.Models.Version;
using Microsoft.AspNetCore.Authorization;

namespace MovieTheater.Controllers
{
    [Authorize(Roles = "Admin")]
    public class VersionController : Controller
    {
        private readonly IVersionRepository _versionRepo;

        public VersionController(IVersionRepository versionRepo)
        {
            _versionRepo = versionRepo;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(Version model)
        {
            if (ModelState.IsValid)
            {
                _versionRepo.Add(model);
                _versionRepo.Save();
                TempData["ToastMessage"] = "Version created successfully!";
                // Redirect to the page that shows the version list
                return RedirectToAction("MainPage", "Admin", new { tab = "VersionMg" });
            }
            TempData["ErrorMessage"] = "Invalid data!";
            return RedirectToAction("MainPage", "Admin", new { tab = "VersionMg" });
        }

        [HttpPost]
        public IActionResult Edit([FromBody] Version model)
        {
            if (ModelState.IsValid)
            {
                _versionRepo.Update(model);
                TempData["ToastMessage"] = "Version updated successfully!";
                return Json(new { success = true });
            }
            TempData["ErrorMessage"] = "Version update unsuccessful!";
            return Json(new { success = false, error = "Invalid data" });
        }

        // POST: VersionController/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id, IFormCollection collection)
        {
            try
            {
                var version = _versionRepo.GetById(id);
                if (version == null)
                {
                    TempData["ToastMessage"] = "Version not found.";
                    return RedirectToAction("MainPage", "Admin", new { tab = "VersionMg" });
                }

                bool success = _versionRepo.Delete(id);

                if (!success)
                {
                    TempData["ErrorMessage"] = "Failed to delete version.";
                    return RedirectToAction("MainPage", "Admin", new { tab = "VersionMg" });
                }

                TempData["ToastMessage"] = "Version deleted successfully!";
                return RedirectToAction("MainPage", "Admin", new { tab = "VersionMg" });
            }
            catch (Exception ex)
            {
                TempData["ToastMessage"] = $"An error occurred during deletion: {ex.Message}";
                return RedirectToAction("MainPage", "Admin", new { tab = "VersionMg" });
            }
        }

        [HttpGet]
        public IActionResult Get(int id)
        {
            var version = _versionRepo.GetById(id);
            if (version == null) return NotFound();
            return Json(version);
        }
    }
}
