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
            // Arrange - Setup mock context properly
            var mockDbSet = new Mock<DbSet<Promotion>>();
            var promotions = new List<Promotion>().AsQueryable();
            mockDbSet.As<IQueryable<Promotion>>().Setup(m => m.Provider).Returns(promotions.Provider);
            mockDbSet.As<IQueryable<Promotion>>().Setup(m => m.Expression).Returns(promotions.Expression);
            mockDbSet.As<IQueryable<Promotion>>().Setup(m => m.ElementType).Returns(promotions.ElementType);
            mockDbSet.As<IQueryable<Promotion>>().Setup(m => m.GetEnumerator()).Returns(promotions.GetEnumerator());
            _mockContext.Setup(c => c.Promotions).Returns(mockDbSet.Object);

            // Act
            var result = _service.GetEligibleFoodPromotions(null);

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public void GetEligibleFoodPromotions_ReturnsEmptyList_WhenEmptySelectedFoods()
        {
            // Arrange - Setup mock context properly
            var mockDbSet = new Mock<DbSet<Promotion>>();
            var promotions = new List<Promotion>().AsQueryable();
            mockDbSet.As<IQueryable<Promotion>>().Setup(m => m.Provider).Returns(promotions.Provider);
            mockDbSet.As<IQueryable<Promotion>>().Setup(m => m.Expression).Returns(promotions.Expression);
            mockDbSet.As<IQueryable<Promotion>>().Setup(m => m.ElementType).Returns(promotions.ElementType);
            mockDbSet.As<IQueryable<Promotion>>().Setup(m => m.GetEnumerator()).Returns(promotions.GetEnumerator());
            _mockContext.Setup(c => c.Promotions).Returns(mockDbSet.Object);

            // Act
            var result = _service.GetEligibleFoodPromotions(new List<(int, int, decimal, string)>());

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public void GetEligibleFoodPromotions_ReturnsFoodPromotions()
        {
            // Arrange
            var selectedFoods = new List<(int, int, decimal, string)> { (1, 2, 100, "Pizza") };
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
            var selectedFoods = new List<(int, int, decimal, string)> 
            { 
                (1, 2, 100, "Pizza"), 
                (2, 1, 150, "Burger") 
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
            var selectedFoods = new List<(int, int, decimal, string)> 
            { 
                (1, 2, 100, "Pizza"), 
                (2, 1, 150, "Burger") 
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
            var selectedFoods = new List<(int, int, decimal, string)> 
            { 
                (1, 2, 100, "Pizza")
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
            var selectedFoods = new List<(int, int, decimal, string)> 
            { 
                (1, 2, 100, "Pizza"),
                (2, 1, 200, "Burger")
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

        [Fact]
        public void ApplyFoodPromotionsToFoods_HandlesFoodNameConditions()
        {
            // Arrange
            var selectedFoods = new List<(int, int, decimal, string)> 
            { 
                (1, 2, 100, "Pizza"),
                (2, 1, 200, "Burger"),
                (3, 1, 150, "Nachos")
            };

            var eligiblePromotions = new List<Promotion>
            {
                new Promotion 
                { 
                    PromotionId = 1, 
                    Title = "Pizza Discount",
                    DiscountLevel = 20,
                    PromotionConditions = new List<PromotionCondition>
                    {
                        new PromotionCondition 
                        { 
                            TargetEntity = "food", 
                            TargetField = "foodname", 
                            TargetValue = "Pizza", 
                            Operator = "=" 
                        }
                    }
                },
                new Promotion 
                { 
                    PromotionId = 2, 
                    Title = "Burger Discount",
                    DiscountLevel = 15,
                    PromotionConditions = new List<PromotionCondition>
                    {
                        new PromotionCondition 
                        { 
                            TargetEntity = "food", 
                            TargetField = "foodname", 
                            TargetValue = "Burger", 
                            Operator = "=" 
                        }
                    }
                }
            };

            // Act
            var result = _service.ApplyFoodPromotionsToFoods(selectedFoods, eligiblePromotions);

            // Assert
            Assert.Equal(3, result.Count);
            // Pizza should get 20% discount
            Assert.Equal(1, result[0].FoodId);
            Assert.Equal(80, result[0].DiscountedPrice); // 100 * 0.8
            Assert.Equal("Pizza Discount", result[0].PromotionName);
            // Burger should get 15% discount
            Assert.Equal(2, result[1].FoodId);
            Assert.Equal(170, result[1].DiscountedPrice); // 200 * 0.85
            Assert.Equal("Burger Discount", result[1].PromotionName);
            // Nachos should get no discount
            Assert.Equal(3, result[2].FoodId);
            Assert.Equal(150, result[2].DiscountedPrice);
            Assert.Null(result[2].PromotionName);
        }

        // Tests for GetBestEligiblePromotionForBooking method (which internally uses IsPromotionEligibleNew)
        [Fact]
        public void GetBestEligiblePromotionForBooking_SeatConditionGreaterEqual_ReturnsEligiblePromotion()
        {
            // Arrange
            var context = new PromotionCheckContext
            {
                MemberId = "member1",
                SeatCount = 3,
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
                    PromotionConditions = new List<PromotionCondition>
                    {
                        new PromotionCondition 
                        { 
                            TargetField = "seat", 
                            TargetValue = "2", 
                            Operator = ">=" 
                        }
                    }
                },
                new Promotion 
                { 
                    PromotionId = 2, 
                    IsActive = true, 
                    DiscountLevel = 20,
                    PromotionConditions = new List<PromotionCondition>
                    {
                        new PromotionCondition 
                        { 
                            TargetField = "seat", 
                            TargetValue = "5", 
                            Operator = ">=" 
                        }
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
            var result = _service.GetBestEligiblePromotionForBooking(context);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1, result.PromotionId); // Should return the first eligible promotion
        }

        [Fact]
        public void GetBestEligiblePromotionForBooking_SeatConditionLessThan_ReturnsNoPromotion()
        {
            // Arrange
            var context = new PromotionCheckContext
            {
                MemberId = "member1",
                SeatCount = 1,
                MovieId = "movie1",
                MovieName = "Test Movie",
                ShowDate = DateTime.Now,
                SelectedSeatTypeIds = new List<int> { 1 },
                SelectedSeatTypeNames = new List<string> { "Standard" },
                SelectedSeatTypePricePercents = new List<decimal> { 100 }
            };

            var promotions = new List<Promotion>
            {
                new Promotion 
                { 
                    PromotionId = 1, 
                    IsActive = true, 
                    DiscountLevel = 10,
                    PromotionConditions = new List<PromotionCondition>
                    {
                        new PromotionCondition 
                        { 
                            TargetField = "seat", 
                            TargetValue = "2", 
                            Operator = ">=" 
                        }
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
            var result = _service.GetBestEligiblePromotionForBooking(context);

            // Assert
            Assert.Null(result); // Should return null as seat count is less than required
        }

        [Fact]
        public void GetBestEligiblePromotionForBooking_TypeNameCondition_ReturnsEligiblePromotion()
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
                    DiscountLevel = 15,
                    PromotionConditions = new List<PromotionCondition>
                    {
                        new PromotionCondition 
                        { 
                            TargetField = "typename", 
                            TargetValue = "VIP", 
                            Operator = "=" 
                        }
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
            var result = _service.GetBestEligiblePromotionForBooking(context);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1, result.PromotionId);
        }

        [Fact]
        public void GetBestEligiblePromotionForBooking_MovieNameCondition_ReturnsEligiblePromotion()
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
                    DiscountLevel = 15,
                    PromotionConditions = new List<PromotionCondition>
                    {
                        new PromotionCondition 
                        { 
                            TargetField = "movienameenglish", 
                            TargetValue = "Test Movie", 
                            Operator = "=" 
                        }
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
            var result = _service.GetBestEligiblePromotionForBooking(context);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1, result.PromotionId);
        }

        [Fact]
        public void GetBestEligiblePromotionForBooking_ShowDateCondition_ReturnsEligiblePromotion()
        {
            // Arrange
            var context = new PromotionCheckContext
            {
                MemberId = "member1",
                SeatCount = 2,
                MovieId = "movie1",
                MovieName = "Test Movie",
                ShowDate = new DateTime(2024, 6, 15),
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
                    DiscountLevel = 15,
                    PromotionConditions = new List<PromotionCondition>
                    {
                        new PromotionCondition 
                        { 
                            TargetField = "showdate", 
                            TargetValue = "2024-06-15", 
                            Operator = "=" 
                        }
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
            var result = _service.GetBestEligiblePromotionForBooking(context);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1, result.PromotionId);
        }

        [Fact]
        public void GetBestEligiblePromotionForBooking_PricePercentCondition_ReturnsEligiblePromotion()
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
                    DiscountLevel = 15,
                    PromotionConditions = new List<PromotionCondition>
                    {
                        new PromotionCondition 
                        { 
                            TargetField = "pricepercent", 
                            TargetValue = "120", 
                            Operator = ">=" 
                        }
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
            var result = _service.GetBestEligiblePromotionForBooking(context);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1, result.PromotionId);
        }

        [Fact]
        public void GetBestEligiblePromotionForBooking_AccountIdCondition_ReturnsEligiblePromotion()
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
                    DiscountLevel = 15,
                    PromotionConditions = new List<PromotionCondition>
                    {
                        new PromotionCondition 
                        { 
                            TargetField = "accountid", 
                            TargetValue = "acc1", 
                            Operator = "=" 
                        }
                    }
                }
            };

            var member = new Member { MemberId = "member1", AccountId = "acc1" };
            var invoices = new List<Invoice> { new Invoice { AccountId = "acc1" } };

            var mockMembers = new Mock<DbSet<Member>>();
            mockMembers.As<IQueryable<Member>>().Setup(m => m.Provider).Returns(new List<Member> { member }.AsQueryable().Provider);
            mockMembers.As<IQueryable<Member>>().Setup(m => m.Expression).Returns(new List<Member> { member }.AsQueryable().Expression);
            mockMembers.As<IQueryable<Member>>().Setup(m => m.ElementType).Returns(new List<Member> { member }.AsQueryable().ElementType);
            mockMembers.As<IQueryable<Member>>().Setup(m => m.GetEnumerator()).Returns(new List<Member> { member }.GetEnumerator());

            var mockInvoices = new Mock<DbSet<Invoice>>();
            mockInvoices.As<IQueryable<Invoice>>().Setup(m => m.Provider).Returns(invoices.AsQueryable().Provider);
            mockInvoices.As<IQueryable<Invoice>>().Setup(m => m.Expression).Returns(invoices.AsQueryable().Expression);
            mockInvoices.As<IQueryable<Invoice>>().Setup(m => m.ElementType).Returns(invoices.AsQueryable().ElementType);
            mockInvoices.As<IQueryable<Invoice>>().Setup(m => m.GetEnumerator()).Returns(invoices.GetEnumerator());

            var mockPromotions = new Mock<DbSet<Promotion>>();
            mockPromotions.As<IQueryable<Promotion>>().Setup(m => m.Provider).Returns(promotions.AsQueryable().Provider);
            mockPromotions.As<IQueryable<Promotion>>().Setup(m => m.Expression).Returns(promotions.AsQueryable().Expression);
            mockPromotions.As<IQueryable<Promotion>>().Setup(m => m.ElementType).Returns(promotions.AsQueryable().ElementType);
            mockPromotions.As<IQueryable<Promotion>>().Setup(m => m.GetEnumerator()).Returns(promotions.GetEnumerator());

            _mockContext.Setup(c => c.Members).Returns(mockMembers.Object);
            _mockContext.Setup(c => c.Invoices).Returns(mockInvoices.Object);
            _mockContext.Setup(c => c.Promotions).Returns(mockPromotions.Object);

            // Act
            var result = _service.GetBestEligiblePromotionForBooking(context);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1, result.PromotionId);
        }

        [Fact]
        public void GetBestEligiblePromotionForBooking_InvalidOperator_ReturnsNoPromotion()
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
                    DiscountLevel = 15,
                    PromotionConditions = new List<PromotionCondition>
                    {
                        new PromotionCondition 
                        { 
                            TargetField = "seat", 
                            TargetValue = "2", 
                            Operator = "invalid" 
                        }
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
            var result = _service.GetBestEligiblePromotionForBooking(context);

            // Assert
            Assert.Null(result); // Should return null due to invalid operator
        }

        [Fact]
        public void GetBestEligiblePromotionForBooking_MultipleConditions_ReturnsEligiblePromotion()
        {
            // Arrange
            var context = new PromotionCheckContext
            {
                MemberId = "member1",
                SeatCount = 3,
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
                    DiscountLevel = 15,
                    PromotionConditions = new List<PromotionCondition>
                    {
                        new PromotionCondition 
                        { 
                            TargetField = "seat", 
                            TargetValue = "2", 
                            Operator = ">=" 
                        },
                        new PromotionCondition 
                        { 
                            TargetField = "typename", 
                            TargetValue = "VIP", 
                            Operator = "=" 
                        }
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
            var result = _service.GetBestEligiblePromotionForBooking(context);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1, result.PromotionId);
        }

        #region IsPromotionEligible Tests

        [Fact]
        public void IsPromotionEligible_NoConditions_ReturnsTrue()
        {
            // Arrange
            var promotion = new Promotion 
            { 
                PromotionId = 1, 
                IsActive = true,
                PromotionConditions = new List<PromotionCondition>()
            };

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

            // Act
            var result = _service.IsPromotionEligible(promotion, context);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void IsPromotionEligible_NullConditions_ReturnsTrue()
        {
            // Arrange
            var promotion = new Promotion 
            { 
                PromotionId = 1, 
                IsActive = true,
                PromotionConditions = null
            };

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

            // Act
            var result = _service.IsPromotionEligible(promotion, context);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void IsPromotionEligible_SeatConditionGreaterEqual_ReturnsTrue()
        {
            // Arrange
            var promotion = new Promotion 
            { 
                PromotionId = 1, 
                IsActive = true,
                PromotionConditions = new List<PromotionCondition>
                {
                    new PromotionCondition 
                    { 
                        TargetField = "seat", 
                        TargetValue = "2", 
                        Operator = ">=" 
                    }
                }
            };

            var context = new PromotionCheckContext
            {
                MemberId = "member1",
                SeatCount = 3,
                MovieId = "movie1",
                MovieName = "Test Movie",
                ShowDate = DateTime.Now,
                SelectedSeatTypeIds = new List<int> { 1, 2, 3 },
                SelectedSeatTypeNames = new List<string> { "Standard", "VIP", "Premium" },
                SelectedSeatTypePricePercents = new List<decimal> { 100, 150, 200 }
            };

            // Act
            var result = _service.IsPromotionEligible(promotion, context);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void IsPromotionEligible_SeatConditionLessThan_ReturnsFalse()
        {
            // Arrange
            var promotion = new Promotion 
            { 
                PromotionId = 1, 
                IsActive = true,
                PromotionConditions = new List<PromotionCondition>
                {
                    new PromotionCondition 
                    { 
                        TargetField = "seat", 
                        TargetValue = "2", 
                        Operator = ">=" 
                    }
                }
            };

            var context = new PromotionCheckContext
            {
                MemberId = "member1",
                SeatCount = 1,
                MovieId = "movie1",
                MovieName = "Test Movie",
                ShowDate = DateTime.Now,
                SelectedSeatTypeIds = new List<int> { 1 },
                SelectedSeatTypeNames = new List<string> { "Standard" },
                SelectedSeatTypePricePercents = new List<decimal> { 100 }
            };

            // Act
            var result = _service.IsPromotionEligible(promotion, context);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void IsPromotionEligible_TypeNameCondition_ReturnsTrue()
        {
            // Arrange
            var promotion = new Promotion 
            { 
                PromotionId = 1, 
                IsActive = true,
                PromotionConditions = new List<PromotionCondition>
                {
                    new PromotionCondition 
                    { 
                        TargetField = "typename", 
                        TargetValue = "VIP", 
                        Operator = "=" 
                    }
                }
            };

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

            // Act
            var result = _service.IsPromotionEligible(promotion, context);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void IsPromotionEligible_TypeNameCondition_ReturnsFalse()
        {
            // Arrange
            var promotion = new Promotion 
            { 
                PromotionId = 1, 
                IsActive = true,
                PromotionConditions = new List<PromotionCondition>
                {
                    new PromotionCondition 
                    { 
                        TargetField = "typename", 
                        TargetValue = "Premium", 
                        Operator = "=" 
                    }
                }
            };

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

            // Act
            var result = _service.IsPromotionEligible(promotion, context);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void IsPromotionEligible_MovieNameCondition_ReturnsTrue()
        {
            // Arrange
            var promotion = new Promotion 
            { 
                PromotionId = 1, 
                IsActive = true,
                PromotionConditions = new List<PromotionCondition>
                {
                    new PromotionCondition 
                    { 
                        TargetField = "movienameenglish", 
                        TargetValue = "Test Movie", 
                        Operator = "=" 
                    }
                }
            };

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

            // Act
            var result = _service.IsPromotionEligible(promotion, context);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void IsPromotionEligible_ShowDateCondition_ReturnsTrue()
        {
            // Arrange
            var promotion = new Promotion 
            { 
                PromotionId = 1, 
                IsActive = true,
                PromotionConditions = new List<PromotionCondition>
                {
                    new PromotionCondition 
                    { 
                        TargetField = "showdate", 
                        TargetValue = "2024-06-15", 
                        Operator = "=" 
                    }
                }
            };

            var context = new PromotionCheckContext
            {
                MemberId = "member1",
                SeatCount = 2,
                MovieId = "movie1",
                MovieName = "Test Movie",
                ShowDate = new DateTime(2024, 6, 15),
                SelectedSeatTypeIds = new List<int> { 1, 2 },
                SelectedSeatTypeNames = new List<string> { "Standard", "VIP" },
                SelectedSeatTypePricePercents = new List<decimal> { 100, 150 }
            };

            // Act
            var result = _service.IsPromotionEligible(promotion, context);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void IsPromotionEligible_PricePercentCondition_ReturnsTrue()
        {
            // Arrange
            var promotion = new Promotion 
            { 
                PromotionId = 1, 
                IsActive = true,
                PromotionConditions = new List<PromotionCondition>
                {
                    new PromotionCondition 
                    { 
                        TargetField = "pricepercent", 
                        TargetValue = "120", 
                        Operator = ">=" 
                    }
                }
            };

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

            // Act
            var result = _service.IsPromotionEligible(promotion, context);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void IsPromotionEligible_AccountIdCondition_ReturnsTrue()
        {
            // Arrange
            var promotion = new Promotion 
            { 
                PromotionId = 1, 
                IsActive = true,
                PromotionConditions = new List<PromotionCondition>
                {
                    new PromotionCondition 
                    { 
                        TargetField = "accountid", 
                        TargetValue = "acc1", 
                        Operator = "=" 
                    }
                }
            };

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

            var member = new Member { MemberId = "member1", AccountId = "acc1" };
            var invoices = new List<Invoice> { new Invoice { AccountId = "acc1" } };

            var mockMembers = new Mock<DbSet<Member>>();
            mockMembers.As<IQueryable<Member>>().Setup(m => m.Provider).Returns(new List<Member> { member }.AsQueryable().Provider);
            mockMembers.As<IQueryable<Member>>().Setup(m => m.Expression).Returns(new List<Member> { member }.AsQueryable().Expression);
            mockMembers.As<IQueryable<Member>>().Setup(m => m.ElementType).Returns(new List<Member> { member }.AsQueryable().ElementType);
            mockMembers.As<IQueryable<Member>>().Setup(m => m.GetEnumerator()).Returns(new List<Member> { member }.GetEnumerator());

            var mockInvoices = new Mock<DbSet<Invoice>>();
            mockInvoices.As<IQueryable<Invoice>>().Setup(m => m.Provider).Returns(invoices.AsQueryable().Provider);
            mockInvoices.As<IQueryable<Invoice>>().Setup(m => m.Expression).Returns(invoices.AsQueryable().Expression);
            mockInvoices.As<IQueryable<Invoice>>().Setup(m => m.ElementType).Returns(invoices.AsQueryable().ElementType);
            mockInvoices.As<IQueryable<Invoice>>().Setup(m => m.GetEnumerator()).Returns(invoices.GetEnumerator());

            _mockContext.Setup(c => c.Members).Returns(mockMembers.Object);
            _mockContext.Setup(c => c.Invoices).Returns(mockInvoices.Object);

            // Act
            var result = _service.IsPromotionEligible(promotion, context);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void IsPromotionEligible_InvalidOperator_ReturnsFalse()
        {
            // Arrange
            var promotion = new Promotion 
            { 
                PromotionId = 1, 
                IsActive = true,
                PromotionConditions = new List<PromotionCondition>
                {
                    new PromotionCondition 
                    { 
                        TargetField = "seat", 
                        TargetValue = "2", 
                        Operator = "invalid" 
                    }
                }
            };

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

            // Act
            var result = _service.IsPromotionEligible(promotion, context);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void IsPromotionEligible_MultipleConditions_AllTrue_ReturnsTrue()
        {
            // Arrange
            var promotion = new Promotion 
            { 
                PromotionId = 1, 
                IsActive = true,
                PromotionConditions = new List<PromotionCondition>
                {
                    new PromotionCondition 
                    { 
                        TargetField = "seat", 
                        TargetValue = "2", 
                        Operator = ">=" 
                    },
                    new PromotionCondition 
                    { 
                        TargetField = "typename", 
                        TargetValue = "VIP", 
                        Operator = "=" 
                    }
                }
            };

            var context = new PromotionCheckContext
            {
                MemberId = "member1",
                SeatCount = 3,
                MovieId = "movie1",
                MovieName = "Test Movie",
                ShowDate = DateTime.Now,
                SelectedSeatTypeIds = new List<int> { 1, 2, 3 },
                SelectedSeatTypeNames = new List<string> { "Standard", "VIP", "Premium" },
                SelectedSeatTypePricePercents = new List<decimal> { 100, 150, 200 }
            };

            // Act
            var result = _service.IsPromotionEligible(promotion, context);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void IsPromotionEligible_MultipleConditions_OneFalse_ReturnsFalse()
        {
            // Arrange
            var promotion = new Promotion 
            { 
                PromotionId = 1, 
                IsActive = true,
                PromotionConditions = new List<PromotionCondition>
                {
                    new PromotionCondition 
                    { 
                        TargetField = "seat", 
                        TargetValue = "2", 
                        Operator = ">=" 
                    },
                    new PromotionCondition 
                    { 
                        TargetField = "typename", 
                        TargetValue = "Premium", 
                        Operator = "=" 
                    }
                }
            };

            var context = new PromotionCheckContext
            {
                MemberId = "member1",
                SeatCount = 3,
                MovieId = "movie1",
                MovieName = "Test Movie",
                ShowDate = DateTime.Now,
                SelectedSeatTypeIds = new List<int> { 1, 2 },
                SelectedSeatTypeNames = new List<string> { "Standard", "VIP" },
                SelectedSeatTypePricePercents = new List<decimal> { 100, 150 }
            };

            // Act
            var result = _service.IsPromotionEligible(promotion, context);

            // Assert
            Assert.False(result);
        }

        #endregion

        #region Additional Edge Case Tests

        [Fact]
        public void IsPromotionEligible_SeatConditionEqual_ReturnsTrue()
        {
            // Arrange
            var promotion = new Promotion 
            { 
                PromotionId = 1, 
                IsActive = true,
                PromotionConditions = new List<PromotionCondition>
                {
                    new PromotionCondition 
                    { 
                        TargetField = "seat", 
                        TargetValue = "2", 
                        Operator = "==" 
                    }
                }
            };

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

            // Act
            var result = _service.IsPromotionEligible(promotion, context);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void IsPromotionEligible_SeatConditionLessEqual_ReturnsTrue()
        {
            // Arrange
            var promotion = new Promotion 
            { 
                PromotionId = 1, 
                IsActive = true,
                PromotionConditions = new List<PromotionCondition>
                {
                    new PromotionCondition 
                    { 
                        TargetField = "seat", 
                        TargetValue = "3", 
                        Operator = "<=" 
                    }
                }
            };

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

            // Act
            var result = _service.IsPromotionEligible(promotion, context);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void IsPromotionEligible_SeatConditionLessThan_ReturnsTrue()
        {
            // Arrange
            var promotion = new Promotion 
            { 
                PromotionId = 1, 
                IsActive = true,
                PromotionConditions = new List<PromotionCondition>
                {
                    new PromotionCondition 
                    { 
                        TargetField = "seat", 
                        TargetValue = "3", 
                        Operator = "<" 
                    }
                }
            };

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

            // Act
            var result = _service.IsPromotionEligible(promotion, context);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void IsPromotionEligible_SeatConditionNotEqual_ReturnsTrue()
        {
            // Arrange
            var promotion = new Promotion 
            { 
                PromotionId = 1, 
                IsActive = true,
                PromotionConditions = new List<PromotionCondition>
                {
                    new PromotionCondition 
                    { 
                        TargetField = "seat", 
                        TargetValue = "3", 
                        Operator = "!=" 
                    }
                }
            };

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

            // Act
            var result = _service.IsPromotionEligible(promotion, context);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void IsPromotionEligible_SeatTypeIdCondition_ReturnsTrue()
        {
            // Arrange
            var promotion = new Promotion 
            { 
                PromotionId = 1, 
                IsActive = true,
                PromotionConditions = new List<PromotionCondition>
                {
                    new PromotionCondition 
                    { 
                        TargetField = "seattypeid", 
                        TargetValue = "2", 
                        Operator = "=" 
                    }
                }
            };

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

            // Act
            var result = _service.IsPromotionEligible(promotion, context);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void IsPromotionEligible_SeatTypeIdConditionNotEqual_ReturnsTrue()
        {
            // Arrange
            var promotion = new Promotion 
            { 
                PromotionId = 1, 
                IsActive = true,
                PromotionConditions = new List<PromotionCondition>
                {
                    new PromotionCondition 
                    { 
                        TargetField = "seattypeid", 
                        TargetValue = "3", 
                        Operator = "!=" 
                    }
                }
            };

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

            // Act
            var result = _service.IsPromotionEligible(promotion, context);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void IsPromotionEligible_TypeNameConditionNotEqual_ReturnsTrue()
        {
            // Arrange
            var promotion = new Promotion 
            { 
                PromotionId = 1, 
                IsActive = true,
                PromotionConditions = new List<PromotionCondition>
                {
                    new PromotionCondition 
                    { 
                        TargetField = "typename", 
                        TargetValue = "Premium", 
                        Operator = "!=" 
                    }
                }
            };

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

            // Act
            var result = _service.IsPromotionEligible(promotion, context);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void IsPromotionEligible_PricePercentConditionGreaterThan_ReturnsTrue()
        {
            // Arrange
            var promotion = new Promotion 
            { 
                PromotionId = 1, 
                IsActive = true,
                PromotionConditions = new List<PromotionCondition>
                {
                    new PromotionCondition 
                    { 
                        TargetField = "pricepercent", 
                        TargetValue = "120", 
                        Operator = ">" 
                    }
                }
            };

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

            // Act
            var result = _service.IsPromotionEligible(promotion, context);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void IsPromotionEligible_PricePercentConditionLessThan_ReturnsTrue()
        {
            // Arrange
            var promotion = new Promotion 
            { 
                PromotionId = 1, 
                IsActive = true,
                PromotionConditions = new List<PromotionCondition>
                {
                    new PromotionCondition 
                    { 
                        TargetField = "pricepercent", 
                        TargetValue = "200", 
                        Operator = "<" 
                    }
                }
            };

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

            // Act
            var result = _service.IsPromotionEligible(promotion, context);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void IsPromotionEligible_PricePercentConditionLessEqual_ReturnsTrue()
        {
            // Arrange
            var promotion = new Promotion 
            { 
                PromotionId = 1, 
                IsActive = true,
                PromotionConditions = new List<PromotionCondition>
                {
                    new PromotionCondition 
                    { 
                        TargetField = "pricepercent", 
                        TargetValue = "150", 
                        Operator = "<=" 
                    }
                }
            };

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

            // Act
            var result = _service.IsPromotionEligible(promotion, context);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void IsPromotionEligible_PricePercentConditionEqual_ReturnsTrue()
        {
            // Arrange
            var promotion = new Promotion 
            { 
                PromotionId = 1, 
                IsActive = true,
                PromotionConditions = new List<PromotionCondition>
                {
                    new PromotionCondition 
                    { 
                        TargetField = "pricepercent", 
                        TargetValue = "150", 
                        Operator = "==" 
                    }
                }
            };

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

            // Act
            var result = _service.IsPromotionEligible(promotion, context);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void IsPromotionEligible_PricePercentConditionNotEqual_ReturnsTrue()
        {
            // Arrange
            var promotion = new Promotion 
            { 
                PromotionId = 1, 
                IsActive = true,
                PromotionConditions = new List<PromotionCondition>
                {
                    new PromotionCondition 
                    { 
                        TargetField = "pricepercent", 
                        TargetValue = "200", 
                        Operator = "!=" 
                    }
                }
            };

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

            // Act
            var result = _service.IsPromotionEligible(promotion, context);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void IsPromotionEligible_MovieNameConditionNotEqual_ReturnsTrue()
        {
            // Arrange
            var promotion = new Promotion 
            { 
                PromotionId = 1, 
                IsActive = true,
                PromotionConditions = new List<PromotionCondition>
                {
                    new PromotionCondition 
                    { 
                        TargetField = "movienameenglish", 
                        TargetValue = "Other Movie", 
                        Operator = "!=" 
                    }
                }
            };

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

            // Act
            var result = _service.IsPromotionEligible(promotion, context);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void IsPromotionEligible_ShowDateConditionNotEqual_ReturnsTrue()
        {
            // Arrange
            var promotion = new Promotion 
            { 
                PromotionId = 1, 
                IsActive = true,
                PromotionConditions = new List<PromotionCondition>
                {
                    new PromotionCondition 
                    { 
                        TargetField = "showdate", 
                        TargetValue = "2024-06-20", 
                        Operator = "!=" 
                    }
                }
            };

            var context = new PromotionCheckContext
            {
                MemberId = "member1",
                SeatCount = 2,
                MovieId = "movie1",
                MovieName = "Test Movie",
                ShowDate = new DateTime(2024, 6, 15),
                SelectedSeatTypeIds = new List<int> { 1, 2 },
                SelectedSeatTypeNames = new List<string> { "Standard", "VIP" },
                SelectedSeatTypePricePercents = new List<decimal> { 100, 150 }
            };

            // Act
            var result = _service.IsPromotionEligible(promotion, context);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void IsPromotionEligible_ShowDateConditionGreaterEqual_ReturnsTrue()
        {
            // Arrange
            var promotion = new Promotion 
            { 
                PromotionId = 1, 
                IsActive = true,
                PromotionConditions = new List<PromotionCondition>
                {
                    new PromotionCondition 
                    { 
                        TargetField = "showdate", 
                        TargetValue = "2024-06-10", 
                        Operator = ">=" 
                    }
                }
            };

            var context = new PromotionCheckContext
            {
                MemberId = "member1",
                SeatCount = 2,
                MovieId = "movie1",
                MovieName = "Test Movie",
                ShowDate = new DateTime(2024, 6, 15),
                SelectedSeatTypeIds = new List<int> { 1, 2 },
                SelectedSeatTypeNames = new List<string> { "Standard", "VIP" },
                SelectedSeatTypePricePercents = new List<decimal> { 100, 150 }
            };

            // Act
            var result = _service.IsPromotionEligible(promotion, context);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void IsPromotionEligible_ShowDateConditionLessEqual_ReturnsTrue()
        {
            // Arrange
            var promotion = new Promotion 
            { 
                PromotionId = 1, 
                IsActive = true,
                PromotionConditions = new List<PromotionCondition>
                {
                    new PromotionCondition 
                    { 
                        TargetField = "showdate", 
                        TargetValue = "2024-06-20", 
                        Operator = "<=" 
                    }
                }
            };

            var context = new PromotionCheckContext
            {
                MemberId = "member1",
                SeatCount = 2,
                MovieId = "movie1",
                MovieName = "Test Movie",
                ShowDate = new DateTime(2024, 6, 15),
                SelectedSeatTypeIds = new List<int> { 1, 2 },
                SelectedSeatTypeNames = new List<string> { "Standard", "VIP" },
                SelectedSeatTypePricePercents = new List<decimal> { 100, 150 }
            };

            // Act
            var result = _service.IsPromotionEligible(promotion, context);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void IsPromotionEligible_AccountIdConditionNotEqual_ReturnsTrue()
        {
            // Arrange
            var promotion = new Promotion 
            { 
                PromotionId = 1, 
                IsActive = true,
                PromotionConditions = new List<PromotionCondition>
                {
                    new PromotionCondition 
                    { 
                        TargetField = "accountid", 
                        TargetValue = "acc2", 
                        Operator = "!=" 
                    }
                }
            };

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

            var member = new Member { MemberId = "member1", AccountId = "acc1" };
            var invoices = new List<Invoice> { new Invoice { AccountId = "acc1" } };

            var mockMembers = new Mock<DbSet<Member>>();
            mockMembers.As<IQueryable<Member>>().Setup(m => m.Provider).Returns(new List<Member> { member }.AsQueryable().Provider);
            mockMembers.As<IQueryable<Member>>().Setup(m => m.Expression).Returns(new List<Member> { member }.AsQueryable().Expression);
            mockMembers.As<IQueryable<Member>>().Setup(m => m.ElementType).Returns(new List<Member> { member }.AsQueryable().ElementType);
            mockMembers.As<IQueryable<Member>>().Setup(m => m.GetEnumerator()).Returns(new List<Member> { member }.GetEnumerator());

            var mockInvoices = new Mock<DbSet<Invoice>>();
            mockInvoices.As<IQueryable<Invoice>>().Setup(m => m.Provider).Returns(invoices.AsQueryable().Provider);
            mockInvoices.As<IQueryable<Invoice>>().Setup(m => m.Expression).Returns(invoices.AsQueryable().Expression);
            mockInvoices.As<IQueryable<Invoice>>().Setup(m => m.ElementType).Returns(invoices.AsQueryable().ElementType);
            mockInvoices.As<IQueryable<Invoice>>().Setup(m => m.GetEnumerator()).Returns(invoices.GetEnumerator());

            _mockContext.Setup(c => c.Members).Returns(mockMembers.Object);
            _mockContext.Setup(c => c.Invoices).Returns(mockInvoices.Object);

            // Act
            var result = _service.IsPromotionEligible(promotion, context);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void IsPromotionEligible_AccountIdConditionNullTargetValue_ReturnsTrue()
        {
            // Arrange
            var promotion = new Promotion 
            { 
                PromotionId = 1, 
                IsActive = true,
                PromotionConditions = new List<PromotionCondition>
                {
                    new PromotionCondition 
                    { 
                        TargetField = "accountid", 
                        TargetValue = null, 
                        Operator = "=" 
                    }
                }
            };

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

            var member = new Member { MemberId = "member1", AccountId = "acc1" };
            var invoices = new List<Invoice>(); // No invoices for this account

            var mockMembers = new Mock<DbSet<Member>>();
            mockMembers.As<IQueryable<Member>>().Setup(m => m.Provider).Returns(new List<Member> { member }.AsQueryable().Provider);
            mockMembers.As<IQueryable<Member>>().Setup(m => m.Expression).Returns(new List<Member> { member }.AsQueryable().Expression);
            mockMembers.As<IQueryable<Member>>().Setup(m => m.ElementType).Returns(new List<Member> { member }.AsQueryable().ElementType);
            mockMembers.As<IQueryable<Member>>().Setup(m => m.GetEnumerator()).Returns(new List<Member> { member }.GetEnumerator());

            var mockInvoices = new Mock<DbSet<Invoice>>();
            mockInvoices.As<IQueryable<Invoice>>().Setup(m => m.Provider).Returns(invoices.AsQueryable().Provider);
            mockInvoices.As<IQueryable<Invoice>>().Setup(m => m.Expression).Returns(invoices.AsQueryable().Expression);
            mockInvoices.As<IQueryable<Invoice>>().Setup(m => m.ElementType).Returns(invoices.AsQueryable().ElementType);
            mockInvoices.As<IQueryable<Invoice>>().Setup(m => m.GetEnumerator()).Returns(invoices.GetEnumerator());

            _mockContext.Setup(c => c.Members).Returns(mockMembers.Object);
            _mockContext.Setup(c => c.Invoices).Returns(mockInvoices.Object);

            // Act
            var result = _service.IsPromotionEligible(promotion, context);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void IsPromotionEligible_AccountIdConditionNullTargetValue_ReturnsFalse()
        {
            // Arrange
            var promotion = new Promotion 
            { 
                PromotionId = 1, 
                IsActive = true,
                PromotionConditions = new List<PromotionCondition>
                {
                    new PromotionCondition 
                    { 
                        TargetField = "accountid", 
                        TargetValue = null, 
                        Operator = "=" 
                    }
                }
            };

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

            var member = new Member { MemberId = "member1", AccountId = "acc1" };
            var invoices = new List<Invoice> { new Invoice { AccountId = "acc1" } }; // Has invoices

            var mockMembers = new Mock<DbSet<Member>>();
            mockMembers.As<IQueryable<Member>>().Setup(m => m.Provider).Returns(new List<Member> { member }.AsQueryable().Provider);
            mockMembers.As<IQueryable<Member>>().Setup(m => m.Expression).Returns(new List<Member> { member }.AsQueryable().Expression);
            mockMembers.As<IQueryable<Member>>().Setup(m => m.ElementType).Returns(new List<Member> { member }.AsQueryable().ElementType);
            mockMembers.As<IQueryable<Member>>().Setup(m => m.GetEnumerator()).Returns(new List<Member> { member }.GetEnumerator());

            var mockInvoices = new Mock<DbSet<Invoice>>();
            mockInvoices.As<IQueryable<Invoice>>().Setup(m => m.Provider).Returns(invoices.AsQueryable().Provider);
            mockInvoices.As<IQueryable<Invoice>>().Setup(m => m.Expression).Returns(invoices.AsQueryable().Expression);
            mockInvoices.As<IQueryable<Invoice>>().Setup(m => m.ElementType).Returns(invoices.AsQueryable().ElementType);
            mockInvoices.As<IQueryable<Invoice>>().Setup(m => m.GetEnumerator()).Returns(invoices.GetEnumerator());

            _mockContext.Setup(c => c.Members).Returns(mockMembers.Object);
            _mockContext.Setup(c => c.Invoices).Returns(mockInvoices.Object);

            // Act
            var result = _service.IsPromotionEligible(promotion, context);

            // Assert
            Assert.False(result);
        }

        #endregion

        #region GetEligiblePromotionsForMember Tests

        [Fact]
        public void GetEligiblePromotionsForMember_ReturnsEligiblePromotions()
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
                    PromotionConditions = new List<PromotionCondition>
                    {
                        new PromotionCondition 
                        { 
                            TargetField = "seat", 
                            TargetValue = "2", 
                            Operator = ">=" 
                        }
                    }
                },
                new Promotion 
                { 
                    PromotionId = 3, 
                    IsActive = false, 
                    DiscountLevel = 30,
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
            var result = _service.GetEligiblePromotionsForMember(context);

            // Assert
            Assert.Equal(2, result.Count);
            Assert.Contains(result, p => p.PromotionId == 1);
            Assert.Contains(result, p => p.PromotionId == 2);
            Assert.DoesNotContain(result, p => p.PromotionId == 3); // Inactive promotion
        }

        [Fact]
        public void GetEligiblePromotionsForMember_NoEligiblePromotions_ReturnsEmptyList()
        {
            // Arrange
            var context = new PromotionCheckContext
            {
                MemberId = "member1",
                SeatCount = 1,
                MovieId = "movie1",
                MovieName = "Test Movie",
                ShowDate = DateTime.Now,
                SelectedSeatTypeIds = new List<int> { 1 },
                SelectedSeatTypeNames = new List<string> { "Standard" },
                SelectedSeatTypePricePercents = new List<decimal> { 100 }
            };

            var promotions = new List<Promotion>
            {
                new Promotion 
                { 
                    PromotionId = 1, 
                    IsActive = true, 
                    DiscountLevel = 10,
                    PromotionConditions = new List<PromotionCondition>
                    {
                        new PromotionCondition 
                        { 
                            TargetField = "seat", 
                            TargetValue = "2", 
                            Operator = ">=" 
                        }
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
            var result = _service.GetEligiblePromotionsForMember(context);

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public void GetEligiblePromotionsForMember_NoPromotions_ReturnsEmptyList()
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

            var promotions = new List<Promotion>();

            var mockDbSet = new Mock<DbSet<Promotion>>();
            mockDbSet.As<IQueryable<Promotion>>().Setup(m => m.Provider).Returns(promotions.AsQueryable().Provider);
            mockDbSet.As<IQueryable<Promotion>>().Setup(m => m.Expression).Returns(promotions.AsQueryable().Expression);
            mockDbSet.As<IQueryable<Promotion>>().Setup(m => m.ElementType).Returns(promotions.AsQueryable().ElementType);
            mockDbSet.As<IQueryable<Promotion>>().Setup(m => m.GetEnumerator()).Returns(promotions.GetEnumerator());

            _mockContext.Setup(c => c.Promotions).Returns(mockDbSet.Object);

            // Act
            var result = _service.GetEligiblePromotionsForMember(context);

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public void GetEligiblePromotionsForMember_OnlyInactivePromotions_ReturnsEmptyList()
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
                },
                new Promotion 
                { 
                    PromotionId = 2, 
                    IsActive = false, 
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
            var result = _service.GetEligiblePromotionsForMember(context);

            // Assert
            Assert.Empty(result);
        }

        #endregion

        #region Edge Cases and Invalid Input Tests

        [Fact]
        public void IsPromotionEligible_InvalidSeatValue_ReturnsFalse()
        {
            // Arrange
            var promotion = new Promotion 
            { 
                PromotionId = 1, 
                IsActive = true,
                PromotionConditions = new List<PromotionCondition>
                {
                    new PromotionCondition 
                    { 
                        TargetField = "seat", 
                        TargetValue = "invalid", 
                        Operator = ">=" 
                    }
                }
            };

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

            // Act
            var result = _service.IsPromotionEligible(promotion, context);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void IsPromotionEligible_InvalidSeatTypeIdValue_ReturnsFalse()
        {
            // Arrange
            var promotion = new Promotion 
            { 
                PromotionId = 1, 
                IsActive = true,
                PromotionConditions = new List<PromotionCondition>
                {
                    new PromotionCondition 
                    { 
                        TargetField = "seattypeid", 
                        TargetValue = "invalid", 
                        Operator = "=" 
                    }
                }
            };

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

            // Act
            var result = _service.IsPromotionEligible(promotion, context);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void IsPromotionEligible_EmptySeatTypeIds_ReturnsFalse()
        {
            // Arrange
            var promotion = new Promotion 
            { 
                PromotionId = 1, 
                IsActive = true,
                PromotionConditions = new List<PromotionCondition>
                {
                    new PromotionCondition 
                    { 
                        TargetField = "seattypeid", 
                        TargetValue = "2", 
                        Operator = "=" 
                    }
                }
            };

            var context = new PromotionCheckContext
            {
                MemberId = "member1",
                SeatCount = 2,
                MovieId = "movie1",
                MovieName = "Test Movie",
                ShowDate = DateTime.Now,
                SelectedSeatTypeIds = new List<int>(), // Empty list
                SelectedSeatTypeNames = new List<string> { "Standard", "VIP" },
                SelectedSeatTypePricePercents = new List<decimal> { 100, 150 }
            };

            // Act
            var result = _service.IsPromotionEligible(promotion, context);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void IsPromotionEligible_EmptySeatTypeNames_ReturnsFalse()
        {
            // Arrange
            var promotion = new Promotion 
            { 
                PromotionId = 1, 
                IsActive = true,
                PromotionConditions = new List<PromotionCondition>
                {
                    new PromotionCondition 
                    { 
                        TargetField = "typename", 
                        TargetValue = "VIP", 
                        Operator = "=" 
                    }
                }
            };

            var context = new PromotionCheckContext
            {
                MemberId = "member1",
                SeatCount = 2,
                MovieId = "movie1",
                MovieName = "Test Movie",
                ShowDate = DateTime.Now,
                SelectedSeatTypeIds = new List<int> { 1, 2 },
                SelectedSeatTypeNames = new List<string>(), // Empty list
                SelectedSeatTypePricePercents = new List<decimal> { 100, 150 }
            };

            // Act
            var result = _service.IsPromotionEligible(promotion, context);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void IsPromotionEligible_EmptyPricePercents_ReturnsFalse()
        {
            // Arrange
            var promotion = new Promotion 
            { 
                PromotionId = 1, 
                IsActive = true,
                PromotionConditions = new List<PromotionCondition>
                {
                    new PromotionCondition 
                    { 
                        TargetField = "pricepercent", 
                        TargetValue = "150", 
                        Operator = ">=" 
                    }
                }
            };

            var context = new PromotionCheckContext
            {
                MemberId = "member1",
                SeatCount = 2,
                MovieId = "movie1",
                MovieName = "Test Movie",
                ShowDate = DateTime.Now,
                SelectedSeatTypeIds = new List<int> { 1, 2 },
                SelectedSeatTypeNames = new List<string> { "Standard", "VIP" },
                SelectedSeatTypePricePercents = new List<decimal>() // Empty list
            };

            // Act
            var result = _service.IsPromotionEligible(promotion, context);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void IsPromotionEligible_InvalidPricePercentValue_ReturnsFalse()
        {
            // Arrange
            var promotion = new Promotion 
            { 
                PromotionId = 1, 
                IsActive = true,
                PromotionConditions = new List<PromotionCondition>
                {
                    new PromotionCondition 
                    { 
                        TargetField = "pricepercent", 
                        TargetValue = "invalid", 
                        Operator = ">=" 
                    }
                }
            };

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

            // Act
            var result = _service.IsPromotionEligible(promotion, context);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void IsPromotionEligible_NullMovieName_ReturnsTrue()
        {
            // Arrange
            var promotion = new Promotion 
            { 
                PromotionId = 1, 
                IsActive = true,
                PromotionConditions = new List<PromotionCondition>
                {
                    new PromotionCondition 
                    { 
                        TargetField = "movienameenglish", 
                        TargetValue = "Test Movie", 
                        Operator = "=" 
                    }
                }
            };

            var context = new PromotionCheckContext
            {
                MemberId = "member1",
                SeatCount = 2,
                MovieId = "movie1",
                MovieName = null, // Null movie name
                ShowDate = DateTime.Now,
                SelectedSeatTypeIds = new List<int> { 1, 2 },
                SelectedSeatTypeNames = new List<string> { "Standard", "VIP" },
                SelectedSeatTypePricePercents = new List<decimal> { 100, 150 }
            };

            // Act
            var result = _service.IsPromotionEligible(promotion, context);

            // Assert
            Assert.True(result); // Null movie name is ignored and returns true
        }

        [Fact]
        public void IsPromotionEligible_EmptyMovieName_ReturnsTrue()
        {
            // Arrange
            var promotion = new Promotion 
            { 
                PromotionId = 1, 
                IsActive = true,
                PromotionConditions = new List<PromotionCondition>
                {
                    new PromotionCondition 
                    { 
                        TargetField = "movienameenglish", 
                        TargetValue = "Test Movie", 
                        Operator = "=" 
                    }
                }
            };

            var context = new PromotionCheckContext
            {
                MemberId = "member1",
                SeatCount = 2,
                MovieId = "movie1",
                MovieName = "", // Empty movie name
                ShowDate = DateTime.Now,
                SelectedSeatTypeIds = new List<int> { 1, 2 },
                SelectedSeatTypeNames = new List<string> { "Standard", "VIP" },
                SelectedSeatTypePricePercents = new List<decimal> { 100, 150 }
            };

            // Act
            var result = _service.IsPromotionEligible(promotion, context);

            // Assert
            Assert.True(result); // Empty movie name is ignored and returns true
        }

        [Fact]
        public void IsPromotionEligible_InvalidShowDateValue_ReturnsTrue()
        {
            // Arrange
            var promotion = new Promotion 
            { 
                PromotionId = 1, 
                IsActive = true,
                PromotionConditions = new List<PromotionCondition>
                {
                    new PromotionCondition 
                    { 
                        TargetField = "showdate", 
                        TargetValue = "invalid-date", 
                        Operator = "=" 
                    }
                }
            };

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

            // Act
            var result = _service.IsPromotionEligible(promotion, context);

            // Assert
            Assert.True(result); // Invalid date is ignored and returns true
        }

        [Fact]
        public void IsPromotionEligible_NullMemberId_ReturnsFalse()
        {
            // Arrange
            var promotion = new Promotion 
            { 
                PromotionId = 1, 
                IsActive = true,
                PromotionConditions = new List<PromotionCondition>
                {
                    new PromotionCondition 
                    { 
                        TargetField = "accountid", 
                        TargetValue = "acc1", 
                        Operator = "=" 
                    }
                }
            };

            var context = new PromotionCheckContext
            {
                MemberId = null, // Null member ID
                SeatCount = 2,
                MovieId = "movie1",
                MovieName = "Test Movie",
                ShowDate = DateTime.Now,
                SelectedSeatTypeIds = new List<int> { 1, 2 },
                SelectedSeatTypeNames = new List<string> { "Standard", "VIP" },
                SelectedSeatTypePricePercents = new List<decimal> { 100, 150 }
            };

            // Act
            var result = _service.IsPromotionEligible(promotion, context);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void IsPromotionEligible_EmptyMemberId_ReturnsFalse()
        {
            // Arrange
            var promotion = new Promotion 
            { 
                PromotionId = 1, 
                IsActive = true,
                PromotionConditions = new List<PromotionCondition>
                {
                    new PromotionCondition 
                    { 
                        TargetField = "accountid", 
                        TargetValue = "acc1", 
                        Operator = "=" 
                    }
                }
            };

            var context = new PromotionCheckContext
            {
                MemberId = "", // Empty member ID
                SeatCount = 2,
                MovieId = "movie1",
                MovieName = "Test Movie",
                ShowDate = DateTime.Now,
                SelectedSeatTypeIds = new List<int> { 1, 2 },
                SelectedSeatTypeNames = new List<string> { "Standard", "VIP" },
                SelectedSeatTypePricePercents = new List<decimal> { 100, 150 }
            };

            // Act
            var result = _service.IsPromotionEligible(promotion, context);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void IsPromotionEligible_MemberNotFound_ReturnsFalse()
        {
            // Arrange
            var promotion = new Promotion 
            { 
                PromotionId = 1, 
                IsActive = true,
                PromotionConditions = new List<PromotionCondition>
                {
                    new PromotionCondition 
                    { 
                        TargetField = "accountid", 
                        TargetValue = "acc1", 
                        Operator = "=" 
                    }
                }
            };

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

            var mockMembers = new Mock<DbSet<Member>>();
            mockMembers.As<IQueryable<Member>>().Setup(m => m.Provider).Returns(new List<Member>().AsQueryable().Provider);
            mockMembers.As<IQueryable<Member>>().Setup(m => m.Expression).Returns(new List<Member>().AsQueryable().Expression);
            mockMembers.As<IQueryable<Member>>().Setup(m => m.ElementType).Returns(new List<Member>().AsQueryable().ElementType);
            mockMembers.As<IQueryable<Member>>().Setup(m => m.GetEnumerator()).Returns(new List<Member>().GetEnumerator());

            _mockContext.Setup(c => c.Members).Returns(mockMembers.Object);

            // Act
            var result = _service.IsPromotionEligible(promotion, context);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void IsPromotionEligible_MemberWithNullAccountId_ReturnsFalse()
        {
            // Arrange
            var promotion = new Promotion 
            { 
                PromotionId = 1, 
                IsActive = true,
                PromotionConditions = new List<PromotionCondition>
                {
                    new PromotionCondition 
                    { 
                        TargetField = "accountid", 
                        TargetValue = "acc1", 
                        Operator = "=" 
                    }
                }
            };

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

            var member = new Member { MemberId = "member1", AccountId = null }; // Null AccountId

            var mockMembers = new Mock<DbSet<Member>>();
            mockMembers.As<IQueryable<Member>>().Setup(m => m.Provider).Returns(new List<Member> { member }.AsQueryable().Provider);
            mockMembers.As<IQueryable<Member>>().Setup(m => m.Expression).Returns(new List<Member> { member }.AsQueryable().Expression);
            mockMembers.As<IQueryable<Member>>().Setup(m => m.ElementType).Returns(new List<Member> { member }.AsQueryable().ElementType);
            mockMembers.As<IQueryable<Member>>().Setup(m => m.GetEnumerator()).Returns(new List<Member> { member }.GetEnumerator());

            _mockContext.Setup(c => c.Members).Returns(mockMembers.Object);

            // Act
            var result = _service.IsPromotionEligible(promotion, context);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void IsPromotionEligible_MemberWithEmptyAccountId_ReturnsFalse()
        {
            // Arrange
            var promotion = new Promotion 
            { 
                PromotionId = 1, 
                IsActive = true,
                PromotionConditions = new List<PromotionCondition>
                {
                    new PromotionCondition 
                    { 
                        TargetField = "accountid", 
                        TargetValue = "acc1", 
                        Operator = "=" 
                    }
                }
            };

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

            var member = new Member { MemberId = "member1", AccountId = "" }; // Empty AccountId

            var mockMembers = new Mock<DbSet<Member>>();
            mockMembers.As<IQueryable<Member>>().Setup(m => m.Provider).Returns(new List<Member> { member }.AsQueryable().Provider);
            mockMembers.As<IQueryable<Member>>().Setup(m => m.Expression).Returns(new List<Member> { member }.AsQueryable().Expression);
            mockMembers.As<IQueryable<Member>>().Setup(m => m.ElementType).Returns(new List<Member> { member }.AsQueryable().ElementType);
            mockMembers.As<IQueryable<Member>>().Setup(m => m.GetEnumerator()).Returns(new List<Member> { member }.GetEnumerator());

            _mockContext.Setup(c => c.Members).Returns(mockMembers.Object);

            // Act
            var result = _service.IsPromotionEligible(promotion, context);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void IsPromotionEligible_UnknownTargetField_ReturnsTrue()
        {
            // Arrange
            var promotion = new Promotion 
            { 
                PromotionId = 1, 
                IsActive = true,
                PromotionConditions = new List<PromotionCondition>
                {
                    new PromotionCondition 
                    { 
                        TargetField = "unknownfield", 
                        TargetValue = "value", 
                        Operator = "=" 
                    }
                }
            };

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

            // Act
            var result = _service.IsPromotionEligible(promotion, context);

            // Assert
            Assert.True(result); // Unknown fields are ignored and return true
        }

        [Fact]
        public void IsPromotionEligible_NullTargetField_ReturnsTrue()
        {
            // Arrange
            var promotion = new Promotion 
            { 
                PromotionId = 1, 
                IsActive = true,
                PromotionConditions = new List<PromotionCondition>
                {
                    new PromotionCondition 
                    { 
                        TargetField = null, 
                        TargetValue = "value", 
                        Operator = "=" 
                    }
                }
            };

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

            // Act
            var result = _service.IsPromotionEligible(promotion, context);

            // Assert
            Assert.True(result); // Null target fields are ignored and return true
        }

        #endregion

        #region Additional ApplyFoodPromotionsToFoods Tests

        [Fact]
        public void ApplyFoodPromotionsToFoods_HandlesInvalidPriceCondition_ReturnsOriginalPrice()
        {
            // Arrange
            var selectedFoods = new List<(int, int, decimal, string)> 
            { 
                (1, 2, 100, "Pizza")
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
            Assert.Equal(90, result[0].DiscountedPrice); // Discount applied as invalid condition is ignored
            Assert.Equal("Food Discount", result[0].PromotionName);
            Assert.Equal(10, result[0].DiscountLevel);
        }

        [Fact]
        public void ApplyFoodPromotionsToFoods_HandlesInvalidOperator_ReturnsOriginalPrice()
        {
            // Arrange
            var selectedFoods = new List<(int, int, decimal, string)> 
            { 
                (1, 2, 100, "Pizza")
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
                            Operator = "invalid" 
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
            Assert.Equal(90, result[0].DiscountedPrice); // Discount applied as invalid operator is ignored
            Assert.Equal("Food Discount", result[0].PromotionName);
            Assert.Equal(10, result[0].DiscountLevel);
        }

        [Fact]
        public void ApplyFoodPromotionsToFoods_HandlesNonFoodCondition_IgnoresCondition()
        {
            // Arrange
            var selectedFoods = new List<(int, int, decimal, string)> 
            { 
                (1, 2, 100, "Pizza")
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
                            TargetEntity = "seat", // Non-food condition
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
            Assert.Single(result);
            Assert.Equal(1, result[0].FoodId);
            Assert.Equal(100, result[0].OriginalPrice);
            Assert.Equal(90, result[0].DiscountedPrice); // Discount applied as non-food condition is ignored
            Assert.Equal("Food Discount", result[0].PromotionName);
            Assert.Equal(10, result[0].DiscountLevel);
        }

        [Fact]
        public void ApplyFoodPromotionsToFoods_HandlesNullTargetEntity_IgnoresCondition()
        {
            // Arrange
            var selectedFoods = new List<(int, int, decimal, string)> 
            { 
                (1, 2, 100, "Pizza")
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
                            TargetEntity = null, // Null target entity
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
            Assert.Single(result);
            Assert.Equal(1, result[0].FoodId);
            Assert.Equal(100, result[0].OriginalPrice);
            Assert.Equal(90, result[0].DiscountedPrice); // Discount applied as null target entity is ignored
            Assert.Equal("Food Discount", result[0].PromotionName);
            Assert.Equal(10, result[0].DiscountLevel);
        }

        [Fact]
        public void ApplyFoodPromotionsToFoods_HandlesNullTargetField_ReturnsOriginalPrice()
        {
            // Arrange
            var selectedFoods = new List<(int, int, decimal, string)> 
            { 
                (1, 2, 100, "Pizza")
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
                            TargetField = null, // Null target field
                            TargetValue = "50", 
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
            Assert.Equal(90, result[0].DiscountedPrice); // Discount applied as null target field is ignored
            Assert.Equal("Food Discount", result[0].PromotionName);
            Assert.Equal(10, result[0].DiscountLevel);
        }

        [Fact]
        public void ApplyFoodPromotionsToFoods_HandlesUnknownTargetField_ReturnsOriginalPrice()
        {
            // Arrange
            var selectedFoods = new List<(int, int, decimal, string)> 
            { 
                (1, 2, 100, "Pizza")
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
                            TargetField = "unknownfield", // Unknown target field
                            TargetValue = "50", 
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
            Assert.Equal(90, result[0].DiscountedPrice); // Discount applied as unknown target field is ignored
            Assert.Equal("Food Discount", result[0].PromotionName);
            Assert.Equal(10, result[0].DiscountLevel);
        }

        [Fact]
        public void ApplyFoodPromotionsToFoods_HandlesNullDiscountLevel_ReturnsOriginalPrice()
        {
            // Arrange
            var selectedFoods = new List<(int, int, decimal, string)> 
            { 
                (1, 2, 100, "Pizza")
            };

            var eligiblePromotions = new List<Promotion>
            {
                new Promotion 
                { 
                    PromotionId = 1, 
                    Title = "Food Discount",
                    DiscountLevel = null, // Null discount level
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
            Assert.Single(result);
            Assert.Equal(1, result[0].FoodId);
            Assert.Equal(100, result[0].OriginalPrice);
            Assert.Equal(100, result[0].DiscountedPrice); // No discount applied due to null discount level
            Assert.Null(result[0].PromotionName);
            Assert.Equal(0, result[0].DiscountLevel);
        }

        [Fact]
        public void ApplyFoodPromotionsToFoods_HandlesZeroDiscountLevel_ReturnsOriginalPrice()
        {
            // Arrange
            var selectedFoods = new List<(int, int, decimal, string)> 
            { 
                (1, 2, 100, "Pizza")
            };

            var eligiblePromotions = new List<Promotion>
            {
                new Promotion 
                { 
                    PromotionId = 1, 
                    Title = "Food Discount",
                    DiscountLevel = 0, // Zero discount level
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
            Assert.Single(result);
            Assert.Equal(1, result[0].FoodId);
            Assert.Equal(100, result[0].OriginalPrice);
            Assert.Equal(100, result[0].DiscountedPrice); // No discount applied due to zero discount level
            Assert.Null(result[0].PromotionName); // No promotion name when discount level is 0
            Assert.Equal(0, result[0].DiscountLevel);
        }

        [Fact]
        public void ApplyFoodPromotionsToFoods_HandlesNegativeDiscountLevel_ReturnsOriginalPrice()
        {
            // Arrange
            var selectedFoods = new List<(int, int, decimal, string)> 
            { 
                (1, 2, 100, "Pizza")
            };

            var eligiblePromotions = new List<Promotion>
            {
                new Promotion 
                { 
                    PromotionId = 1, 
                    Title = "Food Discount",
                    DiscountLevel = -10, // Negative discount level
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
            Assert.Single(result);
            Assert.Equal(1, result[0].FoodId);
            Assert.Equal(100, result[0].OriginalPrice);
            Assert.Equal(100, result[0].DiscountedPrice); // No discount applied due to negative discount level
            Assert.Null(result[0].PromotionName); // No promotion name when discount level is negative
            Assert.Equal(-10, result[0].DiscountLevel);
        }

        #endregion

        [Fact]
        public void GetBestEligiblePromotionForBooking_HandlesNullPromotionConditions_ReturnsEligiblePromotion()
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
                    DiscountLevel = 15,
                    PromotionConditions = null // Null conditions should return true
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
            Assert.Equal(1, result.PromotionId);
        }

        [Fact]
        public void GetBestEligiblePromotionForBooking_HandlesEmptyPromotionConditions_ReturnsEligiblePromotion()
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
                    DiscountLevel = 15,
                    PromotionConditions = new List<PromotionCondition>() // Empty conditions should return true
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
            Assert.Equal(1, result.PromotionId);
        }

        [Fact]
        public void GetBestEligiblePromotionForBooking_HandlesInvalidSeatValue_ReturnsNoPromotion()
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
                    DiscountLevel = 15,
                    PromotionConditions = new List<PromotionCondition>
                    {
                        new PromotionCondition 
                        { 
                            TargetField = "seat", 
                            TargetValue = "invalid", // Invalid seat value
                            Operator = ">=" 
                        }
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
            var result = _service.GetBestEligiblePromotionForBooking(context);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void GetBestEligiblePromotionForBooking_HandlesInvalidSeatTypeIdValue_ReturnsNoPromotion()
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
                    DiscountLevel = 15,
                    PromotionConditions = new List<PromotionCondition>
                    {
                        new PromotionCondition 
                        { 
                            TargetField = "seattypeid", 
                            TargetValue = "invalid", // Invalid seat type ID value
                            Operator = "=" 
                        }
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
            var result = _service.GetBestEligiblePromotionForBooking(context);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void GetBestEligiblePromotionForBooking_HandlesEmptySeatTypeIds_ReturnsNoPromotion()
        {
            // Arrange
            var context = new PromotionCheckContext
            {
                MemberId = "member1",
                SeatCount = 2,
                MovieId = "movie1",
                MovieName = "Test Movie",
                ShowDate = DateTime.Now,
                SelectedSeatTypeIds = new List<int>(), // Empty seat type IDs
                SelectedSeatTypeNames = new List<string> { "Standard", "VIP" },
                SelectedSeatTypePricePercents = new List<decimal> { 100, 150 }
            };

            var promotions = new List<Promotion>
            {
                new Promotion 
                { 
                    PromotionId = 1, 
                    IsActive = true, 
                    DiscountLevel = 15,
                    PromotionConditions = new List<PromotionCondition>
                    {
                        new PromotionCondition 
                        { 
                            TargetField = "seattypeid", 
                            TargetValue = "1", 
                            Operator = "=" 
                        }
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
            var result = _service.GetBestEligiblePromotionForBooking(context);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void GetBestEligiblePromotionForBooking_HandlesEmptySeatTypeNames_ReturnsNoPromotion()
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
                SelectedSeatTypeNames = new List<string>(), // Empty seat type names
                SelectedSeatTypePricePercents = new List<decimal> { 100, 150 }
            };

            var promotions = new List<Promotion>
            {
                new Promotion 
                { 
                    PromotionId = 1, 
                    IsActive = true, 
                    DiscountLevel = 15,
                    PromotionConditions = new List<PromotionCondition>
                    {
                        new PromotionCondition 
                        { 
                            TargetField = "typename", 
                            TargetValue = "VIP", 
                            Operator = "=" 
                        }
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
            var result = _service.GetBestEligiblePromotionForBooking(context);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void GetBestEligiblePromotionForBooking_HandlesEmptyPricePercents_ReturnsNoPromotion()
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
                SelectedSeatTypePricePercents = new List<decimal>() // Empty price percents
            };

            var promotions = new List<Promotion>
            {
                new Promotion 
                { 
                    PromotionId = 1, 
                    IsActive = true, 
                    DiscountLevel = 15,
                    PromotionConditions = new List<PromotionCondition>
                    {
                        new PromotionCondition 
                        { 
                            TargetField = "pricepercent", 
                            TargetValue = "100", 
                            Operator = ">=" 
                        }
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
            var result = _service.GetBestEligiblePromotionForBooking(context);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void GetBestEligiblePromotionForBooking_HandlesInvalidPricePercentValue_ReturnsNoPromotion()
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
                    DiscountLevel = 15,
                    PromotionConditions = new List<PromotionCondition>
                    {
                        new PromotionCondition 
                        { 
                            TargetField = "pricepercent", 
                            TargetValue = "invalid", // Invalid price percent value
                            Operator = ">=" 
                        }
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
            var result = _service.GetBestEligiblePromotionForBooking(context);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void GetBestEligiblePromotionForBooking_HandlesNullMovieName_ReturnsNoPromotion()
        {
            // Arrange
            var context = new PromotionCheckContext
            {
                MemberId = "member1",
                SeatCount = 2,
                MovieId = "movie1",
                MovieName = null, // Null movie name
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
                    DiscountLevel = 15,
                    PromotionConditions = new List<PromotionCondition>
                    {
                        new PromotionCondition 
                        { 
                            TargetField = "movienameenglish", 
                            TargetValue = "Test Movie", 
                            Operator = "=" 
                        }
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
            var result = _service.GetBestEligiblePromotionForBooking(context);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void GetBestEligiblePromotionForBooking_HandlesEmptyMovieName_ReturnsNoPromotion()
        {
            // Arrange
            var context = new PromotionCheckContext
            {
                MemberId = "member1",
                SeatCount = 2,
                MovieId = "movie1",
                MovieName = "", // Empty movie name
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
                    DiscountLevel = 15,
                    PromotionConditions = new List<PromotionCondition>
                    {
                        new PromotionCondition 
                        { 
                            TargetField = "movienameenglish", 
                            TargetValue = "Test Movie", 
                            Operator = "=" 
                        }
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
            var result = _service.GetBestEligiblePromotionForBooking(context);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void GetBestEligiblePromotionForBooking_HandlesInvalidShowDateValue_ReturnsNoPromotion()
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
                    DiscountLevel = 15,
                    PromotionConditions = new List<PromotionCondition>
                    {
                        new PromotionCondition 
                        { 
                            TargetField = "showdate", 
                            TargetValue = "invalid-date", // Invalid date value
                            Operator = "=" 
                        }
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
            var result = _service.GetBestEligiblePromotionForBooking(context);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void GetBestEligiblePromotionForBooking_HandlesNullMemberId_ReturnsNoPromotion()
        {
            // Arrange
            var context = new PromotionCheckContext
            {
                MemberId = null, // Null member ID
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
                    DiscountLevel = 15,
                    PromotionConditions = new List<PromotionCondition>
                    {
                        new PromotionCondition 
                        { 
                            TargetField = "accountid", 
                            TargetValue = "account1", 
                            Operator = "=" 
                        }
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
            var result = _service.GetBestEligiblePromotionForBooking(context);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void GetBestEligiblePromotionForBooking_HandlesEmptyMemberId_ReturnsNoPromotion()
        {
            // Arrange
            var context = new PromotionCheckContext
            {
                MemberId = "", // Empty member ID
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
                    DiscountLevel = 15,
                    PromotionConditions = new List<PromotionCondition>
                    {
                        new PromotionCondition 
                        { 
                            TargetField = "accountid", 
                            TargetValue = "account1", 
                            Operator = "=" 
                        }
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
            var result = _service.GetBestEligiblePromotionForBooking(context);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void GetBestEligiblePromotionForBooking_HandlesMemberNotFound_ReturnsNoPromotion()
        {
            // Arrange
            var context = new PromotionCheckContext
            {
                MemberId = "nonexistent-member",
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
                    DiscountLevel = 15,
                    PromotionConditions = new List<PromotionCondition>
                    {
                        new PromotionCondition 
                        { 
                            TargetField = "accountid", 
                            TargetValue = "account1", 
                            Operator = "=" 
                        }
                    }
                }
            };

            var mockDbSet = new Mock<DbSet<Promotion>>();
            mockDbSet.As<IQueryable<Promotion>>().Setup(m => m.Provider).Returns(promotions.AsQueryable().Provider);
            mockDbSet.As<IQueryable<Promotion>>().Setup(m => m.Expression).Returns(promotions.AsQueryable().Expression);
            mockDbSet.As<IQueryable<Promotion>>().Setup(m => m.ElementType).Returns(promotions.AsQueryable().ElementType);
            mockDbSet.As<IQueryable<Promotion>>().Setup(m => m.GetEnumerator()).Returns(promotions.GetEnumerator());

            _mockContext.Setup(c => c.Promotions).Returns(mockDbSet.Object);

            // Mock Members DbSet to return null
            var mockMembersDbSet = new Mock<DbSet<Member>>();
            mockMembersDbSet.As<IQueryable<Member>>().Setup(m => m.Provider).Returns(new List<Member>().AsQueryable().Provider);
            mockMembersDbSet.As<IQueryable<Member>>().Setup(m => m.Expression).Returns(new List<Member>().AsQueryable().Expression);
            mockMembersDbSet.As<IQueryable<Member>>().Setup(m => m.ElementType).Returns(new List<Member>().AsQueryable().ElementType);
            mockMembersDbSet.As<IQueryable<Member>>().Setup(m => m.GetEnumerator()).Returns(new List<Member>().GetEnumerator());

            _mockContext.Setup(c => c.Members).Returns(mockMembersDbSet.Object);

            // Act
            var result = _service.GetBestEligiblePromotionForBooking(context);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void GetBestEligiblePromotionForBooking_HandlesMemberWithNullAccountId_ReturnsNoPromotion()
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
                    DiscountLevel = 15,
                    PromotionConditions = new List<PromotionCondition>
                    {
                        new PromotionCondition 
                        { 
                            TargetField = "accountid", 
                            TargetValue = "account1", 
                            Operator = "=" 
                        }
                    }
                }
            };

            var members = new List<Member>
            {
                new Member { MemberId = "member1", AccountId = null } // Member with null AccountId
            };

            var mockDbSet = new Mock<DbSet<Promotion>>();
            mockDbSet.As<IQueryable<Promotion>>().Setup(m => m.Provider).Returns(promotions.AsQueryable().Provider);
            mockDbSet.As<IQueryable<Promotion>>().Setup(m => m.Expression).Returns(promotions.AsQueryable().Expression);
            mockDbSet.As<IQueryable<Promotion>>().Setup(m => m.ElementType).Returns(promotions.AsQueryable().ElementType);
            mockDbSet.As<IQueryable<Promotion>>().Setup(m => m.GetEnumerator()).Returns(promotions.GetEnumerator());

            var mockMembersDbSet = new Mock<DbSet<Member>>();
            mockMembersDbSet.As<IQueryable<Member>>().Setup(m => m.Provider).Returns(members.AsQueryable().Provider);
            mockMembersDbSet.As<IQueryable<Member>>().Setup(m => m.Expression).Returns(members.AsQueryable().Expression);
            mockMembersDbSet.As<IQueryable<Member>>().Setup(m => m.ElementType).Returns(members.AsQueryable().ElementType);
            mockMembersDbSet.As<IQueryable<Member>>().Setup(m => m.GetEnumerator()).Returns(members.GetEnumerator());

            _mockContext.Setup(c => c.Promotions).Returns(mockDbSet.Object);
            _mockContext.Setup(c => c.Members).Returns(mockMembersDbSet.Object);

            // Act
            var result = _service.GetBestEligiblePromotionForBooking(context);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void GetBestEligiblePromotionForBooking_HandlesMemberWithEmptyAccountId_ReturnsNoPromotion()
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
                    DiscountLevel = 15,
                    PromotionConditions = new List<PromotionCondition>
                    {
                        new PromotionCondition 
                        { 
                            TargetField = "accountid", 
                            TargetValue = "account1", 
                            Operator = "=" 
                        }
                    }
                }
            };

            var members = new List<Member>
            {
                new Member { MemberId = "member1", AccountId = "" } // Member with empty AccountId
            };

            var mockDbSet = new Mock<DbSet<Promotion>>();
            mockDbSet.As<IQueryable<Promotion>>().Setup(m => m.Provider).Returns(promotions.AsQueryable().Provider);
            mockDbSet.As<IQueryable<Promotion>>().Setup(m => m.Expression).Returns(promotions.AsQueryable().Expression);
            mockDbSet.As<IQueryable<Promotion>>().Setup(m => m.ElementType).Returns(promotions.AsQueryable().ElementType);
            mockDbSet.As<IQueryable<Promotion>>().Setup(m => m.GetEnumerator()).Returns(promotions.GetEnumerator());

            var mockMembersDbSet = new Mock<DbSet<Member>>();
            mockMembersDbSet.As<IQueryable<Member>>().Setup(m => m.Provider).Returns(members.AsQueryable().Provider);
            mockMembersDbSet.As<IQueryable<Member>>().Setup(m => m.Expression).Returns(members.AsQueryable().Expression);
            mockMembersDbSet.As<IQueryable<Member>>().Setup(m => m.ElementType).Returns(members.AsQueryable().ElementType);
            mockMembersDbSet.As<IQueryable<Member>>().Setup(m => m.GetEnumerator()).Returns(members.GetEnumerator());

            _mockContext.Setup(c => c.Promotions).Returns(mockDbSet.Object);
            _mockContext.Setup(c => c.Members).Returns(mockMembersDbSet.Object);

            // Act
            var result = _service.GetBestEligiblePromotionForBooking(context);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void GetBestEligiblePromotionForBooking_HandlesUnknownTargetField_ReturnsEligiblePromotion()
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
                    DiscountLevel = 15,
                    PromotionConditions = new List<PromotionCondition>
                    {
                        new PromotionCondition 
                        { 
                            TargetField = "unknownfield", // Unknown target field
                            TargetValue = "value", 
                            Operator = "=" 
                        }
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
            var result = _service.GetBestEligiblePromotionForBooking(context);

            // Assert
            Assert.NotNull(result); // Should return eligible promotion for unknown field
            Assert.Equal(1, result.PromotionId);
        }

        [Fact]
        public void GetBestEligiblePromotionForBooking_HandlesNullTargetField_ReturnsEligiblePromotion()
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
                    DiscountLevel = 15,
                    PromotionConditions = new List<PromotionCondition>
                    {
                        new PromotionCondition 
                        { 
                            TargetField = null, // Null target field
                            TargetValue = "value", 
                            Operator = "=" 
                        }
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
            var result = _service.GetBestEligiblePromotionForBooking(context);

            // Assert
            Assert.NotNull(result); // Should return eligible promotion for null field
            Assert.Equal(1, result.PromotionId);
        }

        [Fact]
        public void GetBestEligiblePromotionForBooking_HandlesSeatConditionEqual_ReturnsEligiblePromotion()
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
                    DiscountLevel = 15,
                    PromotionConditions = new List<PromotionCondition>
                    {
                        new PromotionCondition 
                        { 
                            TargetField = "seat", 
                            TargetValue = "2", 
                            Operator = "==" 
                        }
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
            var result = _service.GetBestEligiblePromotionForBooking(context);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1, result.PromotionId);
        }

        [Fact]
        public void GetBestEligiblePromotionForBooking_HandlesSeatConditionLessEqual_ReturnsEligiblePromotion()
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
                    DiscountLevel = 15,
                    PromotionConditions = new List<PromotionCondition>
                    {
                        new PromotionCondition 
                        { 
                            TargetField = "seat", 
                            TargetValue = "3", 
                            Operator = "<=" 
                        }
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
            var result = _service.GetBestEligiblePromotionForBooking(context);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1, result.PromotionId);
        }

        [Fact]
        public void GetBestEligiblePromotionForBooking_HandlesSeatConditionLessThan_ReturnsEligiblePromotion()
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
                    DiscountLevel = 15,
                    PromotionConditions = new List<PromotionCondition>
                    {
                        new PromotionCondition 
                        { 
                            TargetField = "seat", 
                            TargetValue = "3", 
                            Operator = "<" 
                        }
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
            var result = _service.GetBestEligiblePromotionForBooking(context);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1, result.PromotionId);
        }

        [Fact]
        public void GetBestEligiblePromotionForBooking_HandlesSeatConditionNotEqual_ReturnsEligiblePromotion()
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
                    DiscountLevel = 15,
                    PromotionConditions = new List<PromotionCondition>
                    {
                        new PromotionCondition 
                        { 
                            TargetField = "seat", 
                            TargetValue = "3", 
                            Operator = "!=" 
                        }
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
            var result = _service.GetBestEligiblePromotionForBooking(context);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1, result.PromotionId);
        }

        [Fact]
        public void GetBestEligiblePromotionForBooking_HandlesSeatTypeIdConditionNotEqual_ReturnsEligiblePromotion()
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
                    DiscountLevel = 15,
                    PromotionConditions = new List<PromotionCondition>
                    {
                        new PromotionCondition 
                        { 
                            TargetField = "seattypeid", 
                            TargetValue = "3", 
                            Operator = "!=" 
                        }
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
            var result = _service.GetBestEligiblePromotionForBooking(context);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1, result.PromotionId);
        }

        [Fact]
        public void GetBestEligiblePromotionForBooking_HandlesTypeNameConditionNotEqual_ReturnsEligiblePromotion()
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
                    DiscountLevel = 15,
                    PromotionConditions = new List<PromotionCondition>
                    {
                        new PromotionCondition 
                        { 
                            TargetField = "typename", 
                            TargetValue = "Premium", 
                            Operator = "!=" 
                        }
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
            var result = _service.GetBestEligiblePromotionForBooking(context);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1, result.PromotionId);
        }

        [Fact]
        public void GetBestEligiblePromotionForBooking_HandlesPricePercentConditionGreaterThan_ReturnsEligiblePromotion()
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
                    DiscountLevel = 15,
                    PromotionConditions = new List<PromotionCondition>
                    {
                        new PromotionCondition 
                        { 
                            TargetField = "pricepercent", 
                            TargetValue = "90", 
                            Operator = ">" 
                        }
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
            var result = _service.GetBestEligiblePromotionForBooking(context);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1, result.PromotionId);
        }

        [Fact]
        public void GetBestEligiblePromotionForBooking_HandlesPricePercentConditionLessThan_ReturnsEligiblePromotion()
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
                    DiscountLevel = 15,
                    PromotionConditions = new List<PromotionCondition>
                    {
                        new PromotionCondition 
                        { 
                            TargetField = "pricepercent", 
                            TargetValue = "200", 
                            Operator = "<" 
                        }
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
            var result = _service.GetBestEligiblePromotionForBooking(context);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1, result.PromotionId);
        }

        [Fact]
        public void GetBestEligiblePromotionForBooking_HandlesPricePercentConditionLessEqual_ReturnsEligiblePromotion()
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
                    DiscountLevel = 15,
                    PromotionConditions = new List<PromotionCondition>
                    {
                        new PromotionCondition 
                        { 
                            TargetField = "pricepercent", 
                            TargetValue = "150", 
                            Operator = "<=" 
                        }
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
            var result = _service.GetBestEligiblePromotionForBooking(context);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1, result.PromotionId);
        }

        [Fact]
        public void GetBestEligiblePromotionForBooking_HandlesPricePercentConditionEqual_ReturnsEligiblePromotion()
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
                    DiscountLevel = 15,
                    PromotionConditions = new List<PromotionCondition>
                    {
                        new PromotionCondition 
                        { 
                            TargetField = "pricepercent", 
                            TargetValue = "100", 
                            Operator = "==" 
                        }
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
            var result = _service.GetBestEligiblePromotionForBooking(context);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1, result.PromotionId);
        }

        [Fact]
        public void GetBestEligiblePromotionForBooking_HandlesPricePercentConditionNotEqual_ReturnsEligiblePromotion()
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
                    DiscountLevel = 15,
                    PromotionConditions = new List<PromotionCondition>
                    {
                        new PromotionCondition 
                        { 
                            TargetField = "pricepercent", 
                            TargetValue = "200", 
                            Operator = "!=" 
                        }
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
            var result = _service.GetBestEligiblePromotionForBooking(context);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1, result.PromotionId);
        }

        [Fact]
        public void GetBestEligiblePromotionForBooking_HandlesMovieNameConditionNotEqual_ReturnsEligiblePromotion()
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
                    DiscountLevel = 15,
                    PromotionConditions = new List<PromotionCondition>
                    {
                        new PromotionCondition 
                        { 
                            TargetField = "movienameenglish", 
                            TargetValue = "Different Movie", 
                            Operator = "!=" 
                        }
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
            var result = _service.GetBestEligiblePromotionForBooking(context);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1, result.PromotionId);
        }

        [Fact]
        public void GetBestEligiblePromotionForBooking_HandlesShowDateConditionNotEqual_ReturnsEligiblePromotion()
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
                    DiscountLevel = 15,
                    PromotionConditions = new List<PromotionCondition>
                    {
                        new PromotionCondition 
                        { 
                            TargetField = "showdate", 
                            TargetValue = DateTime.Now.AddDays(1).ToString("yyyy-MM-dd"), 
                            Operator = "!=" 
                        }
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
            var result = _service.GetBestEligiblePromotionForBooking(context);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1, result.PromotionId);
        }

        [Fact]
        public void GetBestEligiblePromotionForBooking_HandlesShowDateConditionGreaterEqual_ReturnsEligiblePromotion()
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
                    DiscountLevel = 15,
                    PromotionConditions = new List<PromotionCondition>
                    {
                        new PromotionCondition 
                        { 
                            TargetField = "showdate", 
                            TargetValue = DateTime.Now.AddDays(-1).ToString("yyyy-MM-dd"), 
                            Operator = ">=" 
                        }
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
            var result = _service.GetBestEligiblePromotionForBooking(context);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1, result.PromotionId);
        }

        [Fact]
        public void GetBestEligiblePromotionForBooking_HandlesShowDateConditionLessEqual_ReturnsEligiblePromotion()
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
                    DiscountLevel = 15,
                    PromotionConditions = new List<PromotionCondition>
                    {
                        new PromotionCondition 
                        { 
                            TargetField = "showdate", 
                            TargetValue = DateTime.Now.AddDays(1).ToString("yyyy-MM-dd"), 
                            Operator = "<=" 
                        }
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
            var result = _service.GetBestEligiblePromotionForBooking(context);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1, result.PromotionId);
        }

        [Fact]
        public void GetBestEligiblePromotionForBooking_HandlesAccountIdConditionNotEqual_ReturnsEligiblePromotion()
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
                    DiscountLevel = 15,
                    PromotionConditions = new List<PromotionCondition>
                    {
                        new PromotionCondition 
                        { 
                            TargetField = "accountid", 
                            TargetValue = "different-account", 
                            Operator = "!=" 
                        }
                    }
                }
            };

            var members = new List<Member>
            {
                new Member { MemberId = "member1", AccountId = "account1" }
            };

            var invoices = new List<Invoice>
            {
                new Invoice { AccountId = "account1" }
            };

            var mockDbSet = new Mock<DbSet<Promotion>>();
            mockDbSet.As<IQueryable<Promotion>>().Setup(m => m.Provider).Returns(promotions.AsQueryable().Provider);
            mockDbSet.As<IQueryable<Promotion>>().Setup(m => m.Expression).Returns(promotions.AsQueryable().Expression);
            mockDbSet.As<IQueryable<Promotion>>().Setup(m => m.ElementType).Returns(promotions.AsQueryable().ElementType);
            mockDbSet.As<IQueryable<Promotion>>().Setup(m => m.GetEnumerator()).Returns(promotions.GetEnumerator());

            var mockMembersDbSet = new Mock<DbSet<Member>>();
            mockMembersDbSet.As<IQueryable<Member>>().Setup(m => m.Provider).Returns(members.AsQueryable().Provider);
            mockMembersDbSet.As<IQueryable<Member>>().Setup(m => m.Expression).Returns(members.AsQueryable().Expression);
            mockMembersDbSet.As<IQueryable<Member>>().Setup(m => m.ElementType).Returns(members.AsQueryable().ElementType);
            mockMembersDbSet.As<IQueryable<Member>>().Setup(m => m.GetEnumerator()).Returns(members.GetEnumerator());

            var mockInvoicesDbSet = new Mock<DbSet<Invoice>>();
            mockInvoicesDbSet.As<IQueryable<Invoice>>().Setup(m => m.Provider).Returns(invoices.AsQueryable().Provider);
            mockInvoicesDbSet.As<IQueryable<Invoice>>().Setup(m => m.Expression).Returns(invoices.AsQueryable().Expression);
            mockInvoicesDbSet.As<IQueryable<Invoice>>().Setup(m => m.ElementType).Returns(invoices.AsQueryable().ElementType);
            mockInvoicesDbSet.As<IQueryable<Invoice>>().Setup(m => m.GetEnumerator()).Returns(invoices.GetEnumerator());

            _mockContext.Setup(c => c.Promotions).Returns(mockDbSet.Object);
            _mockContext.Setup(c => c.Members).Returns(mockMembersDbSet.Object);
            _mockContext.Setup(c => c.Invoices).Returns(mockInvoicesDbSet.Object);

            // Act
            var result = _service.GetBestEligiblePromotionForBooking(context);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1, result.PromotionId);
        }

        [Fact]
        public void GetBestEligiblePromotionForBooking_HandlesAccountIdConditionNullTargetValue_ReturnsEligiblePromotion()
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
                    DiscountLevel = 15,
                    PromotionConditions = new List<PromotionCondition>
                    {
                        new PromotionCondition 
                        { 
                            TargetField = "accountid", 
                            TargetValue = null, // Null target value
                            Operator = "=" 
                        }
                    }
                }
            };

            var members = new List<Member>
            {
                new Member { MemberId = "member1", AccountId = "account1" }
            };

            var invoices = new List<Invoice>(); // Empty invoices list - no invoices for this account

            var mockDbSet = new Mock<DbSet<Promotion>>();
            mockDbSet.As<IQueryable<Promotion>>().Setup(m => m.Provider).Returns(promotions.AsQueryable().Provider);
            mockDbSet.As<IQueryable<Promotion>>().Setup(m => m.Expression).Returns(promotions.AsQueryable().Expression);
            mockDbSet.As<IQueryable<Promotion>>().Setup(m => m.ElementType).Returns(promotions.AsQueryable().ElementType);
            mockDbSet.As<IQueryable<Promotion>>().Setup(m => m.GetEnumerator()).Returns(promotions.GetEnumerator());

            var mockMembersDbSet = new Mock<DbSet<Member>>();
            mockMembersDbSet.As<IQueryable<Member>>().Setup(m => m.Provider).Returns(members.AsQueryable().Provider);
            mockMembersDbSet.As<IQueryable<Member>>().Setup(m => m.Expression).Returns(members.AsQueryable().Expression);
            mockMembersDbSet.As<IQueryable<Member>>().Setup(m => m.ElementType).Returns(members.AsQueryable().ElementType);
            mockMembersDbSet.As<IQueryable<Member>>().Setup(m => m.GetEnumerator()).Returns(members.GetEnumerator());

            var mockInvoicesDbSet = new Mock<DbSet<Invoice>>();
            mockInvoicesDbSet.As<IQueryable<Invoice>>().Setup(m => m.Provider).Returns(invoices.AsQueryable().Provider);
            mockInvoicesDbSet.As<IQueryable<Invoice>>().Setup(m => m.Expression).Returns(invoices.AsQueryable().Expression);
            mockInvoicesDbSet.As<IQueryable<Invoice>>().Setup(m => m.ElementType).Returns(invoices.AsQueryable().ElementType);
            mockInvoicesDbSet.As<IQueryable<Invoice>>().Setup(m => m.GetEnumerator()).Returns(invoices.GetEnumerator());

            _mockContext.Setup(c => c.Promotions).Returns(mockDbSet.Object);
            _mockContext.Setup(c => c.Members).Returns(mockMembersDbSet.Object);
            _mockContext.Setup(c => c.Invoices).Returns(mockInvoicesDbSet.Object);

            // Act
            var result = _service.GetBestEligiblePromotionForBooking(context);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1, result.PromotionId);
        }

        [Fact]
        public void GetBestEligiblePromotionForBooking_HandlesAccountIdConditionNullTargetValue_ReturnsNoPromotion()
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
                    DiscountLevel = 15,
                    PromotionConditions = new List<PromotionCondition>
                    {
                        new PromotionCondition 
                        { 
                            TargetField = "accountid", 
                            TargetValue = null, // Null target value
                            Operator = "=" 
                        }
                    }
                }
            };

            var members = new List<Member>
            {
                new Member { MemberId = "member1", AccountId = "account1" }
            };

            var invoices = new List<Invoice>
            {
                new Invoice { AccountId = "account1" }
            };

            var mockDbSet = new Mock<DbSet<Promotion>>();
            mockDbSet.As<IQueryable<Promotion>>().Setup(m => m.Provider).Returns(promotions.AsQueryable().Provider);
            mockDbSet.As<IQueryable<Promotion>>().Setup(m => m.Expression).Returns(promotions.AsQueryable().Expression);
            mockDbSet.As<IQueryable<Promotion>>().Setup(m => m.ElementType).Returns(promotions.AsQueryable().ElementType);
            mockDbSet.As<IQueryable<Promotion>>().Setup(m => m.GetEnumerator()).Returns(promotions.GetEnumerator());

            var mockMembersDbSet = new Mock<DbSet<Member>>();
            mockMembersDbSet.As<IQueryable<Member>>().Setup(m => m.Provider).Returns(members.AsQueryable().Provider);
            mockMembersDbSet.As<IQueryable<Member>>().Setup(m => m.Expression).Returns(members.AsQueryable().Expression);
            mockMembersDbSet.As<IQueryable<Member>>().Setup(m => m.ElementType).Returns(members.AsQueryable().ElementType);
            mockMembersDbSet.As<IQueryable<Member>>().Setup(m => m.GetEnumerator()).Returns(members.GetEnumerator());

            var mockInvoicesDbSet = new Mock<DbSet<Invoice>>();
            mockInvoicesDbSet.As<IQueryable<Invoice>>().Setup(m => m.Provider).Returns(invoices.AsQueryable().Provider);
            mockInvoicesDbSet.As<IQueryable<Invoice>>().Setup(m => m.Expression).Returns(invoices.AsQueryable().Expression);
            mockInvoicesDbSet.As<IQueryable<Invoice>>().Setup(m => m.ElementType).Returns(invoices.AsQueryable().ElementType);
            mockInvoicesDbSet.As<IQueryable<Invoice>>().Setup(m => m.GetEnumerator()).Returns(invoices.GetEnumerator());

            _mockContext.Setup(c => c.Promotions).Returns(mockDbSet.Object);
            _mockContext.Setup(c => c.Members).Returns(mockMembersDbSet.Object);
            _mockContext.Setup(c => c.Invoices).Returns(mockInvoicesDbSet.Object);

            // Act
            var result = _service.GetBestEligiblePromotionForBooking(context);

            // Assert
            Assert.Null(result); // Should return null when account has invoices
        }
    }
} 