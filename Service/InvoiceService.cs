using MovieTheater.Models;
using MovieTheater.Repository;

namespace MovieTheater.Service
{
    public class InvoiceService : IInvoiceService
    {

        private readonly IInvoiceRepository _invoiceRepository;

        public InvoiceService(IInvoiceRepository invoiceRepository)
        {
            _invoiceRepository = invoiceRepository;
        }

        public IEnumerable<Invoice> GetAll()
        {
            return _invoiceRepository.GetAll();
        }

        public Invoice? GetById(string invoiceId)
        {
            return _invoiceRepository.GetById(invoiceId);
        }
    }
}
