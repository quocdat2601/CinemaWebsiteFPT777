using MovieTheater.Models;
using MovieTheater.Repository;

namespace MovieTheater.Service
{
    public class InvoiceService : IInvoiceService
    {

        private readonly IInvoiceRepository _invoiceRepository;
        private readonly IVoucherRepository _voucherRepository;

        public InvoiceService(IInvoiceRepository invoiceRepository, IVoucherRepository voucherRepository)
        {
            _invoiceRepository = invoiceRepository;
            _voucherRepository = voucherRepository;
        }

        public IEnumerable<Invoice> GetAll()
        {
            return _invoiceRepository.GetAll();
        }

        public Invoice? GetById(string invoiceId)
        {
            return _invoiceRepository.GetById(invoiceId);
        }

        public void Update(Invoice invoice)
        {
            _invoiceRepository.Update(invoice);
        }

        public void Save()
        {
            _invoiceRepository.Save();
        }

        public async Task MarkInvoiceAsCompletedAsync(string invoiceId)
        {
            var invoice = _invoiceRepository.GetById(invoiceId);
            if (invoice != null && invoice.Status != InvoiceStatus.Completed)
            {
                invoice.Status = InvoiceStatus.Completed;
                _invoiceRepository.Update(invoice);
                _invoiceRepository.Save();
            }
        }

        public async Task MarkVoucherAsUsedAsync(string voucherId)
        {
            var voucher = _voucherRepository.GetById(voucherId);
            if (voucher != null && voucher.IsUsed != true)
            {
                voucher.IsUsed = true;
                _voucherRepository.Update(voucher);
                _voucherRepository.Save();
            }
        }

        public async Task UpdateInvoiceStatusAsync(string invoiceId, InvoiceStatus status)
        {
            var invoice = _invoiceRepository.GetById(invoiceId);
            if (invoice != null)
            {
                invoice.Status = status;
                _invoiceRepository.Update(invoice);
                _invoiceRepository.Save();
            }
        }

        public Invoice? FindInvoiceByOrderId(string orderId)
        {
            return _invoiceRepository.FindInvoiceByOrderId(orderId);
        }

        public Invoice? FindInvoiceByAmountAndTime(decimal amount, DateTime? recentTime = null)
        {
            return _invoiceRepository.FindInvoiceByAmountAndTime(amount, recentTime);
        }
    }
}
