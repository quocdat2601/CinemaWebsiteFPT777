using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MovieTheater.Models;

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

                // Add in-memory database for testing
                services.AddDbContext<MovieTheaterContext>(options =>
                {
                    options.UseInMemoryDatabase("TestDb");
                });

                // Remove any SQL Server related services
                var sqlServerDescriptors = services.Where(d => 
                    d.ServiceType.Name.Contains("SqlServer") ||
                    d.ImplementationType?.Name.Contains("SqlServer") == true).ToList();

                foreach (var descriptor in sqlServerDescriptors)
                {
                    services.Remove(descriptor);
                }
            });
        }

        // Optional: Add method to seed test data
        // private void SeedTestData(MovieTheaterContext context)
        // {
        //     // Add test data here
        // }
    }
} 