using MovieTheater.Models;

namespace MovieTheater.Service
{
    // Thêm model filter voucher
    public class VoucherFilterModel
    {
        public string Keyword { get; set; }
        public string StatusFilter { get; set; }
        public string ExpiryFilter { get; set; }
    }

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
        IEnumerable<Voucher> GetFilteredVouchers(VoucherFilterModel filter);

        // Validate voucher và trả về kết quả với giá trị thực tế có thể sử dụng
        VoucherValidationResult ValidateVoucherUsage(string voucherId, string accountId, decimal orderTotal);
    }
}