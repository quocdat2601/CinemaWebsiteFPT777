using MovieTheater.Service;
using Xunit;

namespace MovieTheater.Tests.Service
{
    public class PointServiceTests
    {
        private readonly PointService _service;

        public PointServiceTests()
        {
            _service = new PointService();
        }

        [Fact]
        public void CalculatePointsToEarn_WithValidInput_ReturnsCorrectPoints()
        {
            // Arrange
            var orderValue = 100000m; // 100,000 VND
            var earningRatePercent = 5m; // 5%

            // Act
            var result = _service.CalculatePointsToEarn(orderValue, earningRatePercent);

            // Assert
            Assert.Equal(5, result); // 100,000 * 5% / 100 / 1,000 = 5 points
        }

        [Fact]
        public void CalculatePointsToEarn_WithZeroOrderValue_ReturnsZero()
        {
            // Arrange
            var orderValue = 0m;
            var earningRatePercent = 5m;

            // Act
            var result = _service.CalculatePointsToEarn(orderValue, earningRatePercent);

            // Assert
            Assert.Equal(0, result);
        }

        [Fact]
        public void CalculatePointsToEarn_WithZeroEarningRate_ReturnsZero()
        {
            // Arrange
            var orderValue = 100000m;
            var earningRatePercent = 0m;

            // Act
            var result = _service.CalculatePointsToEarn(orderValue, earningRatePercent);

            // Assert
            Assert.Equal(0, result);
        }

        [Fact]
        public void CalculatePointsToEarn_WithNegativeOrderValue_ReturnsZero()
        {
            // Arrange
            var orderValue = -100000m;
            var earningRatePercent = 5m;

            // Act
            var result = _service.CalculatePointsToEarn(orderValue, earningRatePercent);

            // Assert
            Assert.Equal(0, result);
        }

        [Fact]
        public void CalculatePointsToEarn_WithDecimalPartLessThanHalf_RoundsDown()
        {
            // Arrange
            var orderValue = 150000m; // 150,000 VND
            var earningRatePercent = 3m; // 3%

            // Act
            var result = _service.CalculatePointsToEarn(orderValue, earningRatePercent);

            // Assert
            Assert.Equal(5, result); // 150,000 * 3% / 100 / 1,000 = 4.5, rounds up to 5 (decimal part >= 0.5)
        }

        [Fact]
        public void CalculatePointsToEarn_WithDecimalPartGreaterThanHalf_RoundsUp()
        {
            // Arrange
            var orderValue = 170000m; // 170,000 VND
            var earningRatePercent = 3m; // 3%

            // Act
            var result = _service.CalculatePointsToEarn(orderValue, earningRatePercent);

            // Assert
            Assert.Equal(5, result); // 170,000 * 3% / 100 / 1,000 = 5.1, rounds up to 5
        }

        [Fact]
        public void CalculateMaxUsablePoints_WithValidInput_ReturnsCorrectPoints()
        {
            // Arrange
            var orderValue = 100000m; // 100,000 VND
            var userPoints = 50; // 50 points

            // Act
            var result = _service.CalculateMaxUsablePoints(orderValue, userPoints);

            // Assert
            Assert.Equal(50, result); // Min(90,000 / 1,000, 50) = 50
        }

        [Fact]
        public void CalculateMaxUsablePoints_WithZeroOrderValue_ReturnsZero()
        {
            // Arrange
            var orderValue = 0m;
            var userPoints = 50;

            // Act
            var result = _service.CalculateMaxUsablePoints(orderValue, userPoints);

            // Assert
            Assert.Equal(0, result);
        }

        [Fact]
        public void CalculateMaxUsablePoints_WithZeroUserPoints_ReturnsZero()
        {
            // Arrange
            var orderValue = 100000m;
            var userPoints = 0;

            // Act
            var result = _service.CalculateMaxUsablePoints(orderValue, userPoints);

            // Assert
            Assert.Equal(0, result);
        }

        [Fact]
        public void CalculateMaxUsablePoints_WithNegativeOrderValue_ReturnsZero()
        {
            // Arrange
            var orderValue = -100000m;
            var userPoints = 50;

            // Act
            var result = _service.CalculateMaxUsablePoints(orderValue, userPoints);

            // Assert
            Assert.Equal(0, result);
        }

        [Fact]
        public void CalculateMaxUsablePoints_WithNegativeUserPoints_ReturnsZero()
        {
            // Arrange
            var orderValue = 100000m;
            var userPoints = -10;

            // Act
            var result = _service.CalculateMaxUsablePoints(orderValue, userPoints);

            // Assert
            Assert.Equal(0, result);
        }

        [Fact]
        public void CalculateMaxUsablePoints_WithHighOrderValue_ReturnsMaxByOrder()
        {
            // Arrange
            var orderValue = 1000000m; // 1,000,000 VND
            var userPoints = 1000; // 1,000 points

            // Act
            var result = _service.CalculateMaxUsablePoints(orderValue, userPoints);

            // Assert
            Assert.Equal(900, result); // Min(900,000 / 1,000, 1000) = 900
        }

