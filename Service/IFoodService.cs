using MovieTheater.ViewModels;
using MovieTheater.Models;

namespace MovieTheater.Service
{
    public interface IFoodService
    {
        Task<FoodListViewModel> GetAllAsync(string? searchKeyword = null, string? categoryFilter = null, bool? statusFilter = null);
        Task<FoodViewModel?> GetByIdAsync(int id);
        Task<bool> CreateAsync(FoodViewModel model, string webRootPath);
        Task<bool> UpdateAsync(FoodViewModel model, string webRootPath);
        Task<bool> DeleteAsync(int id);
        Task<bool> ToggleStatusAsync(int id);
        Task<List<string>> GetCategoriesAsync();
        Task<Food?> GetDomainByIdAsync(int id);
    }
}