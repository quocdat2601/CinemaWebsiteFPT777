using Microsoft.EntityFrameworkCore;
using MovieTheater.Models;

namespace MovieTheater.Repository
{
    public class PromotionRepository : IPromotionRepository
    {
        private readonly MovieTheaterContext _context;
        public PromotionRepository(MovieTheaterContext context)
        {
            _context = context;
        }

        public IEnumerable<Promotion> GetAll()
        {
            return _context.Promotions.Include(p => p.PromotionConditions)
                .ToList();
        }

        public Promotion? GetById(int id)
        {
            return _context.Promotions
                .Include(p => p.PromotionConditions)
                    .ThenInclude(pc => pc.ConditionType)
                .FirstOrDefault(p => p.PromotionId == id);
        }

        public void Add(Promotion promotion)
        {
            _context.Promotions.Add(promotion);
        }

        public void Update(Promotion promotion)
        {
            var existingPromotion = _context.Promotions
                .Include(p => p.PromotionConditions)
                .FirstOrDefault(p => p.PromotionId == promotion.PromotionId);

            if (existingPromotion != null)
            {
                // Update basic properties
                existingPromotion.Title = promotion.Title;
                existingPromotion.Detail = promotion.Detail;
                existingPromotion.DiscountLevel = promotion.DiscountLevel;
                existingPromotion.StartTime = promotion.StartTime;
                existingPromotion.EndTime = promotion.EndTime;
                existingPromotion.Image = promotion.Image;
                existingPromotion.IsActive = promotion.IsActive;

                // Update promotion conditions
                foreach (var condition in promotion.PromotionConditions)
                {
                    var existingCondition = existingPromotion.PromotionConditions
                        .FirstOrDefault(c => c.ConditionId == condition.ConditionId);

                    if (existingCondition != null)
                    {
                        // Update existing condition
                        existingCondition.TargetEntity = condition.TargetEntity;
                        existingCondition.TargetField = condition.TargetField;
                        existingCondition.Operator = condition.Operator;
                        existingCondition.TargetValue = condition.TargetValue;
                    }
                    else
                    {
                        // Add new condition
                        existingPromotion.PromotionConditions.Add(condition);
                    }
                }
            }
        }

        public void Delete(int id)
        {
            var promotion = _context.Promotions
                .Include(p => p.PromotionConditions)
                    .ThenInclude(pc => pc.ConditionType)
                .FirstOrDefault(p => p.PromotionId == id);

            if (promotion != null)
            {
                _context.Promotions.Remove(promotion);
                _context.SaveChanges();
            }
        }

        public void Save()
        {
            _context.SaveChanges();
        }

    }
}
