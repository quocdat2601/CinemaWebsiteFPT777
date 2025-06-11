using MovieTheater.Models;

namespace MovieTheater.Repository
{
    public interface IInvoiceRepository
    {
        public IEnumerable<Invoice> GetAll();
    }
}
