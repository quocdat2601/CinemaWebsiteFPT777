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
    }
} 