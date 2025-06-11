using MovieTheater.Models;

namespace MovieTheater.Service
{
    public interface IInvoiceService
    {
        public IEnumerable<Invoice> GetAll();
    }
}
