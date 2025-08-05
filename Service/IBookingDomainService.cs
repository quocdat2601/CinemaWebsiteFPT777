using MovieTheater.ViewModels;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MovieTheater.Service
{
    public interface IBookingDomainService
    {
        Task<ConfirmBookingViewModel> BuildConfirmBookingViewModelAsync(
            string movieId, DateOnly showDate, string showTime, List<int> selectedSeatIds, int movieShowId, List<int>? foodIds, List<int>? foodQtys, string userId);
        Task<BookingResult> ConfirmBookingAsync(ConfirmBookingViewModel model, string userId);
        Task<BookingSuccessViewModel> BuildSuccessViewModelAsync(string invoiceId, string userId);
        // Admin ticket selling (for BookingController)
        Task<ConfirmTicketAdminViewModel> BuildConfirmTicketAdminViewModelAsync(int movieShowId, List<int> selectedSeatIds, List<int> foodIds, List<int> foodQtys, string memberId = null);
        Task<BookingResult> ConfirmTicketForAdminAsync(ConfirmTicketAdminViewModel model);
        Task<ConfirmTicketAdminViewModel> BuildTicketBookingConfirmedViewModelAsync(string invoiceId);
    }
} 