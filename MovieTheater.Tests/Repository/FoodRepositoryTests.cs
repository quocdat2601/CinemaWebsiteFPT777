using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using MovieTheater.Models;
using MovieTheater.Repository;
using Xunit;

namespace MovieTheater.Tests.Repository
{
    public class FoodRepositoryTests
    {
        private MovieTheaterContext GetInMemoryContext()
        {
            var options = new DbContextOptionsBuilder<MovieTheaterContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            return new MovieTheaterContext(options);
        }

        [Fact]
        public async Task GetAllAsync_ReturnsAllFoodsOrderedByCreatedDateDescending()
        {
            // Arrange
            using var context = GetInMemoryContext();
            var foods = new List<Food>
            {
                new Food { FoodId = 1, Name = "Popcorn", Category = "Snacks", Price = 15.50m, Status = true, CreatedDate = DateTime.Now.AddDays(-2) },
                new Food { FoodId = 2, Name = "Coke", Category = "Beverages", Price = 8.00m, Status = true, CreatedDate = DateTime.Now.AddDays(-1) },
                new Food { FoodId = 3, Name = "Nachos", Category = "Snacks", Price = 12.00m, Status = false, CreatedDate = DateTime.Now }
            };
            context.Foods.AddRange(foods);
            context.SaveChanges();

            var repo = new FoodRepository(context);

            // Act
            var result = await repo.GetAllAsync();

            // Assert
            var resultList = result.ToList();
            Assert.Equal(3, resultList.Count);
            Assert.Equal("Nachos", resultList[0].Name); // Most recent first
            Assert.Equal("Coke", resultList[1].Name);
            Assert.Equal("Popcorn", resultList[2].Name);
        }

