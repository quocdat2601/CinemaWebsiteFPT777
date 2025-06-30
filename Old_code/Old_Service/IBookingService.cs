using MovieTheater.Models;
using MovieTheater.ViewModels;

namespace MovieTheater.Service
{
    public interface IBookingService
    {
        Task<List<Movie>> GetAvailableMoviesAsync();
        Movie GetById(string movieId);
        Task<List<DateTime>> GetShowDatesAsync(string movieId);
        Task<List<string>> GetShowTimesAsync(string movieId, DateTime date);
        Task SaveInvoiceAsync(Invoice invoice);
        Task UpdateInvoiceAsync(Invoice invoice);
        Task<string> GenerateInvoiceIdAsync();

        public List<Promotion> GetApplicablePromotions(ConfirmBookingViewModel booking);
    }
}
