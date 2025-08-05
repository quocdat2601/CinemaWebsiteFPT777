using Xunit;
using Moq;
using MovieTheater.Controllers;
using MovieTheater.Service;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using MovieTheater.ViewModels;
using System;
using System.Linq;

namespace MovieTheater.Tests.Controller
{
    public class ScoreControllerTests
    {
        private ScoreController CreateController(Mock<IScoreService> mockScoreService = null, ClaimsPrincipal user = null)
        {
            mockScoreService ??= new Mock<IScoreService>();
            var controller = new ScoreController(mockScoreService.Object);
            if (user != null)
            {
                controller.ControllerContext = new ControllerContext()
                {
                    HttpContext = new DefaultHttpContext() { User = user }
                };
            }
            return controller;
        }

        [Fact]
        public void ScoreHistory_ReturnsView_WhenUserLoggedIn()
        {
            // Arrange
            var mockScoreService = new Mock<IScoreService>();
            var fakeAccountId = "test-account";
            mockScoreService.Setup(s => s.GetCurrentScore(fakeAccountId)).Returns(100);
            mockScoreService.Setup(s => s.GetScoreHistory(fakeAccountId, null, null, "add"))
                .Returns(new List<ScoreHistoryViewModel> { new ScoreHistoryViewModel { CurrentScore = 100 } });
            var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
            {
                new Claim(ClaimTypes.NameIdentifier, fakeAccountId)
            }, "mock"));
            var controller = CreateController(mockScoreService, user);

