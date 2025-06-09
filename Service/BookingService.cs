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

        public Task<List<Movie>> GetAvailableMoviesAsync()
        {
            return _repo.GetAllMoviesAsync();
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

        public Task<List<DateTime>> GetShowDatesAsync(string movieId)
        {
            return _repo.GetShowDatesAsync(movieId);
        }

        public Task<List<string>> GetShowTimesAsync(string movieId, DateTime date)
        {
            return _repo.GetShowTimesAsync(movieId, date);
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
                var numberPart = id.Substring(3); // Bỏ "INV"
                if (int.TryParse(numberPart, out int num))
                {
                    if (num > maxNumber)
                        maxNumber = num;
                }
            }

            int nextNumber = maxNumber + 1;

            // Trả về "INV" + số với padding 3 chữ số
            return "INV" + nextNumber.ToString("D3");
        }

    }
}
