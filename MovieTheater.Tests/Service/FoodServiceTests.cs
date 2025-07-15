using Xunit;
using Moq;
using MovieTheater.Service;
using MovieTheater.Repository;
using MovieTheater.Models;
using MovieTheater.ViewModels;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

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
    }
} 