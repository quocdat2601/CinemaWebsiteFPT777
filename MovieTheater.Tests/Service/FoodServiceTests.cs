using Xunit;
using Moq;
using MovieTheater.Service;
using MovieTheater.Repository;
using MovieTheater.Models;
using MovieTheater.ViewModels;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.AspNetCore.Http;
using System;
using System.IO;

namespace MovieTheater.Tests.Service
{
    public class FoodServiceTests
    {
        private readonly Mock<IFoodRepository> _mockRepo;
        private readonly FoodService _service;

        public FoodServiceTests()
        {
            _mockRepo = new Mock<IFoodRepository>();
            _service = new FoodService(_mockRepo.Object);
        }

        [Fact]
        public async Task GetAllAsync_ReturnsAllFoods()
        {
            // Arrange
            var foods = new List<Food>
            {
                new Food { FoodId = 1, Name = "Popcorn", Category = "food", Price = 50000, Status = true },
                new Food { FoodId = 2, Name = "Coke", Category = "drink", Price = 30000, Status = true }
            };
            _mockRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(foods);

            // Act
            var result = await _service.GetAllAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Foods.Count);
            Assert.Contains(result.Foods, f => f.Name == "Popcorn");
            Assert.Contains(result.Foods, f => f.Name == "Coke");
        }

        [Fact]
        public async Task GetByIdAsync_ReturnsFoodViewModel_WhenExists()
        {
            // Arrange
            var food = new Food { FoodId = 1, Name = "Popcorn", Category = "food", Price = 50000, Status = true };
            _mockRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(food);

            // Act
            var result = await _service.GetByIdAsync(1);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Popcorn", result.Name);
        }

