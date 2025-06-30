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
                .FirstOrDefault(m => m.PromotionId == promotion.PromotionId);
            if (existingPromotion != null)
            {
                existingPromotion.Title = promotion.Title;
                existingPromotion.Detail = promotion.Detail;
                existingPromotion.DiscountLevel = promotion.DiscountLevel;
                existingPromotion.StartTime = promotion.StartTime;
                existingPromotion.EndTime = promotion.EndTime;
                existingPromotion.Image = promotion.Image;
                existingPromotion.IsActive = promotion.IsActive;

                // Update or add PromotionCondition
                var newCond = promotion.PromotionConditions.FirstOrDefault();
                var existingCond = existingPromotion.PromotionConditions.FirstOrDefault();
                if (existingCond != null && newCond != null)
                {
                    existingCond.ConditionTypeId = newCond.ConditionTypeId;
                    existingCond.TargetEntity = newCond.TargetEntity;
                    existingCond.TargetField = newCond.TargetField;
                    existingCond.Operator = newCond.Operator;
                    existingCond.TargetValue = newCond.TargetValue;
                }
                else if (newCond != null && existingCond == null)
                {
                    existingPromotion.PromotionConditions.Add(new PromotionCondition
                    {
                        ConditionTypeId = newCond.ConditionTypeId,
                        TargetEntity = newCond.TargetEntity,
                        TargetField = newCond.TargetField,
                        Operator = newCond.Operator,
                        TargetValue = newCond.TargetValue,
                        PromotionId = existingPromotion.PromotionId
                    });
                }
            }
        }

        public void Delete(int id)
        {
            var promotion = _context.Promotions
                .Include(p => p.PromotionConditions)
                .FirstOrDefault(p => p.PromotionId == id);

            if (promotion != null)
            {
                // Remove all related PromotionCondition records
                var conditions = _context.PromotionConditions.Where(pc => pc.PromotionId == id).ToList();
                _context.PromotionConditions.RemoveRange(conditions);

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
