using MovieTheater.Models;
using MovieTheater.Repository;

namespace MovieTheater.Service
{
    public class PromotionService : IPromotionService
    {
        private readonly IPromotionRepository _promotionRepository;

        public PromotionService(IPromotionRepository promotionRepository)
        {
            _promotionRepository = promotionRepository;
        }

        public IEnumerable<Promotion> GetAll()
        {
            return _promotionRepository.GetAll();
        }

        public Promotion? GetById(int id)
        {
            return _promotionRepository.GetById(id);
        }

        public bool Add(Promotion promotion)
        {
            if (promotion == null)
                return false;

            _promotionRepository.Add(promotion);
            return true;
        }

        public bool Update(Promotion promotion)
        {
           
            return true;
        }

        public bool Delete(int id)
        {
            _promotionRepository.Delete(id);
            return true;
        }

        public void Save()
        {
            _promotionRepository.Save();
        }

        public Promotion? GetBestPromotionForShowDate(DateOnly showDate)
        {
            var allPromotions = _promotionRepository.GetAll();
            var validPromotions = allPromotions
                .Where(p => p.IsActive &&
                            DateOnly.FromDateTime((DateTime)p.StartTime) <= showDate &&
                            DateOnly.FromDateTime((DateTime)p.EndTime) >= showDate &&
                            p.DiscountLevel.HasValue)
                .OrderByDescending(p => p.DiscountLevel)
                .ToList();
            return validPromotions.FirstOrDefault();
        }
    }
}
