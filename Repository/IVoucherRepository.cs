using MovieTheater.Models;

namespace MovieTheater.Repository
{
    public interface IVoucherRepository
    {
        Voucher? GetById(string voucherId);
        IEnumerable<Voucher> GetAll();
        void Add(Voucher voucher);
        void Update(Voucher voucher);
        void Delete(string voucherId);
        string GenerateVoucherId();
        IEnumerable<Voucher> GetAvailableVouchers(string accountId);
    }
}