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
        public async Task GetDomainByIdAsync_ReturnsFood_WhenExists()
        {
            // Arrange
            var food = new Food { FoodId = 1, Name = "Popcorn", Category = "food", Price = 50000, Status = true };
            _mockRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(food);

            // Act
            var result = await _service.GetDomainByIdAsync(1);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Popcorn", result.Name);
        }

        [Fact]
        public async Task GetDomainByIdAsync_ReturnsNull_WhenNotExists()
        {
            // Arrange
            _mockRepo.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Food)null);

            // Act
            var result = await _service.GetDomainByIdAsync(99);

            // Assert
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
        public async Task CreateAsync_ReturnsTrue_WhenSuccessWithImage()
        {
            // Arrange
            var model = new FoodViewModel 
            { 
                Name = "Popcorn", 
                Category = "food", 
                Price = 50000, 
                Status = true,
                ImageFile = MockImageFile("test.jpg", 1000)
            };
            _mockRepo.Setup(r => r.CreateAsync(It.IsAny<Food>())).ReturnsAsync(new Food { FoodId = 1 });

            // Act
            var result = await _service.CreateAsync(model, "wwwroot");

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task CreateAsync_ReturnsTrue_WhenSuccessWithTrimmedData()
        {
            // Arrange
            var model = new FoodViewModel 
            { 
                Name = "  Popcorn  ", 
                Category = "  food  ", 
                Description = "  Test description  ",
                Price = 50000, 
                Status = true 
            };
            _mockRepo.Setup(r => r.CreateAsync(It.IsAny<Food>())).ReturnsAsync(new Food { FoodId = 1 });

            // Act
            var result = await _service.CreateAsync(model, "wwwroot");

            // Assert
            Assert.True(result);
            _mockRepo.Verify(r => r.CreateAsync(It.Is<Food>(f => 
                f.Name == "Popcorn" && 
                f.Category == "food" && 
                f.Description == "Test description")), Times.Once);
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
        public async Task CreateAsync_ReturnsFalse_WhenImageFileThrowsException()
        {
            var model = new FoodViewModel
            {
                Name = "Popcorn",
                Category = "food",
                Price = 50000,
                Status = true,
                ImageFile = MockImageFileWithException("test.jpg", 1000)
            };
            var result = await _service.CreateAsync(model, "wwwroot");
            Assert.False(result);
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
        public async Task UpdateAsync_ReturnsTrue_WhenSuccessWithImage()
        {
            // Arrange
            var food = new Food { FoodId = 1, Name = "Popcorn", Category = "food", Price = 50000, Status = true };
            _mockRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(food);
            _mockRepo.Setup(r => r.UpdateAsync(It.IsAny<Food>())).ReturnsAsync(food);
            var model = new FoodViewModel 
            { 
                FoodId = 1, 
                Name = "Popcorn", 
                Category = "food", 
                Price = 50000, 
                Status = true,
                ImageFile = MockImageFile("test.jpg", 1000)
            };

            // Act
            var result = await _service.UpdateAsync(model, "wwwroot");

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task UpdateAsync_ReturnsTrue_WhenSuccessWithImageAndOldImageExists()
        {
            // Arrange
            var food = new Food { FoodId = 1, Name = "Popcorn", Category = "food", Price = 50000, Status = true, Image = "/images/foods/old.jpg" };
            _mockRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(food);
            _mockRepo.Setup(r => r.UpdateAsync(It.IsAny<Food>())).ReturnsAsync(food);
            var model = new FoodViewModel 
            { 
                FoodId = 1, 
                Name = "Popcorn", 
                Category = "food", 
                Price = 50000, 
                Status = true,
                ImageFile = MockImageFile("test.jpg", 1000)
            };

            // Act
            var result = await _service.UpdateAsync(model, "wwwroot");

            // Assert
            Assert.True(result);
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
        public async Task UpdateAsync_ReturnsFalse_WhenImageFileInvalidExtension()
        {
            var food = new Food { FoodId = 1, Name = "Popcorn", Category = "food", Price = 50000, Status = true };
            _mockRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(food);
            var model = new FoodViewModel
            {
                FoodId = 1,
                Name = "Popcorn",
                Category = "food",
                Price = 50000,
                Status = true,
                ImageFile = MockImageFile("test.txt", 1000)
            };
            var result = await _service.UpdateAsync(model, "wwwroot");
            Assert.False(result);
        }

        [Fact]
        public async Task UpdateAsync_ReturnsFalse_WhenImageFileTooLarge()
        {
            var food = new Food { FoodId = 1, Name = "Popcorn", Category = "food", Price = 50000, Status = true };
            _mockRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(food);
            var model = new FoodViewModel
            {
                FoodId = 1,
                Name = "Popcorn",
                Category = "food",
                Price = 50000,
                Status = true,
                ImageFile = MockImageFile("test.jpg", 3 * 1024 * 1024)
            };
            var result = await _service.UpdateAsync(model, "wwwroot");
            Assert.False(result);
        }

        [Fact]
        public async Task UpdateAsync_ReturnsFalse_WhenFileNameInvalid()
        {
            var food = new Food { FoodId = 1, Name = "Popcorn", Category = "food", Price = 50000, Status = true };
            _mockRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(food);
            var model = new FoodViewModel
            {
                FoodId = 1,
                Name = "Popcorn",
                Category = "food",
                Price = 50000,
                Status = true,
                ImageFile = MockImageFile("test/?.jpg", 1000)
            };
            var result = await _service.UpdateAsync(model, "wwwroot");
            Assert.False(result);
        }

        [Fact]
        public async Task DeleteAsync_ReturnsTrue_WhenSuccess()
        {
            _mockRepo.Setup(r => r.DeleteAsync(1)).Returns(Task.CompletedTask);
            var result = await _service.DeleteAsync(1);
            Assert.True(result);
        }

        [Fact]
        public async Task DeleteAsync_ReturnsFalse_WhenRepositoryThrows()
        {
            _mockRepo.Setup(r => r.DeleteAsync(1)).ThrowsAsync(new Exception());
            var result = await _service.DeleteAsync(1);
            Assert.False(result);
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
        public async Task GetAllAsync_UsesSearchAsync_WhenSearchKeywordProvidedInDescription()
        {
            _mockRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Food> { new Food { FoodId = 1, Name = "Popcorn", Description = "Delicious popcorn" } });
            var result = await _service.GetAllAsync("delicious", null, null);
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
        public async Task GetAllAsync_CombinesAllFilters()
        {
            _mockRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Food> { 
                new Food { FoodId = 1, Name = "Popcorn", Category = "food", Status = true, Description = "Delicious popcorn" } 
            });
            var result = await _service.GetAllAsync("delicious", "food", true);
            Assert.Single(result.Foods);
            Assert.Equal("Popcorn", result.Foods[0].Name);
        }

        [Fact]
        public async Task GetAllAsync_ReturnsEmptyList_WhenNoFoodsMatchSearch()
        {
            _mockRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Food> { 
                new Food { FoodId = 1, Name = "Popcorn", Description = "Delicious popcorn" } 
            });
            var result = await _service.GetAllAsync("nonexistent", null, null);
            Assert.Empty(result.Foods);
        }

        [Fact]
        public async Task GetAllAsync_ReturnsEmptyList_WhenNoFoodsMatchCategory()
        {
            _mockRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Food> { 
                new Food { FoodId = 1, Name = "Popcorn", Category = "food" } 
            });
            var result = await _service.GetAllAsync(null, "drink", null);
            Assert.Empty(result.Foods);
        }

        [Fact]
        public async Task GetAllAsync_ReturnsEmptyList_WhenNoFoodsMatchStatus()
        {
            _mockRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Food> { 
                new Food { FoodId = 1, Name = "Popcorn", Status = true } 
            });
            var result = await _service.GetAllAsync(null, null, false);
            Assert.Empty(result.Foods);
        }

        [Fact]
        public async Task GetAllAsync_HandlesNullValuesInFoodProperties()
        {
            _mockRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Food> { 
                new Food { FoodId = 1, Name = null, Description = null, Category = null } 
            });
            var result = await _service.GetAllAsync("test", "test", true);
            Assert.Empty(result.Foods);
        }

        [Fact]
        public async Task GetAllAsync_HandlesEmptyStringsInFoodProperties()
        {
            _mockRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Food> { 
                new Food { FoodId = 1, Name = "", Description = "", Category = "" } 
            });
            var result = await _service.GetAllAsync("test", "test", true);
            Assert.Empty(result.Foods);
        }

        [Fact]
        public async Task GetAllAsync_HandlesWhitespaceStringsInFoodProperties()
        {
            _mockRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Food> { 
                new Food { FoodId = 1, Name = "   ", Description = "   ", Category = "   " } 
            });
            var result = await _service.GetAllAsync("test", "test", true);
            Assert.Empty(result.Foods);
        }

        [Fact]
        public async Task GetAllAsync_ReturnsCorrectViewModelProperties()
        {
            var testDate = DateTime.Now;
            _mockRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Food> { 
                new Food { 
                    FoodId = 1, 
                    Name = "Popcorn", 
                    Category = "food", 
                    Price = 50000, 
                    Description = "Delicious popcorn",
                    Image = "/images/foods/popcorn.jpg",
                    Status = true,
                    CreatedDate = testDate,
                    UpdatedDate = testDate
                } 
            });
            var result = await _service.GetAllAsync();
            
            Assert.Single(result.Foods);
            var food = result.Foods[0];
            Assert.Equal(1, food.FoodId);
            Assert.Equal("Popcorn", food.Name);
            Assert.Equal("food", food.Category);
            Assert.Equal(50000, food.Price);
            Assert.Equal("Delicious popcorn", food.Description);
            Assert.Equal("/images/foods/popcorn.jpg", food.Image);
            Assert.True(food.Status);
            Assert.Equal(testDate, food.CreatedDate);
            Assert.Equal(testDate, food.UpdatedDate);
        }

        [Fact]
        public async Task GetByIdAsync_ReturnsCorrectViewModelProperties()
        {
            var testDate = DateTime.Now;
            var food = new Food { 
                FoodId = 1, 
                Name = "Popcorn", 
                Category = "food", 
                Price = 50000, 
                Description = "Delicious popcorn",
                Image = "/images/foods/popcorn.jpg",
                Status = true,
                CreatedDate = testDate,
                UpdatedDate = testDate
            };
            _mockRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(food);

            var result = await _service.GetByIdAsync(1);

            Assert.NotNull(result);
            Assert.Equal(1, result.FoodId);
            Assert.Equal("Popcorn", result.Name);
            Assert.Equal("food", result.Category);
            Assert.Equal(50000, result.Price);
            Assert.Equal("Delicious popcorn", result.Description);
            Assert.Equal("/images/foods/popcorn.jpg", result.Image);
            Assert.True(result.Status);
            Assert.Equal(testDate, result.CreatedDate);
            Assert.Equal(testDate, result.UpdatedDate);
        }

        [Fact]
        public async Task CreateAsync_ReturnsTrue_WhenSuccessWithoutImage()
        {
            var model = new FoodViewModel { Name = "Popcorn", Category = "food", Price = 50000, Status = true };
            _mockRepo.Setup(r => r.CreateAsync(It.IsAny<Food>())).ReturnsAsync(new Food { FoodId = 1 });

            var result = await _service.CreateAsync(model, "wwwroot");

            Assert.True(result);
            _mockRepo.Verify(r => r.CreateAsync(It.Is<Food>(f => 
                f.Name == "Popcorn" && 
                f.Category == "food" && 
                f.Price == 50000 && 
                f.Status == true)), Times.Once);
        }

        [Fact]
        public async Task CreateAsync_ReturnsTrue_WhenSuccessWithNullDescription()
        {
            var model = new FoodViewModel { 
                Name = "Popcorn", 
                Category = "food", 
                Price = 50000, 
                Status = true,
                Description = null
            };
            _mockRepo.Setup(r => r.CreateAsync(It.IsAny<Food>())).ReturnsAsync(new Food { FoodId = 1 });

            var result = await _service.CreateAsync(model, "wwwroot");

            Assert.True(result);
            _mockRepo.Verify(r => r.CreateAsync(It.Is<Food>(f => 
                f.Description == null)), Times.Once);
        }

        [Fact]
        public async Task UpdateAsync_ReturnsTrue_WhenSuccessWithoutImage()
        {
            var food = new Food { FoodId = 1, Name = "Popcorn", Category = "food", Price = 50000, Status = true };
            _mockRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(food);
            _mockRepo.Setup(r => r.UpdateAsync(It.IsAny<Food>())).ReturnsAsync(food);
            var model = new FoodViewModel { FoodId = 1, Name = "Updated Popcorn", Category = "food", Price = 60000, Status = false };

            var result = await _service.UpdateAsync(model, "wwwroot");

            Assert.True(result);
            _mockRepo.Verify(r => r.UpdateAsync(It.Is<Food>(f => 
                f.Name == "Updated Popcorn" && 
                f.Price == 60000 && 
                f.Status == false)), Times.Once);
        }

        [Fact]
        public async Task UpdateAsync_ReturnsTrue_WhenSuccessWithNullDescription()
        {
            var food = new Food { FoodId = 1, Name = "Popcorn", Category = "food", Price = 50000, Status = true };
            _mockRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(food);
            _mockRepo.Setup(r => r.UpdateAsync(It.IsAny<Food>())).ReturnsAsync(food);
            var model = new FoodViewModel { 
                FoodId = 1, 
                Name = "Popcorn", 
                Category = "food", 
                Price = 50000, 
                Status = true,
                Description = null
            };

            var result = await _service.UpdateAsync(model, "wwwroot");

            Assert.True(result);
            _mockRepo.Verify(r => r.UpdateAsync(It.Is<Food>(f => 
                f.Description == null)), Times.Once);
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

        [Fact]
        public async Task GetCategoriesAsync_ReturnsEmptyList_WhenNoFoods()
        {
            _mockRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Food>());
            var result = await _service.GetCategoriesAsync();
            Assert.Empty(result);
        }

        [Fact]
        public async Task GetCategoriesAsync_ReturnsSingleNull_WhenAllCategoriesAreNull()
        {
            _mockRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Food> {
                new Food { FoodId = 1, Category = null },
                new Food { FoodId = 2, Category = null }
            });
            var result = await _service.GetCategoriesAsync();
            Assert.Single(result);
            Assert.Null(result[0]);
        }

        [Fact]
        public async Task GetCategoriesAsync_ReturnsSingleEmptyString_WhenAllCategoriesAreEmpty()
        {
            _mockRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Food> {
                new Food { FoodId = 1, Category = "" },
                new Food { FoodId = 2, Category = "" }
            });
            var result = await _service.GetCategoriesAsync();
            Assert.Single(result);
            Assert.Equal("", result[0]);
        }

        [Fact]
        public async Task GetCategoriesAsync_HandlesMixedNullAndValidCategories()
        {
            _mockRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Food> {
                new Food { FoodId = 1, Category = "food" },
                new Food { FoodId = 2, Category = null },
                new Food { FoodId = 3, Category = "drink" },
                new Food { FoodId = 4, Category = "" }
            });
            var result = await _service.GetCategoriesAsync();
            Assert.Equal(4, result.Count);
            Assert.Contains("food", result);
            Assert.Contains("drink", result);
            Assert.Contains(null, result);
            Assert.Contains("", result);
        }

        [Fact]
        public async Task HasRelatedInvoicesAsync_ReturnsTrue_WhenHasRelatedInvoices()
        {
            // Arrange
            _mockRepo.Setup(r => r.HasRelatedInvoicesAsync(1)).ReturnsAsync(true);

            // Act
            var result = await _service.HasRelatedInvoicesAsync(1);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task HasRelatedInvoicesAsync_ReturnsFalse_WhenNoRelatedInvoices()
        {
            // Arrange
            _mockRepo.Setup(r => r.HasRelatedInvoicesAsync(1)).ReturnsAsync(false);

            // Act
            var result = await _service.HasRelatedInvoicesAsync(1);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task HasRelatedInvoicesAsync_ReturnsFalse_WhenRepositoryThrows()
        {
            // Arrange
            _mockRepo.Setup(r => r.HasRelatedInvoicesAsync(1)).ThrowsAsync(new Exception());

            // Act
            var result = await _service.HasRelatedInvoicesAsync(1);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task CreateAsync_ReturnsTrue_WhenDirectoryDoesNotExist()
        {
            var model = new FoodViewModel 
            { 
                Name = "Popcorn", 
                Category = "food", 
                Price = 50000, 
                Status = true,
                ImageFile = MockImageFile("test.jpg", 1000)
            };
            _mockRepo.Setup(r => r.CreateAsync(It.IsAny<Food>())).ReturnsAsync(new Food { FoodId = 1 });

            var result = await _service.CreateAsync(model, "wwwroot");

            Assert.True(result);
        }

        [Fact]
        public async Task UpdateAsync_ReturnsTrue_WhenDirectoryDoesNotExist()
        {
            var food = new Food { FoodId = 1, Name = "Popcorn", Category = "food", Price = 50000, Status = true };
            _mockRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(food);
            _mockRepo.Setup(r => r.UpdateAsync(It.IsAny<Food>())).ReturnsAsync(food);
            var model = new FoodViewModel 
            { 
                FoodId = 1, 
                Name = "Popcorn", 
                Category = "food", 
                Price = 50000, 
                Status = true,
                ImageFile = MockImageFile("test.jpg", 1000)
            };

            var result = await _service.UpdateAsync(model, "wwwroot");

            Assert.True(result);
        }

        [Fact]
        public async Task UpdateAsync_ReturnsTrue_WhenOldImageDoesNotExist()
        {
            var food = new Food { FoodId = 1, Name = "Popcorn", Category = "food", Price = 50000, Status = true, Image = "/images/foods/nonexistent.jpg" };
            _mockRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(food);
            _mockRepo.Setup(r => r.UpdateAsync(It.IsAny<Food>())).ReturnsAsync(food);
            var model = new FoodViewModel 
            { 
                FoodId = 1, 
                Name = "Popcorn", 
                Category = "food", 
                Price = 50000, 
                Status = true,
                ImageFile = MockImageFile("test.jpg", 1000)
            };

            var result = await _service.UpdateAsync(model, "wwwroot");

            Assert.True(result);
        }

        [Fact]
        public async Task UpdateAsync_ReturnsTrue_WhenOldImageIsEmpty()
        {
            var food = new Food { FoodId = 1, Name = "Popcorn", Category = "food", Price = 50000, Status = true, Image = "" };
            _mockRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(food);
            _mockRepo.Setup(r => r.UpdateAsync(It.IsAny<Food>())).ReturnsAsync(food);
            var model = new FoodViewModel 
            { 
                FoodId = 1, 
                Name = "Popcorn", 
                Category = "food", 
                Price = 50000, 
                Status = true,
                ImageFile = MockImageFile("test.jpg", 1000)
            };

            var result = await _service.UpdateAsync(model, "wwwroot");

            Assert.True(result);
        }

        [Fact]
        public async Task UpdateAsync_ReturnsTrue_WhenOldImageIsNull()
        {
            var food = new Food { FoodId = 1, Name = "Popcorn", Category = "food", Price = 50000, Status = true, Image = null };
            _mockRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(food);
            _mockRepo.Setup(r => r.UpdateAsync(It.IsAny<Food>())).ReturnsAsync(food);
            var model = new FoodViewModel 
            { 
                FoodId = 1, 
                Name = "Popcorn", 
                Category = "food", 
                Price = 50000, 
                Status = true,
                ImageFile = MockImageFile("test.jpg", 1000)
            };

            var result = await _service.UpdateAsync(model, "wwwroot");

            Assert.True(result);
        }

        [Fact]
        public async Task GetAllAsync_ReturnsCorrectViewModelProperties_WithNullValues()
        {
            var testDate = DateTime.Now;
            _mockRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Food> { 
                new Food { 
                    FoodId = 1, 
                    Name = "Popcorn", 
                    Category = "food", 
                    Price = 50000, 
                    Description = null,
                    Image = null,
                    Status = true,
                    CreatedDate = testDate,
                    UpdatedDate = testDate
                } 
            });
            var result = await _service.GetAllAsync();
            
            Assert.Single(result.Foods);
            var food = result.Foods[0];
            Assert.Equal(1, food.FoodId);
            Assert.Equal("Popcorn", food.Name);
            Assert.Equal("food", food.Category);
            Assert.Equal(50000, food.Price);
            Assert.Null(food.Description);
            Assert.Null(food.Image);
            Assert.True(food.Status);
            Assert.Equal(testDate, food.CreatedDate);
            Assert.Equal(testDate, food.UpdatedDate);
        }

        [Fact]
        public async Task GetByIdAsync_ReturnsCorrectViewModelProperties_WithNullValues()
        {
            var testDate = DateTime.Now;
            var food = new Food { 
                FoodId = 1, 
                Name = "Popcorn", 
                Category = "food", 
                Price = 50000, 
                Description = null,
                Image = null,
                Status = true,
                CreatedDate = testDate,
                UpdatedDate = testDate
            };
            _mockRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(food);

            var result = await _service.GetByIdAsync(1);

            Assert.NotNull(result);
            Assert.Equal(1, result.FoodId);
            Assert.Equal("Popcorn", result.Name);
            Assert.Equal("food", result.Category);
            Assert.Equal(50000, result.Price);
            Assert.Null(result.Description);
            Assert.Null(result.Image);
            Assert.True(result.Status);
            Assert.Equal(testDate, result.CreatedDate);
            Assert.Equal(testDate, result.UpdatedDate);
        }

        private IFormFile MockImageFile(string fileName, long length)
        {
            var fileMock = new Mock<IFormFile>();
            fileMock.Setup(f => f.FileName).Returns(fileName);
            fileMock.Setup(f => f.Length).Returns(length);
            fileMock.Setup(f => f.CopyToAsync(It.IsAny<Stream>(), default)).Returns(Task.CompletedTask);
            return fileMock.Object;
        }

        private IFormFile MockImageFileWithException(string fileName, long length)
        {
            var fileMock = new Mock<IFormFile>();
            fileMock.Setup(f => f.FileName).Returns(fileName);
            fileMock.Setup(f => f.Length).Returns(length);
            fileMock.Setup(f => f.CopyToAsync(It.IsAny<Stream>(), default)).ThrowsAsync(new Exception("File copy failed"));
            return fileMock.Object;
        }
    }
} 