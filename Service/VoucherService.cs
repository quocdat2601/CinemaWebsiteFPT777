using MovieTheater.Models;
using MovieTheater.Repository;

namespace MovieTheater.Service
{
    public class VoucherValidationResult
    {
        public bool IsValid { get; set; }
        public string ErrorMessage { get; set; }
        public decimal VoucherValue { get; set; }
        public Voucher Voucher { get; set; }
    }

    public class VoucherService : IVoucherService
    {
        private readonly IVoucherRepository _voucherRepository;
        private readonly IMemberRepository _memberRepository;

        public VoucherService(IVoucherRepository voucherRepository, IMemberRepository memberRepository)
        {
            _voucherRepository = voucherRepository;
            _memberRepository = memberRepository;
        }

        public VoucherValidationResult ValidateVoucherUsage(string voucherId, string accountId, decimal orderTotal)
        {
            var voucher = _voucherRepository.GetById(voucherId);
            if (voucher == null)
            {
                return new VoucherValidationResult
                {
                    IsValid = false,
                    ErrorMessage = "Voucher not found."
                };
            }

            if (voucher.AccountId != accountId)
            {
                return new VoucherValidationResult
                {
                    IsValid = false,
                    ErrorMessage = "Voucher does not belong to this account."
                };
            }

            if (voucher.IsUsed == true)
            {
                return new VoucherValidationResult
                {
                    IsValid = false,
                    ErrorMessage = "Voucher already used."
                };
            }

            if (voucher.ExpiryDate <= DateTime.Now)
            {
                return new VoucherValidationResult
                {
                    IsValid = false,
                    ErrorMessage = "Voucher expired."
                };
            }

            // Giới hạn giá trị voucher không vượt quá tổng đơn hàng
            var actualVoucherValue = Math.Min(voucher.Value, orderTotal);

            return new VoucherValidationResult
            {
                IsValid = true,
                VoucherValue = actualVoucherValue,
                Voucher = voucher
            };
        }

        public Voucher? GetById(string voucherId)
        {
            return _voucherRepository.GetById(voucherId);
        }

        public IEnumerable<Voucher> GetAll()
        {
            return _voucherRepository.GetAll();
        }

        public void Add(Voucher voucher)
        {
            _voucherRepository.Add(voucher);
        }

        public void Update(Voucher voucher)
        {
            _voucherRepository.Update(voucher);
        }

        public void Delete(string voucherId)
        {
            _voucherRepository.Delete(voucherId);
        }

        public string GenerateVoucherId()
        {
            return _voucherRepository.GenerateVoucherId();
        }

        public IEnumerable<Member> GetAllMembers()
        {
            return _memberRepository.GetAll();
        }

        public IEnumerable<Voucher> GetAvailableVouchers(string accountId)
        {
            return _voucherRepository.GetAvailableVouchers(accountId);
        }

        public IEnumerable<Voucher> GetFilteredVouchers(VoucherFilterModel filter)
        {
            var vouchers = _voucherRepository.GetAll().AsQueryable();
            var now = DateTime.Now;
            if (filter != null)
            {
                if (!string.IsNullOrEmpty(filter.Keyword))
                {
                    vouchers = vouchers.Where(v =>
                        (v.VoucherId != null && v.VoucherId.ToLower().Contains(filter.Keyword.ToLower())) ||
                        (v.Code != null && v.Code.ToLower().Contains(filter.Keyword.ToLower())) ||
                        (v.AccountId != null && v.AccountId.ToLower().Contains(filter.Keyword.ToLower())));
                }
                if (!string.IsNullOrEmpty(filter.StatusFilter))
                {
                    switch (filter.StatusFilter.ToLower())
                    {
                        case "active":
                            vouchers = vouchers.Where(v => (!v.IsUsed.HasValue || !v.IsUsed.Value) && v.ExpiryDate > now);
                            break;
                        case "used":
                            vouchers = vouchers.Where(v => v.IsUsed.HasValue && v.IsUsed.Value);
                            break;
                        case "expired":
                            vouchers = vouchers.Where(v => v.ExpiryDate <= now);
                            break;
                    }
                }
                if (!string.IsNullOrEmpty(filter.ExpiryFilter))
                {
                    switch (filter.ExpiryFilter.ToLower())
                    {
                        case "expiring-soon":
                            var sevenDaysFromNow = now.AddDays(7);
                            vouchers = vouchers.Where(v => v.ExpiryDate > now && v.ExpiryDate <= sevenDaysFromNow);
                            break;
                        case "expired":
                            vouchers = vouchers.Where(v => v.ExpiryDate <= now);
                            break;
                        case "valid":
                            vouchers = vouchers.Where(v => v.ExpiryDate > now);
                            break;
                    }
                }
            }
            return vouchers.ToList();
        }
    }
}