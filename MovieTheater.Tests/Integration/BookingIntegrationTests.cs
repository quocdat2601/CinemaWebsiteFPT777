using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace MovieTheater.Tests.Integration
{
    public class BookingIntegrationTests : IClassFixture<CustomWebApplicationFactory<Program>>
    {
        private readonly HttpClient _client;

        public BookingIntegrationTests(CustomWebApplicationFactory<Program> factory)
        {
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task ConfirmBooking_ReturnsSuccessStatusCode()
        {
            // Act
            var response = await _client.GetAsync("/Booking/ConfirmBooking");

            // Assert
            response.EnsureSuccessStatusCode();
        }

        [Fact]
        public async Task ConfirmBooking_ReturnsHtmlContent()
        {
            // Act
            var response = await _client.GetAsync("/Booking/ConfirmBooking");
            var content = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Contains("<!DOCTYPE html>", content);
            Assert.Contains("<html", content);
            Assert.Contains("</html>", content);
        }

        [Fact]
        public async Task ConfirmBooking_ContainsBookingElements()
        {
            // Act
            var response = await _client.GetAsync("/Booking/ConfirmBooking");
            var content = await response.Content.ReadAsStringAsync();

            // Assert - Check for booking elements
            Assert.Contains("booking", content);
            Assert.Contains("confirm", content);
        }

        [Fact]
        public async Task ConfirmTicketAdmin_ReturnsSuccessStatusCode()
        {
            // Act
            var response = await _client.GetAsync("/Booking/ConfirmTicketAdmin");

            // Assert
            response.EnsureSuccessStatusCode();
        }

        [Fact]
        public async Task ConfirmTicketAdmin_ReturnsHtmlContent()
        {
            // Act
            var response = await _client.GetAsync("/Booking/ConfirmTicketAdmin");
            var content = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Contains("<!DOCTYPE html>", content);
            Assert.Contains("<html", content);
            Assert.Contains("</html>", content);
        }

        [Fact]
        public async Task ConfirmTicketAdmin_ContainsTicketElements()
        {
            // Act
            var response = await _client.GetAsync("/Booking/ConfirmTicketAdmin");
            var content = await response.Content.ReadAsStringAsync();

            // Assert - Check for ticket elements
            Assert.Contains("ticket", content);
            Assert.Contains("admin", content);
        }

        [Fact]
        public async Task BookingProgress_ReturnsSuccessStatusCode()
        {
            // Act
            var response = await _client.GetAsync("/Booking/BookingProgress");

            // Assert
            response.EnsureSuccessStatusCode();
        }

        [Fact]
        public async Task BookingProgress_ReturnsHtmlContent()
        {
            // Act
            var response = await _client.GetAsync("/Booking/BookingProgress");
            var content = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Contains("<!DOCTYPE html>", content);
            Assert.Contains("<html", content);
            Assert.Contains("</html>", content);
        }

        [Fact]
        public async Task BookingProgress_ContainsProgressElements()
        {
            // Act
            var response = await _client.GetAsync("/Booking/BookingProgress");
            var content = await response.Content.ReadAsStringAsync();

            // Assert - Check for progress elements
            Assert.Contains("progress", content);
            Assert.Contains("booking", content);
        }

        [Fact]
        public async Task BookingHistory_ReturnsSuccessStatusCode()
        {
            // Act
            var response = await _client.GetAsync("/Booking/BookingHistory");

            // Assert
            response.EnsureSuccessStatusCode();
        }

        [Fact]
        public async Task BookingHistory_ReturnsHtmlContent()
        {
            // Act
            var response = await _client.GetAsync("/Booking/BookingHistory");
            var content = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Contains("<!DOCTYPE html>", content);
            Assert.Contains("<html", content);
            Assert.Contains("</html>", content);
        }

        [Fact]
        public async Task BookingHistory_ContainsHistoryElements()
        {
            // Act
            var response = await _client.GetAsync("/Booking/BookingHistory");
            var content = await response.Content.ReadAsStringAsync();

            // Assert - Check for history elements
            Assert.Contains("history", content);
            Assert.Contains("booking", content);
        }

        [Fact]
        public async Task BookingResult_ReturnsSuccessStatusCode()
        {
            // Act
            var response = await _client.GetAsync("/Booking/BookingResult");

            // Assert
            response.EnsureSuccessStatusCode();
        }

        [Fact]
        public async Task BookingResult_ReturnsHtmlContent()
        {
            // Act
            var response = await _client.GetAsync("/Booking/BookingResult");
            var content = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Contains("<!DOCTYPE html>", content);
            Assert.Contains("<html", content);
            Assert.Contains("</html>", content);
        }

        [Fact]
        public async Task BookingResult_ContainsResultElements()
        {
            // Act
            var response = await _client.GetAsync("/Booking/BookingResult");
            var content = await response.Content.ReadAsStringAsync();

            // Assert - Check for result elements
            Assert.Contains("result", content);
            Assert.Contains("booking", content);
        }
    }
} 