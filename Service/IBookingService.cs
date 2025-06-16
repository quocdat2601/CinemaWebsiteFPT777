using MovieTheater.Models;

namespace MovieTheater.Service
{
    public interface IBookingService
    {
        public IEnumerable<Movie> GetAvailableMovies();
        Movie GetById(string movieId);
        Task<List<DateTime>> GetShowDates(string movieId);
        Task <List<string>> GetShowTimes(string movieId, DateTime date);
        Task SaveInvoiceAsync(Invoice invoice);
        Task<string> GenerateInvoiceIdAsync();
        Invoice? GetInvoiceById(string invoiceId);
    }
}
