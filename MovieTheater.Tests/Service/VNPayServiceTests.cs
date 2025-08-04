using Microsoft.Extensions.Configuration;
using MovieTheater.Models;
using MovieTheater.Service;
using System;
using System.Collections.Generic;
using System.Globalization;
using Xunit;
using System.Linq;

namespace MovieTheater.Tests.Service
{
    public class VNPayServiceTests
    {
        private readonly VNPayService _service;
        private readonly VNPayConfig _testConfig;

        public VNPayServiceTests()
        {
            _testConfig = new VNPayConfig
            {
                Version = "2.1.0",
                Command = "pay",
                TmnCode = "TESTTMN",
                HashSecret = "TESTHASHSECRET",
                CurrCode = "VND",
                Locale = "vn",
                ReturnUrl = "https://localhost:5001/payment/return",
                BaseUrl = "https://sandbox.vnpayment.vn/paymentv2/vpcpay.html",
                ExpiredTime = 15
            };

            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                    {"VNPay:Version", _testConfig.Version},
                    {"VNPay:Command", _testConfig.Command},
                    {"VNPay:TmnCode", _testConfig.TmnCode},
                    {"VNPay:HashSecret", _testConfig.HashSecret},
                    {"VNPay:CurrCode", _testConfig.CurrCode},
                    {"VNPay:Locale", _testConfig.Locale},
                    {"VNPay:ReturnUrl", _testConfig.ReturnUrl},
                    {"VNPay:BaseUrl", _testConfig.BaseUrl},
                    {"VNPay:ExpiredTime", _testConfig.ExpiredTime.ToString()}
                })
                .Build();

