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

        public bool Delete(string voucherId)
        {
            try
            {
                _voucherRepository.Delete(voucherId);
                return true;
            }
            catch (InvalidOperationException ex)
            {
                // Log the error for debugging
                Serilog.Log.Warning(ex, "Failed to delete voucher {VoucherId}: {Message}", voucherId, ex.Message);
                throw; // Re-throw the exception to be handled by the controller
            }
            catch (Exception ex)
            {
                // Log unexpected errors
                Serilog.Log.Error(ex, "Unexpected error deleting voucher {VoucherId}: {Message}", voucherId, ex.Message);
                throw;
            }
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
            var vouchers = _voucherRepository.GetAll();

            if (!string.IsNullOrEmpty(filter.Keyword))
            {
                vouchers = vouchers.Where(v => v.VoucherId.Contains(filter.Keyword) || v.AccountId.Contains(filter.Keyword) || v.Code.Contains(filter.Keyword));
            }

            if (!string.IsNullOrEmpty(filter.StatusFilter))
            {
                switch (filter.StatusFilter.ToLower())
                {
                    case "used":
                        vouchers = vouchers.Where(v => v.IsUsed == true);
                        break;
                    case "unused":
                        vouchers = vouchers.Where(v => v.IsUsed == false || v.IsUsed == null);
                        break;
                    case "active":
                        vouchers = vouchers.Where(v => (v.IsUsed == false || v.IsUsed == null) && v.ExpiryDate > DateTime.Now);
                        break;
                    case "expired":
                        vouchers = vouchers.Where(v => v.ExpiryDate <= DateTime.Now);
                        break;
                }
            }

            if (!string.IsNullOrEmpty(filter.ExpiryFilter))
            {
                var today = DateTime.Today;
                switch (filter.ExpiryFilter.ToLower())
                {
                    case "week":
                        vouchers = vouchers.Where(v => v.ExpiryDate.Date > today && v.ExpiryDate.Date <= today.AddDays(7));
                        break;
                    case "month":
                        vouchers = vouchers.Where(v => v.ExpiryDate.Date > today && v.ExpiryDate.Date <= today.AddMonths(1));
                        break;
                    case "expiring-soon":
                        vouchers = vouchers.Where(v => v.ExpiryDate.Date > today && v.ExpiryDate.Date <= today.AddDays(7));
                        break;
                    case "expired":
                        vouchers = vouchers.Where(v => v.ExpiryDate.Date <= today);
                        break;
                    case "valid":
                        vouchers = vouchers.Where(v => v.ExpiryDate.Date > today);
                        break;
                }
            }

            return vouchers;
        }

        public bool CanDelete(string voucherId)
        {
            return _voucherRepository.CanDelete(voucherId);
        }

        public int GetInvoiceCountForVoucher(string voucherId)
        {
            return _voucherRepository.GetInvoiceCountForVoucher(voucherId);
        }

        public async Task MarkVoucherAsUsedAsync(string voucherId)
        {
            var voucher = _voucherRepository.GetById(voucherId);
            if (voucher != null && voucher.IsUsed != true)
            {
                voucher.IsUsed = true;
                _voucherRepository.Update(voucher);
                _voucherRepository.Save();
            }
        }
    }
}