using Xunit;
using Moq;
using Microsoft.AspNetCore.Mvc;
using MovieTheater.Controllers;
using MovieTheater.Repository;
using Version = MovieTheater.Models.Version; // Alias to avoid conflict with System.Version
using System.Collections.Generic;
using System;
using Microsoft.AspNetCore.Http; // For DefaultHttpContext, IFormCollection
using Microsoft.AspNetCore.Mvc.ViewFeatures; // For TempDataDictionary, ITempDataProvider
using Microsoft.AspNetCore.Routing; // For RouteData
using System.Reflection; // Add this for the GetJsonValue helper

namespace MovieTheater.Tests.Controller
{
    public class VersionControllerTests
    {
        private readonly Mock<IVersionRepository> _mockVersionRepo;

        public VersionControllerTests()
        {
            _mockVersionRepo = new Mock<IVersionRepository>();
        }
        private T GetJsonValue<T>(object jsonValue, string key)
        {
            if (jsonValue == null)
            {
                throw new ArgumentNullException(nameof(jsonValue), $"Cannot get value for key '{key}' from a null JSON object.");
            }

            // Try treating it as a dictionary (common when anonymous objects are serialized)
            if (jsonValue is IDictionary<string, object> dict)
            {
                if (dict.TryGetValue(key, out object value))
                {
                    if (value is T castValue)
                    {
                        return castValue;
                    }
                    // Handle cases where the type might be different (e.g., int vs long, etc.)
                    return (T)Convert.ChangeType(value, typeof(T));
                }
            }

            // Try treating it as a direct object (when anonymous type is preserved)
            // This uses reflection, which is what 'dynamic' would do under the hood, but more explicitly.
            var property = jsonValue.GetType().GetProperty(key);
            if (property != null)
            {
                var value = property.GetValue(jsonValue);
                if (value is T castValue)
                {
                    return castValue;
                }
                return (T)Convert.ChangeType(value, typeof(T));
            }

            throw new InvalidOperationException($"Could not find property '{key}' or convert its value to type {typeof(T).Name} from the JSON result.");
        }

        private VersionController BuildController()
        {
            var controller = new VersionController(_mockVersionRepo.Object);

            // Setup HttpContext and TempData for the controller
            var httpContext = new DefaultHttpContext();
            var tempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>());

            controller.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext,
                RouteData = new RouteData()
            };
            controller.TempData = tempData;

