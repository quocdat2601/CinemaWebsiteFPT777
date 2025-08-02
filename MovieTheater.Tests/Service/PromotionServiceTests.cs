using System;
using System.Collections.Generic;
using System.Linq;
using Moq;
using MovieTheater.Models;
using MovieTheater.Repository;
using MovieTheater.Service;
using Xunit;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace MovieTheater.Tests.Service
{
    public class PromotionServiceTests
    {
        private readonly Mock<IPromotionRepository> _mockRepo;
        private readonly Mock<MovieTheaterContext> _mockContext;
        private readonly PromotionService _service;

        public PromotionServiceTests()
        {
            _mockRepo = new Mock<IPromotionRepository>();
            _mockContext = new Mock<MovieTheaterContext>();
            _service = new PromotionService(_mockRepo.Object, _mockContext.Object);
        }

        [Fact]
        public void GetAll_ReturnsAllPromotions()
        {
            // Arrange
            var promotions = new List<Promotion> { new Promotion { PromotionId = 1 }, new Promotion { PromotionId = 2 } };
            _mockRepo.Setup(r => r.GetAll()).Returns(promotions);

            // Act
            var result = _service.GetAll();

            // Assert
            Assert.Equal(2, result.Count());
        }

        [Fact]
        public void GetById_ReturnsCorrectPromotion()
        {
            // Arrange
            var promotion = new Promotion { PromotionId = 1 };
            _mockRepo.Setup(r => r.GetById(1)).Returns(promotion);

            // Act
            var result = _service.GetById(1);

            // Assert
            Assert.Equal(1, result.PromotionId);
        }

        [Fact]
        public void GetById_ReturnsNull_WhenPromotionNotFound()
        {
            // Arrange
            _mockRepo.Setup(r => r.GetById(999)).Returns((Promotion)null);

            // Act
            var result = _service.GetById(999);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void Add_NullPromotion_ReturnsFalse()
        {
            // Act
            var result = _service.Add(null);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void Add_ValidPromotion_CallsRepositoryAdd()
        {
            // Arrange
            var promotion = new Promotion { PromotionId = 1 };

            // Act
            var result = _service.Add(promotion);

            // Assert
            _mockRepo.Verify(r => r.Add(promotion), Times.Once);
            Assert.True(result);
        }

        [Fact]
        public void Update_NullPromotion_ReturnsFalse()
        {
            // Act
            var result = _service.Update(null);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void Update_ValidPromotion_CallsRepositoryUpdateAndSave()
        {
            // Arrange
            var promotion = new Promotion { PromotionId = 1 };

            // Act
            var result = _service.Update(promotion);

            // Assert
            _mockRepo.Verify(r => r.Update(promotion), Times.Once);
            _mockRepo.Verify(r => r.Save(), Times.Once);
            Assert.True(result);
        }

        [Fact]
        public void Delete_CallsRepositoryDelete()
        {
            // Act
            var result = _service.Delete(1);

            // Assert
            _mockRepo.Verify(r => r.Delete(1), Times.Once);
            Assert.True(result);
        }

        [Fact]
        public void Save_CallsRepositorySave()
        {
            // Act
            _service.Save();

            // Assert
            _mockRepo.Verify(r => r.Save(), Times.Once);
        }

        [Fact]
        public void GetBestPromotionForShowDate_ReturnsBestPromotion()
        {
            // Arrange
            var showDate = new DateOnly(2024, 6, 10);
            var promotions = new List<Promotion>
            {
                new Promotion { PromotionId = 1, IsActive = true, StartTime = new DateTime(2024, 6, 1), EndTime = new DateTime(2024, 6, 30), DiscountLevel = 10 },
                new Promotion { PromotionId = 2, IsActive = true, StartTime = new DateTime(2024, 6, 5), EndTime = new DateTime(2024, 6, 15), DiscountLevel = 20 },
                new Promotion { PromotionId = 3, IsActive = false, StartTime = new DateTime(2024, 6, 1), EndTime = new DateTime(2024, 6, 30), DiscountLevel = 50 }
            };
            _mockRepo.Setup(r => r.GetAll()).Returns(promotions);

            // Act
            var result = _service.GetBestPromotionForShowDate(showDate);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.PromotionId); // Promotion có DiscountLevel cao nhất và hợp lệ
        }

        [Fact]
        public void GetBestPromotionForShowDate_ReturnsNull_WhenNoValidPromotions()
        {
            // Arrange
            var showDate = new DateOnly(2024, 6, 10);
            var promotions = new List<Promotion>
            {
                new Promotion { PromotionId = 1, IsActive = false, StartTime = new DateTime(2024, 6, 1), EndTime = new DateTime(2024, 6, 30), DiscountLevel = 10 },
                new Promotion { PromotionId = 2, IsActive = true, StartTime = new DateTime(2024, 7, 1), EndTime = new DateTime(2024, 7, 30), DiscountLevel = 20 }
            };
            _mockRepo.Setup(r => r.GetAll()).Returns(promotions);

            // Act
            var result = _service.GetBestPromotionForShowDate(showDate);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void GetBestPromotionForShowDate_ReturnsNull_WhenNoPromotions()
        {
            // Arrange
            var showDate = new DateOnly(2024, 6, 10);
            _mockRepo.Setup(r => r.GetAll()).Returns(new List<Promotion>());

            // Act
            var result = _service.GetBestPromotionForShowDate(showDate);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void GetBestPromotionForShowDate_ReturnsNull_WhenNoDiscountLevel()
        {
            // Arrange
            var showDate = new DateOnly(2024, 6, 10);
            var promotions = new List<Promotion>
            {
                new Promotion { PromotionId = 1, IsActive = true, StartTime = new DateTime(2024, 6, 1), EndTime = new DateTime(2024, 6, 30), DiscountLevel = null }
            };
            _mockRepo.Setup(r => r.GetAll()).Returns(promotions);

            // Act
            var result = _service.GetBestPromotionForShowDate(showDate);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void GetBestEligiblePromotionForBooking_ReturnsBestPromotion()
        {
            // Arrange
            var context = new PromotionCheckContext
            {
                MemberId = "member1",
                SeatCount = 2,
                MovieId = "movie1",
                MovieName = "Test Movie",
                ShowDate = DateTime.Now,
                SelectedSeatTypeIds = new List<int> { 1, 2 },
                SelectedSeatTypeNames = new List<string> { "Standard", "VIP" },
                SelectedSeatTypePricePercents = new List<decimal> { 100, 150 }
            };

            var promotions = new List<Promotion>
            {
                new Promotion 
                { 
                    PromotionId = 1, 
                    IsActive = true, 
                    DiscountLevel = 10,
                    PromotionConditions = new List<PromotionCondition>()
                },
                new Promotion 
                { 
                    PromotionId = 2, 
                    IsActive = true, 
                    DiscountLevel = 20,
                    PromotionConditions = new List<PromotionCondition>()
                }
            };

            var mockDbSet = new Mock<DbSet<Promotion>>();
            mockDbSet.As<IQueryable<Promotion>>().Setup(m => m.Provider).Returns(promotions.AsQueryable().Provider);
            mockDbSet.As<IQueryable<Promotion>>().Setup(m => m.Expression).Returns(promotions.AsQueryable().Expression);
            mockDbSet.As<IQueryable<Promotion>>().Setup(m => m.ElementType).Returns(promotions.AsQueryable().ElementType);
            mockDbSet.As<IQueryable<Promotion>>().Setup(m => m.GetEnumerator()).Returns(promotions.GetEnumerator());

            _mockContext.Setup(c => c.Promotions).Returns(mockDbSet.Object);

            // Act
            var result = _service.GetBestEligiblePromotionForBooking(context);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.PromotionId); // Should return the one with higher discount
        }

        [Fact]
        public void GetBestEligiblePromotionForBooking_ReturnsNull_WhenNoEligiblePromotions()
        {
            // Arrange
            var context = new PromotionCheckContext
            {
                MemberId = "member1",
                SeatCount = 2,
                MovieId = "movie1",
                MovieName = "Test Movie",
                ShowDate = DateTime.Now,
                SelectedSeatTypeIds = new List<int> { 1, 2 },
                SelectedSeatTypeNames = new List<string> { "Standard", "VIP" },
                SelectedSeatTypePricePercents = new List<decimal> { 100, 150 }
            };

            var promotions = new List<Promotion>
            {
                new Promotion 
                { 
                    PromotionId = 1, 
                    IsActive = false, 
                    DiscountLevel = 10,
                    PromotionConditions = new List<PromotionCondition>()
                }
            };

            var mockDbSet = new Mock<DbSet<Promotion>>();
            mockDbSet.As<IQueryable<Promotion>>().Setup(m => m.Provider).Returns(promotions.AsQueryable().Provider);
            mockDbSet.As<IQueryable<Promotion>>().Setup(m => m.Expression).Returns(promotions.AsQueryable().Expression);
            mockDbSet.As<IQueryable<Promotion>>().Setup(m => m.ElementType).Returns(promotions.AsQueryable().ElementType);
            mockDbSet.As<IQueryable<Promotion>>().Setup(m => m.GetEnumerator()).Returns(promotions.GetEnumerator());

            _mockContext.Setup(c => c.Promotions).Returns(mockDbSet.Object);

            // Act
            var result = _service.GetBestEligiblePromotionForBooking(context);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void GetEligibleFoodPromotions_ReturnsEmptyList_WhenNoSelectedFoods()
        {
            // Act
            var result = _service.GetEligibleFoodPromotions(null);

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public void GetEligibleFoodPromotions_ReturnsEmptyList_WhenEmptySelectedFoods()
        {
            // Act
            var result = _service.GetEligibleFoodPromotions(new List<(int, int, decimal)>());

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public void GetEligibleFoodPromotions_ReturnsFoodPromotions()
        {
            // Arrange
            var selectedFoods = new List<(int, int, decimal)> { (1, 2, 100) };
            var promotions = new List<Promotion>
            {
                new Promotion 
                { 
                    PromotionId = 1, 
                    IsActive = true,
                    PromotionConditions = new List<PromotionCondition>
                    {
                        new PromotionCondition { TargetEntity = "food" }
                    }
                }
            };

            var mockDbSet = new Mock<DbSet<Promotion>>();
            mockDbSet.As<IQueryable<Promotion>>().Setup(m => m.Provider).Returns(promotions.AsQueryable().Provider);
            mockDbSet.As<IQueryable<Promotion>>().Setup(m => m.Expression).Returns(promotions.AsQueryable().Expression);
            mockDbSet.As<IQueryable<Promotion>>().Setup(m => m.ElementType).Returns(promotions.AsQueryable().ElementType);
            mockDbSet.As<IQueryable<Promotion>>().Setup(m => m.GetEnumerator()).Returns(promotions.GetEnumerator());

            _mockContext.Setup(c => c.Promotions).Returns(mockDbSet.Object);

            // Act
            var result = _service.GetEligibleFoodPromotions(selectedFoods);

            // Assert
            Assert.Single(result);
            Assert.Equal(1, result[0].PromotionId);
        }

        [Fact]
        public void ApplyFoodPromotionsToFoods_ReturnsCorrectResults()
        {
            // Arrange
            var selectedFoods = new List<(int, int, decimal)> 
            { 
                (1, 2, 100), 
                (2, 1, 150) 
            };

            var eligiblePromotions = new List<Promotion>
            {
                new Promotion 
                { 
                    PromotionId = 1, 
                    Title = "Food Discount",
                    DiscountLevel = 10,
                    PromotionConditions = new List<PromotionCondition>
                    {
                        new PromotionCondition 
                        { 
                            TargetEntity = "food", 
                            TargetField = "price", 
                            TargetValue = "50", 
                            Operator = ">=" 
                        }
                    }
                }
            };

            // Act
            var result = _service.ApplyFoodPromotionsToFoods(selectedFoods, eligiblePromotions);

            // Assert
            Assert.Equal(2, result.Count);
            Assert.Equal(1, result[0].FoodId);
            Assert.Equal(100, result[0].OriginalPrice);
            Assert.Equal(90, result[0].DiscountedPrice); // 10% discount
            Assert.Equal("Food Discount", result[0].PromotionName);
            Assert.Equal(10, result[0].DiscountLevel);
        }

        [Fact]
        public void ApplyFoodPromotionsToFoods_ReturnsOriginalPrices_WhenNoEligiblePromotions()
        {
            // Arrange
            var selectedFoods = new List<(int, int, decimal)> 
            { 
                (1, 2, 100), 
                (2, 1, 150) 
            };

            var eligiblePromotions = new List<Promotion>();

            // Act
            var result = _service.ApplyFoodPromotionsToFoods(selectedFoods, eligiblePromotions);

            // Assert
            Assert.Equal(2, result.Count);
            Assert.Equal(1, result[0].FoodId);
            Assert.Equal(100, result[0].OriginalPrice);
            Assert.Equal(100, result[0].DiscountedPrice); // No discount
            Assert.Null(result[0].PromotionName);
            Assert.Equal(0, result[0].DiscountLevel);
        }

        [Fact]
        public void ApplyFoodPromotionsToFoods_HandlesInvalidPriceConditions()
        {
            // Arrange
            var selectedFoods = new List<(int, int, decimal)> 
            { 
                (1, 2, 100)
            };

            var eligiblePromotions = new List<Promotion>
            {
                new Promotion 
                { 
                    PromotionId = 1, 
                    Title = "Food Discount",
                    DiscountLevel = 10,
                    PromotionConditions = new List<PromotionCondition>
                    {
                        new PromotionCondition 
                        { 
                            TargetEntity = "food", 
                            TargetField = "price", 
                            TargetValue = "invalid", 
                            Operator = ">=" 
                        }
                    }
                }
            };

            // Act
            var result = _service.ApplyFoodPromotionsToFoods(selectedFoods, eligiblePromotions);

            // Assert
            Assert.Single(result);
            Assert.Equal(1, result[0].FoodId);
            Assert.Equal(100, result[0].OriginalPrice);
            // When TargetValue is invalid, the condition is ignored and promotion is applied
            Assert.Equal(90.0m, result[0].DiscountedPrice); // 10% discount applied
            Assert.Equal("Food Discount", result[0].PromotionName);
            Assert.Equal(10, result[0].DiscountLevel);
        }

        [Fact]
        public void ApplyFoodPromotionsToFoods_HandlesMultipleOperators()
        {
            // Arrange
            var selectedFoods = new List<(int, int, decimal)> 
            { 
                (1, 2, 100),
                (2, 1, 200)
            };

            var eligiblePromotions = new List<Promotion>
            {
                new Promotion 
                { 
                    PromotionId = 1, 
                    Title = "High Price Discount",
                    DiscountLevel = 15,
                    PromotionConditions = new List<PromotionCondition>
                    {
                        new PromotionCondition 
                        { 
                            TargetEntity = "food", 
                            TargetField = "price", 
                            TargetValue = "150", 
                            Operator = ">" 
                        }
                    }
                },
                new Promotion 
                { 
                    PromotionId = 2, 
                    Title = "Standard Discount",
                    DiscountLevel = 10,
                    PromotionConditions = new List<PromotionCondition>
                    {
                        new PromotionCondition 
                        { 
                            TargetEntity = "food", 
                            TargetField = "price", 
                            TargetValue = "50", 
                            Operator = ">=" 
                        }
                    }
                }
            };

            // Act
            var result = _service.ApplyFoodPromotionsToFoods(selectedFoods, eligiblePromotions);

            // Assert
            Assert.Equal(2, result.Count);
            // First food should get 10% discount (standard)
            Assert.Equal(1, result[0].FoodId);
            Assert.Equal(90, result[0].DiscountedPrice);
            Assert.Equal("Standard Discount", result[0].PromotionName);
            // Second food should get 15% discount (high price)
            Assert.Equal(2, result[1].FoodId);
            Assert.Equal(170, result[1].DiscountedPrice);
            Assert.Equal("High Price Discount", result[1].PromotionName);
        }
    }
} 