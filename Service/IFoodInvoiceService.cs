using MovieTheater.ViewModels;

namespace MovieTheater.Service
{
    public interface IFoodInvoiceService
    {
        Task<IEnumerable<FoodViewModel>> GetFoodsByInvoiceIdAsync(string invoiceId);
        Task<bool> SaveFoodOrderAsync(string invoiceId, List<FoodViewModel> selectedFoods);
        Task<decimal> GetTotalFoodPriceByInvoiceIdAsync(string invoiceId);
    }
}