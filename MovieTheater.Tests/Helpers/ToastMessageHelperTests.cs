using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using MovieTheater.Helpers;
using Xunit;

namespace MovieTheater.Tests.Helpers
{
    public class ToastMessageHelperTests
    {
        [Fact]
        public void GetMessage_WhenValidCategoryActionType_ReturnsMessage()
        {
            // Arrange
            var category = "test";
            var action = "create";
            var type = "success";

            // Act
            var result = ToastMessageHelper.GetMessage(category, action, type);

            // Assert
            Assert.NotNull(result);
            Assert.Contains("success message for test.create", result);
        }

        [Fact]
        public void GetMessage_WhenInvalidCategory_ReturnsDefaultMessage()
        {
            // Arrange
            var category = "invalid_category";
            var action = "invalid_action";
            var type = "invalid_type";

            // Act
            var result = ToastMessageHelper.GetMessage(category, action, type);

            // Assert
            Assert.NotNull(result);
            Assert.Contains("invalid_type message for invalid_category.invalid_action", result);
        }

        [Fact]
        public void GetMessage_WhenNullParameters_ReturnsDefaultMessage()
        {
            // Arrange
            string? category = null;
            string? action = null;
            string? type = null;

            // Act
            var result = ToastMessageHelper.GetMessage(category!, action!, type!);

            // Assert
            Assert.NotNull(result);
            Assert.Contains(" message for .", result);
        }

        [Fact]
        public void SetToastMessage_WhenSuccessType_SetsToastMessage()
        {
            // Arrange
            var controller = new TestController();
            var category = "test";
            var action = "create";
            var type = "success";

            // Act
            ToastMessageHelper.SetToastMessage(controller, category, action, type);

            // Assert
            Assert.True(controller.TempData.ContainsKey("ToastMessage"));
            Assert.False(controller.TempData.ContainsKey("ErrorMessage"));
            Assert.NotNull(controller.TempData["ToastMessage"]);
        }

        [Fact]
        public void SetToastMessage_WhenErrorType_SetsErrorMessage()
        {
            // Arrange
            var controller = new TestController();
            var category = "test";
            var action = "create";
            var type = "error";

            // Act
            ToastMessageHelper.SetToastMessage(controller, category, action, type);

            // Assert
            Assert.True(controller.TempData.ContainsKey("ErrorMessage"));
            Assert.False(controller.TempData.ContainsKey("ToastMessage"));
            Assert.NotNull(controller.TempData["ErrorMessage"]);
        }

        [Fact]
        public void SetToastMessage_WhenWarningType_SetsErrorMessage()
        {
            // Arrange
            var controller = new TestController();
            var category = "test";
            var action = "create";
            var type = "warning";

            // Act
            ToastMessageHelper.SetToastMessage(controller, category, action, type);

            // Assert
            Assert.True(controller.TempData.ContainsKey("ErrorMessage"));
            Assert.False(controller.TempData.ContainsKey("ToastMessage"));
            Assert.NotNull(controller.TempData["ErrorMessage"]);
        }

        [Fact]
        public void SetToastMessage_WhenInfoType_SetsErrorMessage()
        {
            // Arrange
            var controller = new TestController();
            var category = "test";
            var action = "create";
            var type = "info";

            // Act
            ToastMessageHelper.SetToastMessage(controller, category, action, type);

            // Assert
            Assert.True(controller.TempData.ContainsKey("ErrorMessage"));
            Assert.False(controller.TempData.ContainsKey("ToastMessage"));
            Assert.NotNull(controller.TempData["ErrorMessage"]);
        }

        [Fact]
        public void SetToastMessage_WithNullController_DoesNotThrowException()
        {
            // Arrange
            Microsoft.AspNetCore.Mvc.Controller? controller = null;
            var category = "test";
            var action = "create";
            var type = "success";

            // Act & Assert - Should not throw exception
            var exception = Record.Exception(() => 
                ToastMessageHelper.SetToastMessage(controller!, category, action, type));
        }

        [Fact]
        public void SetToastMessage_WithNullParameters_DoesNotThrowException()
        {
            // Arrange
            var controller = new TestController();
            string? category = null;
            string? action = null;
            string? type = null;

            // Act & Assert - Should not throw exception
            var exception = Record.Exception(() => 
                ToastMessageHelper.SetToastMessage(controller, category!, action!, type!));
            Assert.Null(exception);
        }

        // Helper class for testing
        private class TestController : Microsoft.AspNetCore.Mvc.Controller
        {
            public TestController()
            {
                var httpContext = new DefaultHttpContext();
                var tempDataProvider = new MockTempDataProvider();
                TempData = new TempDataDictionary(httpContext, tempDataProvider);
            }
        }

        // Mock TempDataProvider for testing
        private class MockTempDataProvider : ITempDataProvider
        {
            private readonly Dictionary<string, object> _data = new();

            public IDictionary<string, object> LoadTempData(Microsoft.AspNetCore.Http.HttpContext context)
            {
                return _data;
            }

            public void SaveTempData(Microsoft.AspNetCore.Http.HttpContext context, IDictionary<string, object> values)
            {
                foreach (var kvp in values)
                {
                    _data[kvp.Key] = kvp.Value;
                }
            }
        }
    }
} 