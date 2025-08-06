using Xunit;
using MovieTheater.Repository;
using MovieTheater.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

namespace MovieTheater.Tests.Repository
{
    public class FoodInvoiceRepositoryTests
    {
        private MovieTheaterContext GetInMemoryContext()
        {
            var options = new DbContextOptionsBuilder<MovieTheaterContext>()
                .UseInMemoryDatabase(databaseName: System.Guid.NewGuid().ToString())
                .Options;
            return new MovieTheaterContext(options);
        }

        [Fact]
        public async Task GetByInvoiceIdAsync_ReturnsCorrectFoodInvoices()
        {
            using var context = GetInMemoryContext();
            // Thêm dữ liệu Food liên quan với đủ trường bắt buộc
            context.Foods.Add(new Food { FoodId = 1, Name = "Popcorn", Category = "food", Price = 10000, Status = true });
            context.FoodInvoices.Add(new FoodInvoice { FoodInvoiceId = 1, InvoiceId = "inv1", FoodId = 1 });
            context.FoodInvoices.Add(new FoodInvoice { FoodInvoiceId = 2, InvoiceId = "inv2", FoodId = 2 });
            context.SaveChanges();
            var repo = new FoodInvoiceRepository(context);
            var result = await repo.GetByInvoiceIdAsync("inv1");
            Assert.Single(result);
            Assert.Equal("inv1", result.First().InvoiceId);
        }

        [Fact]
        public async Task CreateAsync_AddsFoodInvoice()
        {
            using var context = GetInMemoryContext();
            var repo = new FoodInvoiceRepository(context);
            var foodInvoice = new FoodInvoice { FoodInvoiceId = 1, InvoiceId = "inv1", FoodId = 1 };
            var result = await repo.CreateAsync(foodInvoice);
            Assert.Equal(foodInvoice, result);
            Assert.Single(context.FoodInvoices);
        }

        [Fact]
        public async Task CreateMultipleAsync_AddsMultipleFoodInvoices()
        {
            using var context = GetInMemoryContext();
            var repo = new FoodInvoiceRepository(context);
            var foodInvoices = new List<FoodInvoice>
            {
                new FoodInvoice { FoodInvoiceId = 1, InvoiceId = "inv1", FoodId = 1 },
                new FoodInvoice { FoodInvoiceId = 2, InvoiceId = "inv1", FoodId = 2 }
            };
            var result = await repo.CreateMultipleAsync(foodInvoices);
            Assert.Equal(2, context.FoodInvoices.Count());
            Assert.Equal(2, result.Count());
        }

        [Fact]
        public async Task DeleteByInvoiceIdAsync_DeletesFoodInvoicesAndReturnsTrue()
        {
            using var context = GetInMemoryContext();
            context.FoodInvoices.Add(new FoodInvoice { FoodInvoiceId = 1, InvoiceId = "inv1", FoodId = 1 });
            context.FoodInvoices.Add(new FoodInvoice { FoodInvoiceId = 2, InvoiceId = "inv1", FoodId = 2 });
            context.SaveChanges();
            var repo = new FoodInvoiceRepository(context);
            var result = await repo.DeleteByInvoiceIdAsync("inv1");
            Assert.True(result);
            Assert.Empty(context.FoodInvoices);
        }

        [Fact]
        public async Task DeleteByInvoiceIdAsync_ReturnsFalse_WhenNoInvoicesFound()
        {
            using var context = GetInMemoryContext();
            var repo = new FoodInvoiceRepository(context);
            var result = await repo.DeleteByInvoiceIdAsync("notfound");
            Assert.False(result);
        }
    }
} 