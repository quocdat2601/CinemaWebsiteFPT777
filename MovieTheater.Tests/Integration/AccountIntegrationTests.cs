using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace MovieTheater.Tests.Integration
{
    public class AccountIntegrationTests : IClassFixture<CustomWebApplicationFactory<Program>>
    {
        private readonly HttpClient _client;

        public AccountIntegrationTests(CustomWebApplicationFactory<Program> factory)
        {
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task Login_ReturnsSuccessStatusCode()
        {
            // Act
            var response = await _client.GetAsync("/Account/Login");

            // Assert
            response.EnsureSuccessStatusCode();
        }

        [Fact]
        public async Task Login_ReturnsHtmlContent()
        {
            // Act
            var response = await _client.GetAsync("/Account/Login");
            var content = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Contains("<!DOCTYPE html>", content);
            Assert.Contains("<html", content);
            Assert.Contains("</html>", content);
        }

        [Fact]
        public async Task Login_ContainsLoginFormElements()
        {
            // Act
            var response = await _client.GetAsync("/Account/Login");
            var content = await response.Content.ReadAsStringAsync();

            // Assert - Check for login form elements
            Assert.Contains("login", content);
            Assert.Contains("form", content);
            Assert.Contains("password", content);
        }

        [Fact]
        public async Task ForgetPassword_ReturnsSuccessStatusCode()
        {
            // Act
            var response = await _client.GetAsync("/Account/ForgetPassword");

            // Assert
            response.EnsureSuccessStatusCode();
        }

        [Fact]
        public async Task ForgetPassword_ReturnsHtmlContent()
        {
            // Act
            var response = await _client.GetAsync("/Account/ForgetPassword");
            var content = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Contains("<!DOCTYPE html>", content);
            Assert.Contains("<html", content);
            Assert.Contains("</html>", content);
        }

        [Fact]
        public async Task ForgetPassword_ContainsPasswordResetElements()
        {
            // Act
            var response = await _client.GetAsync("/Account/ForgetPassword");
            var content = await response.Content.ReadAsStringAsync();

            // Assert - Check for password reset elements
            Assert.Contains("forget", content);
            Assert.Contains("password", content);
            Assert.Contains("email", content);
        }

        [Fact]
        public async Task AccessDenied_ReturnsSuccessStatusCode()
        {
            // Act
            var response = await _client.GetAsync("/Account/AccessDenied");

            // Assert
            response.EnsureSuccessStatusCode();
        }

        [Fact]
        public async Task AccessDenied_ReturnsHtmlContent()
        {
            // Act
            var response = await _client.GetAsync("/Account/AccessDenied");
            var content = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Contains("<!DOCTYPE html>", content);
            Assert.Contains("<html", content);
            Assert.Contains("</html>", content);
        }

        [Fact]
        public async Task AccessDenied_ContainsAccessDeniedElements()
        {
            // Act
            var response = await _client.GetAsync("/Account/AccessDenied");
            var content = await response.Content.ReadAsStringAsync();

            // Assert - Check for access denied elements
            Assert.Contains("access", content.ToLower());
            Assert.Contains("denied", content.ToLower());
        }

        [Fact]
        public async Task Profile_ReturnsSuccessStatusCode()
        {
            // Act
            var response = await _client.GetAsync("/Account/Profile");

            // Assert
            response.EnsureSuccessStatusCode();
        }

        [Fact]
        public async Task Profile_ReturnsHtmlContent()
        {
            // Act
            var response = await _client.GetAsync("/Account/Profile");
            var content = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Contains("<!DOCTYPE html>", content);
            Assert.Contains("<html", content);
            Assert.Contains("</html>", content);
        }

        [Fact]
        public async Task Profile_ContainsProfileElements()
        {
            // Act
            var response = await _client.GetAsync("/Account/Profile");
            var content = await response.Content.ReadAsStringAsync();

            // Assert - Profile might return HTML or redirect, check both cases
            if (response.StatusCode == System.Net.HttpStatusCode.Found)
            {
                // If redirecting, check it goes to login
                Assert.Contains("/Account/Login", response.Headers.Location?.ToString());
            }
            else
            {
                // If returning HTML, check for common elements that should be present
                Assert.Contains("<!doctype html>", content.ToLower());
                Assert.Contains("<html", content.ToLower());
                Assert.Contains("</html>", content.ToLower());
            }
        }
    }
}