namespace MovieTheater.Service
{
    public class PointCalculationResult
    {
        public int PointsToEarn { get; set; }
        public int PointsToUse { get; set; }
        public decimal DiscountAmount { get; set; }
        public List<string> ValidationErrors { get; set; } = new();
    }

    public interface IPointService
    {
        int CalculatePointsToEarn(decimal orderValue, decimal earningRatePercent);
        int CalculateMaxUsablePoints(decimal orderValue, int userPoints);
        PointCalculationResult ValidatePointUsage(int requestedPoints, decimal orderValue, int userPoints);
    }
}