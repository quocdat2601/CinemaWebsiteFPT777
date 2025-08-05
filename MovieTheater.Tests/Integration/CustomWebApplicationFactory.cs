using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MovieTheater.Models;
using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using System.Text.Encodings.Web;

namespace MovieTheater.Tests.Integration
{
    public class CustomWebApplicationFactory<TStartup> : WebApplicationFactory<TStartup> where TStartup : class
    {
        protected override void ConfigureWebHost(Microsoft.AspNetCore.Hosting.IWebHostBuilder builder)
        {
            builder.ConfigureServices(services =>
            {
                // Remove all existing DbContext registrations
                var dbContextDescriptors = services.Where(d => 
                    d.ServiceType == typeof(DbContextOptions<MovieTheaterContext>) ||
                    d.ServiceType == typeof(MovieTheaterContext)).ToList();

                foreach (var descriptor in dbContextDescriptors)
                {
                    services.Remove(descriptor);
                }

                // Remove any SQL Server related services more comprehensively
                var sqlServerDescriptors = services.Where(d => 
                    d.ServiceType.Name.Contains("SqlServer") ||
                    d.ImplementationType?.Name.Contains("SqlServer") == true ||
                    d.ServiceType.FullName?.Contains("SqlServer") == true ||
                    d.ImplementationType?.FullName?.Contains("SqlServer") == true ||
                    d.ServiceType.FullName?.Contains("Microsoft.EntityFrameworkCore.SqlServer") == true ||
                    d.ImplementationType?.FullName?.Contains("Microsoft.EntityFrameworkCore.SqlServer") == true).ToList();

                foreach (var descriptor in sqlServerDescriptors)
                {
                    services.Remove(descriptor);
                }

                // Remove Entity Framework services that might conflict
                var efDescriptors = services.Where(d =>
                    d.ServiceType.Name.Contains("EntityFramework") ||
                    d.ImplementationType?.Name.Contains("EntityFramework") == true ||
                    d.ServiceType.FullName?.Contains("Microsoft.EntityFrameworkCore") == true ||
                    d.ImplementationType?.FullName?.Contains("Microsoft.EntityFrameworkCore") == true).ToList();

                foreach (var descriptor in efDescriptors)
                {
                    services.Remove(descriptor);
                }

                // Remove any database connection string configurations
                var connectionStringDescriptors = services.Where(d =>
                    d.ServiceType.Name.Contains("Connection") ||
                    d.ImplementationType?.Name.Contains("Connection") == true).ToList();

                foreach (var descriptor in connectionStringDescriptors)
                {
                    services.Remove(descriptor);
                }

                // Add SignalR services for testing
                services.AddSignalR();

                // Add in-memory database for testing
                services.AddDbContext<MovieTheaterContext>(options =>
                {
                    options.UseInMemoryDatabase("TestDb_" + Guid.NewGuid().ToString());
                });

                // Configure authentication for testing
                services.AddAuthentication("TestScheme")
                    .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("TestScheme", options => { });

                // Configure authorization
                services.AddAuthorization(options =>
                {
                    options.AddPolicy("Admin", policy => policy.RequireRole("Admin"));
                    options.AddPolicy("Employee", policy => policy.RequireRole("Employee"));
                });
            });
        }

        // Helper method to create authenticated client
        public HttpClient CreateAuthenticatedClient(string role = "Admin")
        {
            var client = CreateClient();
            client.DefaultRequestHeaders.Add("X-Test-Auth", role);
            return client;
        }
    }

    // Test authentication handler
    public class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        public TestAuthHandler(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder, ISystemClock clock)
            : base(options, logger, encoder, clock)
        {
        }

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            if (Context.Request.Headers.ContainsKey("X-Test-Auth"))
            {
                var role = Context.Request.Headers["X-Test-Auth"].ToString();
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, "testuser"),
                    new Claim(ClaimTypes.NameIdentifier, "1"),
                    new Claim(ClaimTypes.Role, role)
                };

                var identity = new ClaimsIdentity(claims, "TestScheme");
                var principal = new ClaimsPrincipal(identity);
                var ticket = new AuthenticationTicket(principal, "TestScheme");

                return Task.FromResult(AuthenticateResult.Success(ticket));
            }

            return Task.FromResult(AuthenticateResult.Fail("No authentication header"));
        }

        protected override Task HandleChallengeAsync(AuthenticationProperties properties)
        {
            Response.StatusCode = 401;
            return Task.CompletedTask;
        }

        protected override Task HandleForbiddenAsync(AuthenticationProperties properties)
        {
            Response.StatusCode = 403;
            return Task.CompletedTask;
        }
    }
} 