using System;
using System.Collections.Generic;

namespace MovieTheater.Service
{
    public class PointService : IPointService
    {
        private const int PointValue = 1000; // 1 point = 1,000 VND
        private const int MinPoints = 20;
        private const decimal MaxPercent = 0.9m; // 90%

        public int CalculatePointsToEarn(decimal orderValue, decimal earningRatePercent)
        {
            if (orderValue <= 0 || earningRatePercent <= 0) return 0;
            decimal rawPoints = (orderValue * earningRatePercent) / 100m / PointValue;
            decimal decimalPart = rawPoints - Math.Floor(rawPoints);
            int points;
            if (decimalPart < 0.5m)
                points = (int)Math.Floor(rawPoints);
            else
                points = (int)Math.Ceiling(rawPoints);
            return points;
        }

        public int CalculateMaxUsablePoints(decimal orderValue, int userPoints)
        {
            if (orderValue <= 0 || userPoints <= 0) return 0;
            int maxByOrder = (int)Math.Floor((orderValue * MaxPercent) / PointValue);
            return Math.Min(maxByOrder, userPoints);
        }

        public PointCalculationResult ValidatePointUsage(int requestedPoints, decimal orderValue, int userPoints)
        {
            var result = new PointCalculationResult();
            int maxPoints = CalculateMaxUsablePoints(orderValue, userPoints);
            result.PointsToUse = Math.Min(requestedPoints, maxPoints);
            result.DiscountAmount = result.PointsToUse * PointValue;
            if (requestedPoints < MinPoints)
                result.ValidationErrors.Add($"Minimum {MinPoints} points required.");
            if (requestedPoints > maxPoints)
                result.ValidationErrors.Add($"You can use up to {maxPoints} points (90% of order, or your balance).");
            if (requestedPoints > userPoints)
                result.ValidationErrors.Add("You do not have enough points.");
            return result;
        }
    }
} 