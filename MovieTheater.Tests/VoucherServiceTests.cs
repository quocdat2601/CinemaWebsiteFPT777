using Moq;
using MovieTheater.Models;
using MovieTheater.Repository;
using MovieTheater.Service;
using System;
using System.Collections.Generic;
using Xunit;
using Microsoft.EntityFrameworkCore;

namespace MovieTheater.Tests
{
    public class VoucherServiceTests
    {
        private readonly Mock<IVoucherRepository> _mockVoucherRepository;
        private readonly Mock<IMemberRepository> _mockMemberRepository;
        private readonly VoucherService _voucherService;

        public VoucherServiceTests()
        {
            _mockVoucherRepository = new Mock<IVoucherRepository>();
            _mockMemberRepository = new Mock<IMemberRepository>();
            _voucherService = new VoucherService(_mockVoucherRepository.Object, _mockMemberRepository.Object);
        }

        [Fact]
        public void ValidateVoucherUsage_ValidVoucher_ReturnsValidResult()
        {
            // Arrange
            var voucherId = "VC001";
            var accountId = "ACC001";
            var orderTotal = 1000m;

            var voucher = new Voucher
            {
                VoucherId = voucherId,
                AccountId = accountId,
                Value = 500m,
                IsUsed = false,
                ExpiryDate = DateTime.Now.AddDays(30)
            };

            _mockVoucherRepository.Setup(r => r.GetById(voucherId)).Returns(voucher);

            // Act
            var result = _voucherService.ValidateVoucherUsage(voucherId, accountId, orderTotal);

            // Assert
            Assert.True(result.IsValid);
            Assert.Equal(500m, result.VoucherValue);
            Assert.Equal(voucher, result.Voucher);
        }

        [Fact]
        public void ValidateVoucherUsage_VoucherNotFound_ReturnsInvalidResult()
        {
            // Arrange
            var voucherId = "VC001";
            var accountId = "ACC001";
            var orderTotal = 1000m;

            _mockVoucherRepository.Setup(r => r.GetById(voucherId)).Returns((Voucher)null);

            // Act
            var result = _voucherService.ValidateVoucherUsage(voucherId, accountId, orderTotal);

            // Assert
            Assert.False(result.IsValid);
            Assert.Equal("Voucher not found.", result.ErrorMessage);
        }

        [Fact]
        public void ValidateVoucherUsage_WrongAccount_ReturnsInvalidResult()
        {
            // Arrange
            var voucherId = "VC001";
            var accountId = "ACC001";
            var wrongAccountId = "ACC002";
            var orderTotal = 1000m;

            var voucher = new Voucher
            {
                VoucherId = voucherId,
                AccountId = accountId,
                Value = 500m,
                IsUsed = false,
                ExpiryDate = DateTime.Now.AddDays(30)
            };

            _mockVoucherRepository.Setup(r => r.GetById(voucherId)).Returns(voucher);

            // Act
            var result = _voucherService.ValidateVoucherUsage(voucherId, wrongAccountId, orderTotal);

            // Assert
            Assert.False(result.IsValid);
            Assert.Equal("Voucher does not belong to this account.", result.ErrorMessage);
        }

        [Fact]
        public void ValidateVoucherUsage_VoucherAlreadyUsed_ReturnsInvalidResult()
        {
            // Arrange
            var voucherId = "VC001";
            var accountId = "ACC001";
            var orderTotal = 1000m;

            var voucher = new Voucher
            {
                VoucherId = voucherId,
                AccountId = accountId,
                Value = 500m,
                IsUsed = true,
                ExpiryDate = DateTime.Now.AddDays(30)
            };

            _mockVoucherRepository.Setup(r => r.GetById(voucherId)).Returns(voucher);

            // Act
            var result = _voucherService.ValidateVoucherUsage(voucherId, accountId, orderTotal);

            // Assert
            Assert.False(result.IsValid);
            Assert.Equal("Voucher already used.", result.ErrorMessage);
        }

        [Fact]
        public void ValidateVoucherUsage_VoucherExpired_ReturnsInvalidResult()
        {
            // Arrange
            var voucherId = "VC001";
            var accountId = "ACC001";
            var orderTotal = 1000m;

            var voucher = new Voucher
            {
                VoucherId = voucherId,
                AccountId = accountId,
                Value = 500m,
                IsUsed = false,
                ExpiryDate = DateTime.Now.AddDays(-1)
            };

            _mockVoucherRepository.Setup(r => r.GetById(voucherId)).Returns(voucher);

            // Act
            var result = _voucherService.ValidateVoucherUsage(voucherId, accountId, orderTotal);

            // Assert
            Assert.False(result.IsValid);
            Assert.Equal("Voucher expired.", result.ErrorMessage);
        }

