using MovieTheater.Models;
using MovieTheater.Repository;
using MovieTheater.ViewModels;

namespace MovieTheater.Service
{
    public class FoodInvoiceService : IFoodInvoiceService
    {
        private readonly IFoodInvoiceRepository _foodInvoiceRepository;

        public FoodInvoiceService(IFoodInvoiceRepository foodInvoiceRepository)
        {
            _foodInvoiceRepository = foodInvoiceRepository;
        }

        public async Task<IEnumerable<FoodViewModel>> GetFoodsByInvoiceIdAsync(string invoiceId)
        {
            var foodInvoices = await _foodInvoiceRepository.GetByInvoiceIdAsync(invoiceId);
            
            return foodInvoices.Select(fi => new FoodViewModel
            {
                FoodId = fi.Food.FoodId,
                Name = fi.Food.Name,
                Price = fi.Price, // Use the price at the time of order
                OriginalPrice = fi.Food.Price, // Giá gốc
                Image = fi.Food.Image,
                Description = fi.Food.Description,
                Category = fi.Food.Category,
                Status = fi.Food.Status,
                Quantity = fi.Quantity,
                CreatedDate = fi.Food.CreatedDate,
                UpdatedDate = fi.Food.UpdatedDate
            });
        }

        public async Task<bool> SaveFoodOrderAsync(string invoiceId, List<FoodViewModel> selectedFoods)
        {
            try
            {
                if (selectedFoods == null || !selectedFoods.Any())
                    return true; // No food to save

                var foodInvoices = selectedFoods.Select(food => new FoodInvoice
                {
                    InvoiceId = invoiceId,
                    FoodId = food.FoodId,
                    Quantity = food.Quantity,
                    Price = food.Price
                });

                await _foodInvoiceRepository.CreateMultipleAsync(foodInvoices);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<decimal> GetTotalFoodPriceByInvoiceIdAsync(string invoiceId)
        {
            var foodInvoices = await _foodInvoiceRepository.GetByInvoiceIdAsync(invoiceId);
            return foodInvoices.Sum(fi => fi.Price * fi.Quantity);
        }
    }
} 