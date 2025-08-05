using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace MovieTheater.Tests.Integration
{
    public class AdminIntegrationTests : IClassFixture<CustomWebApplicationFactory<Program>>
    {
        private readonly HttpClient _client;

        public AdminIntegrationTests(CustomWebApplicationFactory<Program> factory)
        {
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task AdminMainPage_ReturnsSuccessStatusCode()
        {
            // Act
            var response = await _client.GetAsync("/Admin/MainPage");

            // Assert
            response.EnsureSuccessStatusCode();
        }

        [Fact]
        public async Task AdminMainPage_ReturnsHtmlContent()
        {
            // Act
            var response = await _client.GetAsync("/Admin/MainPage");
            var content = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Contains("<!DOCTYPE html>", content);
            Assert.Contains("<html", content);
            Assert.Contains("</html>", content);
        }

        [Fact]
        public async Task AdminMainPage_ContainsAdminElements()
        {
            // Act
            var response = await _client.GetAsync("/Admin/MainPage");
            var content = await response.Content.ReadAsStringAsync();

            // Assert - Check for admin elements
            Assert.Contains("admin", content);
            Assert.Contains("dashboard", content);
        }

        [Fact]
        public async Task AdminDashboard_ReturnsSuccessStatusCode()
        {
            // Act
            var response = await _client.GetAsync("/Admin/Dashboard");

            // Assert
            response.EnsureSuccessStatusCode();
        }

        [Fact]
        public async Task AdminDashboard_ReturnsHtmlContent()
        {
            // Act
            var response = await _client.GetAsync("/Admin/Dashboard");
            var content = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Contains("<!DOCTYPE html>", content);
            Assert.Contains("<html", content);
            Assert.Contains("</html>", content);
        }

        [Fact]
        public async Task AdminDashboard_ContainsDashboardElements()
        {
            // Act
            var response = await _client.GetAsync("/Admin/Dashboard");
            var content = await response.Content.ReadAsStringAsync();

            // Assert - Check for dashboard elements
            Assert.Contains("dashboard", content);
            Assert.Contains("statistics", content);
        }

        [Fact]
        public async Task AdminMovieMg_ReturnsSuccessStatusCode()
        {
            // Act
            var response = await _client.GetAsync("/Admin/MovieMg");

            // Assert
            response.EnsureSuccessStatusCode();
        }

        [Fact]
        public async Task AdminMovieMg_ReturnsHtmlContent()
        {
            // Act
            var response = await _client.GetAsync("/Admin/MovieMg");
            var content = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Contains("<!DOCTYPE html>", content);
            Assert.Contains("<html", content);
            Assert.Contains("</html>", content);
        }

        [Fact]
        public async Task AdminMovieMg_ContainsMovieManagementElements()
        {
            // Act
            var response = await _client.GetAsync("/Admin/MovieMg");
            var content = await response.Content.ReadAsStringAsync();

            // Assert - Check for movie management elements
            Assert.Contains("movie", content);
            Assert.Contains("management", content);
        }

        [Fact]
        public async Task AdminBookingMg_ReturnsSuccessStatusCode()
        {
            // Act
            var response = await _client.GetAsync("/Admin/BookingMg");

            // Assert
            response.EnsureSuccessStatusCode();
        }

        [Fact]
        public async Task AdminBookingMg_ReturnsHtmlContent()
        {
            // Act
            var response = await _client.GetAsync("/Admin/BookingMg");
            var content = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Contains("<!DOCTYPE html>", content);
            Assert.Contains("<html", content);
            Assert.Contains("</html>", content);
        }

        [Fact]
        public async Task AdminBookingMg_ContainsBookingManagementElements()
        {
            // Act
            var response = await _client.GetAsync("/Admin/BookingMg");
            var content = await response.Content.ReadAsStringAsync();

            // Assert - Check for booking management elements
            Assert.Contains("booking", content);
            Assert.Contains("management", content);
        }

        [Fact]
        public async Task AdminEmployeeMg_ReturnsSuccessStatusCode()
        {
            // Act
            var response = await _client.GetAsync("/Admin/EmployeeMg");

            // Assert
            response.EnsureSuccessStatusCode();
        }

        [Fact]
        public async Task AdminEmployeeMg_ReturnsHtmlContent()
        {
            // Act
            var response = await _client.GetAsync("/Admin/EmployeeMg");
            var content = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Contains("<!DOCTYPE html>", content);
            Assert.Contains("<html", content);
            Assert.Contains("</html>", content);
        }

        [Fact]
        public async Task AdminEmployeeMg_ContainsEmployeeManagementElements()
        {
            // Act
            var response = await _client.GetAsync("/Admin/EmployeeMg");
            var content = await response.Content.ReadAsStringAsync();

            // Assert - Check for employee management elements
            Assert.Contains("employee", content);
            Assert.Contains("management", content);
        }

        [Fact]
        public async Task AdminFoodMg_ReturnsSuccessStatusCode()
        {
            // Act
            var response = await _client.GetAsync("/Admin/FoodMg");

            // Assert
            response.EnsureSuccessStatusCode();
        }

        [Fact]
        public async Task AdminFoodMg_ReturnsHtmlContent()
        {
            // Act
            var response = await _client.GetAsync("/Admin/FoodMg");
            var content = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Contains("<!DOCTYPE html>", content);
            Assert.Contains("<html", content);
            Assert.Contains("</html>", content);
        }

        [Fact]
        public async Task AdminFoodMg_ContainsFoodManagementElements()
        {
            // Act
            var response = await _client.GetAsync("/Admin/FoodMg");
            var content = await response.Content.ReadAsStringAsync();

            // Assert - Check for food management elements
            Assert.Contains("food", content);
            Assert.Contains("management", content);
        }
    }
} 