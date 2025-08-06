using Microsoft.AspNetCore.Http; // For DefaultHttpContext, IFormFile
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures; // For TempDataDictionary, ITempDataProvider
using Microsoft.AspNetCore.Routing; // For RouteData
using Moq;
using MovieTheater.Controllers;
using MovieTheater.Models;
using MovieTheater.Repository;
using MovieTheater.ViewModels;
using System;
using System.Collections.Generic;
using System.IO; // For MemoryStream
using System.Linq;
using System.Security.Claims;
using Xunit;

namespace MovieTheater.Tests.Controller
{
    public class CastControllerTests
    {
        private readonly Mock<IPersonRepository> _mockPersonRepository;

        public CastControllerTests()
        {
            _mockPersonRepository = new Mock<IPersonRepository>();
        }

        private CastController BuildController(string role = null)
        {
            var controller = new CastController(_mockPersonRepository.Object);

            var httpContext = new DefaultHttpContext();

            if (role != null)
            {
                var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Role, role)
        };
                httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "mock"));
            }

            var tempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>());
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext,
                RouteData = new RouteData()
            };
            controller.TempData = tempData;

            return controller;
        }


        // --- Detail Action Tests ---

        [Fact]
        public void Detail_ReturnsViewWithModel_WhenPersonExists()
        {
            // Arrange
            int personId = 1;
            var person = new Person { PersonId = personId, Name = "Tom Hanks" };
            var movies = new List<Movie> { new Movie { MovieId = "MV001", MovieNameEnglish = "Forrest Gump" } };

            _mockPersonRepository.Setup(r => r.GetById(personId)).Returns(person);
            _mockPersonRepository.Setup(r => r.GetMovieByPerson(personId)).Returns(movies);

            var controller = BuildController();

            // Act
            var result = controller.Detail(personId);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<CastDetailViewModel>(viewResult.Model);
            Assert.Equal(person, model.Person);
            Assert.Equal(movies, model.Movies);
        }

        [Fact]
        public void Detail_ReturnsNotFound_WhenPersonDoesNotExist()
        {
            // Arrange
            int personId = 99; // Non-existent ID
            _mockPersonRepository.Setup(r => r.GetById(personId)).Returns((Person)null);

            var controller = BuildController();

            // Act
            var result = controller.Detail(personId);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        // --- Create (GET) Action Tests ---

        [Fact]
        public void Create_Get_ReturnsViewWithNewPersonFormModel()
        {
            // Arrange
            var controller = BuildController();

            // Act
            var result = controller.Create();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.IsType<PersonFormModel>(viewResult.Model);
            // Ensure properties are default or empty
            var model = (PersonFormModel)viewResult.Model;
            Assert.Null(model.Name); // Or whatever default your PersonFormModel has
            Assert.Null(model.DateOfBirth);
        }

        // --- Create (POST) Action Tests ---

        [Fact]
        public void Create_Post_RedirectsToAdminPage_WhenModelIsValidAndImageProvided()
        {
            // Arrange
            var personFormModel = new PersonFormModel
            {
                Name = "New Cast",
                DateOfBirth = new DateOnly(1980, 1, 1),
                Nationality = "USA",
                Gender = false, // As per your PersonFormModel, this is a bool?
                IsDirector = false,
                Description = "A talented actor."
            };

            // Mock IFormFile
            var mockFile = new Mock<IFormFile>();
            var fileName = "test.jpg";
            var content = "This is a dummy image file.";
            var ms = new MemoryStream();
            var writer = new StreamWriter(ms);
            writer.Write(content);
            writer.Flush();
            ms.Position = 0;
            mockFile.Setup(_ => _.FileName).Returns(fileName);
            mockFile.Setup(_ => _.Length).Returns(content.Length);
            mockFile.Setup(_ => _.CopyTo(It.IsAny<Stream>())).Callback<Stream>((stream) => ms.CopyTo(stream));

            // Setup repository mocks
            _mockPersonRepository.Setup(r => r.Add(It.IsAny<Person>()));
            _mockPersonRepository.Setup(r => r.Save());

            var controller = BuildController();

            // Ensure the target directory for image uploads exists for the test
            var uploadPath = Path.Combine(Path.GetTempPath(), "wwwroot", "images", "avatars");
            if (!Directory.Exists(uploadPath))
            {
                Directory.CreateDirectory(uploadPath);
            }

            // Act
            var result = controller.Create(personFormModel, mockFile.Object);

            var redirectToActionResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("MainPage", redirectToActionResult.ActionName);
            Assert.Equal("Employee", redirectToActionResult.ControllerName);
            Assert.Equal("CastMg", redirectToActionResult.RouteValues["tab"]);

            _mockPersonRepository.Verify(r => r.Add(It.IsAny<Person>()), Times.Once());
            _mockPersonRepository.Verify(r => r.Save(), Times.Once());
            Assert.Equal("Cast created successfully.", controller.TempData["ToastMessage"]);

            // Clean up the created dummy directory/files (optional but good practice)
            Directory.Delete(uploadPath, true);
        }

        [Fact]
        public void Create_Post_RedirectsToAdminPage_WhenModelIsValidAndNoImageProvided()
        {
            // Arrange
            var personFormModel = new PersonFormModel
            {
                Name = "New Cast No Image",
                DateOfBirth = new DateOnly(1990, 2, 2),
                Nationality = "UK",
                Gender = true,
                IsDirector = true,
                Description = "A great director."
            };

            // No IFormFile provided (null)
            IFormFile? ImageFile = null;

            _mockPersonRepository.Setup(r => r.Add(It.IsAny<Person>()));
            _mockPersonRepository.Setup(r => r.Save());

            var controller = BuildController();

            // Act
            var result = controller.Create(personFormModel, ImageFile);

            // Assert
            var redirectToActionResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("MainPage", redirectToActionResult.ActionName);
            Assert.Equal("Employee", redirectToActionResult.ControllerName);
            Assert.Equal("CastMg", redirectToActionResult.RouteValues["tab"]);

            _mockPersonRepository.Verify(r => r.Add(It.Is<Person>(p => p.Image == "/image/default-movie.png")), Times.Once());
            _mockPersonRepository.Verify(r => r.Save(), Times.Once());
            Assert.Equal("Cast created successfully.", controller.TempData["ToastMessage"]);
        }

        [Fact]
        public void Create_Post_ReturnsViewWithModel_WhenModelIsInvalid()
        {
            // Arrange
            var personFormModel = new PersonFormModel
            {
                Name = null // Make model invalid
            };

            var mockFile = new Mock<IFormFile>(); // Can be null or mocked, won't affect ModelState.IsValid for Name

            var controller = BuildController();
            controller.ModelState.AddModelError("Name", "Name is required."); // Simulate validation error

            // Act
            var result = controller.Create(personFormModel, mockFile.Object);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<PersonFormModel>(viewResult.Model);
            Assert.Equal(personFormModel, model);
            Assert.False(controller.ModelState.IsValid);
            Assert.Contains("Name is required.", controller.TempData["ValidationErrors"].ToString()); // Check the specific TempData error
            _mockPersonRepository.Verify(r => r.Add(It.IsAny<Person>()), Times.Never());
            _mockPersonRepository.Verify(r => r.Save(), Times.Never());
        }

        [Fact]
        public void Create_Post_ReturnsViewWithErrorMessage_WhenExceptionOccurs()
        {
            // Arrange
            var personFormModel = new PersonFormModel
            {
                Name = "Valid Name",
                DateOfBirth = new DateOnly(2000, 1, 1),
                Nationality = "Test",
                Gender = false,
                IsDirector = false,
                Description = "Test Description"
            };

            var mockFile = new Mock<IFormFile>();
            mockFile.Setup(_ => _.FileName).Returns("test.jpg");
            mockFile.Setup(_ => _.Length).Returns(100);
            mockFile.Setup(_ => _.CopyTo(It.IsAny<Stream>())).Throws(new Exception("File save error")); // Simulate file save error

            // Simulate repository error on Save (or Add)
            _mockPersonRepository.Setup(r => r.Add(It.IsAny<Person>())).Throws(new Exception("Database add error"));
            // If the exception occurs during file copy, Add might not even be called, but for robust testing, mock both paths if applicable.

            var controller = BuildController();

            // Ensure the target directory for image uploads exists for the test, even if it fails later
            var uploadPath = Path.Combine(Path.GetTempPath(), "wwwroot", "images", "avatars");
            if (!Directory.Exists(uploadPath))
            {
                Directory.CreateDirectory(uploadPath);
            }

            // Act
            var result = controller.Create(personFormModel, mockFile.Object);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.IsType<PersonFormModel>(viewResult.Model);
            Assert.Equal("Cast created unsuccessfully.", controller.TempData["ErrorMessage"]);
            Assert.False(controller.ModelState.IsValid); // An error was added to ModelState
            Assert.True(controller.ModelState.ContainsKey("")); // Check for model-level error
            Assert.Contains("Unable to save changes. Try again, and if the problem persists, see your system administrator.", controller.ModelState[""].Errors.First().ErrorMessage);

            // Clean up
            Directory.Delete(uploadPath, true);
        }

        // --- Edit (GET) Action Tests ---

        [Fact]
        public void Edit_Get_ReturnsViewWithModel_WhenPersonExists()
        {
            // Arrange
            int personId = 1;
            var existingPerson = new Person
            {
                PersonId = personId,
                Name = "Original Name",
                DateOfBirth = new DateOnly(1970, 5, 15),
                Nationality = "German",
                Gender = true,
                IsDirector = true,
                Description = "Veteran director",
                Image = "/images/avatars/original.jpg"
            };
            _mockPersonRepository.Setup(r => r.GetById(personId)).Returns(existingPerson);

            var controller = BuildController();

            // Act
            var result = controller.Edit(personId);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<PersonFormModel>(viewResult.Model);
            Assert.Equal(existingPerson.PersonId, model.PersonId);
            Assert.Equal(existingPerson.Name, model.Name);
            Assert.Equal(existingPerson.DateOfBirth, model.DateOfBirth);
            Assert.Equal(existingPerson.Nationality, model.Nationality);
            Assert.Equal(existingPerson.Gender, model.Gender);
            Assert.Equal(existingPerson.IsDirector, model.IsDirector);
            Assert.Equal(existingPerson.Description, model.Description);
            Assert.Equal(existingPerson.Image, model.Image);
        }
        [Fact]
        public void Edit_Post_ValidPerson_UpdatesAndRedirects()
        {
            // Arrange
            var personId = 1;
            var existingPerson = new Person
            {
                PersonId = personId,
                Name = "Old Name",
                DateOfBirth = new DateOnly(1980, 1, 1),
                Nationality = "Old Country",
                Gender = true,
                IsDirector = false,
                Description = "Old Description",
                Image = "/images/avatars/old.jpg"
            };

            var model = new PersonFormModel
            {
                PersonId = personId,
                Name = "New Name",
                DateOfBirth = new DateOnly(1990, 5, 5),
                Nationality = "New Country",
                Gender = true,
                IsDirector = true,
                Description = "Updated description",
                Image = "/images/avatars/old.jpg" // kept same if no new file
            };

            var mockRepo = new Mock<IPersonRepository>();
            mockRepo.Setup(r => r.GetById(personId)).Returns(existingPerson);

            var controller = new CastController(mockRepo.Object);

            // Set up HttpContext, role, and TempData
            var httpContext = new DefaultHttpContext();
            httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(new[] {
        new Claim(ClaimTypes.Role, "Employee")
    }, "mock"));

            var tempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>());
            controller.ControllerContext = new ControllerContext()
            {
                HttpContext = httpContext
            };
            controller.TempData = tempData;

            // Manually clear ImageFile validation if needed (simulating what controller does)
            controller.ModelState.Remove("ImageFile");

            // Act
            var result = controller.Edit(personId, model, null);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("MainPage", redirectResult.ActionName);
            Assert.Equal("Employee", redirectResult.ControllerName);
            Assert.Equal("CastMg", redirectResult.RouteValues["tab"]);

            mockRepo.Verify(r => r.Update(It.Is<Person>(p =>
                p.PersonId == personId &&
                p.Name == model.Name &&
                p.DateOfBirth == model.DateOfBirth &&
                p.Nationality == model.Nationality &&
                p.Gender == model.Gender &&
                p.IsDirector == model.IsDirector &&
                p.Description == model.Description
            )), Times.Once);

            mockRepo.Verify(r => r.Save(), Times.Once);
        }
        [Fact]
        public void Create_Post_ValidPerson_RedirectsToMainPage()
        {
            // Arrange
            var controller = BuildController("Admin");

            var person = new PersonFormModel
            {
                Name = "Test Name",
                DateOfBirth = DateOnly.FromDateTime(DateTime.Now.AddYears(-30)),
                Nationality = "Test Country",
                Gender = false,
                IsDirector = false,
                Description = "Test description"
            };

            var mockFile = new Mock<IFormFile>();
            mockFile.Setup(f => f.Length).Returns(0); // No file uploaded

            _mockPersonRepository.Setup(r => r.Add(It.IsAny<Person>()));
            _mockPersonRepository.Setup(r => r.Save());

            // Act
            var result = controller.Create(person, mockFile.Object);

            // Assert
            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("MainPage", redirect.ActionName);
            Assert.Equal("Admin", redirect.ControllerName);
            Assert.Equal("CastMg", redirect.RouteValues["tab"]);
        }


        [Fact]
        public void Edit_Get_ReturnsNotFound_WhenPersonDoesNotExist()
        {
            // Arrange
            int personId = 99;
            _mockPersonRepository.Setup(r => r.GetById(personId)).Returns((Person)null);

            var controller = BuildController();

            // Act
            var result = controller.Edit(personId);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        // --- Edit (POST) Action Tests ---

        [Fact]
        public void Edit_Post_RedirectsToAdminPage_WhenModelIsValidAndImageProvided()
        {
            // Arrange
            int personId = 1;
            var existingPerson = new Person { PersonId = personId, Name = "Old Name", Image = "/images/avatars/old.jpg" };
            var updatedPersonFormModel = new PersonFormModel
            {
                PersonId = personId,
                Name = "Updated Name",
                DateOfBirth = new DateOnly(1985, 3, 10),
                Nationality = "French",
                Gender = false,
                IsDirector = false,
                Description = "Updated actor description"
            };

            // Mock IFormFile for new image
            var mockFile = new Mock<IFormFile>();
            var fileName = "new_test.png";
            var content = "New image data.";
            var ms = new MemoryStream();
            var writer = new StreamWriter(ms);
            writer.Write(content);
            writer.Flush();
            ms.Position = 0;
            mockFile.Setup(_ => _.FileName).Returns(fileName);
            mockFile.Setup(_ => _.Length).Returns(content.Length);
            mockFile.Setup(_ => _.CopyTo(It.IsAny<Stream>())).Callback<Stream>((stream) => ms.CopyTo(stream));

            _mockPersonRepository.Setup(r => r.GetById(personId)).Returns(existingPerson);
            _mockPersonRepository.Setup(r => r.Update(It.IsAny<Person>()));
            _mockPersonRepository.Setup(r => r.Save());

            var controller = BuildController("Admin");

            // Ensure the target directory for image uploads exists for the test
            var uploadPath = Path.Combine(Path.GetTempPath(), "wwwroot", "images", "avatars");
            if (!Directory.Exists(uploadPath))
            {
                Directory.CreateDirectory(uploadPath);
            }

            // Act
            var result = controller.Edit(personId, updatedPersonFormModel, mockFile.Object);

            // Assert
            var redirectToActionResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("MainPage", redirectToActionResult.ActionName);
            Assert.Equal("Admin", redirectToActionResult.ControllerName);
            Assert.Equal("CastMg", redirectToActionResult.RouteValues["tab"]);

            _mockPersonRepository.Verify(r => r.Update(It.Is<Person>(p =>
                p.Name == updatedPersonFormModel.Name &&
                p.Image.Contains("/images/avatars/") && // Check if image path was updated
                p.Image.Contains(Path.GetExtension(fileName)) // Check for the new file extension
            )), Times.Once());
            _mockPersonRepository.Verify(r => r.Save(), Times.Once());
            Assert.Equal("Cast updated successfully.", controller.TempData["ToastMessage"]);

            // Clean up
            Directory.Delete(uploadPath, true);
        }
        [Fact]
        public void Edit_ReturnsFormModel_WithFallbackValues_WhenPersonFieldsAreNull()
        {
            // Arrange
            var personId = 1;
            var person = new Person
            {
                PersonId = personId,
                Name = "Null Fields",
                DateOfBirth = null,             // Trigger fallback
                Nationality = null,             // Trigger fallback
                Gender = true,
                IsDirector = false,
                Description = null,             // Trigger fallback
                Image = "some.jpg"
            };

            _mockPersonRepository.Setup(r => r.GetById(personId)).Returns(person);

            var controller = BuildController("Admin");

            // Act
            var result = controller.Edit(personId);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<PersonFormModel>(viewResult.Model);

            Assert.Equal(personId, model.PersonId);
            Assert.True(model.DateOfBirth >= DateOnly.FromDateTime(DateTime.Now).AddDays(-1));
            Assert.Equal(string.Empty, model.Nationality);
            Assert.Equal(string.Empty, model.Description);
        }

        [Fact]
        public async Task Update_AssignsCurrentDate_WhenDateOfBirthIsNull()
        {
            // Arrange
            int personId = 1;
            var existingPerson = new Person
            {
                PersonId = personId,
                Name = "Original Name",
                Image = "/images/avatars/original.png",
                DateOfBirth = new DateOnly(2000, 1, 1)
            };

            _mockPersonRepository.Setup(r => r.GetById(personId)).Returns(existingPerson);

            var model = new PersonFormModel
            {
                PersonId = personId,
                Name = "Updated Name",
                DateOfBirth = null, // <- this triggers the null branch
                Image = existingPerson.Image
            };

            var controller = BuildController("Admin");

            // Act
            var result = controller.Edit(personId, model, null);

            // Assert
            _mockPersonRepository.Verify(r => r.Update(It.Is<Person>(p =>
                p.Name == model.Name &&
                p.DateOfBirth != null &&
                p.DateOfBirth >= DateOnly.FromDateTime(DateTime.Now).AddDays(-1) && // allows 1-day leeway
                p.DateOfBirth <= DateOnly.FromDateTime(DateTime.Now).AddDays(1)
            )), Times.Once);

            _mockPersonRepository.Verify(r => r.Save(), Times.Once);

            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("MainPage", redirect.ActionName);
        }

        [Fact]
        public void Edit_Post_RedirectsToAdminPage_WhenModelIsValidAndNoNewImageProvided()
        {
            // Arrange
            int personId = 1;
            var existingPerson = new Person { PersonId = personId, Name = "Old Name", Image = "/images/avatars/existing.jpg" };
            var updatedPersonFormModel = new PersonFormModel
            {
                PersonId = personId,
                Name = "Updated Name",
                DateOfBirth = new DateOnly(1985, 3, 10),
                Nationality = "French",
                Gender = false,
                IsDirector = false,
                Description = "Updated actor description",
                Image = existingPerson.Image // Important: Retain existing image path in model
            };

            // No new IFormFile (null)
            IFormFile? ImageFile = null;

            _mockPersonRepository.Setup(r => r.GetById(personId)).Returns(existingPerson);
            _mockPersonRepository.Setup(r => r.Update(It.IsAny<Person>()));
            _mockPersonRepository.Setup(r => r.Save());

            var controller = BuildController();

            // Act
            var result = controller.Edit(personId, updatedPersonFormModel, ImageFile);

            // Assert
            var redirectToActionResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("MainPage", redirectToActionResult.ActionName);
            Assert.Equal("Employee", redirectToActionResult.ControllerName);
            Assert.Equal("CastMg", redirectToActionResult.RouteValues["tab"]);

            // Verify that the image path remains the same as the existing one
            _mockPersonRepository.Verify(r => r.Update(It.Is<Person>(p =>
                p.Name == updatedPersonFormModel.Name &&
                p.Image == existingPerson.Image // Crucial check for no new image
            )), Times.Once());
            _mockPersonRepository.Verify(r => r.Save(), Times.Once());
            Assert.Equal("Cast updated successfully.", controller.TempData["ToastMessage"]);
        }

        [Fact]
        public void Edit_Post_ReturnsNotFound_WhenIdMismatch()
        {
            // Arrange
            int idInRoute = 1;
            var personFormModel = new PersonFormModel { PersonId = 2, Name = "Mismatch" }; // Different ID
            IFormFile? ImageFile = null;

            var controller = BuildController();

            // Act
            var result = controller.Edit(idInRoute, personFormModel, ImageFile);

            // Assert
            Assert.IsType<NotFoundResult>(result);
            Assert.Equal("ID mismatch error.", controller.TempData["ErrorMessage"]);
            _mockPersonRepository.Verify(r => r.Update(It.IsAny<Person>()), Times.Never());
            _mockPersonRepository.Verify(r => r.Save(), Times.Never());
        }

        [Fact]
        public void Edit_Post_ReturnsViewWithErrorMessage_WhenModelIsInvalid()
        {
            // Arrange
            int personId = 1;
            var existingPerson = new Person { PersonId = personId, Name = "Original", DateOfBirth = new DateOnly(1980, 1, 1) };
            var personFormModel = new PersonFormModel { PersonId = personId, Name = null }; // Invalid model state

            _mockPersonRepository.Setup(r => r.GetById(personId)).Returns(existingPerson); // Need to mock existing for the return View(person) path
            var mockFile = new Mock<IFormFile>(); // Can be null or mocked

            var controller = BuildController();
            controller.ModelState.AddModelError("Name", "Name is required."); // Simulate validation error

            // Act
            var result = controller.Edit(personId, personFormModel, mockFile.Object);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<PersonFormModel>(viewResult.Model);
            Assert.Equal(personFormModel, model);
            Assert.False(controller.ModelState.IsValid);
            Assert.Contains("Invalid model state: Name is required.", controller.TempData["ErrorMessage"].ToString());
            _mockPersonRepository.Verify(r => r.Update(It.IsAny<Person>()), Times.Never());
            _mockPersonRepository.Verify(r => r.Save(), Times.Never());
        }

        [Fact]
        public void Edit_Post_ReturnsNotFound_WhenExistingPersonNotFound()
        {
            // Arrange
            int personId = 1;
            var personFormModel = new PersonFormModel { PersonId = personId, Name = "Valid Name" };
            IFormFile? ImageFile = null;

            _mockPersonRepository.Setup(r => r.GetById(personId)).Returns((Person)null); // Person not found

            var controller = BuildController();

            // Act
            var result = controller.Edit(personId, personFormModel, ImageFile);

            // Assert
            Assert.IsType<NotFoundResult>(result);
            Assert.Equal("Person not found.", controller.TempData["ErrorMessage"]);
            _mockPersonRepository.Verify(r => r.Update(It.IsAny<Person>()), Times.Never());
            _mockPersonRepository.Verify(r => r.Save(), Times.Never());
        }

        [Fact]
        public void Edit_Post_ReturnsViewWithErrorMessage_WhenDatabaseUpdateFails()
        {
            // Arrange
            int personId = 1;
            var existingPerson = new Person { PersonId = personId, Name = "Original Name", Image = "/images/avatars/original.jpg" };
            var updatedPersonFormModel = new PersonFormModel
            {
                PersonId = personId,
                Name = "Updated Name",
                DateOfBirth = new DateOnly(1985, 3, 10),
                Nationality = "French",
                Gender = false, // As per your PersonFormModel, this is a bool?
                IsDirector = false,
                Description = "Updated actor description",
                Image = existingPerson.Image
            };
            IFormFile? ImageFile = null; // No new image

            _mockPersonRepository.Setup(r => r.GetById(personId)).Returns(existingPerson);
            _mockPersonRepository.Setup(r => r.Update(It.IsAny<Person>()));
            _mockPersonRepository.Setup(r => r.Save()).Throws(new Exception("Simulated DB error during save."));

            var controller = BuildController();

            // Act
            var result = controller.Edit(personId, updatedPersonFormModel, ImageFile);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<PersonFormModel>(viewResult.Model);
            Assert.Equal(updatedPersonFormModel, model); // Ensure model is returned to view
            Assert.Contains("Database error:", controller.TempData["ErrorMessage"].ToString());

            Assert.False(controller.ModelState.ContainsKey(""));
        }

        [Fact]
        public void Edit_Post_ReturnsViewWithErrorMessage_WhenImageUploadFails()
        {
            // Arrange
            int personId = 1;
            var existingPerson = new Person { PersonId = personId, Name = "Original Name", Image = "/images/avatars/original.jpg" };
            var updatedPersonFormModel = new PersonFormModel
            {
                PersonId = personId,
                Name = "Updated Name",
                DateOfBirth = new DateOnly(1985, 3, 10),
                Nationality = "French",
                Gender = false,
                IsDirector = false,
                Description = "Updated actor description"
            };

            var mockFile = new Mock<IFormFile>();
            mockFile.Setup(_ => _.FileName).Returns("bad_image.png");
            mockFile.Setup(_ => _.Length).Returns(100);
            mockFile.Setup(_ => _.CopyTo(It.IsAny<Stream>())).Throws(new IOException("Simulated file system error.")); // Simulate file copy error

            _mockPersonRepository.Setup(r => r.GetById(personId)).Returns(existingPerson);

            var controller = BuildController();

            // Ensure the target directory for image uploads exists for the test
            var uploadPath = Path.Combine(Path.GetTempPath(), "wwwroot", "images", "avatars");
            if (!Directory.Exists(uploadPath))
            {
                Directory.CreateDirectory(uploadPath);
            }

            // Act
            var result = controller.Edit(personId, updatedPersonFormModel, mockFile.Object);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.IsType<PersonFormModel>(viewResult.Model);
            Assert.Contains("Error updating cast:", controller.TempData["ErrorMessage"].ToString());
            Assert.True(controller.ModelState.ContainsKey("")); // Check for model-level error
            _mockPersonRepository.Verify(r => r.Update(It.IsAny<Person>()), Times.Never()); // No update should happen if file copy fails before save
            _mockPersonRepository.Verify(r => r.Save(), Times.Never());

            // Clean up
            Directory.Delete(uploadPath, true);
        }

        // --- Delete (POST) Action Tests ---

        [Fact]
        public async Task Delete_RedirectsToAdminPageWithToast_WhenDeletionSuccessful()
        {
            // Arrange
            int personId = 1;
            var cast = new Person { PersonId = personId, Name = "Cast to Delete" };
            var emptyMovieList = new List<Movie>();

            _mockPersonRepository.Setup(r => r.GetById(personId)).Returns(cast);
            _mockPersonRepository.Setup(r => r.GetMovieByPerson(personId)).Returns(emptyMovieList); // No associated movies
            _mockPersonRepository.Setup(r => r.Delete(personId));
            _mockPersonRepository.Setup(r => r.Save());

            var controller = BuildController("Admin");

            // Act
            var result = await controller.Delete(personId, new FormCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>()));

            // Assert
            var redirectToActionResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("MainPage", redirectToActionResult.ActionName);
            Assert.Equal("Admin", redirectToActionResult.ControllerName);
            Assert.Equal("CastMg", redirectToActionResult.RouteValues["tab"]);
            Assert.Equal($"Successfully deleted {cast.Name}.", controller.TempData["ToastMessage"]);

            _mockPersonRepository.Verify(r => r.Delete(personId), Times.Once());
            _mockPersonRepository.Verify(r => r.Save(), Times.Once());
        }

        [Fact]
        public async Task Delete_RedirectsWithErrorMessage_WhenCastNotFound()
        {
            // Arrange
            int personId = 99;
            _mockPersonRepository.Setup(r => r.GetById(personId)).Returns((Person)null);

            var controller = BuildController();

            // Act
            var result = await controller.Delete(personId, new FormCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>()));

            // Assert
            var redirectToActionResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("MainPage", redirectToActionResult.ActionName);
            Assert.Equal("Employee", redirectToActionResult.ControllerName);
            Assert.Equal("CastMg", redirectToActionResult.RouteValues["tab"]);
            Assert.Equal("Cast not found.", controller.TempData["ErrorMessage"]);

            _mockPersonRepository.Verify(r => r.Delete(It.IsAny<int>()), Times.Never());
            _mockPersonRepository.Verify(r => r.Save(), Times.Never());
        }

        [Fact]
        public async Task Delete_RedirectsWithToastMessage_WhenCastAssociatedWithMovies()
        {
            // Arrange
            int personId = 1;
            var cast = new Person { PersonId = personId, Name = "Cast with Movies" };
            var movies = new List<Movie> { new Movie { MovieId = "MV001", MovieNameEnglish = "Movie A" } };

            _mockPersonRepository.Setup(r => r.GetById(personId)).Returns(cast);
            _mockPersonRepository.Setup(r => r.GetMovieByPerson(personId)).Returns(movies); // Associated movies
            _mockPersonRepository.Setup(r => r.RemovePersonFromAllMovies(personId));
            _mockPersonRepository.Setup(r => r.Delete(personId));
            _mockPersonRepository.Setup(r => r.Save());

            var controller = BuildController();

            // Act
            var result = await controller.Delete(personId, new FormCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>()));

            // Assert
            var redirectToActionResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("MainPage", redirectToActionResult.ActionName);
            Assert.Equal("Employee", redirectToActionResult.ControllerName);
            Assert.Equal("CastMg", redirectToActionResult.RouteValues["tab"]);
            Assert.Equal($"Successfully removed {cast.Name} from {movies.Count()} movie(s) and deleted the cast member.", controller.TempData["ToastMessage"]);

            _mockPersonRepository.Verify(r => r.RemovePersonFromAllMovies(personId), Times.Once());
            _mockPersonRepository.Verify(r => r.Delete(personId), Times.Once());
            _mockPersonRepository.Verify(r => r.Save(), Times.Exactly(2)); // Once for removing from movies, once for deleting person
        }

        [Fact]
        public async Task Delete_RedirectsWithErrorMessage_WhenExceptionOccurs()
        {
            // Arrange
            int personId = 1;
            var cast = new Person { PersonId = personId, Name = "Cast for Error" };

            _mockPersonRepository.Setup(r => r.GetById(personId)).Returns(cast);
            _mockPersonRepository.Setup(r => r.GetMovieByPerson(personId)).Throws(new Exception("Simulated DB error during movie check.")); // Simulate an exception

            var controller = BuildController("Admin");

            // Act
            var result = await controller.Delete(personId, new FormCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>()));

            // Assert
            var redirectToActionResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("MainPage", redirectToActionResult.ActionName);
            Assert.Equal("Admin", redirectToActionResult.ControllerName);
            Assert.Equal("CastMg", redirectToActionResult.RouteValues["tab"]);
            Assert.Contains("An error occurred during deletion:", controller.TempData["ErrorMessage"].ToString());

            _mockPersonRepository.Verify(r => r.Delete(It.IsAny<int>()), Times.Never());
            _mockPersonRepository.Verify(r => r.Save(), Times.Never());
        }

        [Fact]
        public async Task Delete_ValidIdAndNoMovieAssociations_AdminRole_DeletesCastAndRedirects()
        {
            // Arrange
            int castId = 1;

            var cast = new Person { PersonId = castId, Name = "John Doe" };
            var controller = BuildController("Admin");

            _mockPersonRepository.Setup(r => r.GetById(castId)).Returns(cast);
            _mockPersonRepository.Setup(r => r.GetMovieByPerson(castId)).Returns(new List<Movie>());
            _mockPersonRepository.Setup(r => r.Delete(castId));
            _mockPersonRepository.Setup(r => r.Save());

            var formCollection = new FormCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>());

            // Act
            var result = await controller.Delete(castId, formCollection);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("MainPage", redirectResult.ActionName);
            Assert.Equal("Admin", redirectResult.ControllerName);
            Assert.Equal("CastMg", redirectResult.RouteValues["tab"]);
            Assert.Equal($"Successfully deleted {cast.Name}.", controller.TempData["ToastMessage"]);

            _mockPersonRepository.Verify(r => r.Delete(castId), Times.Once);
            _mockPersonRepository.Verify(r => r.Save(), Times.Once);
        }
    }
}