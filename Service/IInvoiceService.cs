using MovieTheater.Models;

namespace MovieTheater.Service
{
    public interface IInvoiceService
    {
        public IEnumerable<Invoice> GetAll();
        Invoice? GetById(string invoiceId);
    }
}
