using MovieTheater.Models;

namespace MovieTheater.Service
{
    public interface IPromotionService
    {
        public IEnumerable<Promotion> GetAll();
        public Promotion? GetById(int id);
        public bool Add(Promotion promotion);
        public bool Update(Promotion promotion);
        public bool Delete(int id);
        public void Save();
        Promotion? GetBestEligiblePromotionForBooking(PromotionCheckContext context);
        List<(int FoodId, decimal OriginalPrice, decimal DiscountedPrice, string PromotionName, decimal DiscountLevel)> ApplyFoodPromotionsToFoods(List<(int FoodId, int Quantity, decimal Price, string FoodName)> selectedFoods, List<Promotion> eligiblePromotions);
        List<Promotion> GetEligibleFoodPromotions(List<(int FoodId, int Quantity, decimal Price, string FoodName)> selectedFoods);
        bool IsPromotionEligible(Promotion promotion, PromotionCheckContext context);
        List<Promotion> GetEligiblePromotionsForMember(PromotionCheckContext context);
    }
}
