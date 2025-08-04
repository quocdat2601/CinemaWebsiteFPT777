using Microsoft.EntityFrameworkCore;
using MovieTheater.Models;

namespace MovieTheater.Repository
{
    public class InvoiceRepository : IInvoiceRepository
    {
        private readonly MovieTheaterContext _context;

        public InvoiceRepository(MovieTheaterContext context)
        {
            _context = context;
        }

        public IEnumerable<Invoice> GetAll()
        {
            return _context.Invoices
                .Include(i => i.Account)
                .Include(e => e.Employee)
                .Include(i => i.MovieShow)
                    .ThenInclude(ms => ms.Movie)
                .Include(i => i.MovieShow)
                    .ThenInclude(ms => ms.CinemaRoom)
                .Include(i => i.MovieShow)
                    .ThenInclude(ms => ms.Schedule)
                .Include(i => i.MovieShow)
                    .ThenInclude(ms => ms.Version)
                .ToList();
        }

        public Invoice? GetById(string invoiceId)
        {
            return _context.Invoices
                .Include(i => i.Account)
                .ThenInclude(a => a.Members)
                .Include(i => i.Employee)
                .Include(i => i.MovieShow)
                    .ThenInclude(ms => ms.Movie)
                .Include(i => i.MovieShow)
                    .ThenInclude(ms => ms.CinemaRoom)
                .Include(i => i.MovieShow)
                    .ThenInclude(ms => ms.Schedule)
                .Include(i => i.MovieShow)
                    .ThenInclude(ms => ms.Version)
                .FirstOrDefault(i => i.InvoiceId == invoiceId);
        }

        public async Task<IEnumerable<Invoice>> GetByAccountIdAsync(string accountId, InvoiceStatus? status = null, bool? isCanceled = null)
        {
            var query = _context.Invoices.Where(i => i.AccountId == accountId);

            if (status.HasValue)
            {
                query = query.Where(i => i.Status == status.Value);
            }
            if (isCanceled.HasValue)
            {
                query = query.Where(i => i.Cancel == isCanceled.Value);
            }

            return await query
                .Include(i => i.MovieShow).ThenInclude(ms => ms.Movie)
                .Include(i => i.MovieShow).ThenInclude(ms => ms.Schedule)
                .OrderByDescending(i => i.BookingDate)
                .ToListAsync();
        }

        public async Task<IEnumerable<Invoice>> GetByDateRangeAsync(string accountId, DateTime? fromDate, DateTime? toDate)
        {
            var query = _context.Invoices.Where(i => i.AccountId == accountId);

            if (fromDate.HasValue)
                query = query.Where(i => i.BookingDate >= fromDate.Value);
            if (toDate.HasValue)
                query = query.Where(i => i.BookingDate <= toDate.Value);

            return await query
                .Include(i => i.MovieShow).ThenInclude(ms => ms.Movie)
                .Include(i => i.MovieShow).ThenInclude(ms => ms.Schedule)
                .OrderByDescending(i => i.BookingDate)
                .ToListAsync();
        }

        public async Task<Invoice?> GetDetailsAsync(string invoiceId, string accountId)
        {
            return await _context.Invoices
                .Where(i => i.InvoiceId == invoiceId && i.AccountId == accountId)
                .Include(i => i.MovieShow)
                    .ThenInclude(ms => ms.Movie)
                .Include(i => i.MovieShow)
                    .ThenInclude(ms => ms.CinemaRoom)
                .Include(i => i.MovieShow)
                    .ThenInclude(ms => ms.Schedule)
                .Include(i => i.MovieShow)
                    .ThenInclude(ms => ms.Version)
                .Include(i => i.ScheduleSeats)
                    .ThenInclude(ss => ss.Seat)
                        .ThenInclude(s => s.SeatType)
                .Include(i => i.Voucher)
                .FirstOrDefaultAsync();
        }

        public async Task<Invoice?> GetForCancelAsync(string invoiceId, string accountId)
        {
            return await _context.Invoices
                .Where(i => i.InvoiceId == invoiceId && i.AccountId == accountId)
                .Include(i => i.MovieShow).ThenInclude(ms => ms.Schedule)
                .FirstOrDefaultAsync();
        }

        public async Task UpdateAsync(Invoice invoice)
        {
            _context.Invoices.Update(invoice);
            await _context.SaveChangesAsync();
        }

        public void Update(Invoice invoice)
        {
            _context.Entry(invoice).State = EntityState.Modified;
        }

        public void Save()
        {
            _context.SaveChanges();
        }

        public Invoice? FindInvoiceByOrderId(string orderId)
        {
            // Logic tìm kiếm linh hoạt từ CassoWebhookController
            return _context.Invoices.FirstOrDefault(i => i.InvoiceId == orderId) // Tìm chính xác
                ?? _context.Invoices.FirstOrDefault(i => i.InvoiceId.ToString() == orderId) // Chuyển số thành chuỗi
                ?? _context.Invoices.FirstOrDefault(i => orderId.Contains(i.InvoiceId.ToString())) // Tìm trong chuỗi
                ?? _context.Invoices.Where(i => orderId.Contains(i.InvoiceId.ToString())).OrderByDescending(i => i.BookingDate).FirstOrDefault() // Tìm theo thời gian gần nhất
                ?? _context.Invoices.FirstOrDefault(i => orderId.Contains(i.InvoiceId)) // Tìm theo pattern linh hoạt hơn
                ?? _context.Invoices.Where(i => i.InvoiceId.Contains(orderId)).OrderByDescending(i => i.BookingDate).FirstOrDefault(); // Tìm ngược lại
        }

        public Invoice? FindInvoiceByAmountAndTime(decimal amount, DateTime? recentTime = null)
        {
            var query = _context.Invoices.Where(i => i.TotalMoney == amount && i.Status != InvoiceStatus.Completed);
            
            if (recentTime.HasValue)
            {
                query = query.Where(i => i.BookingDate >= recentTime.Value);
            }
            
            return query.OrderByDescending(i => i.BookingDate).FirstOrDefault();
        }
    }
}