            return controller;
        }

        // --- Create (POST) Action Tests ---

        [Fact]
        public void Create_Post_RedirectsToAdminPageWithToast_WhenModelIsValid()
        {
            // Arrange
            var versionModel = new Version { VersionId = 0, VersionName = "2D" }; // Id is 0 for new entity

            var controller = BuildController();

            // Act
            var result = controller.Create(versionModel);

            // Assert
            var redirectToActionResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("MainPage", redirectToActionResult.ActionName);
            Assert.Equal("Admin", redirectToActionResult.ControllerName);
            Assert.Equal("VersionMg", redirectToActionResult.RouteValues["tab"]);

            _mockVersionRepo.Verify(r => r.Add(It.IsAny<Version>()), Times.Once());
            _mockVersionRepo.Verify(r => r.Save(), Times.Once());
            Assert.Equal("Version created successfully!", controller.TempData["ToastMessage"]);
        }

        [Fact]
        public void Create_Post_RedirectsToAdminPageWithError_WhenModelIsInvalid()
        {
            // Arrange
            var versionModel = new Version { VersionId = 0, VersionName = null }; // Invalid: VersionName is null

            var controller = BuildController();
            controller.ModelState.AddModelError("VersionName", "Version name is required."); // Simulate validation error

            // Act
            var result = controller.Create(versionModel);

            // Assert
            var redirectToActionResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("MainPage", redirectToActionResult.ActionName);
            Assert.Equal("Admin", redirectToActionResult.ControllerName);
            Assert.Equal("VersionMg", redirectToActionResult.RouteValues["tab"]);

            _mockVersionRepo.Verify(r => r.Add(It.IsAny<Version>()), Times.Never()); // Should not call Add
            _mockVersionRepo.Verify(r => r.Save(), Times.Never()); // Should not call Save
            Assert.Equal("Invalid data!", controller.TempData["ErrorMessage"]);
        }

        // --- Edit (POST) Action Tests ---

        [Fact]
        public void Edit_Post_ReturnsJsonSuccess_WhenModelIsValid()
        {
            // Arrange
            var versionModel = new Version { VersionId = 1, VersionName = "3D" };

            // Act
            var controller = BuildController();
            var result = controller.Edit(versionModel);

            // Assert
            var jsonResult = Assert.IsType<JsonResult>(result);

            // --- Use the helper here ---
            bool success = GetJsonValue<bool>(jsonResult.Value, "success");
            Assert.True(success);
            // --- End helper usage ---

            Assert.Equal("Version updated successfully!", controller.TempData["ToastMessage"]);

            _mockVersionRepo.Verify(r => r.Update(It.IsAny<Version>()), Times.Once());
        }

        [Fact]
        public void Edit_Post_ReturnsJsonError_WhenModelIsInvalid()
        {
            // Arrange
            var versionModel = new Version { VersionId = 1, VersionName = null }; // Invalid: VersionName is null

            var controller = BuildController();
            controller.ModelState.AddModelError("VersionName", "Version name is required."); // Simulate validation error

            // Act
            var result = controller.Edit(versionModel);

            // Assert
            var jsonResult = Assert.IsType<JsonResult>(result);

            // --- Use the helper here ---
            bool success = GetJsonValue<bool>(jsonResult.Value, "success");
            string error = GetJsonValue<string>(jsonResult.Value, "error");
            Assert.False(success);
            Assert.Equal("Invalid data", error);
            // --- End helper usage ---

            Assert.Equal("Version update unsuccessful!", controller.TempData["ErrorMessage"]);

            _mockVersionRepo.Verify(r => r.Update(It.IsAny<Version>()), Times.Never());
        }
        // --- Delete (POST) Action Tests ---

        [Fact]
        public async Task Delete_Post_RedirectsToAdminPageWithToast_WhenDeletionSuccessful()
        {
            // Arrange
            int versionId = 1;
            var version = new Version { VersionId = versionId, VersionName = "2D" };

            _mockVersionRepo.Setup(r => r.GetById(versionId)).Returns(version);
            _mockVersionRepo.Setup(r => r.Delete(versionId)).Returns(true); // Simulate successful deletion

            var controller = BuildController();

            // Act
            var result = await controller.Delete(versionId, new FormCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>()));

            // Assert
            var redirectToActionResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("MainPage", redirectToActionResult.ActionName);
            Assert.Equal("Admin", redirectToActionResult.ControllerName);
            Assert.Equal("VersionMg", redirectToActionResult.RouteValues["tab"]);
            Assert.Equal("Version deleted successfully!", controller.TempData["ToastMessage"]);

            _mockVersionRepo.Verify(r => r.Delete(versionId), Times.Once());
        }

        [Fact]
        public async Task Delete_Post_RedirectsToAdminPageWithToast_WhenVersionNotFound()
        {
            // Arrange
            int versionId = 99; // Non-existent ID

            _mockVersionRepo.Setup(r => r.GetById(versionId)).Returns((Version)null); // Version not found

            var controller = BuildController();

            // Act
            var result = await controller.Delete(versionId, new FormCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>()));

            // Assert
            var redirectToActionResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("MainPage", redirectToActionResult.ActionName);
            Assert.Equal("Admin", redirectToActionResult.ControllerName);
            Assert.Equal("VersionMg", redirectToActionResult.RouteValues["tab"]);
            Assert.Equal("Version not found.", controller.TempData["ToastMessage"]); // Note: Controller sets ToastMessage for not found

            _mockVersionRepo.Verify(r => r.Delete(It.IsAny<int>()), Times.Never()); // Delete should not be called
        }

        [Fact]
        public async Task Delete_Post_RedirectsToAdminPageWithError_WhenDeletionFailsInRepository()
        {
            // Arrange
            int versionId = 1;
            var version = new Version { VersionId = versionId, VersionName = "2D" };

            _mockVersionRepo.Setup(r => r.GetById(versionId)).Returns(version);
            _mockVersionRepo.Setup(r => r.Delete(versionId)).Returns(false); // Simulate failed deletion

            var controller = BuildController();

            // Act
            var result = await controller.Delete(versionId, new FormCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>()));

            // Assert
            var redirectToActionResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("MainPage", redirectToActionResult.ActionName);
            Assert.Equal("Admin", redirectToActionResult.ControllerName);
            Assert.Equal("VersionMg", redirectToActionResult.RouteValues["tab"]);
            Assert.Equal("Failed to delete version.", controller.TempData["ErrorMessage"]); // Note: Controller sets ErrorMessage for failed deletion

            _mockVersionRepo.Verify(r => r.Delete(versionId), Times.Once());
        }

        [Fact]
        public async Task Delete_Post_RedirectsToAdminPageWithError_WhenExceptionOccurs()
        {
            // Arrange
            int versionId = 1;
            // Simulate an exception directly from GetById or Delete call
            _mockVersionRepo.Setup(r => r.GetById(versionId)).Throws(new Exception("Database error during lookup."));

            var controller = BuildController();

            // Act
            var result = await controller.Delete(versionId, new FormCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>()));

            // Assert
            var redirectToActionResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("MainPage", redirectToActionResult.ActionName);
            Assert.Equal("Admin", redirectToActionResult.ControllerName);
            Assert.Equal("VersionMg", redirectToActionResult.RouteValues["tab"]);
            Assert.Contains("An error occurred during deletion:", controller.TempData["ToastMessage"].ToString()); // Controller sets ToastMessage on catch

            _mockVersionRepo.Verify(r => r.Delete(It.IsAny<int>()), Times.Never()); // Should not call Delete
            _mockVersionRepo.Verify(r => r.GetById(It.IsAny<int>()), Times.Once()); // GetById was called and threw exception
        }

        // --- Get (GET) Action Tests ---

        [Fact]
        public void Get_ReturnsJsonVersion_WhenVersionExists()
        {
            // Arrange
            int versionId = 1;
            var version = new Version { VersionId = versionId, VersionName = "IMAX" };

            _mockVersionRepo.Setup(r => r.GetById(versionId)).Returns(version);

            var controller = BuildController();

            // Act
            var result = controller.Get(versionId);

            // Assert
            var jsonResult = Assert.IsType<JsonResult>(result);
            var returnedVersion = Assert.IsType<Version>(jsonResult.Value);
            Assert.Equal(version.VersionId, returnedVersion.VersionId);
            Assert.Equal(version.VersionName, returnedVersion.VersionName);
        }

        [Fact]
        public void Get_ReturnsNotFound_WhenVersionDoesNotExist()
        {
            // Arrange
            int versionId = 99; // Non-existent ID

            _mockVersionRepo.Setup(r => r.GetById(versionId)).Returns((Version)null); // Version not found

            var controller = BuildController();

            // Act
            var result = controller.Get(versionId);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }
    }
}