        [Fact]
        public async Task GetByIdAsync_ReturnsNull_WhenNotExists()
        {
            _mockRepo.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Food)null);
            var result = await _service.GetByIdAsync(99);
            Assert.Null(result);
        }

        [Fact]
        public async Task CreateAsync_ReturnsTrue_WhenSuccess()
        {
            // Arrange
            var model = new FoodViewModel { Name = "Popcorn", Category = "food", Price = 50000, Status = true };
            _mockRepo.Setup(r => r.CreateAsync(It.IsAny<Food>())).ReturnsAsync(new Food { FoodId = 1 });

            // Act
            var result = await _service.CreateAsync(model, "wwwroot");

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task UpdateAsync_ReturnsTrue_WhenSuccess()
        {
            // Arrange
            var food = new Food { FoodId = 1, Name = "Popcorn", Category = "food", Price = 50000, Status = true };
            _mockRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(food);
            _mockRepo.Setup(r => r.UpdateAsync(It.IsAny<Food>())).ReturnsAsync(food);
            var model = new FoodViewModel { FoodId = 1, Name = "Popcorn", Category = "food", Price = 50000, Status = true };

            // Act
            var result = await _service.UpdateAsync(model, "wwwroot");

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task DeleteAsync_ReturnsTrue_WhenSuccess()
        {
            _mockRepo.Setup(r => r.DeleteAsync(1)).Returns(Task.CompletedTask);
            var result = await _service.DeleteAsync(1);
            Assert.True(result);
        }

        [Fact]
        public async Task ToggleStatusAsync_TogglesStatus()
        {
            var food = new Food { FoodId = 1, Name = "Popcorn", Category = "food", Price = 50000, Status = true };
            _mockRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(food);
            _mockRepo.Setup(r => r.UpdateAsync(It.IsAny<Food>())).ReturnsAsync(food);
            var result = await _service.ToggleStatusAsync(1);
            Assert.True(result);
            Assert.False(food.Status); // Đã bị đảo trạng thái
        }

        private IFormFile MockImageFile(string fileName, long length)
        {
            var fileMock = new Mock<IFormFile>();
            fileMock.Setup(f => f.FileName).Returns(fileName);
            fileMock.Setup(f => f.Length).Returns(length);
            fileMock.Setup(f => f.CopyToAsync(It.IsAny<Stream>(), default)).Returns(Task.CompletedTask);
            return fileMock.Object;
        }

        [Fact]
        public async Task CreateAsync_ReturnsFalse_WhenImageFileInvalidExtension()
        {
            var model = new FoodViewModel
            {
                Name = "Popcorn",
                Category = "food",
                Price = 50000,
                Status = true,
                ImageFile = MockImageFile("test.txt", 1000)
            };
            var result = await _service.CreateAsync(model, "wwwroot");
            Assert.False(result);
        }

        [Fact]
        public async Task CreateAsync_ReturnsFalse_WhenImageFileTooLarge()
        {
            var model = new FoodViewModel
            {
                Name = "Popcorn",
                Category = "food",
                Price = 50000,
                Status = true,
                ImageFile = MockImageFile("test.jpg", 3 * 1024 * 1024)
            };
            var result = await _service.CreateAsync(model, "wwwroot");
            Assert.False(result);
        }

        [Fact]
        public async Task CreateAsync_ReturnsFalse_WhenFileNameInvalid()
        {
            var model = new FoodViewModel
            {
                Name = "Popcorn",
                Category = "food",
                Price = 50000,
                Status = true,
                ImageFile = MockImageFile("test/?.jpg", 1000)
            };
            var result = await _service.CreateAsync(model, "wwwroot");
            Assert.False(result);
        }

        [Fact]
        public async Task CreateAsync_ReturnsFalse_WhenRepositoryThrows()
        {
            _mockRepo.Setup(r => r.CreateAsync(It.IsAny<Food>())).ThrowsAsync(new Exception());
            var model = new FoodViewModel { Name = "Popcorn", Category = "food", Price = 50000, Status = true };
            var result = await _service.CreateAsync(model, "wwwroot");
            Assert.False(result);
        }

        [Fact]
        public async Task UpdateAsync_ReturnsFalse_WhenFoodNotFound()
        {
            _mockRepo.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Food)null);
            var model = new FoodViewModel { FoodId = 99, Name = "Popcorn", Category = "food", Price = 50000, Status = true };
            var result = await _service.UpdateAsync(model, "wwwroot");
            Assert.False(result);
        }

        [Fact]
        public async Task UpdateAsync_ReturnsFalse_WhenRepositoryThrows()
        {
            _mockRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new Food { FoodId = 1 });
            _mockRepo.Setup(r => r.UpdateAsync(It.IsAny<Food>())).ThrowsAsync(new Exception());
            var model = new FoodViewModel { FoodId = 1, Name = "Popcorn", Category = "food", Price = 50000, Status = true };
            var result = await _service.UpdateAsync(model, "wwwroot");
            Assert.False(result);
        }

        [Fact]
        public async Task DeleteAsync_ReturnsFalse_WhenRepositoryThrows()
        {
            _mockRepo.Setup(r => r.DeleteAsync(1)).ThrowsAsync(new Exception());
            var result = await _service.DeleteAsync(1);
            Assert.False(result);
        }

        [Fact]
        public async Task ToggleStatusAsync_ReturnsFalse_WhenFoodNotFound()
        {
            _mockRepo.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Food)null);
            var result = await _service.ToggleStatusAsync(99);
            Assert.False(result);
        }

        [Fact]
        public async Task ToggleStatusAsync_ReturnsFalse_WhenRepositoryThrows()
        {
            _mockRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(new Food { FoodId = 1, Status = true });
            _mockRepo.Setup(r => r.UpdateAsync(It.IsAny<Food>())).ThrowsAsync(new Exception());
            var result = await _service.ToggleStatusAsync(1);
            Assert.False(result);
        }

        [Fact]
        public async Task GetAllAsync_UsesSearchAsync_WhenSearchKeywordProvided()
        {
            _mockRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Food> { new Food { FoodId = 1, Name = "Popcorn" } });
            var result = await _service.GetAllAsync("pop", null, null);
            Assert.Single(result.Foods);
            Assert.Equal("Popcorn", result.Foods[0].Name);
        }

        [Fact]
        public async Task GetAllAsync_UsesGetByCategoryAsync_WhenCategoryProvided()
        {
            _mockRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Food> { new Food { FoodId = 1, Name = "Popcorn", Category = "food" } });
            var result = await _service.GetAllAsync(null, "food", null);
            Assert.Single(result.Foods);
            Assert.Equal("food", result.Foods[0].Category);
        }

        [Fact]
        public async Task GetAllAsync_UsesGetByStatusAsync_WhenStatusProvided()
        {
            _mockRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Food> { new Food { FoodId = 1, Name = "Popcorn", Status = true } });
            var result = await _service.GetAllAsync(null, null, true);
            Assert.Single(result.Foods);
            Assert.True(result.Foods[0].Status);
        }

        [Fact]
        public async Task GetCategoriesAsync_ReturnsDistinctCategories()
        {
            _mockRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Food> {
                new Food { FoodId = 1, Category = "food" },
                new Food { FoodId = 2, Category = "drink" },
                new Food { FoodId = 3, Category = "food" }
            });
            var result = await _service.GetCategoriesAsync();
            Assert.Equal(2, result.Count);
            Assert.Contains("food", result);
            Assert.Contains("drink", result);
        }
    }
} 