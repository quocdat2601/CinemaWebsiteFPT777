using MovieTheater.Models;
using MovieTheater.Repository;
using System.Collections.Generic;

namespace MovieTheater.Service
{
    public class VoucherService : IVoucherService
    {
        private readonly IVoucherRepository _voucherRepository;
        public VoucherService(IVoucherRepository voucherRepository)
        {
            _voucherRepository = voucherRepository;
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
    }
} 