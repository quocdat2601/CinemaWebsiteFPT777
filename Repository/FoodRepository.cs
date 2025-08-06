using Microsoft.EntityFrameworkCore;
using MovieTheater.Models;

namespace MovieTheater.Repository
{
    public class FoodRepository : IFoodRepository
    {
        private readonly MovieTheaterContext _context;

        public FoodRepository(MovieTheaterContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Food>> GetAllAsync()
        {
            return await _context.Foods.OrderByDescending(f => f.CreatedDate).ToListAsync();
        }

        public async Task<IEnumerable<Food>> GetByCategoryAsync(string category)
        {
            return await _context.Foods
                .Where(f => f.Category.ToLower() == category.ToLower())
                .OrderByDescending(f => f.CreatedDate)
                .ToListAsync();
        }

        public async Task<IEnumerable<Food>> GetByStatusAsync(bool status)
        {
            return await _context.Foods
                .Where(f => f.Status == status)
                .OrderByDescending(f => f.CreatedDate)
                .ToListAsync();
        }

        public async Task<IEnumerable<Food>> SearchAsync(string keyword)
        {
            return await _context.Foods
                .Where(f => f.Name.Contains(keyword) || f.Description.Contains(keyword))
                .OrderByDescending(f => f.CreatedDate)
                .ToListAsync();
        }

        public async Task<Food?> GetByIdAsync(int id)
        {
            return await _context.Foods.FindAsync(id);
        }

        public async Task<Food> CreateAsync(Food food)
        {
            food.CreatedDate = DateTime.Now;
            _context.Foods.Add(food);
            await _context.SaveChangesAsync();
            return food;
        }

        public async Task<Food> UpdateAsync(Food food)
        {
            food.UpdatedDate = DateTime.Now;
            _context.Foods.Update(food);
            await _context.SaveChangesAsync();
            return food;
        }

        public async Task DeleteAsync(int id)
        {
            var food = await _context.Foods.FindAsync(id);
            if (food != null)
            {
                _context.Foods.Remove(food);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<bool> ExistsAsync(int id)
        {
            return await _context.Foods.AnyAsync(f => f.FoodId == id);
        }

        public async Task<bool> HasRelatedInvoicesAsync(int id)
        {
            return await _context.FoodInvoices.AnyAsync(fi => fi.FoodId == id);
        }
    }
}