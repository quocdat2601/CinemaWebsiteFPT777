using MovieTheater.Models;
using MovieTheater.Repository;
using MovieTheater.ViewModels;

namespace MovieTheater.Service
{
    public class TicketVerificationService : ITicketVerificationService
    {
        private readonly IInvoiceService _invoiceService;
        private readonly IScheduleSeatRepository _scheduleSeatRepository;
        private readonly IMemberRepository _memberRepository;
        private readonly ISeatService _seatService;
        private readonly ISeatTypeService _seatTypeService;
        private readonly ILogger<TicketVerificationService> _logger;

        public TicketVerificationService(
            IInvoiceService invoiceService,
            IScheduleSeatRepository scheduleSeatRepository,
            IMemberRepository memberRepository,
            ISeatService seatService,
            ISeatTypeService seatTypeService,
            ILogger<TicketVerificationService> logger)
        {
            _invoiceService = invoiceService;
            _scheduleSeatRepository = scheduleSeatRepository;
            _memberRepository = memberRepository;
            _seatService = seatService;
            _seatTypeService = seatTypeService;
            _logger = logger;
        }

        public TicketVerificationResultViewModel VerifyTicket(string invoiceId)
        {
            var result = new TicketVerificationResultViewModel();
            try
            {
                if (string.IsNullOrEmpty(invoiceId))
                {
                    result.IsSuccess = false;
                    result.Message = "Mã QR không hợp lệ";
                    return result;
                }
                var invoice = _invoiceService.GetById(invoiceId);
                if (invoice == null)
                {
                    result.IsSuccess = false;
                    result.Message = "Không tìm thấy vé";
                    return result;
                }
                if (invoice.Status != InvoiceStatus.Completed)
                {
                    result.IsSuccess = false;
                    result.Message = "Vé chưa thanh toán";
                    return result;
                }
                var scheduleSeats = _scheduleSeatRepository.GetByInvoiceId(invoiceId).ToList();
                if (scheduleSeats.Any(ss => ss.SeatStatusId == 2))
                {
                    result.IsSuccess = false;
                    result.Message = "Vé đã được sử dụng";
                    return result;
                }
                // Đánh dấu đã sử dụng
                foreach (var seat in scheduleSeats)
                {
                    seat.SeatStatusId = 2;
                    _scheduleSeatRepository.Update(seat);
                }
                _scheduleSeatRepository.Save();
                return BuildTicketInfo(invoice, true, "Xác thực vé thành công!");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi xác thực vé");
                result.IsSuccess = false;
                result.Message = "Có lỗi xảy ra khi xác thực vé";
                return result;
            }
        }

        public TicketVerificationResultViewModel GetTicketInfo(string invoiceId)
        {
            try
            {
                var invoice = _invoiceService.GetById(invoiceId);
                if (invoice == null)
                {
                    return new TicketVerificationResultViewModel { IsSuccess = false, Message = "Không tìm thấy vé" };
                }
                return BuildTicketInfo(invoice, true, "Lấy thông tin vé thành công");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi lấy thông tin vé");
                return new TicketVerificationResultViewModel { IsSuccess = false, Message = "Có lỗi xảy ra khi lấy thông tin vé" };
            }
        }

        public TicketVerificationResultViewModel ConfirmCheckIn(string invoiceId, string staffId)
        {
            var result = new TicketVerificationResultViewModel();
            try
            {
                if (string.IsNullOrEmpty(invoiceId))
                {
                    result.IsSuccess = false;
                    result.Message = "Mã QR không hợp lệ";
                    return result;
                }
                var invoice = _invoiceService.GetById(invoiceId);
                if (invoice == null)
                {
                    result.IsSuccess = false;
                    result.Message = "Không tìm thấy vé";
                    return result;
                }
                if (invoice.Status != InvoiceStatus.Completed)
                {
                    result.IsSuccess = false;
                    result.Message = "Vé chưa thanh toán";
                    return result;
                }
                var scheduleSeats = _scheduleSeatRepository.GetByInvoiceId(invoiceId).ToList();
                if (scheduleSeats.Any(ss => ss.SeatStatusId == 2))
                {
                    result.IsSuccess = false;
                    result.Message = "Vé đã được check-in";
                    return result;
                }
                foreach (var seat in scheduleSeats)
                {
                    seat.SeatStatusId = 2;
                    _scheduleSeatRepository.Update(seat);
                }
                _scheduleSeatRepository.Save();
                // TODO: Lưu log check-in nếu cần
                return BuildTicketInfo(invoice, true, "Check-in thành công!");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi check-in vé");
                result.IsSuccess = false;
                result.Message = "Có lỗi xảy ra khi check-in vé";
                return result;
            }
        }

        private TicketVerificationResultViewModel BuildTicketInfo(Invoice invoice, bool isSuccess, string message)
        {
            var member = _memberRepository.GetByAccountId(invoice.AccountId);
            var seatNames = (invoice.Seat ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim())
                .Where(s => !string.IsNullOrEmpty(s))
                .ToArray();
            return new TicketVerificationResultViewModel
            {
                InvoiceId = invoice.InvoiceId,
                MovieName = invoice.MovieShow.Movie.MovieNameEnglish,
                ShowDate = invoice.MovieShow.ShowDate.ToString(),
                ShowTime = invoice.MovieShow.Schedule.ScheduleTime.ToString(),
                CustomerName = member?.Account?.FullName ?? "N/A",
                CustomerPhone = member?.Account?.PhoneNumber ?? "N/A",
                Seats = string.Join(", ", seatNames),
                TotalAmount = invoice.TotalMoney?.ToString("N0") + " VND",
                IsSuccess = isSuccess,
                VerificationTime = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss"),
                Message = message
            };
        }
    }
}