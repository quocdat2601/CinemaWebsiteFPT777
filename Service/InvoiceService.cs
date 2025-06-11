using MovieTheater.Models;
using MovieTheater.Repository;
using NuGet.Protocol.Core.Types;

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
    }
}
