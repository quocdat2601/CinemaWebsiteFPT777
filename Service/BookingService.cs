using Microsoft.EntityFrameworkCore;
using MovieTheater.Models;
using MovieTheater.Repository;

namespace MovieTheater.Service
{
    public class BookingService : IBookingService
    {
        private readonly IMovieRepository _repo;
        private readonly MovieTheaterContext _context;

        public BookingService(IMovieRepository repo, MovieTheaterContext context)
        {
            _repo = repo;
            _context = context;
        }

        public IEnumerable<Movie> GetAvailableMovies()
        {
            return _repo.GetAll();
        }

        public Movie GetById(string movieId)
        {
            return _repo.GetById(movieId);
        }

        public List<Schedule> GetSchedulesByIds(List<int> ids)
        {
            return _repo.GetSchedulesByIds(ids);
        }

        public List<ShowDate> GetShowDatesByIds(List<int> ids)
        {
            return _repo.GetShowDatesByIds(ids);
        }

        public async Task<List<DateTime>> GetShowDates(string movieId)
        {
            return await _repo.GetShowDatesAsync(movieId);
        }

        public async Task<List<string>> GetShowTimes(string movieId, DateTime date)
        {
            return await _repo.GetShowTimesAsync(movieId, date);
        }

        public async Task SaveInvoiceAsync(Invoice invoice)
        {
            _context.Invoices.Add(invoice);
            await _context.SaveChangesAsync();
        }

        public async Task<string> GenerateInvoiceIdAsync()
        {
            // Lấy tất cả InvoiceId từ DB bắt đầu bằng "INV"
            var allIds = await _context.Invoices
                .Where(i => i.InvoiceId.StartsWith("INV"))
                .Select(i => i.InvoiceId)
                .ToListAsync();

            int maxNumber = 0;
            foreach (var id in allIds)
            {
                if (int.TryParse(id.Substring(3), out int number))
                {
                    maxNumber = Math.Max(maxNumber, number);
                }
            }

            return $"INV{(maxNumber + 1):D3}";
        }

        public Invoice? GetInvoiceById(string invoiceId)
        {
            return _context.Invoices.FirstOrDefault(i => i.InvoiceId == invoiceId);
        }

    }
}
