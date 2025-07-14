using MovieTheater.Models;

namespace MovieTheater.Service
{
    public interface IVoucherService
    {
        Voucher? GetById(string voucherId);
        IEnumerable<Voucher> GetAll();
        void Add(Voucher voucher);
        void Update(Voucher voucher);
        void Delete(string voucherId);
        string GenerateVoucherId();
        IEnumerable<Member> GetAllMembers();
        IEnumerable<Voucher> GetAvailableVouchers(string accountId);
        
        // Validate voucher và trả về kết quả với giá trị thực tế có thể sử dụng
        VoucherValidationResult ValidateVoucherUsage(string voucherId, string accountId, decimal orderTotal);
    }
}