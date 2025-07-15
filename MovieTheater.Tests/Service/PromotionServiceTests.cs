using System;
using System.Collections.Generic;
using System.Linq;
using Moq;
using MovieTheater.Models;
using MovieTheater.Repository;
using MovieTheater.Service;
using Xunit;

namespace MovieTheater.Tests.Service
{
    public class PromotionServiceTests
    {
        private readonly Mock<IPromotionRepository> _mockRepo;
        private readonly PromotionService _service;

        public PromotionServiceTests()
        {
            _mockRepo = new Mock<IPromotionRepository>();
            _service = new PromotionService(_mockRepo.Object);
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
        public void Delete_CallsRepositoryDelete()
        {
            // Act
            var result = _service.Delete(1);

            // Assert
            _mockRepo.Verify(r => r.Delete(1), Times.Once);
            Assert.True(result);
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
    }
} 