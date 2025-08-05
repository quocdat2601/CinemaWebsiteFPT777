using MovieTheater.Repository;
using MovieTheater.Models;
using Microsoft.EntityFrameworkCore;
using Xunit;
using System.Collections.Generic;
using System.Linq;

namespace MovieTheater.Tests.Repository
{
    public class PromotionRepositoryTests : IDisposable
    {
        private readonly DbContextOptions<MovieTheaterContext> _options;
        private readonly MovieTheaterContext _context;
        private readonly PromotionRepository _repository;

        public PromotionRepositoryTests()
        {
            _options = new DbContextOptionsBuilder<MovieTheaterContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new MovieTheaterContext(_options);
            _repository = new PromotionRepository(_context);
        }

        [Fact]
        public void GetAll_ReturnsAllPromotionsWithConditions()
        {
            // Arrange
            var promotion1 = new Promotion 
            { 
                PromotionId = 1, 
                Title = "Test Promotion 1",
                PromotionConditions = new List<PromotionCondition>
                {
                    new PromotionCondition { ConditionId = 1, TargetEntity = "Member", TargetField = "Age", Operator = ">", TargetValue = "18" }
                }
            };

            var promotion2 = new Promotion 
            { 
                PromotionId = 2, 
                Title = "Test Promotion 2",
                PromotionConditions = new List<PromotionCondition>()
            };

            _context.Promotions.Add(promotion1);
            _context.Promotions.Add(promotion2);
            _context.SaveChanges();

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
            var conditionType = new ConditionType { ConditionTypeId = 1, Name = "Age Condition" };
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
                        ConditionType = conditionType
                    }
                }
            };

            _context.Promotions.Add(promotion);
            _context.SaveChanges();

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
            _repository.Save();

            // Assert
            var savedPromotion = _context.Promotions.FirstOrDefault(p => p.Title == "New Promotion");
            Assert.NotNull(savedPromotion);
            Assert.Equal("New Promotion", savedPromotion.Title);
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
            _repository.Save();

            // Assert
            var savedPromotion = _context.Promotions
                .Include(p => p.PromotionConditions)
                .FirstOrDefault(p => p.Title == "New Promotion");
            Assert.NotNull(savedPromotion);
            Assert.Single(savedPromotion.PromotionConditions);
            Assert.Equal("Member", savedPromotion.PromotionConditions.First().TargetEntity);
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

            _context.Promotions.Add(existingPromotion);
            _context.SaveChanges();

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

            // Act
            _repository.Update(updatedPromotion);
            _repository.Save();

            // Assert
            var result = _context.Promotions.FirstOrDefault(p => p.PromotionId == 1);
            Assert.NotNull(result);
            Assert.Equal("Updated Title", result.Title);
            Assert.Equal("Updated Detail", result.Detail);
            Assert.Equal(20, result.DiscountLevel);
            Assert.False(result.IsActive);
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

            _context.Promotions.Add(existingPromotion);
            _context.SaveChanges();

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

            // Act
            _repository.Update(updatedPromotion);
            _repository.Save();

            // Assert
            var result = _context.Promotions
                .Include(p => p.PromotionConditions)
                .FirstOrDefault(p => p.PromotionId == 1);
            Assert.NotNull(result);
            var condition = result.PromotionConditions.FirstOrDefault(c => c.ConditionId == 1);
            Assert.NotNull(condition);
            Assert.Equal(">=", condition.Operator);
            Assert.Equal("21", condition.TargetValue);
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

            _context.Promotions.Add(existingPromotion);
            _context.SaveChanges();

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

            // Act
            _repository.Update(updatedPromotion);
            _repository.Save();

            // Assert
            var result = _context.Promotions
                .Include(p => p.PromotionConditions)
                .FirstOrDefault(p => p.PromotionId == 1);
            Assert.NotNull(result);
            Assert.Single(result.PromotionConditions);
            Assert.Equal("Member", result.PromotionConditions.First().TargetEntity);
            Assert.Equal("Age", result.PromotionConditions.First().TargetField);
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

            _context.Promotions.Add(existingPromotion);
            _context.SaveChanges();

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

            // Act
            _repository.Update(updatedPromotion);
            _repository.Save();

            // Assert
            var result = _context.Promotions
                .Include(p => p.PromotionConditions)
                .FirstOrDefault(p => p.PromotionId == 1);
            Assert.NotNull(result);
            // Note: The repository doesn't remove old conditions, it only adds/updates
            // So we expect 3 conditions: 2 original + 1 new
            Assert.Equal(3, result.PromotionConditions.Count);
            
            var ageCondition = result.PromotionConditions.FirstOrDefault(c => c.ConditionId == 1);
            Assert.NotNull(ageCondition);
            Assert.Equal(">=", ageCondition.Operator);
            Assert.Equal("21", ageCondition.TargetValue);

            var cityCondition = result.PromotionConditions.FirstOrDefault(c => c.TargetField == "City");
            Assert.NotNull(cityCondition);
            Assert.Equal("Hanoi", cityCondition.TargetValue);
        }

        [Fact]
        public void Update_DoesNothing_WhenPromotionDoesNotExist()
        {
            // Arrange
            var updatedPromotion = new Promotion
            {
                PromotionId = 999,
                Title = "Non-existent Promotion",
                PromotionConditions = new List<PromotionCondition>()
            };

            // Act
            _repository.Update(updatedPromotion);
            _repository.Save();

            // Assert
            var result = _context.Promotions.FirstOrDefault(p => p.PromotionId == 999);
            Assert.Null(result);
        }

        [Fact]
        public void Delete_RemovesPromotionFromContext_WhenPromotionExists()
        {
            // Arrange
            var conditionType = new ConditionType { ConditionTypeId = 1, Name = "Age Condition" };
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
                        ConditionType = conditionType
                    }
                }
            };

            _context.Promotions.Add(promotion);
            _context.SaveChanges();

            // Act
            _repository.Delete(1);

            // Assert
            var result = _context.Promotions.FirstOrDefault(p => p.PromotionId == 1);
            Assert.Null(result);
        }

        [Fact]
        public void Delete_DoesNothing_WhenPromotionDoesNotExist()
        {
            // Act
            _repository.Delete(999);

            // Assert
            // Should not throw any exception
        }

        [Fact]
        public void Delete_RemovesPromotionWithMultipleConditions()
        {
            // Arrange
            var conditionType1 = new ConditionType { ConditionTypeId = 1, Name = "Age Condition" };
            var conditionType2 = new ConditionType { ConditionTypeId = 2, Name = "Gender Condition" };
            
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
                        ConditionType = conditionType1
                    },
                    new PromotionCondition
                    {
                        ConditionId = 2,
                        TargetEntity = "Member",
                        TargetField = "Gender",
                        Operator = "=",
                        TargetValue = "Male",
                        ConditionType = conditionType2
                    }
                }
            };

            _context.Promotions.Add(promotion);
            _context.SaveChanges();

            // Act
            _repository.Delete(1);

            // Assert
            var result = _context.Promotions.FirstOrDefault(p => p.PromotionId == 1);
            Assert.Null(result);
        }

        [Fact]
        public void Save_CallsSaveChangesOnContext()
        {
            // Arrange
            var promotion = new Promotion
            {
                Title = "Test Promotion",
                Detail = "Test Detail",
                DiscountLevel = 10,
                StartTime = DateTime.Now,
                EndTime = DateTime.Now.AddDays(30),
                IsActive = true
            };

            _context.Promotions.Add(promotion);

            // Act
            _repository.Save();

            // Assert
            var result = _context.Promotions.FirstOrDefault(p => p.Title == "Test Promotion");
            Assert.NotNull(result);
        }

        [Fact]
        public void Save_CalledMultipleTimes_CallsSaveChangesMultipleTimes()
        {
            // Arrange
            var promotion1 = new Promotion { PromotionId = 1, Title = "Test 1", Detail = "Detail 1", DiscountLevel = 10, StartTime = DateTime.Now, EndTime = DateTime.Now.AddDays(30), IsActive = true };
            var promotion2 = new Promotion { PromotionId = 2, Title = "Test 2", Detail = "Detail 2", DiscountLevel = 20, StartTime = DateTime.Now, EndTime = DateTime.Now.AddDays(30), IsActive = true };
            var promotion3 = new Promotion { PromotionId = 3, Title = "Test 3", Detail = "Detail 3", DiscountLevel = 30, StartTime = DateTime.Now, EndTime = DateTime.Now.AddDays(30), IsActive = true };

            _context.Promotions.Add(promotion1);
            _repository.Save();

            _context.Promotions.Add(promotion2);
            _repository.Save();

            _context.Promotions.Add(promotion3);
            _repository.Save();

            // Assert
            var results = _context.Promotions.ToList();
            Assert.Equal(3, results.Count);
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }
    }
} 