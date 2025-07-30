using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MovieTheater.Models;

namespace MovieTheater.Service
{
    public class GuestInvoiceService : IGuestInvoiceService
    {
        private readonly MovieTheaterContext _context;
        private readonly ILogger<GuestInvoiceService> _logger;

        public GuestInvoiceService(MovieTheaterContext context, ILogger<GuestInvoiceService> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Tạo và lưu invoice cho guest payment
        /// </summary>
        public async Task<bool> CreateGuestInvoiceAsync(string orderId, decimal amount, string customerName, 
            string customerPhone, string movieName, string showTime, string seatInfo, int movieShowId = 1)
        {
            try
            {
                _logger.LogInformation("Creating guest invoice for order: {OrderId}, amount: {Amount}", orderId, amount);

                // Kiểm tra xem invoice đã tồn tại chưa
                if (await InvoiceExistsAsync(orderId))
                {
                    _logger.LogWarning("Invoice already exists for order: {OrderId}", orderId);
                    return true; // Đã tồn tại thì coi như thành công
                }

                // Tạo invoice cho guest
                var invoice = new Invoice
                {
                    InvoiceId = orderId, // Sử dụng orderId làm invoiceId cho guest
                    AccountId = "GUEST", // Guest account
                    AddScore = 0, // Guest không có điểm
                    BookingDate = DateTime.Now,
                    Status = InvoiceStatus.Completed,
                    TotalMoney = amount,
                    UseScore = 0,
                    Seat = seatInfo,
                    SeatIds = "0", // Placeholder cho guest
                    MovieShowId = movieShowId,
                    PromotionDiscount = "0",
                    VoucherId = null,
                    RankDiscountPercentage = 0
                };

                // Lưu invoice vào database
                _context.Invoices.Add(invoice);
                await _context.SaveChangesAsync();
                
                _logger.LogInformation("Guest invoice created successfully with ID: {InvoiceId}", invoice.InvoiceId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating guest invoice for order: {OrderId}", orderId);
                return false;
            }
        }

        /// <summary>
        /// Kiểm tra xem invoice đã tồn tại chưa
        /// </summary>
        public async Task<bool> InvoiceExistsAsync(string orderId)
        {
            try
            {
                return await _context.Invoices.AnyAsync(i => i.InvoiceId == orderId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking invoice existence for order: {OrderId}", orderId);
                return false;
            }
        }

        /// <summary>
        /// Lấy thông tin invoice theo orderId
        /// </summary>
        public async Task<Invoice?> GetInvoiceByOrderIdAsync(string orderId)
        {
            try
            {
                return await _context.Invoices.FirstOrDefaultAsync(i => i.InvoiceId == orderId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting invoice for order: {OrderId}", orderId);
                return null;
            }
        }
    }
} 