using MovieTheater.Models;
using System.Collections.Generic;

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
    }
} 