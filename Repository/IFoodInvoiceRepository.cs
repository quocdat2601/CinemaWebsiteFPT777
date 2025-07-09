using MovieTheater.Models;

namespace MovieTheater.Repository
{
    public interface IFoodInvoiceRepository
    {
        Task<IEnumerable<FoodInvoice>> GetByInvoiceIdAsync(string invoiceId);
        Task<FoodInvoice> CreateAsync(FoodInvoice foodInvoice);
        Task<IEnumerable<FoodInvoice>> CreateMultipleAsync(IEnumerable<FoodInvoice> foodInvoices);
        Task<bool> DeleteByInvoiceIdAsync(string invoiceId);
    }
} 