using MovieTheater.Models;
using System.Collections.Generic;

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
    }
} 