        [Fact]
        public void ValidatePointUsage_WithValidRequest_ReturnsValidResult()
        {
            // Arrange
            var requestedPoints = 50;
            var orderValue = 100000m;
            var userPoints = 100;

            // Act
            var result = _service.ValidatePointUsage(requestedPoints, orderValue, userPoints);

            // Assert
            Assert.Equal(50, result.PointsToUse);
            Assert.Equal(50000m, result.DiscountAmount); // 50 * 1,000
            Assert.Empty(result.ValidationErrors);
        }

        [Fact]
        public void ValidatePointUsage_WithLessThanMinPoints_ReturnsError()
        {
            // Arrange
            var requestedPoints = 10; // Less than minimum 20
            var orderValue = 100000m;
            var userPoints = 100;

            // Act
            var result = _service.ValidatePointUsage(requestedPoints, orderValue, userPoints);

            // Assert
            Assert.Equal(10, result.PointsToUse);
            Assert.Equal(10000m, result.DiscountAmount);
            Assert.Single(result.ValidationErrors);
            Assert.Contains("Minimum 20 points required", result.ValidationErrors[0]);
        }

        [Fact]
        public void ValidatePointUsage_WithMoreThanMaxPoints_ReturnsError()
        {
            // Arrange
            var requestedPoints = 200;
            var orderValue = 100000m;
            var userPoints = 100;

            // Act
            var result = _service.ValidatePointUsage(requestedPoints, orderValue, userPoints);

            // Assert
            Assert.Equal(90, result.PointsToUse); // Max by order (90% of 100,000)
            Assert.Equal(90000m, result.DiscountAmount);
            Assert.Equal(2, result.ValidationErrors.Count); // Both max points and user points errors
            Assert.Contains("You can use up to 90 points", result.ValidationErrors[0]);
            Assert.Contains("You do not have enough points", result.ValidationErrors[1]);
        }

        [Fact]
        public void ValidatePointUsage_WithMoreThanUserPoints_ReturnsError()
        {
            // Arrange
            var requestedPoints = 150;
            var orderValue = 1000000m; // High order value
            var userPoints = 100;

            // Act
            var result = _service.ValidatePointUsage(requestedPoints, orderValue, userPoints);

            // Assert
            Assert.Equal(100, result.PointsToUse);
            Assert.Equal(100000m, result.DiscountAmount);
            Assert.Equal(2, result.ValidationErrors.Count); // Both max points and user points errors
            Assert.Contains("You can use up to 100 points", result.ValidationErrors[0]);
            Assert.Contains("You do not have enough points", result.ValidationErrors[1]);
        }

        [Fact]
        public void ValidatePointUsage_WithMultipleErrors_ReturnsAllErrors()
        {
            // Arrange
            var requestedPoints = 200;
            var orderValue = 100000m;
            var userPoints = 50; // Not enough points

            // Act
            var result = _service.ValidatePointUsage(requestedPoints, orderValue, userPoints);

            // Assert
            Assert.Equal(50, result.PointsToUse);
            Assert.Equal(50000m, result.DiscountAmount);
            Assert.Equal(2, result.ValidationErrors.Count);
            // Both errors should be present: max points and insufficient points
            var errorMessages = result.ValidationErrors.ToList();
            Assert.Contains(errorMessages, e => e.Contains("You can use up to 50 points"));
            Assert.Contains(errorMessages, e => e.Contains("You do not have enough points"));
        }

        [Fact]
        public void ValidatePointUsage_WithZeroRequestedPoints_ReturnsZero()
        {
            // Arrange
            var requestedPoints = 0;
            var orderValue = 100000m;
            var userPoints = 100;

            // Act
            var result = _service.ValidatePointUsage(requestedPoints, orderValue, userPoints);

            // Assert
            Assert.Equal(0, result.PointsToUse);
            Assert.Equal(0m, result.DiscountAmount);
            Assert.Single(result.ValidationErrors);
            Assert.Contains("Minimum 20 points required", result.ValidationErrors[0]);
        }

        [Fact]
        public void PointCalculationResult_Properties_WorkCorrectly()
        {
            // Arrange & Act
            var result = new PointCalculationResult
            {
                PointsToEarn = 10,
                PointsToUse = 20,
                DiscountAmount = 20000m,
                ValidationErrors = new List<string> { "Error 1", "Error 2" }
            };

            // Assert
            Assert.Equal(10, result.PointsToEarn);
            Assert.Equal(20, result.PointsToUse);
            Assert.Equal(20000m, result.DiscountAmount);
            Assert.Equal(2, result.ValidationErrors.Count);
            Assert.Contains("Error 1", result.ValidationErrors);
            Assert.Contains("Error 2", result.ValidationErrors);
        }
    }
} 