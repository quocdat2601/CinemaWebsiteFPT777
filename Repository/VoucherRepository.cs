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
            _context.Vouchers.Add(voucher);
            _context.SaveChanges();
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
                _context.Vouchers.Remove(voucher);
                _context.SaveChanges();
            }
        }
        public string GenerateVoucherId()
        {
            var latestVoucher = _context.Vouchers
                .OrderByDescending(v => v.VoucherId)
                .FirstOrDefault();
            if (latestVoucher == null)
            {
                return "VC001";
            }
            if (int.TryParse(latestVoucher.VoucherId.Substring(2, 3), out int number))
            {
                return $"VC{(number + 1):D3}";
            }
            return $"VC{System.DateTime.Now:yyyyMMddHHmmss}";
        }
        public IEnumerable<Voucher> GetAvailableVouchers(string accountId)
        {
            return _context.Vouchers
                .Where(v => v.AccountId == accountId
                           && (v.IsUsed == null || v.IsUsed == false)
                           && v.ExpiryDate > System.DateTime.Now)
                .OrderBy(v => v.ExpiryDate)
                .ToList();
        }
    }
}