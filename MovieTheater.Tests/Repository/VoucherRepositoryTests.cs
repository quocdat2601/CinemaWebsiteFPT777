using Microsoft.EntityFrameworkCore;
using MovieTheater.Models;
using MovieTheater.Repository;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace MovieTheater.Tests.Repository
{
    public class VoucherRepositoryTests : IDisposable
    {
        private readonly MovieTheaterContext _context;
        private readonly VoucherRepository _repository;

        public VoucherRepositoryTests()
        {
            var options = new DbContextOptionsBuilder<MovieTheaterContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new MovieTheaterContext(options);
            _repository = new VoucherRepository(_context);
            
            // Seed test data
            SeedTestData();
        }

        private void SeedTestData()
        {
            // Add test accounts
            var account1 = new Account { AccountId = "ACC001", FullName = "Test User 1", Email = "test1@example.com" };
            var account2 = new Account { AccountId = "ACC002", FullName = "Test User 2", Email = "test2@example.com" };
            _context.Accounts.AddRange(account1, account2);

            // Add test vouchers
            var vouchers = new List<Voucher>
            {
                new Voucher
                {
                    VoucherId = "VC001",
                    AccountId = "ACC001",
                    Code = "TEST001",
                    Value = 50000,
                    CreatedDate = DateTime.Now.AddDays(-10),
                    ExpiryDate = DateTime.Now.AddDays(20),
                    IsUsed = false
                },
                new Voucher
                {
                    VoucherId = "VC002",
                    AccountId = "ACC001",
                    Code = "TEST002",
                    Value = 100000,
                    CreatedDate = DateTime.Now.AddDays(-5),
                    ExpiryDate = DateTime.Now.AddDays(15),
                    IsUsed = true
                },
                new Voucher
                {
                    VoucherId = "VC003",
                    AccountId = "ACC002",
                    Code = "TEST003",
                    Value = 75000,
                    CreatedDate = DateTime.Now.AddDays(-3),
                    ExpiryDate = DateTime.Now.AddDays(-1), // Expired
                    IsUsed = false
                },
                new Voucher
                {
                    VoucherId = "VC004",
                    AccountId = "ACC001",
                    Code = "TEST004",
                    Value = 25000,
                    CreatedDate = DateTime.Now.AddDays(-1),
                    ExpiryDate = DateTime.Now.AddDays(30),
                    IsUsed = false
                }
            };
            _context.Vouchers.AddRange(vouchers);

            // Add test invoices
            var invoices = new List<Invoice>
            {
                new Invoice
                {
                    InvoiceId = "INV001",
                    AccountId = "ACC001",
                    VoucherId = "VC002", // Used voucher
                    TotalMoney = 150000,
                    BookingDate = DateTime.Now.AddDays(-2),
                    Status = InvoiceStatus.Completed
                },
                new Invoice
                {
                    InvoiceId = "INV002",
                    AccountId = "ACC001",
                    VoucherId = null, // No voucher used
                    TotalMoney = 200000,
                    BookingDate = DateTime.Now.AddDays(-1),
                    Status = InvoiceStatus.Completed
                }
            };
            _context.Invoices.AddRange(invoices);

            _context.SaveChanges();
        }

        [Fact]
        public void GetById_ExistingVoucher_ReturnsVoucher()
        {
            // Arrange
            string voucherId = "VC001";

            // Act
            var result = _repository.GetById(voucherId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(voucherId, result.VoucherId);
            Assert.Equal("TEST001", result.Code);
        }

        [Fact]
        public void GetById_NonExistingVoucher_ReturnsNull()
        {
            // Arrange
            string voucherId = "NONEXISTENT";

            // Act
            var result = _repository.GetById(voucherId);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void GetAll_ReturnsAllVouchers()
        {
            // Act
            var result = _repository.GetAll();

            // Assert
            Assert.Equal(4, result.Count());
        }

        [Fact]
        public void Add_ValidVoucher_SavesToDatabase()
        {
            // Arrange
            var voucher = new Voucher
            {
                VoucherId = "VC005",
                AccountId = "ACC001",
                Code = "NEWVOUCHER",
                Value = 50000,
                CreatedDate = DateTime.Now,
                ExpiryDate = DateTime.Now.AddDays(30),
                IsUsed = false
            };

            // Act
            _repository.Add(voucher);

            // Assert
            var savedVoucher = _context.Vouchers.Find("VC005");
            Assert.NotNull(savedVoucher);
            Assert.Equal("NEWVOUCHER", savedVoucher.Code);
        }

        [Fact]
        public void Update_ExistingVoucher_UpdatesSuccessfully()
        {
            // Arrange
            var voucher = _repository.GetById("VC001");
            voucher.Value = 75000;

            // Act
            _repository.Update(voucher);

            // Assert
            var updatedVoucher = _repository.GetById("VC001");
            Assert.Equal(75000, updatedVoucher.Value);
        }

        [Fact]
        public void Delete_VoucherNotUsed_DeletesSuccessfully()
        {
            // Arrange
            string voucherId = "VC001";

            // Act
            _repository.Delete(voucherId);

            // Assert
            var deletedVoucher = _repository.GetById(voucherId);
            Assert.Null(deletedVoucher);
        }

        [Fact]
        public void Delete_VoucherUsedInInvoice_ThrowsInvalidOperationException()
        {
            // Arrange
            string voucherId = "VC002"; // This voucher is used in an invoice

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() => _repository.Delete(voucherId));
            Assert.Contains("Cannot delete voucher", exception.Message);
            Assert.Contains("1 invoice(s)", exception.Message);
        }

        [Fact]
        public void Delete_NonExistingVoucher_DoesNothing()
        {
            // Arrange
            string voucherId = "NONEXISTENT";

            // Act
            _repository.Delete(voucherId);

            // Assert - No exception should be thrown
            Assert.True(true);
        }

        [Fact]
        public void GenerateVoucherId_WithExistingVouchers_ReturnsNextId()
        {
            // Act
            var newId = _repository.GenerateVoucherId();

            // Assert
            Assert.StartsWith("VC", newId);
            // Logic: VC001, VC002, VC003, VC004 exist, so next should be VC005
            // But VC005 has 5 characters, not 6
            Assert.Equal("VC005", newId);
        }

        [Fact]
        public void GenerateVoucherId_WithNoExistingVouchers_ReturnsVC001()
        {
            // Arrange - Clear all vouchers
            _context.Vouchers.RemoveRange(_context.Vouchers.ToList());
            _context.SaveChanges();

            // Act
            var newId = _repository.GenerateVoucherId();

            // Assert
            Assert.Equal("VC001", newId);
        }

        [Fact]
        public void GetAvailableVouchers_ValidAccountId_ReturnsAvailableVouchers()
        {
            // Arrange
            string accountId = "ACC001";

            // Act
            var result = _repository.GetAvailableVouchers(accountId);

            // Assert
            var vouchers = result.ToList();
            Assert.Equal(2, vouchers.Count); // VC001 and VC004 (not expired, not used)
            Assert.All(vouchers, v => Assert.Equal(accountId, v.AccountId));
            Assert.All(vouchers, v => Assert.False(v.IsUsed));
            Assert.All(vouchers, v => Assert.True(v.ExpiryDate > DateTime.Now));
        }

        [Fact]
        public void GetAvailableVouchers_AccountWithNoVouchers_ReturnsEmptyList()
        {
            // Arrange
            string accountId = "ACC999"; // Non-existing account

            // Act
            var result = _repository.GetAvailableVouchers(accountId);

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public void CanDelete_VoucherNotUsed_ReturnsTrue()
        {
            // Arrange
            string voucherId = "VC001";

            // Act
            bool canDelete = _repository.CanDelete(voucherId);

            // Assert
            Assert.True(canDelete);
        }

        [Fact]
        public void CanDelete_VoucherUsedInInvoice_ReturnsFalse()
        {
            // Arrange
            string voucherId = "VC002"; // Used in invoice

            // Act
            bool canDelete = _repository.CanDelete(voucherId);

            // Assert
            Assert.False(canDelete);
        }

        [Fact]
        public void CanDelete_NonExistingVoucher_ReturnsTrue()
        {
            // Arrange
            string voucherId = "NONEXISTENT";

            // Act
            bool canDelete = _repository.CanDelete(voucherId);

            // Assert
            Assert.True(canDelete);
        }

        [Fact]
        public void GetInvoiceCountForVoucher_VoucherUsedInInvoice_ReturnsCorrectCount()
        {
            // Arrange
            string voucherId = "VC002"; // Used in 1 invoice

            // Act
            int count = _repository.GetInvoiceCountForVoucher(voucherId);

            // Assert
            Assert.Equal(1, count);
        }

        [Fact]
        public void GetInvoiceCountForVoucher_VoucherNotUsed_ReturnsZero()
        {
            // Arrange
            string voucherId = "VC001"; // Not used in any invoice

            // Act
            int count = _repository.GetInvoiceCountForVoucher(voucherId);

            // Assert
            Assert.Equal(0, count);
        }

        [Fact]
        public void GetInvoiceCountForVoucher_NonExistingVoucher_ReturnsZero()
        {
            // Arrange
            string voucherId = "NONEXISTENT";

            // Act
            int count = _repository.GetInvoiceCountForVoucher(voucherId);

            // Assert
            Assert.Equal(0, count);
        }

        [Fact]
        public void Save_SavesChangesToDatabase()
        {
            // Arrange
            var voucher = new Voucher
            {
                VoucherId = "VC006",
                AccountId = "ACC001",
                Code = "SAVETEST",
                Value = 30000,
                CreatedDate = DateTime.Now,
                ExpiryDate = DateTime.Now.AddDays(30),
                IsUsed = false
            };
            _context.Vouchers.Add(voucher);

            // Act
            _repository.Save();

            // Assert
            var savedVoucher = _repository.GetById("VC006");
            Assert.NotNull(savedVoucher);
        }

        public void Dispose()
        {
            _context?.Dispose();
        }
    }
} 