using MovieTheater.Models;
using MovieTheater.Repository;
using MovieTheater.ViewModels;

namespace MovieTheater.Service
{
    public class FoodService : IFoodService
    {
        private readonly IFoodRepository _foodRepository;

        public FoodService(IFoodRepository foodRepository)
        {
            _foodRepository = foodRepository;
        }

        public async Task<FoodListViewModel> GetAllAsync(string? searchKeyword = null, string? categoryFilter = null, bool? statusFilter = null)
        {
            IEnumerable<Food> foods;

            if (!string.IsNullOrEmpty(searchKeyword))
            {
                foods = await _foodRepository.SearchAsync(searchKeyword);
            }
            else if (!string.IsNullOrEmpty(categoryFilter))
            {
                foods = await _foodRepository.GetByCategoryAsync(categoryFilter);
            }
            else if (statusFilter.HasValue)
            {
                foods = await _foodRepository.GetByStatusAsync(statusFilter.Value);
            }
            else
            {
                foods = await _foodRepository.GetAllAsync();
            }

            var foodViewModels = foods.Select(f => new FoodViewModel
            {
                FoodId = f.FoodId,
                Category = f.Category,
                Name = f.Name,
                Price = f.Price,
                Description = f.Description,
                Image = f.Image,
                Status = f.Status,
                CreatedDate = f.CreatedDate,
                UpdatedDate = f.UpdatedDate
            }).ToList();

            return new FoodListViewModel
            {
                Foods = foodViewModels,
                SearchKeyword = searchKeyword,
                CategoryFilter = categoryFilter,
                StatusFilter = statusFilter
            };
        }

        public async Task<FoodViewModel?> GetByIdAsync(int id)
        {
            var food = await _foodRepository.GetByIdAsync(id);
            if (food == null) return null;

            return new FoodViewModel
            {
                FoodId = food.FoodId,
                Category = food.Category,
                Name = food.Name,
                Price = food.Price,
                Description = food.Description,
                Image = food.Image,
                Status = food.Status,
                CreatedDate = food.CreatedDate,
                UpdatedDate = food.UpdatedDate
            };
        }

        public async Task<bool> CreateAsync(FoodViewModel model, string webRootPath)
        {
            try
            {
                // Làm sạch dữ liệu đầu vào
                var safeCategory = model.Category?.Trim();
                var safeName = model.Name?.Trim();
                var safeDescription = model.Description?.Trim();

                // Nếu muốn chống XSS mạnh hơn, có thể dùng AntiXSS hoặc HtmlEncoder
                // safeDescription = Microsoft.Security.Application.Sanitizer.GetSafeHtmlFragment(safeDescription);
                // hoặc dùng System.Text.Encodings.Web.HtmlEncoder.Default.Encode(safeDescription)

                var food = new Food
                {
                    Category = safeCategory,
                    Name = safeName,
                    Price = model.Price,
                    Description = safeDescription,
                    Status = model.Status
                };

                // Handle image upload
                if (model.ImageFile != null && model.ImageFile.Length > 0)
                {
                    var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
                    var uploadsFolder = Path.Combine(webRootPath, "images", "foods");
                    if (!Directory.Exists(uploadsFolder))
                    {
                        Directory.CreateDirectory(uploadsFolder);
                    }

                    var originalFileName = Path.GetFileName(model.ImageFile.FileName);
                    if (originalFileName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
                    {
                        throw new ArgumentException("Tên file không hợp lệ.");
                    }

                    var fileExtension = Path.GetExtension(originalFileName).ToLowerInvariant();
                    if (!allowedExtensions.Contains(fileExtension) || model.ImageFile.Length > 2 * 1024 * 1024)
                    {
                        throw new ArgumentException("Chỉ cho phép file ảnh JPG, PNG, GIF nhỏ hơn 2MB.");
                    }

                    var uniqueFileName = Guid.NewGuid().ToString() + "_" + originalFileName;
                    var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await model.ImageFile.CopyToAsync(fileStream);
                    }

                    food.Image = "/images/foods/" + uniqueFileName;
                }

                await _foodRepository.CreateAsync(food);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> UpdateAsync(FoodViewModel model, string webRootPath)
        {
            try
            {
                var existingFood = await _foodRepository.GetByIdAsync(model.FoodId);
                if (existingFood == null) return false;

                existingFood.Category = model.Category;
                existingFood.Name = model.Name;
                existingFood.Price = model.Price;
                existingFood.Description = model.Description;
                existingFood.Status = model.Status;

                // Handle image upload
                if (model.ImageFile != null && model.ImageFile.Length > 0)
                {
                    var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
                    var uploadsFolder = Path.Combine(webRootPath, "images", "foods");
                    if (!Directory.Exists(uploadsFolder))
                    {
                        Directory.CreateDirectory(uploadsFolder);
                    }

                    var originalFileName = Path.GetFileName(model.ImageFile.FileName);
                    if (originalFileName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
                    {
                        throw new ArgumentException("Tên file không hợp lệ.");
                    }

                    var fileExtension = Path.GetExtension(originalFileName).ToLowerInvariant();
                    if (!allowedExtensions.Contains(fileExtension) || model.ImageFile.Length > 2 * 1024 * 1024)
                    {
                        throw new ArgumentException("Chỉ cho phép file ảnh JPG, PNG, GIF nhỏ hơn 2MB.");
                    }

                    var uniqueFileName = Guid.NewGuid().ToString() + "_" + originalFileName;
                    var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await model.ImageFile.CopyToAsync(fileStream);
                    }

                    // Delete old image if exists
                    if (!string.IsNullOrEmpty(existingFood.Image))
                    {
                        var oldImagePath = Path.Combine(webRootPath, existingFood.Image.TrimStart('/'));
                        if (File.Exists(oldImagePath))
                        {
                            File.Delete(oldImagePath);
                        }
                    }

                    existingFood.Image = "/images/foods/" + uniqueFileName;
                }

                await _foodRepository.UpdateAsync(existingFood);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> DeleteAsync(int id)
        {
            try
            {
                await _foodRepository.DeleteAsync(id);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> ToggleStatusAsync(int id)
        {
            try
            {
                var food = await _foodRepository.GetByIdAsync(id);
                if (food == null) return false;

                food.Status = !food.Status;
                await _foodRepository.UpdateAsync(food);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<List<string>> GetCategoriesAsync()
        {
            var foods = await _foodRepository.GetAllAsync();
            return foods.Select(f => f.Category).Distinct().ToList();
        }
    }
}