            // Act
            var result = controller.ScoreHistory();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Equal("~/Views/Account/Tabs/Score.cshtml", viewResult.ViewName);
            Assert.IsAssignableFrom<IEnumerable<ScoreHistoryViewModel>>(viewResult.Model);
        }

        [Fact]
        public void ScoreHistory_RedirectsToLogin_WhenUserNotLoggedIn()
        {
            // Arrange
            var controller = CreateController();
            controller.ControllerContext = new ControllerContext()
            {
                HttpContext = new DefaultHttpContext() // Không có user
            };

            // Act
            var result = controller.ScoreHistory();

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Login", redirectResult.ActionName);
            Assert.Equal("Account", redirectResult.ControllerName);
        }

        [Fact]
        public void ScoreHistory_FilterByDateAndType_ReturnsCorrectRecords()
        {
            // Arrange
            var mockScoreService = new Mock<IScoreService>();
            var fakeAccountId = "test-account";
            var fromDate = new DateTime(2024, 6, 1);
            var toDate = new DateTime(2024, 6, 30);
            var historyType = "use";
            var fakeData = new List<ScoreHistoryViewModel>
            {
                new ScoreHistoryViewModel { DateCreated = new DateTime(2024, 6, 10), MovieName = "Movie A", Score = -50, Type = "use" }
            };
            mockScoreService.Setup(s => s.GetCurrentScore(fakeAccountId)).Returns(100);
            mockScoreService.Setup(s => s.GetScoreHistory(fakeAccountId, fromDate, toDate, historyType)).Returns(fakeData);
            var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
            {
                new Claim(ClaimTypes.NameIdentifier, fakeAccountId)
            }, "mock"));
            var controller = CreateController(mockScoreService, user);

            // Act
            var result = controller.ScoreHistory(fromDate, toDate, historyType);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsAssignableFrom<IEnumerable<ScoreHistoryViewModel>>(viewResult.Model);
            Assert.Single(model);
            var record = model.First();
            Assert.Equal(new DateTime(2024, 6, 10), record.DateCreated);
            Assert.Equal("Movie A", record.MovieName);
            Assert.Equal(-50, record.Score);
            Assert.Equal("use", record.Type);
        }

        [Fact]
        public void ScoreHistory_NoRecordsFound_SetsNoHistoryMessage()
        {
            // Arrange
            var mockScoreService = new Mock<IScoreService>();
            var fakeAccountId = "test-account";
            var fromDate = new DateTime(2024, 6, 1);
            var toDate = new DateTime(2024, 6, 30);
            var historyType = "add";
            mockScoreService.Setup(s => s.GetCurrentScore(fakeAccountId)).Returns(100);
            mockScoreService.Setup(s => s.GetScoreHistory(fakeAccountId, fromDate, toDate, historyType)).Returns(new List<ScoreHistoryViewModel>());
            var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
            {
                new Claim(ClaimTypes.NameIdentifier, fakeAccountId)
            }, "mock"));
            var controller = CreateController(mockScoreService, user);

            // Act
            var result = controller.ScoreHistory(fromDate, toDate, historyType);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Equal("No score history found for the selected period.", controller.ViewBag.Message);
        }

        [Fact]
        public void ScoreHistory_ModelStateInvalid_RedirectsToScoreHistory()
        {
            // Arrange
            var mockScoreService = new Mock<IScoreService>();
            var fakeAccountId = "test-account";
            var fromDate = new DateTime(2024, 6, 1);
            var toDate = new DateTime(2024, 6, 30);
            var historyType = "add";
            var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
            {
                new Claim(ClaimTypes.NameIdentifier, fakeAccountId)
            }, "mock"));
            var controller = CreateController(mockScoreService, user);
            controller.ModelState.AddModelError("Test", "Test error");

            // Act
            var result = controller.ScoreHistory(fromDate, toDate, historyType);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("ScoreHistory", redirectResult.ActionName);
        }

        [Fact]
        public void ScoreHistory_UserNotLoggedIn_RedirectsToLogin()
        {
            // Arrange
            var mockScoreService = new Mock<IScoreService>();
            var fromDate = new DateTime(2024, 6, 1);
            var toDate = new DateTime(2024, 6, 30);
            var historyType = "add";
            // Create user without any claims
            var identity = new ClaimsIdentity();
            var principal = new ClaimsPrincipal(identity);
            var controller = CreateController(mockScoreService, principal);

            // Act
            var result = controller.ScoreHistory(fromDate, toDate, historyType);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Login", redirectResult.ActionName);
            Assert.Equal("Account", redirectResult.ControllerName);
        }

        [Fact]
        public void ScoreHistory_UserHasEmptyAccountId_RedirectsToLogin()
        {
            // Arrange
            var mockScoreService = new Mock<IScoreService>();
            var fromDate = new DateTime(2024, 6, 1);
            var toDate = new DateTime(2024, 6, 30);
            var historyType = "add";
            var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
            {
                new Claim(ClaimTypes.NameIdentifier, "")
            }, "mock"));
            var controller = CreateController(mockScoreService, user);

            // Act
            var result = controller.ScoreHistory(fromDate, toDate, historyType);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Login", redirectResult.ActionName);
            Assert.Equal("Account", redirectResult.ControllerName);
        }

        [Fact]
        public void ScoreHistory_UserHasNullAccountId_RedirectsToLogin()
        {
            // Arrange
            var mockScoreService = new Mock<IScoreService>();
            var fromDate = new DateTime(2024, 6, 1);
            var toDate = new DateTime(2024, 6, 30);
            var historyType = "add";
            // Create user with null NameIdentifier claim
            var claims = new List<Claim> { new Claim(ClaimTypes.Role, "Member") };
            var identity = new ClaimsIdentity(claims, "TestAuthType");
            var principal = new ClaimsPrincipal(identity);
            var controller = CreateController(mockScoreService, principal);

            // Act
            var result = controller.ScoreHistory(fromDate, toDate, historyType);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Login", redirectResult.ActionName);
            Assert.Equal("Account", redirectResult.ControllerName);
        }

        [Fact]
        public void ScoreHistory_WithValidData_SetsViewBagCorrectly()
        {
            // Arrange
            var mockScoreService = new Mock<IScoreService>();
            var fakeAccountId = "test-account";
            var fromDate = new DateTime(2024, 6, 1);
            var toDate = new DateTime(2024, 6, 30);
            var historyType = "use";
            var fakeData = new List<ScoreHistoryViewModel>
            {
                new ScoreHistoryViewModel { DateCreated = new DateTime(2024, 6, 10), MovieName = "Movie A", Score = -50, Type = "use" }
            };
            mockScoreService.Setup(s => s.GetCurrentScore(fakeAccountId)).Returns(100);
            mockScoreService.Setup(s => s.GetScoreHistory(fakeAccountId, fromDate, toDate, historyType)).Returns(fakeData);
            var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
            {
                new Claim(ClaimTypes.NameIdentifier, fakeAccountId)
            }, "mock"));
            var controller = CreateController(mockScoreService, user);

            // Act
            var result = controller.ScoreHistory(fromDate, toDate, historyType);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Equal(100, controller.ViewBag.CurrentScore);
            Assert.Equal("~/Views/Account/Tabs/Score.cshtml", viewResult.ViewName);
        }

        [Fact]
        public void ScoreHistoryPartial_ReturnsJson_WhenUserLoggedIn()
        {
            // Arrange
            var mockScoreService = new Mock<IScoreService>();
            var fakeAccountId = "test-account";
            var fromDate = "2024-06-01";
            var toDate = "2024-06-30";
            var historyType = "add";
            var fakeData = new List<ScoreHistoryViewModel>
            {
                new ScoreHistoryViewModel 
                { 
                    DateCreated = new DateTime(2024, 6, 10), 
                    MovieName = "Movie A", 
                    Score = 100, 
                    Type = "add" 
                }
            };
            mockScoreService.Setup(s => s.GetCurrentScore(fakeAccountId)).Returns(500);
            mockScoreService.Setup(s => s.GetScoreHistory(fakeAccountId, It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), historyType)).Returns(fakeData);
            var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
            {
                new Claim(ClaimTypes.NameIdentifier, fakeAccountId)
            }, "mock"));
            var controller = CreateController(mockScoreService, user);

            // Act
            var result = controller.ScoreHistoryPartial(fromDate, toDate, historyType);

            // Assert
            var jsonResult = Assert.IsType<JsonResult>(result);
            // Use reflection to access properties of anonymous type
            var response = jsonResult.Value;
            var successProperty = response.GetType().GetProperty("success");
            var currentScoreProperty = response.GetType().GetProperty("currentScore");
            Assert.NotNull(successProperty);
            Assert.NotNull(currentScoreProperty);
            Assert.True((bool)successProperty.GetValue(response));
            Assert.Equal(500, currentScoreProperty.GetValue(response));
        }

        [Fact]
        public void ScoreHistoryPartial_ReturnsJson_WhenUserNotLoggedIn()
        {
            // Arrange
            var mockScoreService = new Mock<IScoreService>();
            var controller = CreateController(mockScoreService);
            // Ensure User is not null but has no NameIdentifier claim
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity()) }
            };

            // Act
            var result = controller.ScoreHistoryPartial("2024-06-01", "2024-06-30", "add");

            // Assert
            var jsonResult = Assert.IsType<JsonResult>(result);
            var response = jsonResult.Value;
            var successProperty = response.GetType().GetProperty("success");
            var messageProperty = response.GetType().GetProperty("message");
            Assert.NotNull(successProperty);
            Assert.NotNull(messageProperty);
            Assert.False((bool)successProperty.GetValue(response));
            Assert.Equal("Not logged in.", messageProperty.GetValue(response));
        }

        [Fact]
        public void ScoreHistoryPartial_ReturnsJson_WhenUserHasEmptyAccountId()
        {
            // Arrange
            var mockScoreService = new Mock<IScoreService>();
            var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
            {
                new Claim(ClaimTypes.NameIdentifier, "")
            }, "mock"));
            var controller = CreateController(mockScoreService, user);

            // Act
            var result = controller.ScoreHistoryPartial("2024-06-01", "2024-06-30", "add");

            // Assert
            var jsonResult = Assert.IsType<JsonResult>(result);
            var response = jsonResult.Value;
            var successProperty = response.GetType().GetProperty("success");
            var messageProperty = response.GetType().GetProperty("message");
            Assert.NotNull(successProperty);
            Assert.NotNull(messageProperty);
            Assert.False((bool)successProperty.GetValue(response));
            Assert.Equal("Not logged in.", messageProperty.GetValue(response));
        }

        [Fact]
        public void ScoreHistoryPartial_WithInvalidDates_HandlesGracefully()
        {
            // Arrange
            var mockScoreService = new Mock<IScoreService>();
            var fakeAccountId = "test-account";
            var fromDate = "invalid-date";
            var toDate = "invalid-date";
            var historyType = "add";
            var fakeData = new List<ScoreHistoryViewModel>();
            mockScoreService.Setup(s => s.GetCurrentScore(fakeAccountId)).Returns(100);
            mockScoreService.Setup(s => s.GetScoreHistory(fakeAccountId, null, null, historyType)).Returns(fakeData);
            var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
            {
                new Claim(ClaimTypes.NameIdentifier, fakeAccountId)
            }, "mock"));
            var controller = CreateController(mockScoreService, user);

            // Act
            var result = controller.ScoreHistoryPartial(fromDate, toDate, historyType);

            // Assert
            var jsonResult = Assert.IsType<JsonResult>(result);
            var response = jsonResult.Value;
            var successProperty = response.GetType().GetProperty("success");
            var currentScoreProperty = response.GetType().GetProperty("currentScore");
            Assert.NotNull(successProperty);
            Assert.NotNull(currentScoreProperty);
            Assert.True((bool)successProperty.GetValue(response));
            Assert.Equal(100, currentScoreProperty.GetValue(response));
        }

        [Fact]
        public void ScoreHistoryPartial_WithValidDates_ReturnsOrderedData()
        {
            // Arrange
            var mockScoreService = new Mock<IScoreService>();
            var fakeAccountId = "test-account";
            var fromDate = "2024-06-01";
            var toDate = "2024-06-30";
            var historyType = "add";
            var fakeData = new List<ScoreHistoryViewModel>
            {
                new ScoreHistoryViewModel 
                { 
                    DateCreated = new DateTime(2024, 6, 10), 
                    MovieName = "Movie A", 
                    Score = 100, 
                    Type = "add" 
                },
                new ScoreHistoryViewModel 
                { 
                    DateCreated = new DateTime(2024, 6, 15), 
                    MovieName = "Movie B", 
                    Score = 200, 
                    Type = "add" 
                }
            };
            mockScoreService.Setup(s => s.GetCurrentScore(fakeAccountId)).Returns(500);
            mockScoreService.Setup(s => s.GetScoreHistory(fakeAccountId, It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), historyType)).Returns(fakeData);
            var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
            {
                new Claim(ClaimTypes.NameIdentifier, fakeAccountId)
            }, "mock"));
            var controller = CreateController(mockScoreService, user);

            // Act
            var result = controller.ScoreHistoryPartial(fromDate, toDate, historyType);

            // Assert
            var jsonResult = Assert.IsType<JsonResult>(result);
            var response = jsonResult.Value;
            var successProperty = response.GetType().GetProperty("success");
            var dataProperty = response.GetType().GetProperty("data");
            var currentScoreProperty = response.GetType().GetProperty("currentScore");
            Assert.NotNull(successProperty);
            Assert.NotNull(dataProperty);
            Assert.NotNull(currentScoreProperty);
            Assert.True((bool)successProperty.GetValue(response));
            Assert.Equal(500, currentScoreProperty.GetValue(response));
            
            // Check data property - it might be an empty list or null
            var data = dataProperty.GetValue(response);
            Assert.NotNull(data); // Data should not be null
            // Convert to list to check count
            var dataList = data as System.Collections.IEnumerable;
            Assert.NotNull(dataList);
            var count = 0;
            foreach (var item in dataList) count++;
            Assert.Equal(2, count);
        }

        [Fact]
        public void ScoreHistoryPartial_WithNullDates_HandlesGracefully()
        {
            // Arrange
            var mockScoreService = new Mock<IScoreService>();
            var fakeAccountId = "test-account";
            string fromDate = null;
            string toDate = null;
            var historyType = "add";
            var fakeData = new List<ScoreHistoryViewModel>();
            mockScoreService.Setup(s => s.GetCurrentScore(fakeAccountId)).Returns(100);
            mockScoreService.Setup(s => s.GetScoreHistory(fakeAccountId, null, null, historyType)).Returns(fakeData);
            var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
            {
                new Claim(ClaimTypes.NameIdentifier, fakeAccountId)
            }, "mock"));
            var controller = CreateController(mockScoreService, user);

            // Act
            var result = controller.ScoreHistoryPartial(fromDate, toDate, historyType);

            // Assert
            var jsonResult = Assert.IsType<JsonResult>(result);
            var response = jsonResult.Value;
            var successProperty = response.GetType().GetProperty("success");
            var currentScoreProperty = response.GetType().GetProperty("currentScore");
            Assert.NotNull(successProperty);
            Assert.NotNull(currentScoreProperty);
            Assert.True((bool)successProperty.GetValue(response));
            Assert.Equal(100, currentScoreProperty.GetValue(response));
        }

        [Fact]
        public void ScoreHistoryPartial_WithEmptyDates_HandlesGracefully()
        {
            // Arrange
            var mockScoreService = new Mock<IScoreService>();
            var fakeAccountId = "test-account";
            var fromDate = "";
            var toDate = "";
            var historyType = "add";
            var fakeData = new List<ScoreHistoryViewModel>();
            mockScoreService.Setup(s => s.GetCurrentScore(fakeAccountId)).Returns(100);
            mockScoreService.Setup(s => s.GetScoreHistory(fakeAccountId, null, null, historyType)).Returns(fakeData);
            var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
            {
                new Claim(ClaimTypes.NameIdentifier, fakeAccountId)
            }, "mock"));
            var controller = CreateController(mockScoreService, user);

            // Act
            var result = controller.ScoreHistoryPartial(fromDate, toDate, historyType);

            // Assert
            var jsonResult = Assert.IsType<JsonResult>(result);
            var response = jsonResult.Value;
            var successProperty = response.GetType().GetProperty("success");
            var currentScoreProperty = response.GetType().GetProperty("currentScore");
            Assert.NotNull(successProperty);
            Assert.NotNull(currentScoreProperty);
            Assert.True((bool)successProperty.GetValue(response));
            Assert.Equal(100, currentScoreProperty.GetValue(response));
        }

        [Fact]
        public void ScoreHistory_WithDifferentHistoryTypes_WorksCorrectly()
        {
            // Arrange
            var mockScoreService = new Mock<IScoreService>();
            var fakeAccountId = "test-account";
            var fromDate = new DateTime(2024, 6, 1);
            var toDate = new DateTime(2024, 6, 30);
            var historyTypes = new[] { "add", "use", "all" };

            foreach (var historyType in historyTypes)
            {
                var fakeData = new List<ScoreHistoryViewModel>
                {
                    new ScoreHistoryViewModel { DateCreated = new DateTime(2024, 6, 10), MovieName = "Movie A", Score = 100, Type = historyType }
                };
                mockScoreService.Setup(s => s.GetCurrentScore(fakeAccountId)).Returns(100);
                mockScoreService.Setup(s => s.GetScoreHistory(fakeAccountId, fromDate, toDate, historyType)).Returns(fakeData);
                var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
                {
                    new Claim(ClaimTypes.NameIdentifier, fakeAccountId)
                }, "mock"));
                var controller = CreateController(mockScoreService, user);

                // Act
                var result = controller.ScoreHistory(fromDate, toDate, historyType);

                // Assert
                var viewResult = Assert.IsType<ViewResult>(result);
                var model = Assert.IsAssignableFrom<IEnumerable<ScoreHistoryViewModel>>(viewResult.Model);
                Assert.Single(model);
            }
        }

        [Fact]
        public void ScoreHistory_WhenServiceThrowsException_HandlesGracefully()
        {
            // Arrange
            var mockScoreService = new Mock<IScoreService>();
            var fakeAccountId = "test-account";
            var fromDate = new DateTime(2024, 6, 1);
            var toDate = new DateTime(2024, 6, 30);
            var historyType = "add";
            mockScoreService.Setup(s => s.GetCurrentScore(fakeAccountId)).Throws(new InvalidOperationException("Service error"));
            var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
            {
                new Claim(ClaimTypes.NameIdentifier, fakeAccountId)
            }, "mock"));
            var controller = CreateController(mockScoreService, user);

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() => controller.ScoreHistory(fromDate, toDate, historyType));
            Assert.Equal("Service error", exception.Message);
        }

        [Fact]
        public void ScoreHistoryPartial_WhenServiceThrowsException_HandlesGracefully()
        {
            // Arrange
            var mockScoreService = new Mock<IScoreService>();
            var fakeAccountId = "test-account";
            mockScoreService.Setup(s => s.GetCurrentScore(fakeAccountId)).Throws(new InvalidOperationException("Service error"));
            var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
            {
                new Claim(ClaimTypes.NameIdentifier, fakeAccountId)
            }, "mock"));
            var controller = CreateController(mockScoreService, user);

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() => controller.ScoreHistoryPartial("2024-06-01", "2024-06-30", "add"));
            Assert.Equal("Service error", exception.Message);
        }
    }
} 