            _service = new VNPayService(configuration);
        }

        [Fact]
        public void CreatePaymentUrl_ValidParameters_ReturnsValidUrl()
        {
            // Arrange
            decimal amount = 100000;
            string orderInfo = "Test Order";
            string orderId = "TEST001";

            // Act
            var result = _service.CreatePaymentUrl(amount, orderInfo, orderId);

            // Assert
            Assert.NotNull(result);
            Assert.StartsWith(_testConfig.BaseUrl, result);
            Assert.Contains("vnp_Version=", result);
            Assert.Contains("vnp_Command=", result);
            Assert.Contains("vnp_TmnCode=", result);
            Assert.Contains("vnp_Amount=10000000", result); // amount * 100
            Assert.Contains("vnp_CurrCode=", result);
            Assert.Contains("vnp_Locale=", result);
            Assert.Contains("vnp_OrderInfo=", result);
            Assert.Contains("vnp_TxnRef=", result);
            Assert.Contains("vnp_SecureHash=", result);
        }

        [Fact]
        public void CreatePaymentUrl_ZeroAmount_ReturnsValidUrl()
        {
            // Arrange
            decimal amount = 0;
            string orderInfo = "Free Order";
            string orderId = "FREE001";

            // Act
            var result = _service.CreatePaymentUrl(amount, orderInfo, orderId);

            // Assert
            Assert.NotNull(result);
            Assert.Contains("vnp_Amount=0", result);
        }

        [Fact]
        public void CreatePaymentUrl_LargeAmount_ReturnsValidUrl()
        {
            // Arrange
            decimal amount = 999999999;
            string orderInfo = "Large Order";
            string orderId = "LARGE001";

            // Act
            var result = _service.CreatePaymentUrl(amount, orderInfo, orderId);

            // Assert
            Assert.NotNull(result);
            Assert.Contains("vnp_Amount=99999999900", result); // amount * 100
        }

        [Fact]
        public void CreatePaymentUrl_EmptyOrderInfo_ReturnsValidUrl()
        {
            // Arrange
            decimal amount = 50000;
            string orderInfo = "";
            string orderId = "EMPTY001";

            // Act
            var result = _service.CreatePaymentUrl(amount, orderInfo, orderId);

            // Assert
            Assert.NotNull(result);
            Assert.Contains("vnp_OrderInfo=", result);
        }

        [Fact]
        public void CreatePaymentUrl_SpecialCharactersInOrderInfo_ReturnsValidUrl()
        {
            // Arrange
            decimal amount = 50000;
            string orderInfo = "Test Order with @#$%^&*()";
            string orderId = "SPECIAL001";

            // Act
            var result = _service.CreatePaymentUrl(amount, orderInfo, orderId);

            // Assert
            Assert.NotNull(result);
            Assert.Contains("vnp_OrderInfo=", result);
            Assert.Contains("vnp_SecureHash=", result);
        }

        [Fact]
        public void CreatePaymentUrl_ContainsExpireDate()
        {
            // Arrange
            decimal amount = 100000;
            string orderInfo = "Test Order";
            string orderId = "EXP001";

            // Act
            var result = _service.CreatePaymentUrl(amount, orderInfo, orderId);

            // Assert
            Assert.NotNull(result);
            Assert.Contains("vnp_ExpireDate=", result);
        }

        [Fact]
        public void ValidateSignature_ValidSignature_ReturnsTrue()
        {
            // Arrange
            var vnpayData = new Dictionary<string, string>
            {
                {"vnp_Version", "2.1.0"},
                {"vnp_Command", "pay"},
                {"vnp_TmnCode", "TESTTMN"},
                {"vnp_Amount", "10000000"},
                {"vnp_CreateDate", DateTime.Now.ToString("yyyyMMddHHmmss")},
                {"vnp_CurrCode", "VND"},
                {"vnp_IpAddr", "127.0.0.1"},
                {"vnp_Locale", "vn"},
                {"vnp_OrderInfo", "Test Order"},
                {"vnp_OrderType", "billpayment"},
                {"vnp_ReturnUrl", "https://localhost:5001/payment/return"},
                {"vnp_TxnRef", "TEST001"},
                {"vnp_ExpireDate", DateTime.Now.AddMinutes(15).ToString("yyyyMMddHHmmss")}
            };

            // Create a valid signature
            var signData = string.Join("&", vnpayData
                .Where(x => !string.IsNullOrEmpty(x.Value) && x.Key.StartsWith("vnp_") && x.Key != "vnp_SecureHash" && x.Key != "vnp_SecureHashType")
                .OrderBy(x => x.Key)
                .Select(x => $"{x.Key}={x.Value}"));

            var hash = HmacSHA512(_testConfig.HashSecret, signData);
            vnpayData.Add("vnp_SecureHash", hash);

            // Act
            var result = _service.ValidateSignature(vnpayData, hash);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void ValidateSignature_InvalidSignature_ReturnsFalse()
        {
            // Arrange
            var vnpayData = new Dictionary<string, string>
            {
                {"vnp_Version", "2.1.0"},
                {"vnp_Command", "pay"},
                {"vnp_TmnCode", "TESTTMN"},
                {"vnp_Amount", "10000000"},
                {"vnp_CreateDate", DateTime.Now.ToString("yyyyMMddHHmmss")},
                {"vnp_CurrCode", "VND"},
                {"vnp_IpAddr", "127.0.0.1"},
                {"vnp_Locale", "vn"},
                {"vnp_OrderInfo", "Test Order"},
                {"vnp_OrderType", "billpayment"},
                {"vnp_ReturnUrl", "https://localhost:5001/payment/return"},
                {"vnp_TxnRef", "TEST001"},
                {"vnp_ExpireDate", DateTime.Now.AddMinutes(15).ToString("yyyyMMddHHmmss")},
                {"vnp_SecureHash", "INVALID_HASH"}
            };

            // Act
            var result = _service.ValidateSignature(vnpayData, "INVALID_HASH");

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void ValidateSignature_EmptyData_ReturnsFalse()
        {
            // Arrange
            var vnpayData = new Dictionary<string, string>();

            // Act
            var result = _service.ValidateSignature(vnpayData, "ANY_HASH");

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void ValidateSignature_NullData_ReturnsFalse()
        {
            // Arrange
            Dictionary<string, string> vnpayData = null;

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => _service.ValidateSignature(vnpayData, "ANY_HASH"));
        }

        [Fact]
        public void ValidateSignature_CaseInsensitive_ReturnsTrue()
        {
            // Arrange
            var vnpayData = new Dictionary<string, string>
            {
                {"vnp_Version", "2.1.0"},
                {"vnp_Command", "pay"},
                {"vnp_TmnCode", "TESTTMN"},
                {"vnp_Amount", "10000000"},
                {"vnp_CreateDate", DateTime.Now.ToString("yyyyMMddHHmmss")},
                {"vnp_CurrCode", "VND"},
                {"vnp_IpAddr", "127.0.0.1"},
                {"vnp_Locale", "vn"},
                {"vnp_OrderInfo", "Test Order"},
                {"vnp_OrderType", "billpayment"},
                {"vnp_ReturnUrl", "https://localhost:5001/payment/return"},
                {"vnp_TxnRef", "TEST001"},
                {"vnp_ExpireDate", DateTime.Now.AddMinutes(15).ToString("yyyyMMddHHmmss")}
            };

            var signData = string.Join("&", vnpayData
                .Where(x => !string.IsNullOrEmpty(x.Value) && x.Key.StartsWith("vnp_") && x.Key != "vnp_SecureHash" && x.Key != "vnp_SecureHashType")
                .OrderBy(x => x.Key)
                .Select(x => $"{x.Key}={x.Value}"));

            var hash = HmacSHA512(_testConfig.HashSecret, signData);
            vnpayData.Add("vnp_SecureHash", hash.ToUpper());

            // Act
            var result = _service.ValidateSignature(vnpayData, hash.ToUpper());

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void Constructor_InvalidConfiguration_ThrowsException()
        {
            // Arrange
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                    // Missing VNPay configuration
                })
                .Build();

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => new VNPayService(configuration));
        }

        [Fact]
        public void Constructor_ValidConfiguration_CreatesService()
        {
            // Arrange
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                    {"VNPay:Version", "2.1.0"},
                    {"VNPay:Command", "pay"},
                    {"VNPay:TmnCode", "TESTTMN"},
                    {"VNPay:HashSecret", "TESTHASHSECRET"},
                    {"VNPay:CurrCode", "VND"},
                    {"VNPay:Locale", "vn"},
                    {"VNPay:ReturnUrl", "https://localhost:5001/payment/return"},
                    {"VNPay:BaseUrl", "https://sandbox.vnpayment.vn/paymentv2/vpcpay.html"},
                    {"VNPay:ExpiredTime", "15"}
                })
                .Build();

            // Act
            var service = new VNPayService(configuration);

            // Assert
            Assert.NotNull(service);
        }

        private string HmacSHA512(string key, string inputData)
        {
            var hash = new System.Text.StringBuilder();
            var keyBytes = System.Text.Encoding.UTF8.GetBytes(key);
            var inputBytes = System.Text.Encoding.UTF8.GetBytes(inputData);
            using (var hmac = new System.Security.Cryptography.HMACSHA512(keyBytes))
            {
                var hashValue = hmac.ComputeHash(inputBytes);
                foreach (var theByte in hashValue)
                {
                    hash.Append(theByte.ToString("x2"));
                }
            }
            return hash.ToString();
        }
    }
} 