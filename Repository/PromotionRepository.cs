using Microsoft.EntityFrameworkCore;
using MovieTheater.Models;
using System.Collections.Generic;
using System.Linq;

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
            var existingMovie = _context.Promotions.FirstOrDefault(m => m.PromotionId == promotion.PromotionId);
            if (existingMovie != null)
            {
                
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
