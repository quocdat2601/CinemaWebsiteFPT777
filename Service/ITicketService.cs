using MovieTheater.ViewModels;

public interface ITicketService
{
    Task<IEnumerable<object>> GetUserTicketsAsync(string accountId, int? status = null);
    Task<TicketDetailsViewModel> GetTicketDetailsAsync(string ticketId, string accountId);
    Task<(bool Success, List<string> Messages)> CancelTicketAsync(string ticketId, string accountId);
    Task<IEnumerable<object>> GetHistoryPartialAsync(string accountId, DateTime? fromDate, DateTime? toDate, string status);
    Task<(bool Success, List<string> Messages)> CancelTicketByAdminAsync(string ticketId);
}