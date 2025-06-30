using MovieTheater.Models;

namespace MovieTheater.Repository
{
    public interface IInvoiceRepository
    {
        public IEnumerable<Invoice> GetAll();
        Invoice? GetById(string invoiceId);
        void Update(Invoice invoice);
        void Save();
    }
}
