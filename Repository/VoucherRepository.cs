using MovieTheater.Models;

namespace MovieTheater.Repository
{
    public class VoucherRepository : IVoucherRepository
    {
        private readonly MovieTheaterContext _context;
        public VoucherRepository(MovieTheaterContext context)
        {
            _context = context;
        }
        public Voucher? GetById(string voucherId)
        {
            return _context.Vouchers.FirstOrDefault(v => v.VoucherId == voucherId);
        }
        public IEnumerable<Voucher> GetAll()
        {
            return _context.Vouchers.ToList();
        }
        public void Add(Voucher voucher)
        {
            int maxRetries = 3;
            int retryCount = 0;

            while (retryCount < maxRetries)
            {
                try
                {
                    _context.Vouchers.Add(voucher);
                    _context.SaveChanges();
                    return; // Success, exit the method
                }
                catch (Microsoft.EntityFrameworkCore.DbUpdateException ex) when (ex.InnerException is Microsoft.Data.SqlClient.SqlException sqlEx && sqlEx.Number == 2627)
                {
                    // Duplicate key error - regenerate the voucher ID and retry
                    retryCount++;
                    if (retryCount >= maxRetries)
                    {
                        // If we've exhausted retries, use timestamp-based ID
                        voucher.VoucherId = $"VC{DateTime.Now:yyyyMMddHHmmss}";
                        _context.Vouchers.Add(voucher);
                        _context.SaveChanges();
                        return;
                    }

                    // Regenerate the voucher ID
                    voucher.VoucherId = GenerateVoucherId();
                }
                catch
                {
                    // For any other exception, rethrow
                    throw;
                }
            }
        }
        public void Update(Voucher voucher)
        {
            _context.Vouchers.Update(voucher);
            _context.SaveChanges();
        }
        public void Delete(string voucherId)
        {
            var voucher = GetById(voucherId);
            if (voucher != null)
            {
                // Kiểm tra xem voucher có được sử dụng trong invoice nào không
                var invoicesUsingVoucher = _context.Invoices
                    .Where(i => i.VoucherId == voucherId)
                    .ToList();

                if (invoicesUsingVoucher.Any())
                {
                    throw new InvalidOperationException($"Cannot delete voucher '{voucherId}' because it is being used by {invoicesUsingVoucher.Count} invoice(s).");
                }

                _context.Vouchers.Remove(voucher);
                _context.SaveChanges();
            }
        }
        public string GenerateVoucherId()
        {
            // Get all existing voucher IDs
            var existingIds = _context.Vouchers
                .Select(v => v.VoucherId)
                .ToList();

            // Find the highest number from existing IDs
            int maxNumber = 0;
            foreach (var id in existingIds)
            {
                if (id.StartsWith("VC") && int.TryParse(id.Substring(2), out int number))
                {
                    maxNumber = Math.Max(maxNumber, number);
                }
            }

            // Generate the next ID
            string newId = $"VC{(maxNumber + 1):D3}";

            // If the generated ID already exists, use timestamp
            if (existingIds.Contains(newId))
            {
                newId = $"VC{DateTime.Now:yyyyMMddHHmmss}";
            }

            return newId;
        }
        public IEnumerable<Voucher> GetAvailableVouchers(string accountId)
        {
            return _context.Vouchers
                .Where(v => v.AccountId == accountId && v.IsUsed == false && v.ExpiryDate > DateTime.Now)
                .ToList();
        }

        public void Save()
        {
            _context.SaveChanges();
        }

        public bool CanDelete(string voucherId)
        {
            // Kiểm tra xem voucher có được sử dụng trong invoice nào không
            var invoicesUsingVoucher = _context.Invoices
                .Where(i => i.VoucherId == voucherId)
                .Any();

            return !invoicesUsingVoucher;
        }

        public int GetInvoiceCountForVoucher(string voucherId)
        {
            return _context.Invoices
                .Where(i => i.VoucherId == voucherId)
                .Count();
        }
    }
}