        [Fact]
        public void ValidateVoucherUsage_VoucherValueExceedsOrderTotal_ReturnsCappedValue()
        {
            // Arrange
            var voucherId = "VC001";
            var accountId = "ACC001";
            var orderTotal = 300m;

            var voucher = new Voucher
            {
                VoucherId = voucherId,
                AccountId = accountId,
                Value = 500m,
                IsUsed = false,
                ExpiryDate = DateTime.Now.AddDays(30)
            };

            _mockVoucherRepository.Setup(r => r.GetById(voucherId)).Returns(voucher);

            // Act
            var result = _voucherService.ValidateVoucherUsage(voucherId, accountId, orderTotal);

            // Assert
            Assert.True(result.IsValid);
            // Voucher 500k nhưng đơn hàng chỉ 300k -> giới hạn ở 300k
            Assert.Equal(300m, result.VoucherValue);
        }

        [Fact]
        public void Add_CallsRepositoryAdd()
        {
            // Arrange
            var voucher = new Voucher { VoucherId = "VC002", AccountId = "ACC002", Value = 100, CreatedDate = DateTime.Now, ExpiryDate = DateTime.Now.AddDays(10) };

            // Act
            _voucherService.Add(voucher);

            // Assert
            _mockVoucherRepository.Verify(r => r.Add(voucher), Times.Once);
        }

        [Fact]
        public void Update_CallsRepositoryUpdate()
        {
            // Arrange
            var voucher = new Voucher { VoucherId = "VC003", AccountId = "ACC003" };

            // Act
            _voucherService.Update(voucher);

            // Assert
            _mockVoucherRepository.Verify(r => r.Update(voucher), Times.Once);
        }

        [Fact]
        public void Delete_CallsRepositoryDelete()
        {
            // Arrange
            var voucherId = "VC004";

            // Act
            _voucherService.Delete(voucherId);

            // Assert
            _mockVoucherRepository.Verify(r => r.Delete(voucherId), Times.Once);
        }

        [Fact]
        public void GetById_ReturnsVoucher()
        {
            // Arrange
            var voucher = new Voucher { VoucherId = "VC005", AccountId = "ACC005" };
            _mockVoucherRepository.Setup(r => r.GetById("VC005")).Returns(voucher);

            // Act
            var result = _voucherService.GetById("VC005");

            // Assert
            Assert.Equal(voucher, result);
        }

        [Fact]
        public void GetAll_ReturnsAllVouchers()
        {
            // Arrange
            var vouchers = new List<Voucher> { new Voucher(), new Voucher() };
            _mockVoucherRepository.Setup(r => r.GetAll()).Returns(vouchers);

            // Act
            var result = _voucherService.GetAll();

            // Assert
            Assert.Equal(vouchers, result);
        }

        [Fact]
        public void GenerateVoucherId_ReturnsGeneratedId()
        {
            // Arrange
            var expectedId = "VC999";
            _mockVoucherRepository.Setup(r => r.GenerateVoucherId()).Returns(expectedId);

            // Act
            var result = _voucherService.GenerateVoucherId();

            // Assert
            Assert.Equal(expectedId, result);
        }

        [Fact]
        public void GetAllMembers_ReturnsAllMembers()
        {
            // Arrange
            var members = new List<Member> { new Member(), new Member() };
            _mockMemberRepository.Setup(r => r.GetAll()).Returns(members);

            // Act
            var result = _voucherService.GetAllMembers();

            // Assert
            Assert.Equal(members, result);
        }

        [Fact]
        public void GetAvailableVouchers_ReturnsAvailableVouchers()
        {
            // Arrange
            var accountId = "ACC007";
            var vouchers = new List<Voucher> { new Voucher(), new Voucher() };
            _mockVoucherRepository.Setup(r => r.GetAvailableVouchers(accountId)).Returns(vouchers);

            // Act
            var result = _voucherService.GetAvailableVouchers(accountId);

            // Assert
            Assert.Equal(vouchers, result);
        }
    }
} 