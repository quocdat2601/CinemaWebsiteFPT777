using MovieTheater.ViewModels;

namespace MovieTheater.Service
{
    public interface ITicketVerificationService
    {
        TicketVerificationResultViewModel VerifyTicket(string invoiceId);
        TicketVerificationResultViewModel GetTicketInfo(string invoiceId);
        TicketVerificationResultViewModel ConfirmCheckIn(string invoiceId, string staffId);
    }
} 