        [Fact]
        public async Task GetAllAsync_EmptyDatabase_ReturnsEmptyList()
        {
            // Arrange
            using var context = GetInMemoryContext();
            var repo = new FoodRepository(context);

            // Act
            var result = await repo.GetAllAsync();

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public async Task GetByCategoryAsync_ValidCategory_ReturnsMatchingFoods()
        {
            // Arrange
            using var context = GetInMemoryContext();
            var foods = new List<Food>
            {
                new Food { FoodId = 1, Name = "Popcorn", Category = "Snacks", Price = 15.50m, Status = true, CreatedDate = DateTime.Now.AddDays(-1) },
                new Food { FoodId = 2, Name = "Coke", Category = "Beverages", Price = 8.00m, Status = true, CreatedDate = DateTime.Now },
                new Food { FoodId = 3, Name = "Nachos", Category = "Snacks", Price = 12.00m, Status = true, CreatedDate = DateTime.Now.AddDays(-2) }
            };
            context.Foods.AddRange(foods);
            context.SaveChanges();

            var repo = new FoodRepository(context);

            // Act
            var result = await repo.GetByCategoryAsync("Snacks");

            // Assert
            var resultList = result.ToList();
            Assert.Equal(2, resultList.Count);
            Assert.All(resultList, food => Assert.Equal("Snacks", food.Category));
            Assert.Equal("Popcorn", resultList[0].Name); // Most recent first (1 day ago)
            Assert.Equal("Nachos", resultList[1].Name); // 2 days ago
        }

        [Fact]
        public async Task GetByCategoryAsync_CaseInsensitive_ReturnsMatchingFoods()
        {
            // Arrange
            using var context = GetInMemoryContext();
            var foods = new List<Food>
            {
                new Food { FoodId = 1, Name = "Popcorn", Category = "Snacks", Price = 15.50m, Status = true, CreatedDate = DateTime.Now }
            };
            context.Foods.AddRange(foods);
            context.SaveChanges();

            var repo = new FoodRepository(context);

            // Act
            var result = await repo.GetByCategoryAsync("SNACKS");

            // Assert
            var resultList = result.ToList();
            Assert.Single(resultList);
            Assert.Equal("Popcorn", resultList[0].Name);
        }

        [Fact]
        public async Task GetByCategoryAsync_NonExistentCategory_ReturnsEmptyList()
        {
            // Arrange
            using var context = GetInMemoryContext();
            var repo = new FoodRepository(context);

            // Act
            var result = await repo.GetByCategoryAsync("NonExistent");

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public async Task GetByStatusAsync_ActiveFoods_ReturnsActiveFoods()
        {
            // Arrange
            using var context = GetInMemoryContext();
            var foods = new List<Food>
            {
                new Food { FoodId = 1, Name = "Popcorn", Category = "Snacks", Price = 15.50m, Status = true, CreatedDate = DateTime.Now },
                new Food { FoodId = 2, Name = "Coke", Category = "Beverages", Price = 8.00m, Status = false, CreatedDate = DateTime.Now.AddDays(-1) },
                new Food { FoodId = 3, Name = "Nachos", Category = "Snacks", Price = 12.00m, Status = true, CreatedDate = DateTime.Now.AddDays(-2) }
            };
            context.Foods.AddRange(foods);
            context.SaveChanges();

            var repo = new FoodRepository(context);

            // Act
            var result = await repo.GetByStatusAsync(true);

            // Assert
            var resultList = result.ToList();
            Assert.Equal(2, resultList.Count);
            Assert.All(resultList, food => Assert.True(food.Status));
            Assert.Equal("Popcorn", resultList[0].Name); // Most recent first
            Assert.Equal("Nachos", resultList[1].Name);
        }

        [Fact]
        public async Task GetByStatusAsync_InactiveFoods_ReturnsInactiveFoods()
        {
            // Arrange
            using var context = GetInMemoryContext();
            var foods = new List<Food>
            {
                new Food { FoodId = 1, Name = "Popcorn", Category = "Snacks", Price = 15.50m, Status = true, CreatedDate = DateTime.Now },
                new Food { FoodId = 2, Name = "Coke", Category = "Beverages", Price = 8.00m, Status = false, CreatedDate = DateTime.Now.AddDays(-1) }
            };
            context.Foods.AddRange(foods);
            context.SaveChanges();

            var repo = new FoodRepository(context);

            // Act
            var result = await repo.GetByStatusAsync(false);

            // Assert
            var resultList = result.ToList();
            Assert.Single(resultList);
            Assert.Equal("Coke", resultList[0].Name);
            Assert.False(resultList[0].Status);
        }

        [Fact]
        public async Task SearchAsync_KeywordInName_ReturnsMatchingFoods()
        {
            // Arrange
            using var context = GetInMemoryContext();
            var foods = new List<Food>
            {
                new Food { FoodId = 1, Name = "Popcorn", Category = "Snacks", Price = 15.50m, Status = true, CreatedDate = DateTime.Now },
                new Food { FoodId = 2, Name = "Coke", Category = "Beverages", Price = 8.00m, Status = true, CreatedDate = DateTime.Now.AddDays(-1) },
                new Food { FoodId = 3, Name = "Popcorn Deluxe", Category = "Snacks", Price = 20.00m, Status = true, CreatedDate = DateTime.Now.AddDays(-2) }
            };
            context.Foods.AddRange(foods);
            context.SaveChanges();

            var repo = new FoodRepository(context);

            // Act
            var result = await repo.SearchAsync("Popcorn");

            // Assert
            var resultList = result.ToList();
            Assert.Equal(2, resultList.Count);
            Assert.All(resultList, food => Assert.Contains("Popcorn", food.Name));
            Assert.Equal("Popcorn", resultList[0].Name); // Most recent first
            Assert.Equal("Popcorn Deluxe", resultList[1].Name);
        }

        [Fact]
        public async Task SearchAsync_KeywordInDescription_ReturnsMatchingFoods()
        {
            // Arrange
            using var context = GetInMemoryContext();
            var foods = new List<Food>
            {
                new Food { FoodId = 1, Name = "Popcorn", Category = "Snacks", Price = 15.50m, Description = "Fresh popcorn", Status = true, CreatedDate = DateTime.Now },
                new Food { FoodId = 2, Name = "Coke", Category = "Beverages", Price = 8.00m, Description = "Cold drink", Status = true, CreatedDate = DateTime.Now.AddDays(-1) }
            };
            context.Foods.AddRange(foods);
            context.SaveChanges();

            var repo = new FoodRepository(context);

            // Act
            var result = await repo.SearchAsync("Fresh");

            // Assert
            var resultList = result.ToList();
            Assert.Single(resultList);
            Assert.Equal("Popcorn", resultList[0].Name);
            Assert.Contains("Fresh", resultList[0].Description);
        }

        [Fact]
        public async Task SearchAsync_NonExistentKeyword_ReturnsEmptyList()
        {
            // Arrange
            using var context = GetInMemoryContext();
            var repo = new FoodRepository(context);

            // Act
            var result = await repo.SearchAsync("NonExistent");

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public async Task GetByIdAsync_ExistingFood_ReturnsFood()
        {
            // Arrange
            using var context = GetInMemoryContext();
            var food = new Food { FoodId = 1, Name = "Popcorn", Category = "Snacks", Price = 15.50m, Status = true };
            context.Foods.Add(food);
            context.SaveChanges();

            var repo = new FoodRepository(context);

            // Act
            var result = await repo.GetByIdAsync(1);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Popcorn", result.Name);
            Assert.Equal("Snacks", result.Category);
            Assert.Equal(15.50m, result.Price);
        }

        [Fact]
        public async Task GetByIdAsync_NonExistentFood_ReturnsNull()
        {
            // Arrange
            using var context = GetInMemoryContext();
            var repo = new FoodRepository(context);

            // Act
            var result = await repo.GetByIdAsync(999);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task CreateAsync_ValidFood_CreatesAndReturnsFood()
        {
            // Arrange
            using var context = GetInMemoryContext();
            var repo = new FoodRepository(context);
            var food = new Food
            {
                Name = "Popcorn",
                Category = "Snacks",
                Price = 15.50m,
                Description = "Fresh popcorn",
                Image = "popcorn.jpg",
                Status = true
            };

            // Act
            var result = await repo.CreateAsync(food);

            // Assert
            Assert.NotNull(result);
            Assert.NotEqual(default(DateTime), result.CreatedDate);
            Assert.Equal("Popcorn", result.Name);
            Assert.Equal(1, context.Foods.Count());
            Assert.Equal(food.FoodId, context.Foods.First().FoodId);
        }

        [Fact]
        public async Task UpdateAsync_ExistingFood_UpdatesAndReturnsFood()
        {
            // Arrange
            using var context = GetInMemoryContext();
            var food = new Food { FoodId = 1, Name = "Popcorn", Category = "Snacks", Price = 15.50m, Status = true };
            context.Foods.Add(food);
            context.SaveChanges();

            var repo = new FoodRepository(context);
            food.Name = "Popcorn Deluxe";
            food.Price = 20.00m;

            // Act
            var result = await repo.UpdateAsync(food);

            // Assert
            Assert.NotNull(result);
            Assert.NotEqual(default(DateTime), result.UpdatedDate);
            Assert.Equal("Popcorn Deluxe", result.Name);
            Assert.Equal(20.00m, result.Price);
            
            var updatedFood = context.Foods.Find(1);
            Assert.Equal("Popcorn Deluxe", updatedFood.Name);
            Assert.Equal(20.00m, updatedFood.Price);
        }

        [Fact]
        public async Task DeleteAsync_ExistingFood_RemovesFood()
        {
            // Arrange
            using var context = GetInMemoryContext();
            var food = new Food { FoodId = 1, Name = "Popcorn", Category = "Snacks", Price = 15.50m, Status = true };
            context.Foods.Add(food);
            context.SaveChanges();

            var repo = new FoodRepository(context);

            // Act
            await repo.DeleteAsync(1);

            // Assert
            Assert.Empty(context.Foods);
        }

        [Fact]
        public async Task DeleteAsync_NonExistentFood_DoesNothing()
        {
            // Arrange
            using var context = GetInMemoryContext();
            var repo = new FoodRepository(context);

            // Act & Assert - Should not throw exception
            await repo.DeleteAsync(999);
        }

        [Fact]
        public async Task ExistsAsync_ExistingFood_ReturnsTrue()
        {
            // Arrange
            using var context = GetInMemoryContext();
            var food = new Food { FoodId = 1, Name = "Popcorn", Category = "Snacks", Price = 15.50m, Status = true };
            context.Foods.Add(food);
            context.SaveChanges();

            var repo = new FoodRepository(context);

            // Act
            var result = await repo.ExistsAsync(1);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task ExistsAsync_NonExistentFood_ReturnsFalse()
        {
            // Arrange
            using var context = GetInMemoryContext();
            var repo = new FoodRepository(context);

            // Act
            var result = await repo.ExistsAsync(999);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task HasRelatedInvoicesAsync_FoodWithInvoices_ReturnsTrue()
        {
            // Arrange
            using var context = GetInMemoryContext();
            var food = new Food { FoodId = 1, Name = "Popcorn", Category = "Snacks", Price = 15.50m, Status = true };
            var foodInvoice = new FoodInvoice { FoodInvoiceId = 1, InvoiceId = "INV-001", FoodId = 1, Quantity = 2, Price = 15.50m };
            
            context.Foods.Add(food);
            context.FoodInvoices.Add(foodInvoice);
            context.SaveChanges();

            var repo = new FoodRepository(context);

            // Act
            var result = await repo.HasRelatedInvoicesAsync(1);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task HasRelatedInvoicesAsync_FoodWithoutInvoices_ReturnsFalse()
        {
            // Arrange
            using var context = GetInMemoryContext();
            var food = new Food { FoodId = 1, Name = "Popcorn", Category = "Snacks", Price = 15.50m, Status = true };
            context.Foods.Add(food);
            context.SaveChanges();

            var repo = new FoodRepository(context);

            // Act
            var result = await repo.HasRelatedInvoicesAsync(1);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task HasRelatedInvoicesAsync_NonExistentFood_ReturnsFalse()
        {
            // Arrange
            using var context = GetInMemoryContext();
            var repo = new FoodRepository(context);

            // Act
            var result = await repo.HasRelatedInvoicesAsync(999);

            // Assert
            Assert.False(result);
        }
    }
} 