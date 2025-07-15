using Xunit;
using MovieTheater.Repository;
using MovieTheater.Models;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;

namespace MovieTheater.Tests.Repository
{
    public class FoodRepositoryTests
    {
        private MovieTheaterContext GetDbContext(string dbName)
        {
            var options = new DbContextOptionsBuilder<MovieTheaterContext>()
                .UseInMemoryDatabase(databaseName: dbName)
                .Options;
            return new MovieTheaterContext(options);
        }

        [Fact]
        public async Task GetAllAsync_ReturnsAllFoods()
        {
            var context = GetDbContext("GetAllAsync");
            context.Foods.AddRange(new List<Food>
            {
                new Food { FoodId = 1, Name = "Popcorn", Category = "food", Price = 50000, Status = true },
                new Food { FoodId = 2, Name = "Coke", Category = "drink", Price = 30000, Status = true }
            });
            context.SaveChanges();
            var repo = new FoodRepository(context);
            var result = await repo.GetAllAsync();
            Assert.Equal(2, result.Count());
        }

        [Fact]
        public async Task GetByCategoryAsync_ReturnsCorrectFoods()
        {
            var context = GetDbContext("GetByCategoryAsync");
            context.Foods.AddRange(new List<Food>
            {
                new Food { FoodId = 1, Name = "Popcorn", Category = "food", Price = 50000, Status = true },
                new Food { FoodId = 2, Name = "Coke", Category = "drink", Price = 30000, Status = true }
            });
            context.SaveChanges();
            var repo = new FoodRepository(context);
            var result = await repo.GetByCategoryAsync("food");
            Assert.Single(result);
            Assert.Equal("Popcorn", result.First().Name);
        }

        [Fact]
        public async Task GetByStatusAsync_ReturnsCorrectFoods()
        {
            var context = GetDbContext("GetByStatusAsync");
            context.Foods.AddRange(new List<Food>
            {
                new Food { FoodId = 1, Name = "Popcorn", Category = "food", Price = 50000, Status = true },
                new Food { FoodId = 2, Name = "Coke", Category = "drink", Price = 30000, Status = false }
            });
            context.SaveChanges();
            var repo = new FoodRepository(context);
            var result = await repo.GetByStatusAsync(true);
            Assert.Single(result);
            Assert.Equal("Popcorn", result.First().Name);
        }

        [Fact]
        public async Task SearchAsync_ReturnsFoodsByKeyword()
        {
            var context = GetDbContext("SearchAsync");
            context.Foods.AddRange(new List<Food>
            {
                new Food { FoodId = 1, Name = "Popcorn", Description = "Sweet", Category = "food", Price = 50000, Status = true },
                new Food { FoodId = 2, Name = "Coke", Description = "Drink", Category = "drink", Price = 30000, Status = true }
            });
            context.SaveChanges();
            var repo = new FoodRepository(context);
            var result = await repo.SearchAsync("Pop");
            Assert.Single(result);
            Assert.Equal("Popcorn", result.First().Name);
        }

        [Fact]
        public async Task GetByIdAsync_ReturnsCorrectFood()
        {
            var context = GetDbContext("GetByIdAsync");
            context.Foods.Add(new Food { FoodId = 1, Name = "Popcorn", Category = "food", Price = 50000, Status = true });
            context.SaveChanges();
            var repo = new FoodRepository(context);
            var result = await repo.GetByIdAsync(1);
            Assert.NotNull(result);
            Assert.Equal("Popcorn", result.Name);
        }

        [Fact]
        public async Task CreateAsync_AddsFood()
        {
            var context = GetDbContext("CreateAsync");
            var repo = new FoodRepository(context);
            var food = new Food { Name = "Popcorn", Category = "food", Price = 50000, Status = true };
            var result = await repo.CreateAsync(food);
            Assert.True(result.FoodId > 0);
            Assert.Single(context.Foods);
        }

        [Fact]
        public async Task UpdateAsync_UpdatesFood()
        {
            var context = GetDbContext("UpdateAsync");
            var food = new Food { FoodId = 1, Name = "Popcorn", Category = "food", Price = 50000, Status = true };
            context.Foods.Add(food);
            context.SaveChanges();
            var repo = new FoodRepository(context);
            food.Name = "Updated Popcorn";
            var result = await repo.UpdateAsync(food);
            Assert.Equal("Updated Popcorn", context.Foods.First().Name);
        }

        [Fact]
        public async Task DeleteAsync_RemovesFood()
        {
            var context = GetDbContext("DeleteAsync");
            var food = new Food { FoodId = 1, Name = "Popcorn", Category = "food", Price = 50000, Status = true };
            context.Foods.Add(food);
            context.SaveChanges();
            var repo = new FoodRepository(context);
            await repo.DeleteAsync(1);
            Assert.Empty(context.Foods);
        }

        [Fact]
        public async Task ExistsAsync_ReturnsTrueIfExists()
        {
            var context = GetDbContext("ExistsAsync");
            context.Foods.Add(new Food { FoodId = 1, Name = "Popcorn", Category = "food", Price = 50000, Status = true });
            context.SaveChanges();
            var repo = new FoodRepository(context);
            var exists = await repo.ExistsAsync(1);
            Assert.True(exists);
        }
    }
} 