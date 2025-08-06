using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MovieTheater.Models;
using MovieTheater.Repository;
using MovieTheater.ViewModels;
using System.Security.Claims;

namespace MovieTheater.Controllers
{

    public class CastController : Controller
    {
        private readonly IPersonRepository _personRepository;
        public string role => User?.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value;

        public CastController(IPersonRepository personRepository)
        {
            _personRepository = personRepository;
        }

        // GET: CastController/Detail/5
        public ActionResult Detail(int id)
        {
            var person = _personRepository.GetById(id);
            if (person == null)
            {
                return NotFound();
            }

            var movies = _personRepository.GetMovieByPerson(id);
            var viewModel = new CastDetailViewModel
            {
                Person = person,
                Movies = movies
            };

            return View(viewModel);
        }

        // GET: CastController/Create
        [Authorize(Roles = "Admin, Employee")]
        public ActionResult Create()
        {
            return View(new PersonFormModel());
        }

        // POST: CastController/Create
        [Authorize(Roles = "Admin, Employee")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(PersonFormModel person, IFormFile ImageFile)
        {
            // Remove ImageFile from validation since it's optional
            ModelState.Remove("ImageFile");


            if (!ModelState.IsValid)
            {
                var errors = ModelState
                    .Where(x => x.Value.Errors.Count > 0)
                    .Select(x => new { Field = x.Key, Errors = x.Value.Errors.Select(e => e.ErrorMessage) })
                    .ToList();

                TempData["ValidationErrors"] = string.Join("; ", errors.SelectMany(e => e.Errors));
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Create new Person entity from PersonFormModel
                    var newPerson = new Person
                    {
                        Name = person.Name,
                        DateOfBirth = person.DateOfBirth ?? DateOnly.FromDateTime(DateTime.Now),
                        Nationality = person.Nationality,
                        Gender = person.Gender,
                        IsDirector = person.IsDirector,
                        Description = person.Description
                    };

                    // Handle image upload
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
                        newPerson.Image = "/images/avatars/" + fileName;
                    }
                    else
                    {
                        newPerson.Image = "/image/default-movie.png";
                    }

                    _personRepository.Add(newPerson);
                    _personRepository.Save();
                    TempData["ToastMessage"] = "Cast created successfully.";
                    if (role == "Admin")
                        return RedirectToAction("MainPage", "Admin", new { tab = "CastMg" });
                    else
                        return RedirectToAction("MainPage", "Employee", new { tab = "CastMg" });
                }
                catch (Exception ex)
                {
                    TempData["ErrorMessage"] = "Cast created unsuccessfully.";
                    ModelState.AddModelError("", "Unable to save changes. Try again, and if the problem persists, see your system administrator.");
                    return View(person);
                }
            }
            return View(person);
        }

        // GET: CastController/Edit/5
        [Authorize(Roles = "Admin, Employee")]
        public ActionResult Edit(int id)
        {
            var person = _personRepository.GetById(id);
            if (person == null)
            {
                return NotFound();
            }

            // Map Person to PersonFormModel for the view
            var personFormModel = new PersonFormModel
            {
                PersonId = person.PersonId,
                Name = person.Name,
                DateOfBirth = person.DateOfBirth ?? DateOnly.FromDateTime(DateTime.Now),
                Nationality = person.Nationality ?? string.Empty,
                Gender = person.Gender,
                IsDirector = person.IsDirector,
                Description = person.Description ?? string.Empty,
                Image = person.Image
            };

            return View(personFormModel);
        }

        // POST: CastController/Edit/5
        [Authorize(Roles = "Admin, Employee")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(int id, PersonFormModel person, IFormFile? ImageFile)
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
                var existingPerson = _personRepository.GetById(id);
                if (existingPerson == null)
                {
                    TempData["ErrorMessage"] = "Person not found.";
                    return NotFound();
                }

                // Update all fields
                existingPerson.Name = person.Name;
                existingPerson.DateOfBirth = person.DateOfBirth ?? DateOnly.FromDateTime(DateTime.Now);
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
                    if (role == "Admin")
                        return RedirectToAction("MainPage", "Admin", new { tab = "CastMg" });
                    else
                        return RedirectToAction("MainPage", "Employee", new { tab = "CastMg" });
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
        [Authorize(Roles = "Admin, Employee")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id, IFormCollection collection)
        {
            try
            {
                var cast = _personRepository.GetById(id);
                if (cast == null)
                {
                    TempData["ErrorMessage"] = "Cast not found.";
                    if (role == "Admin")
                        return RedirectToAction("MainPage", "Admin", new { tab = "CastMg" });
                    else
                        return RedirectToAction("MainPage", "Employee", new { tab = "CastMg" });
                }

                // Check if person is associated with any movies
                var movies = _personRepository.GetMovieByPerson(id);
                if (movies != null && movies.Any())
                {
                    // Remove person from all movies first
                    _personRepository.RemovePersonFromAllMovies(id);
                    _personRepository.Save();
                    
                    TempData["ToastMessage"] = $"Successfully removed {cast.Name} from {movies.Count()} movie(s) and deleted the cast member.";
                }
                else
                {
                    TempData["ToastMessage"] = $"Successfully deleted {cast.Name}.";
                }

                _personRepository.Delete(id);
                _personRepository.Save(); // Ensure changes are committed
                
                if (role == "Admin")
                    return RedirectToAction("MainPage", "Admin", new { tab = "CastMg" });
                else
                    return RedirectToAction("MainPage", "Employee", new { tab = "CastMg" });
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"An error occurred during deletion: {ex.Message}. Details: {ex.InnerException?.Message}";
                if (role == "Admin")
                    return RedirectToAction("MainPage", "Admin", new { tab = "CastMg" });
                else
                    return RedirectToAction("MainPage", "Employee", new { tab = "CastMg" });
            }
        }
    }
}
