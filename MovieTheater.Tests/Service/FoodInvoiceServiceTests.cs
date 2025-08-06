using Xunit;
using Moq;
using MovieTheater.Models;
using MovieTheater.Repository;
using MovieTheater.Service;
using MovieTheater.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MovieTheater.Tests.Service
{
    public class FoodInvoiceServiceTests
    {
        private readonly Mock<IFoodInvoiceRepository> _mockRepository;
        private readonly FoodInvoiceService _service;

        public FoodInvoiceServiceTests()
        {
            _mockRepository = new Mock<IFoodInvoiceRepository>();
            _service = new FoodInvoiceService(_mockRepository.Object);
        }

        [Fact]
        public async Task GetFoodsByInvoiceIdAsync_WithValidInvoiceId_ReturnsFoodViewModels()
        {
            // Arrange
            var invoiceId = "INV-001";
            var foodInvoices = new List<FoodInvoice>
            {
                new FoodInvoice
                {
                    FoodInvoiceId = 1,
                    InvoiceId = invoiceId,
                    FoodId = 1,
                    Quantity = 2,
                    Price = 15.50m,
                    Food = new Food
                    {
                        FoodId = 1,
                        Name = "Popcorn",
                        Price = 12.00m,
                        Image = "popcorn.jpg",
                        Description = "Fresh popcorn",
                        Category = "Snacks",
                        Status = true,
                        CreatedDate = DateTime.Now.AddDays(-1),
                        UpdatedDate = DateTime.Now
                    }
                },
                new FoodInvoice
                {
                    FoodInvoiceId = 2,
                    InvoiceId = invoiceId,
                    FoodId = 2,
                    Quantity = 1,
                    Price = 8.00m,
                    Food = new Food
                    {
                        FoodId = 2,
                        Name = "Coke",
                        Price = 7.50m,
                        Image = "coke.jpg",
                        Description = "Cold drink",
                        Category = "Beverages",
                        Status = true,
                        CreatedDate = DateTime.Now.AddDays(-2),
                        UpdatedDate = DateTime.Now
                    }
                }
            };

            _mockRepository.Setup(r => r.GetByInvoiceIdAsync(invoiceId))
                .ReturnsAsync(foodInvoices);

            // Act
            var result = await _service.GetFoodsByInvoiceIdAsync(invoiceId);

            // Assert
            Assert.NotNull(result);
            var resultList = result.ToList();
            Assert.Equal(2, resultList.Count);

            // Check first food
            var firstFood = resultList[0];
            Assert.Equal(1, firstFood.FoodId);
            Assert.Equal("Popcorn", firstFood.Name);
            Assert.Equal(15.50m, firstFood.Price);
            Assert.Equal(12.00m, firstFood.OriginalPrice);
            Assert.Equal("popcorn.jpg", firstFood.Image);
            Assert.Equal("Fresh popcorn", firstFood.Description);
            Assert.Equal("Snacks", firstFood.Category);
            Assert.True(firstFood.Status);
            Assert.Equal(2, firstFood.Quantity);

            // Check second food
            var secondFood = resultList[1];
            Assert.Equal(2, secondFood.FoodId);
            Assert.Equal("Coke", secondFood.Name);
            Assert.Equal(8.00m, secondFood.Price);
            Assert.Equal(7.50m, secondFood.OriginalPrice);
            Assert.Equal("coke.jpg", secondFood.Image);
            Assert.Equal("Cold drink", secondFood.Description);
            Assert.Equal("Beverages", secondFood.Category);
            Assert.True(secondFood.Status);
            Assert.Equal(1, secondFood.Quantity);

            _mockRepository.Verify(r => r.GetByInvoiceIdAsync(invoiceId), Times.Once);
        }

        [Fact]
        public async Task GetFoodsByInvoiceIdAsync_WithEmptyResult_ReturnsEmptyList()
        {
            // Arrange
            var invoiceId = "INV-001";
            _mockRepository.Setup(r => r.GetByInvoiceIdAsync(invoiceId))
                .ReturnsAsync(new List<FoodInvoice>());

            // Act
            var result = await _service.GetFoodsByInvoiceIdAsync(invoiceId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(0, result.Count());

            _mockRepository.Verify(r => r.GetByInvoiceIdAsync(invoiceId), Times.Once);
        }

        [Fact]
        public async Task SaveFoodOrderAsync_WithValidFoods_ReturnsTrue()
        {
            // Arrange
            var invoiceId = "INV-001";
            var selectedFoods = new List<FoodViewModel>
            {
                new FoodViewModel
                {
                    FoodId = 1,
                    Name = "Popcorn",
                    Price = 15.50m,
                    Quantity = 2
                },
                new FoodViewModel
                {
                    FoodId = 2,
                    Name = "Coke",
                    Price = 8.00m,
                    Quantity = 1
                }
            };

            var expectedFoodInvoices = new List<FoodInvoice>
            {
                new FoodInvoice
                {
                    InvoiceId = invoiceId,
                    FoodId = 1,
                    Quantity = 2,
                    Price = 15.50m
                },
                new FoodInvoice
                {
                    InvoiceId = invoiceId,
                    FoodId = 2,
                    Quantity = 1,
                    Price = 8.00m
                }
            };

            _mockRepository.Setup(r => r.CreateMultipleAsync(It.IsAny<IEnumerable<FoodInvoice>>()))
                .ReturnsAsync(expectedFoodInvoices);

            // Act
            var result = await _service.SaveFoodOrderAsync(invoiceId, selectedFoods);

            // Assert
            Assert.True(result);
            _mockRepository.Verify(r => r.CreateMultipleAsync(It.Is<IEnumerable<FoodInvoice>>(fi => 
                fi.Count() == 2 &&
                fi.Any(f => f.InvoiceId == invoiceId && f.FoodId == 1 && f.Quantity == 2 && f.Price == 15.50m) &&
                fi.Any(f => f.InvoiceId == invoiceId && f.FoodId == 2 && f.Quantity == 1 && f.Price == 8.00m)
            )), Times.Once);
        }

        [Fact]
        public async Task SaveFoodOrderAsync_WithNullFoods_ReturnsTrue()
        {
            // Arrange
            var invoiceId = "INV-001";
            List<FoodViewModel> selectedFoods = null;

            // Act
            var result = await _service.SaveFoodOrderAsync(invoiceId, selectedFoods);

            // Assert
            Assert.True(result);
            // Repository should not be called when foods is null
            _mockRepository.Verify(r => r.CreateMultipleAsync(It.IsAny<IEnumerable<FoodInvoice>>()), Times.Never);
        }

        [Fact]
        public async Task SaveFoodOrderAsync_WithEmptyFoods_ReturnsTrue()
        {
            // Arrange
            var invoiceId = "INV-001";
            var selectedFoods = new List<FoodViewModel>();

            // Act
            var result = await _service.SaveFoodOrderAsync(invoiceId, selectedFoods);

            // Assert
            Assert.True(result);
            // Repository should not be called when there are no foods to save
            _mockRepository.Verify(r => r.CreateMultipleAsync(It.IsAny<IEnumerable<FoodInvoice>>()), Times.Never);
        }

        [Fact]
        public async Task SaveFoodOrderAsync_WhenRepositoryThrowsException_ReturnsFalse()
        {
            // Arrange
            var invoiceId = "INV-001";
            var selectedFoods = new List<FoodViewModel>
            {
                new FoodViewModel
                {
                    FoodId = 1,
                    Name = "Popcorn",
                    Price = 15.50m,
                    Quantity = 2
                }
            };

            _mockRepository.Setup(r => r.CreateMultipleAsync(It.IsAny<IEnumerable<FoodInvoice>>()))
                .ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _service.SaveFoodOrderAsync(invoiceId, selectedFoods);

            // Assert
            Assert.False(result);
            _mockRepository.Verify(r => r.CreateMultipleAsync(It.IsAny<IEnumerable<FoodInvoice>>()), Times.Once);
        }

        [Fact]
        public async Task GetTotalFoodPriceByInvoiceIdAsync_WithValidInvoiceId_ReturnsCorrectTotal()
        {
            // Arrange
            var invoiceId = "INV-001";
            var foodInvoices = new List<FoodInvoice>
            {
                new FoodInvoice
                {
                    FoodInvoiceId = 1,
                    InvoiceId = invoiceId,
                    FoodId = 1,
                    Quantity = 2,
                    Price = 15.50m
                },
                new FoodInvoice
                {
                    FoodInvoiceId = 2,
                    InvoiceId = invoiceId,
                    FoodId = 2,
                    Quantity = 1,
                    Price = 8.00m
                },
                new FoodInvoice
                {
                    FoodInvoiceId = 3,
                    InvoiceId = invoiceId,
                    FoodId = 3,
                    Quantity = 3,
                    Price = 5.25m
                }
            };

            _mockRepository.Setup(r => r.GetByInvoiceIdAsync(invoiceId))
                .ReturnsAsync(foodInvoices);

            // Act
            var result = await _service.GetTotalFoodPriceByInvoiceIdAsync(invoiceId);

            // Assert
            var expectedTotal = (2 * 15.50m) + (1 * 8.00m) + (3 * 5.25m);
            Assert.Equal(expectedTotal, result);

            _mockRepository.Verify(r => r.GetByInvoiceIdAsync(invoiceId), Times.Once);
        }

        [Fact]
        public async Task GetTotalFoodPriceByInvoiceIdAsync_WithEmptyResult_ReturnsZero()
        {
            // Arrange
            var invoiceId = "INV-001";
            _mockRepository.Setup(r => r.GetByInvoiceIdAsync(invoiceId))
                .ReturnsAsync(new List<FoodInvoice>());

            // Act
            var result = await _service.GetTotalFoodPriceByInvoiceIdAsync(invoiceId);

            // Assert
            Assert.Equal(0m, result);

            _mockRepository.Verify(r => r.GetByInvoiceIdAsync(invoiceId), Times.Once);
        }

        [Fact]
        public async Task GetTotalFoodPriceByInvoiceIdAsync_WithZeroQuantities_ReturnsZero()
        {
            // Arrange
            var invoiceId = "INV-001";
            var foodInvoices = new List<FoodInvoice>
            {
                new FoodInvoice
                {
                    FoodInvoiceId = 1,
                    InvoiceId = invoiceId,
                    FoodId = 1,
                    Quantity = 0,
                    Price = 15.50m
                },
                new FoodInvoice
                {
                    FoodInvoiceId = 2,
                    InvoiceId = invoiceId,
                    FoodId = 2,
                    Quantity = 0,
                    Price = 8.00m
                }
            };

            _mockRepository.Setup(r => r.GetByInvoiceIdAsync(invoiceId))
                .ReturnsAsync(foodInvoices);

            // Act
            var result = await _service.GetTotalFoodPriceByInvoiceIdAsync(invoiceId);

            // Assert
            Assert.Equal(0m, result);

            _mockRepository.Verify(r => r.GetByInvoiceIdAsync(invoiceId), Times.Once);
        }

        [Fact]
        public async Task GetTotalFoodPriceByInvoiceIdAsync_WithZeroPrices_ReturnsZero()
        {
            // Arrange
            var invoiceId = "INV-001";
            var foodInvoices = new List<FoodInvoice>
            {
                new FoodInvoice
                {
                    FoodInvoiceId = 1,
                    InvoiceId = invoiceId,
                    FoodId = 1,
                    Quantity = 2,
                    Price = 0m
                },
                new FoodInvoice
                {
                    FoodInvoiceId = 2,
                    InvoiceId = invoiceId,
                    FoodId = 2,
                    Quantity = 1,
                    Price = 0m
                }
            };

            _mockRepository.Setup(r => r.GetByInvoiceIdAsync(invoiceId))
                .ReturnsAsync(foodInvoices);

            // Act
            var result = await _service.GetTotalFoodPriceByInvoiceIdAsync(invoiceId);

            // Assert
            Assert.Equal(0m, result);

            _mockRepository.Verify(r => r.GetByInvoiceIdAsync(invoiceId), Times.Once);
        }

        [Fact]
        public void Constructor_WithValidRepository_CreatesInstance()
        {
            // Arrange & Act
            var service = new FoodInvoiceService(_mockRepository.Object);

            // Assert
            Assert.NotNull(service);
        }
    }
} 