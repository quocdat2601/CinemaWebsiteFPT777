using MovieTheater.Repository;
using MovieTheater.Models;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;
using System.Collections.Generic;
using System.Linq;

namespace MovieTheater.Tests.Repository
{
    public class PromotionRepositoryTests
    {
        private readonly Mock<MovieTheaterContext> _mockContext;
        private readonly Mock<DbSet<Promotion>> _mockPromotionDbSet;
        private readonly Mock<DbSet<PromotionCondition>> _mockConditionDbSet;
        private readonly Mock<DbSet<ConditionType>> _mockConditionTypeDbSet;
        private readonly PromotionRepository _repository;

        public PromotionRepositoryTests()
        {
            _mockContext = new Mock<MovieTheaterContext>();
            _mockPromotionDbSet = new Mock<DbSet<Promotion>>();
            _mockConditionDbSet = new Mock<DbSet<PromotionCondition>>();
            _mockConditionTypeDbSet = new Mock<DbSet<ConditionType>>();

            _mockContext.Setup(c => c.Promotions).Returns(_mockPromotionDbSet.Object);
            _mockContext.Setup(c => c.PromotionConditions).Returns(_mockConditionDbSet.Object);
            _mockContext.Setup(c => c.ConditionTypes).Returns(_mockConditionTypeDbSet.Object);

            _repository = new PromotionRepository(_mockContext.Object);
        }

        [Fact]
        public void GetAll_ReturnsAllPromotionsWithConditions()
        {
            // Arrange
            var promotions = new List<Promotion>
            {
                new Promotion 
                { 
                    PromotionId = 1, 
                    Title = "Test Promotion 1",
                    PromotionConditions = new List<PromotionCondition>
                    {
                        new PromotionCondition { ConditionId = 1, TargetEntity = "Member", TargetField = "Age", Operator = ">", TargetValue = "18" }
                    }
                },
                new Promotion 
                { 
                    PromotionId = 2, 
                    Title = "Test Promotion 2",
                    PromotionConditions = new List<PromotionCondition>()
                }
            };

            var queryablePromotions = promotions.AsQueryable();
            _mockPromotionDbSet.As<IQueryable<Promotion>>().Setup(m => m.Provider).Returns(queryablePromotions.Provider);
            _mockPromotionDbSet.As<IQueryable<Promotion>>().Setup(m => m.Expression).Returns(queryablePromotions.Expression);
            _mockPromotionDbSet.As<IQueryable<Promotion>>().Setup(m => m.ElementType).Returns(queryablePromotions.ElementType);
            _mockPromotionDbSet.As<IQueryable<Promotion>>().Setup(m => m.GetEnumerator()).Returns(queryablePromotions.GetEnumerator());

            // Act
            var result = _repository.GetAll();

            // Assert
            Assert.NotNull(result);
            var resultList = result.ToList();
            Assert.Equal(2, resultList.Count);
            Assert.Equal(1, resultList[0].PromotionId);
            Assert.Equal(2, resultList[1].PromotionId);
            Assert.Single(resultList[0].PromotionConditions);
            Assert.Empty(resultList[1].PromotionConditions);
        }

