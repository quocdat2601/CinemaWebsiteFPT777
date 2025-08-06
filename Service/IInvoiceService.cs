using MovieTheater.Models;

namespace MovieTheater.Service
{
    public interface IInvoiceService
    {
        public IEnumerable<Invoice> GetAll();
        Invoice? GetById(string invoiceId);
        void Update(Invoice invoice);
        void Save();
        Task MarkInvoiceAsCompletedAsync(string invoiceId);
        Task MarkVoucherAsUsedAsync(string voucherId);
        Task UpdateInvoiceStatusAsync(string invoiceId, InvoiceStatus status);
        Invoice? FindInvoiceByOrderId(string orderId);
        Invoice? FindInvoiceByAmountAndTime(decimal amount, DateTime? recentTime = null);
    }
}
