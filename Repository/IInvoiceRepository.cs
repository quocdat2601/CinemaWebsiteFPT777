using MovieTheater.Models;

namespace MovieTheater.Repository
{
    public interface IInvoiceRepository
    {
        public IEnumerable<Invoice> GetAll();
        Invoice? GetById(string invoiceId);
        Task<IEnumerable<Invoice>> GetByAccountIdAsync(string accountId, InvoiceStatus? status = null, bool? isCanceled = null);
        Task<Invoice?> GetDetailsAsync(string invoiceId, string accountId);
        Task<Invoice?> GetForCancelAsync(string invoiceId, string accountId);
        Task UpdateAsync(Invoice invoice);
        Task<IEnumerable<Invoice>> GetByDateRangeAsync(string accountId, DateTime? fromDate, DateTime? toDate);
        void Update(Invoice invoice);
        void Save();
        Invoice? FindInvoiceByOrderId(string orderId);
        Invoice? FindInvoiceByAmountAndTime(decimal amount, DateTime? recentTime = null);
    }
}
