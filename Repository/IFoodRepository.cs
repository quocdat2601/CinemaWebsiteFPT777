using MovieTheater.Models;

namespace MovieTheater.Repository
{
    public interface IFoodRepository
    {
        Task<IEnumerable<Food>> GetAllAsync();
        Task<IEnumerable<Food>> GetByCategoryAsync(string category);
        Task<IEnumerable<Food>> GetByStatusAsync(bool status);
        Task<IEnumerable<Food>> SearchAsync(string keyword);
        Task<Food?> GetByIdAsync(int id);
        Task<Food> CreateAsync(Food food);
        Task<Food> UpdateAsync(Food food);
        Task DeleteAsync(int id);
        Task<bool> ExistsAsync(int id);
    }
} 