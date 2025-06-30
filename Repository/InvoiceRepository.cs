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
            return _context.Invoices.Include(i => i.Account).ToList();
        }

        public Invoice? GetById(string invoiceId)
        {
            return _context.Invoices
                .Include(i => i.Account)
                .ThenInclude(a => a.Members)
                .FirstOrDefault(i => i.InvoiceId == invoiceId);
        }

        public void Update(Invoice invoice)
        {
            _context.Entry(invoice).State = EntityState.Modified;
        }

        public void Save()
        {
            _context.SaveChanges();
        }
    }
}
