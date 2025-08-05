using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace MovieTheater.Tests.Integration
{
    public class HomeIntegrationTests : IClassFixture<CustomWebApplicationFactory<Program>>
    {
        private readonly HttpClient _client;

        public HomeIntegrationTests(CustomWebApplicationFactory<Program> factory)
        {
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task HomeIndex_ReturnsSuccessStatusCode()
        {
            // Act
            var response = await _client.GetAsync("/");

            // Assert
            response.EnsureSuccessStatusCode();
        }

        [Fact]
        public async Task HomeIndex_ReturnsHtmlContent()
        {
            // Act
            var response = await _client.GetAsync("/");
            var content = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Contains("<!DOCTYPE html>", content);
            Assert.Contains("<html", content);
            Assert.Contains("</html>", content);
        }

        [Fact]
        public async Task HomeIndex_ContainsExpectedElements()
        {
            // Act
            var response = await _client.GetAsync("/");
            var content = await response.Content.ReadAsStringAsync();

            // Assert - Check for common layout elements
            Assert.Contains("<head>", content);
            Assert.Contains("<body>", content);
            Assert.Contains("<title>", content);
        }

        [Fact]
        public async Task HomeIndex_ContainsNavigationElements()
        {
            // Act
            var response = await _client.GetAsync("/");
            var content = await response.Content.ReadAsStringAsync();

            // Assert - Check for navigation elements
            Assert.Contains("nav", content);
            Assert.Contains("navbar", content);
        }

        [Fact]
        public async Task HomeIndex_ContainsFooterElements()
        {
            // Act
            var response = await _client.GetAsync("/");
            var content = await response.Content.ReadAsStringAsync();

            // Assert - Check for footer elements
            Assert.Contains("footer", content);
        }
    }
} 