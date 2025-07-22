using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MovieTheater.Models;
using MovieTheater.Repository;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace MovieTheater.Controllers
{
    public class CastController : Controller
    {
        private readonly IPersonRepository _personRepository;

        public CastController(IPersonRepository personRepository)
        {
            _personRepository = personRepository;
        }

        // GET: CastController/Create
        public ActionResult Create()
        {
            return View(new Person());
        }

        // POST: CastController/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(Person person, IFormFile ImageFile)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    if (ImageFile != null && ImageFile.Length > 0)
                    {
                        var fileName = Guid.NewGuid().ToString() + Path.GetExtension(ImageFile.FileName);
                        var uploadPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images/avatars");
                        if (!Directory.Exists(uploadPath))
                        {
                            Directory.CreateDirectory(uploadPath);
                        }
                        var filePath = Path.Combine(uploadPath, fileName);
                        using (var stream = new FileStream(filePath, FileMode.Create))
                        {
                            ImageFile.CopyTo(stream);
                        }
                        person.Image = "/images/avatars/" + fileName;
                    }

                    _personRepository.Add(person);
                    _personRepository.Save();
                    TempData["ToastMessage"] = "Cast created successfully.";
                    return RedirectToAction("MainPage", "Admin", new { tab = "CastMg" });
                }
                catch (Exception ex)
                {
                    TempData["ErrorMessage"] = "Cast created unsuccessfully.";
                    ModelState.AddModelError("", "Unable to save changes. Try again, and if the problem persists, see your system administrator.");
                }
            }
            return View(person);
        }

        // GET: CastController/Edit/5
        public ActionResult Edit(int id)
        {
            var person = _personRepository.GetById(id);
            if (person == null)
            {
                return NotFound();
            }
            return View(person);
        }

        // POST: CastController/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(int id, Person person, IFormFile? ImageFile)
        {
            if (id != person.PersonId)
            {
                TempData["ErrorMessage"] = "ID mismatch error.";
                return NotFound();
            }

            // Remove ImageFile validation since it's optional
            ModelState.Remove("ImageFile");

            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Invalid model state: " + string.Join("; ", ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage));
                return View(person);
            }

            try
            {
                // Get existing person
                var existingPerson = _personRepository.GetById(id);
                if (existingPerson == null)
                {
                    TempData["ErrorMessage"] = "Person not found.";
                    return NotFound();
                }

                // Update all fields
                existingPerson.Name = person.Name;
                existingPerson.DateOfBirth = person.DateOfBirth;
                existingPerson.Nationality = person.Nationality;
                existingPerson.Gender = person.Gender;
                existingPerson.IsDirector = person.IsDirector;
                existingPerson.Description = person.Description;
                
                // Handle image update
                if (ImageFile != null && ImageFile.Length > 0)
                {
                    var fileName = Guid.NewGuid().ToString() + Path.GetExtension(ImageFile.FileName);
                    var uploadPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images/avatars");
                    if (!Directory.Exists(uploadPath))
                    {
                        Directory.CreateDirectory(uploadPath);
                    }
                    var filePath = Path.Combine(uploadPath, fileName);
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        ImageFile.CopyTo(stream);
                    }
                    existingPerson.Image = "/images/avatars/" + fileName;
                }
                else
                {
                    // Keep existing image
                    existingPerson.Image = person.Image;
                }

                try
                {
                    _personRepository.Update(existingPerson);
                    _personRepository.Save();
                    TempData["ToastMessage"] = "Cast updated successfully.";
                    return RedirectToAction("MainPage", "Admin", new { tab = "CastMg" });
                }
                catch (Exception dbEx)
                {
                    TempData["ErrorMessage"] = $"Database error: {dbEx.Message}";
                    return View(person);
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error updating cast: {ex.Message}";
                ModelState.AddModelError("", "Unable to save changes. Try again, and if the problem persists, see your system administrator.");
                return View(person);
            }
        }

        // POST: CastController/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id, IFormCollection collection)
        {
            try
            {
                var cast = _personRepository.GetById(id);
                if (cast == null)
                {
                    TempData["ToastMessage"] = "Cast not found.";
                    return RedirectToAction("MainPage", "Admin", new { tab = "CastMg" });
                }

                _personRepository.Delete(id);
                TempData["ToastMessage"] = "Cast deleted successfully!";
                return RedirectToAction("MainPage", "Admin", new { tab = "CastMg" });
            }
            catch (Exception ex)
            {
                TempData["ToastMessage"] = $"An error occurred during deletion: {ex.Message}";
                return RedirectToAction("MainPage", "Admin", new { tab = "CastMg" });
            }
        }
    }
}