        [Fact]
        public void GetAll_ReturnsEmptyList_WhenNoPromotionsExist()
        {
            // Arrange
            var promotions = new List<Promotion>();
            var queryablePromotions = promotions.AsQueryable();
            _mockPromotionDbSet.As<IQueryable<Promotion>>().Setup(m => m.Provider).Returns(queryablePromotions.Provider);
            _mockPromotionDbSet.As<IQueryable<Promotion>>().Setup(m => m.Expression).Returns(queryablePromotions.Expression);
            _mockPromotionDbSet.As<IQueryable<Promotion>>().Setup(m => m.ElementType).Returns(queryablePromotions.ElementType);
            _mockPromotionDbSet.As<IQueryable<Promotion>>().Setup(m => m.GetEnumerator()).Returns(queryablePromotions.GetEnumerator());

            // Act
            var result = _repository.GetAll();

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public void GetById_ReturnsPromotionWithConditions_WhenPromotionExists()
        {
            // Arrange
            var promotion = new Promotion
            {
                PromotionId = 1,
                Title = "Test Promotion",
                Detail = "Test Detail",
                DiscountLevel = 10,
                StartTime = DateTime.Now,
                EndTime = DateTime.Now.AddDays(30),
                IsActive = true,
                PromotionConditions = new List<PromotionCondition>
                {
                    new PromotionCondition
                    {
                        ConditionId = 1,
                        TargetEntity = "Member",
                        TargetField = "Age",
                        Operator = ">",
                        TargetValue = "18",
                        ConditionType = new ConditionType { ConditionTypeId = 1, Name = "Age Condition" }
                    }
                }
            };

            var queryablePromotions = new List<Promotion> { promotion }.AsQueryable();
            _mockPromotionDbSet.As<IQueryable<Promotion>>().Setup(m => m.Provider).Returns(queryablePromotions.Provider);
            _mockPromotionDbSet.As<IQueryable<Promotion>>().Setup(m => m.Expression).Returns(queryablePromotions.Expression);
            _mockPromotionDbSet.As<IQueryable<Promotion>>().Setup(m => m.ElementType).Returns(queryablePromotions.ElementType);
            _mockPromotionDbSet.As<IQueryable<Promotion>>().Setup(m => m.GetEnumerator()).Returns(queryablePromotions.GetEnumerator());

            // Act
            var result = _repository.GetById(1);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1, result.PromotionId);
            Assert.Equal("Test Promotion", result.Title);
            Assert.Single(result.PromotionConditions);
            Assert.NotNull(result.PromotionConditions.First().ConditionType);
            Assert.Equal("Age Condition", result.PromotionConditions.First().ConditionType.Name);
        }

