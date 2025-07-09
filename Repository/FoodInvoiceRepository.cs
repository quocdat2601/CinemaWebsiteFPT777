using Microsoft.EntityFrameworkCore;
using MovieTheater.Models;

namespace MovieTheater.Repository
{
    public class FoodInvoiceRepository : IFoodInvoiceRepository
    {
        private readonly MovieTheaterContext _context;

        public FoodInvoiceRepository(MovieTheaterContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<FoodInvoice>> GetByInvoiceIdAsync(string invoiceId)
        {
            return await _context.FoodInvoices
                .Include(fi => fi.Food)
                .Where(fi => fi.InvoiceId == invoiceId)
                .ToListAsync();
        }

        public async Task<FoodInvoice> CreateAsync(FoodInvoice foodInvoice)
        {
            _context.FoodInvoices.Add(foodInvoice);
            await _context.SaveChangesAsync();
            return foodInvoice;
        }

        public async Task<IEnumerable<FoodInvoice>> CreateMultipleAsync(IEnumerable<FoodInvoice> foodInvoices)
        {
            _context.FoodInvoices.AddRange(foodInvoices);
            await _context.SaveChangesAsync();
            return foodInvoices;
        }

        public async Task<bool> DeleteByInvoiceIdAsync(string invoiceId)
        {
            var foodInvoices = await _context.FoodInvoices
                .Where(fi => fi.InvoiceId == invoiceId)
                .ToListAsync();

            if (foodInvoices.Any())
            {
                _context.FoodInvoices.RemoveRange(foodInvoices);
                await _context.SaveChangesAsync();
                return true;
            }
            return false;
        }
    }
} 