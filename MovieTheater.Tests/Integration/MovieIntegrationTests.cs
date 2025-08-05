using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace MovieTheater.Tests.Integration
{
    public class MovieIntegrationTests : IClassFixture<CustomWebApplicationFactory<Program>>
    {
        private readonly HttpClient _client;

        public MovieIntegrationTests(CustomWebApplicationFactory<Program> factory)
        {
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task MovieList_ReturnsSuccessStatusCode()
        {
            // Act
            var response = await _client.GetAsync("/Movie/MovieList");

            // Assert
            response.EnsureSuccessStatusCode();
        }

        [Fact]
        public async Task MovieList_ReturnsHtmlContent()
        {
            // Act
            var response = await _client.GetAsync("/Movie/MovieList");
            var content = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Contains("<!DOCTYPE html>", content);
            Assert.Contains("<html", content);
            Assert.Contains("</html>", content);
        }

        [Fact]
        public async Task MovieList_ContainsMovieGridElements()
        {
            // Act
            var response = await _client.GetAsync("/Movie/MovieList");
            var content = await response.Content.ReadAsStringAsync();

            // Assert - Check for movie grid elements
            Assert.Contains("movie-grid", content);
            Assert.Contains("movie-card", content);
        }

        [Fact]
        public async Task MovieList_ContainsFilterElements()
        {
            // Act
            var response = await _client.GetAsync("/Movie/MovieList");
            var content = await response.Content.ReadAsStringAsync();

            // Assert - Check for filter elements
            Assert.Contains("filter", content);
            Assert.Contains("search", content);
        }

        [Fact]
        public async Task MovieDetail_WithValidId_ReturnsSuccessStatusCode()
        {
            // Act - Using a test movie ID
            var response = await _client.GetAsync("/Movie/Detail/MV001");

            // Assert
            response.EnsureSuccessStatusCode();
        }

        [Fact]
        public async Task MovieDetail_ReturnsHtmlContent()
        {
            // Act
            var response = await _client.GetAsync("/Movie/Detail/MV001");
            var content = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Contains("<!DOCTYPE html>", content);
            Assert.Contains("<html", content);
            Assert.Contains("</html>", content);
        }

        [Fact]
        public async Task MovieDetail_ContainsMovieInformationElements()
        {
            // Act
            var response = await _client.GetAsync("/Movie/Detail/MV001");
            var content = await response.Content.ReadAsStringAsync();

            // Assert - Check for movie detail elements
            Assert.Contains("movie-detail", content);
            Assert.Contains("movie-title", content);
            Assert.Contains("movie-synopsis", content);
        }

        [Fact]
        public async Task MovieShow_ReturnsSuccessStatusCode()
        {
            // Act
            var response = await _client.GetAsync("/Movie/MovieShow");

            // Assert
            response.EnsureSuccessStatusCode();
        }

        [Fact]
        public async Task MovieShow_ReturnsHtmlContent()
        {
            // Act
            var response = await _client.GetAsync("/Movie/MovieShow");
            var content = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Contains("<!DOCTYPE html>", content);
            Assert.Contains("<html", content);
            Assert.Contains("</html>", content);
        }

        [Fact]
        public async Task MovieShow_ContainsShowtimeElements()
        {
            // Act
            var response = await _client.GetAsync("/Movie/MovieShow");
            var content = await response.Content.ReadAsStringAsync();

            // Assert - Check for showtime elements
            Assert.Contains("showtime", content);
            Assert.Contains("schedule", content);
        }

        [Fact]
        public async Task ViewShow_ReturnsSuccessStatusCode()
        {
            // Act
            var response = await _client.GetAsync("/Movie/ViewShow");

            // Assert
            response.EnsureSuccessStatusCode();
        }

        [Fact]
        public async Task ViewShow_ReturnsHtmlContent()
        {
            // Act
            var response = await _client.GetAsync("/Movie/ViewShow");
            var content = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Contains("<!DOCTYPE html>", content);
            Assert.Contains("<html", content);
            Assert.Contains("</html>", content);
        }

        [Fact]
        public async Task ViewShow_ContainsShowElements()
        {
            // Act
            var response = await _client.GetAsync("/Movie/ViewShow");
            var content = await response.Content.ReadAsStringAsync();

            // Assert - Check for show elements
            Assert.Contains("show", content);
            Assert.Contains("movie", content);
        }
    }
} 