using MovieTheater.Models;
using MovieTheater.ViewModels;

public interface ITicketService
{
    Task<IEnumerable<object>> GetUserTicketsAsync(string accountId, int? status = null);
    Task<Invoice> GetTicketDetailsAsync(string ticketId, string accountId);
    List<SeatDetailViewModel> BuildSeatDetails(Invoice booking);
    Task<(bool Success, List<string> Messages)> CancelTicketAsync(string ticketId, string accountId);
    Task<IEnumerable<object>> GetHistoryPartialAsync(string accountId, DateTime? fromDate, DateTime? toDate, string status);
    Task<(bool Success, List<string> Messages)> CancelTicketByAdminAsync(string ticketId);
}