        [Fact]
        public void GetById_ReturnsNull_WhenPromotionDoesNotExist()
        {
            // Arrange
            var promotions = new List<Promotion>();
            var queryablePromotions = promotions.AsQueryable();
            _mockPromotionDbSet.As<IQueryable<Promotion>>().Setup(m => m.Provider).Returns(queryablePromotions.Provider);
            _mockPromotionDbSet.As<IQueryable<Promotion>>().Setup(m => m.Expression).Returns(queryablePromotions.Expression);
            _mockPromotionDbSet.As<IQueryable<Promotion>>().Setup(m => m.ElementType).Returns(queryablePromotions.ElementType);
            _mockPromotionDbSet.As<IQueryable<Promotion>>().Setup(m => m.GetEnumerator()).Returns(queryablePromotions.GetEnumerator());

            // Act
            var result = _repository.GetById(999);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void Add_AddsPromotionToContext()
        {
            // Arrange
            var promotion = new Promotion
            {
                Title = "New Promotion",
                Detail = "New Detail",
                DiscountLevel = 15,
                StartTime = DateTime.Now,
                EndTime = DateTime.Now.AddDays(30),
                IsActive = true
            };

            // Act
            _repository.Add(promotion);

            // Assert
            _mockPromotionDbSet.Verify(d => d.Add(promotion), Times.Once);
        }

        [Fact]
        public void Add_AddsPromotionWithConditionsToContext()
        {
            // Arrange
            var promotion = new Promotion
            {
                Title = "New Promotion",
                Detail = "New Detail",
                DiscountLevel = 15,
                StartTime = DateTime.Now,
                EndTime = DateTime.Now.AddDays(30),
                IsActive = true,
                PromotionConditions = new List<PromotionCondition>
                {
                    new PromotionCondition
                    {
                        TargetEntity = "Member",
                        TargetField = "Age",
                        Operator = ">",
                        TargetValue = "18"
                    }
                }
            };

            // Act
            _repository.Add(promotion);

            // Assert
            _mockPromotionDbSet.Verify(d => d.Add(promotion), Times.Once);
        }

        [Fact]
        public void Update_UpdatesExistingPromotionProperties()
        {
            // Arrange
            var existingPromotion = new Promotion
            {
                PromotionId = 1,
                Title = "Old Title",
                Detail = "Old Detail",
                DiscountLevel = 10,
                StartTime = DateTime.Now,
                EndTime = DateTime.Now.AddDays(30),
                IsActive = true,
                PromotionConditions = new List<PromotionCondition>()
            };

            var updatedPromotion = new Promotion
            {
                PromotionId = 1,
                Title = "Updated Title",
                Detail = "Updated Detail",
                DiscountLevel = 20,
                StartTime = DateTime.Now.AddDays(1),
                EndTime = DateTime.Now.AddDays(60),
                IsActive = false,
                PromotionConditions = new List<PromotionCondition>()
            };

            var queryablePromotions = new List<Promotion> { existingPromotion }.AsQueryable();
            _mockPromotionDbSet.As<IQueryable<Promotion>>().Setup(m => m.Provider).Returns(queryablePromotions.Provider);
            _mockPromotionDbSet.As<IQueryable<Promotion>>().Setup(m => m.Expression).Returns(queryablePromotions.Expression);
            _mockPromotionDbSet.As<IQueryable<Promotion>>().Setup(m => m.ElementType).Returns(queryablePromotions.ElementType);
            _mockPromotionDbSet.As<IQueryable<Promotion>>().Setup(m => m.GetEnumerator()).Returns(queryablePromotions.GetEnumerator());

            // Act
            _repository.Update(updatedPromotion);

            // Assert
            Assert.Equal("Updated Title", existingPromotion.Title);
            Assert.Equal("Updated Detail", existingPromotion.Detail);
            Assert.Equal(20, existingPromotion.DiscountLevel);
            Assert.False(existingPromotion.IsActive);
        }

        [Fact]
        public void Update_UpdatesExistingCondition_WhenConditionExists()
        {
            // Arrange
            var existingCondition = new PromotionCondition
            {
                ConditionId = 1,
                TargetEntity = "Member",
                TargetField = "Age",
                Operator = ">",
                TargetValue = "18"
            };

            var existingPromotion = new Promotion
            {
                PromotionId = 1,
                Title = "Test Promotion",
                PromotionConditions = new List<PromotionCondition> { existingCondition }
            };

            var updatedCondition = new PromotionCondition
            {
                ConditionId = 1,
                TargetEntity = "Member",
                TargetField = "Age",
                Operator = ">=",
                TargetValue = "21"
            };

            var updatedPromotion = new Promotion
            {
                PromotionId = 1,
                Title = "Test Promotion",
                PromotionConditions = new List<PromotionCondition> { updatedCondition }
            };

            var queryablePromotions = new List<Promotion> { existingPromotion }.AsQueryable();
            _mockPromotionDbSet.As<IQueryable<Promotion>>().Setup(m => m.Provider).Returns(queryablePromotions.Provider);
            _mockPromotionDbSet.As<IQueryable<Promotion>>().Setup(m => m.Expression).Returns(queryablePromotions.Expression);
            _mockPromotionDbSet.As<IQueryable<Promotion>>().Setup(m => m.ElementType).Returns(queryablePromotions.ElementType);
            _mockPromotionDbSet.As<IQueryable<Promotion>>().Setup(m => m.GetEnumerator()).Returns(queryablePromotions.GetEnumerator());

            // Act
            _repository.Update(updatedPromotion);

            // Assert
            Assert.Equal(">=", existingCondition.Operator);
            Assert.Equal("21", existingCondition.TargetValue);
        }

        [Fact]
        public void Update_AddsNewCondition_WhenConditionDoesNotExist()
        {
            // Arrange
            var existingPromotion = new Promotion
            {
                PromotionId = 1,
                Title = "Test Promotion",
                PromotionConditions = new List<PromotionCondition>()
            };

            var newCondition = new PromotionCondition
            {
                ConditionId = 0, // New condition
                TargetEntity = "Member",
                TargetField = "Age",
                Operator = ">",
                TargetValue = "18"
            };

            var updatedPromotion = new Promotion
            {
                PromotionId = 1,
                Title = "Test Promotion",
                PromotionConditions = new List<PromotionCondition> { newCondition }
            };

            var queryablePromotions = new List<Promotion> { existingPromotion }.AsQueryable();
            _mockPromotionDbSet.As<IQueryable<Promotion>>().Setup(m => m.Provider).Returns(queryablePromotions.Provider);
            _mockPromotionDbSet.As<IQueryable<Promotion>>().Setup(m => m.Expression).Returns(queryablePromotions.Expression);
            _mockPromotionDbSet.As<IQueryable<Promotion>>().Setup(m => m.ElementType).Returns(queryablePromotions.ElementType);
            _mockPromotionDbSet.As<IQueryable<Promotion>>().Setup(m => m.GetEnumerator()).Returns(queryablePromotions.GetEnumerator());

            // Act
            _repository.Update(updatedPromotion);

            // Assert
            Assert.Single(existingPromotion.PromotionConditions);
            Assert.Equal("Member", existingPromotion.PromotionConditions.First().TargetEntity);
            Assert.Equal("Age", existingPromotion.PromotionConditions.First().TargetField);
        }

        [Fact]
        public void Update_HandlesMultipleConditions()
        {
            // Arrange
            var existingCondition1 = new PromotionCondition
            {
                ConditionId = 1,
                TargetEntity = "Member",
                TargetField = "Age",
                Operator = ">",
                TargetValue = "18"
            };

            var existingCondition2 = new PromotionCondition
            {
                ConditionId = 2,
                TargetEntity = "Member",
                TargetField = "Gender",
                Operator = "=",
                TargetValue = "Male"
            };

            var existingPromotion = new Promotion
            {
                PromotionId = 1,
                Title = "Test Promotion",
                PromotionConditions = new List<PromotionCondition> { existingCondition1, existingCondition2 }
            };

            var updatedCondition1 = new PromotionCondition
            {
                ConditionId = 1,
                TargetEntity = "Member",
                TargetField = "Age",
                Operator = ">=",
                TargetValue = "21"
            };

            var newCondition = new PromotionCondition
            {
                ConditionId = 0,
                TargetEntity = "Member",
                TargetField = "City",
                Operator = "=",
                TargetValue = "Hanoi"
            };

            var updatedPromotion = new Promotion
            {
                PromotionId = 1,
                Title = "Test Promotion",
                PromotionConditions = new List<PromotionCondition> { updatedCondition1, newCondition }
            };

            var queryablePromotions = new List<Promotion> { existingPromotion }.AsQueryable();
            _mockPromotionDbSet.As<IQueryable<Promotion>>().Setup(m => m.Provider).Returns(queryablePromotions.Provider);
            _mockPromotionDbSet.As<IQueryable<Promotion>>().Setup(m => m.Expression).Returns(queryablePromotions.Expression);
            _mockPromotionDbSet.As<IQueryable<Promotion>>().Setup(m => m.ElementType).Returns(queryablePromotions.ElementType);
            _mockPromotionDbSet.As<IQueryable<Promotion>>().Setup(m => m.GetEnumerator()).Returns(queryablePromotions.GetEnumerator());

            // Act
            _repository.Update(updatedPromotion);

            // Assert
            Assert.Equal(2, existingPromotion.PromotionConditions.Count);
            Assert.Equal(">=", existingCondition1.Operator);
            Assert.Equal("21", existingCondition1.TargetValue);
            Assert.Contains(existingPromotion.PromotionConditions, c => c.TargetField == "City" && c.TargetValue == "Hanoi");
        }

        [Fact]
        public void Update_DoesNothing_WhenPromotionDoesNotExist()
        {
            // Arrange
            var promotions = new List<Promotion>();
            var queryablePromotions = promotions.AsQueryable();
            _mockPromotionDbSet.As<IQueryable<Promotion>>().Setup(m => m.Provider).Returns(queryablePromotions.Provider);
            _mockPromotionDbSet.As<IQueryable<Promotion>>().Setup(m => m.Expression).Returns(queryablePromotions.Expression);
            _mockPromotionDbSet.As<IQueryable<Promotion>>().Setup(m => m.ElementType).Returns(queryablePromotions.ElementType);
            _mockPromotionDbSet.As<IQueryable<Promotion>>().Setup(m => m.GetEnumerator()).Returns(queryablePromotions.GetEnumerator());

            var updatedPromotion = new Promotion
            {
                PromotionId = 999,
                Title = "Non-existent Promotion",
                PromotionConditions = new List<PromotionCondition>()
            };

            // Act
            _repository.Update(updatedPromotion);

            // Assert
            _mockPromotionDbSet.Verify(d => d.Remove(It.IsAny<Promotion>()), Times.Never);
        }

        [Fact]
        public void Delete_RemovesPromotionFromContext_WhenPromotionExists()
        {
            // Arrange
            var promotion = new Promotion
            {
                PromotionId = 1,
                Title = "Test Promotion",
                PromotionConditions = new List<PromotionCondition>
                {
                    new PromotionCondition
                    {
                        ConditionId = 1,
                        TargetEntity = "Member",
                        TargetField = "Age",
                        Operator = ">",
                        TargetValue = "18",
                        ConditionType = new ConditionType { ConditionTypeId = 1, Name = "Age Condition" }
                    }
                }
            };

            var queryablePromotions = new List<Promotion> { promotion }.AsQueryable();
            _mockPromotionDbSet.As<IQueryable<Promotion>>().Setup(m => m.Provider).Returns(queryablePromotions.Provider);
            _mockPromotionDbSet.As<IQueryable<Promotion>>().Setup(m => m.Expression).Returns(queryablePromotions.Expression);
            _mockPromotionDbSet.As<IQueryable<Promotion>>().Setup(m => m.ElementType).Returns(queryablePromotions.ElementType);
            _mockPromotionDbSet.As<IQueryable<Promotion>>().Setup(m => m.GetEnumerator()).Returns(queryablePromotions.GetEnumerator());

            // Act
            _repository.Delete(1);

            // Assert
            _mockPromotionDbSet.Verify(d => d.Remove(promotion), Times.Once);
            _mockContext.Verify(c => c.SaveChanges(), Times.Once);
        }

        [Fact]
        public void Delete_DoesNothing_WhenPromotionDoesNotExist()
        {
            // Arrange
            var promotions = new List<Promotion>();
            var queryablePromotions = promotions.AsQueryable();
            _mockPromotionDbSet.As<IQueryable<Promotion>>().Setup(m => m.Provider).Returns(queryablePromotions.Provider);
            _mockPromotionDbSet.As<IQueryable<Promotion>>().Setup(m => m.Expression).Returns(queryablePromotions.Expression);
            _mockPromotionDbSet.As<IQueryable<Promotion>>().Setup(m => m.ElementType).Returns(queryablePromotions.ElementType);
            _mockPromotionDbSet.As<IQueryable<Promotion>>().Setup(m => m.GetEnumerator()).Returns(queryablePromotions.GetEnumerator());

            // Act
            _repository.Delete(999);

            // Assert
            _mockPromotionDbSet.Verify(d => d.Remove(It.IsAny<Promotion>()), Times.Never);
            _mockContext.Verify(c => c.SaveChanges(), Times.Once);
        }

        [Fact]
        public void Delete_RemovesPromotionWithMultipleConditions()
        {
            // Arrange
            var promotion = new Promotion
            {
                PromotionId = 1,
                Title = "Test Promotion",
                PromotionConditions = new List<PromotionCondition>
                {
                    new PromotionCondition
                    {
                        ConditionId = 1,
                        TargetEntity = "Member",
                        TargetField = "Age",
                        Operator = ">",
                        TargetValue = "18",
                        ConditionType = new ConditionType { ConditionTypeId = 1, Name = "Age Condition" }
                    },
                    new PromotionCondition
                    {
                        ConditionId = 2,
                        TargetEntity = "Member",
                        TargetField = "Gender",
                        Operator = "=",
                        TargetValue = "Male",
                        ConditionType = new ConditionType { ConditionTypeId = 2, Name = "Gender Condition" }
                    }
                }
            };

            var queryablePromotions = new List<Promotion> { promotion }.AsQueryable();
            _mockPromotionDbSet.As<IQueryable<Promotion>>().Setup(m => m.Provider).Returns(queryablePromotions.Provider);
            _mockPromotionDbSet.As<IQueryable<Promotion>>().Setup(m => m.Expression).Returns(queryablePromotions.Expression);
            _mockPromotionDbSet.As<IQueryable<Promotion>>().Setup(m => m.ElementType).Returns(queryablePromotions.ElementType);
            _mockPromotionDbSet.As<IQueryable<Promotion>>().Setup(m => m.GetEnumerator()).Returns(queryablePromotions.GetEnumerator());

            // Act
            _repository.Delete(1);

            // Assert
            _mockPromotionDbSet.Verify(d => d.Remove(promotion), Times.Once);
            _mockContext.Verify(c => c.SaveChanges(), Times.Once);
        }

        [Fact]
        public void Save_CallsSaveChangesOnContext()
        {
            // Act
            _repository.Save();

            // Assert
            _mockContext.Verify(c => c.SaveChanges(), Times.Once);
        }

        [Fact]
        public void Save_CalledMultipleTimes_CallsSaveChangesMultipleTimes()
        {
            // Act
            _repository.Save();
            _repository.Save();
            _repository.Save();

            // Assert
            _mockContext.Verify(c => c.SaveChanges(), Times.Exactly(3));
        }
    }
} 