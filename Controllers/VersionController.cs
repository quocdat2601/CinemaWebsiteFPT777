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

        // Constants for string literals
        private const string TOAST_MESSAGE = "ToastMessage";
        private const string ERROR_MESSAGE = "ErrorMessage";
        private const string MAIN_PAGE = "MainPage";
        private const string ADMIN_CONTROLLER = "Admin";
        private const string VERSION_MG_TAB = "VersionMg";

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
                TempData[TOAST_MESSAGE] = "Version created successfully!";
                // Redirect to the page that shows the version list
                return RedirectToAction(MAIN_PAGE, ADMIN_CONTROLLER, new { tab = VERSION_MG_TAB });
            }
            TempData[ERROR_MESSAGE] = "Invalid data!";
            return RedirectToAction(MAIN_PAGE, ADMIN_CONTROLLER, new { tab = VERSION_MG_TAB });
        }

        [HttpPost]
        public IActionResult Edit([FromBody] Version model)
        {
            if (ModelState.IsValid)
            {
                _versionRepo.Update(model);
                TempData[TOAST_MESSAGE] = "Version updated successfully!";
                return Json(new { success = true });
            }
            TempData[ERROR_MESSAGE] = "Version update unsuccessful!";
            return Json(new { success = false, error = "Invalid data" });
        }

        // POST: VersionController/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id, IFormCollection collection)
        {
            if (!ModelState.IsValid)
            {
                return RedirectToAction(MAIN_PAGE, ADMIN_CONTROLLER, new { tab = VERSION_MG_TAB });
            }
            try
            {
                var version = _versionRepo.GetById(id);
                if (version == null)
                {
                    TempData[TOAST_MESSAGE] = "Version not found.";
                    return RedirectToAction(MAIN_PAGE, ADMIN_CONTROLLER, new { tab = VERSION_MG_TAB });
                }

                bool success = _versionRepo.Delete(id);

                if (!success)
                {
                    TempData[ERROR_MESSAGE] = "Failed to delete version.";
                    return RedirectToAction(MAIN_PAGE, ADMIN_CONTROLLER, new { tab = VERSION_MG_TAB });
                }

                TempData[TOAST_MESSAGE] = "Version deleted successfully!";
                return RedirectToAction(MAIN_PAGE, ADMIN_CONTROLLER, new { tab = VERSION_MG_TAB });
            }
            catch (Exception ex)
            {
                TempData[TOAST_MESSAGE] = $"An error occurred during deletion: {ex.Message}";
                return RedirectToAction(MAIN_PAGE, ADMIN_CONTROLLER, new { tab = VERSION_MG_TAB });
            }
        }

        [HttpGet]
        public IActionResult Get(int id) // NOSONAR - GET methods don't require ModelState.IsValid check
        {
            var version = _versionRepo.GetById(id);
            if (version == null) return NotFound();
            return Json(version);
        }
    }
}
