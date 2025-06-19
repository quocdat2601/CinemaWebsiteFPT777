using System;
using System.Collections.Generic;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using MovieTheater.Models;
using System.Linq;
using System.Web;

namespace MovieTheater.Service
{
    public class VNPayService
    {
        private readonly VNPayConfig _config;

        public VNPayService(IConfiguration configuration)
        {
            _config = configuration.GetSection("VNPay").Get<VNPayConfig>()
                      ?? throw new InvalidOperationException("VNPay configuration is missing or invalid.");
        }

        public string CreatePaymentUrl(decimal amount, string orderInfo, string orderId)
        {
            var vnpay = new SortedList<string, string>(new VNPayCompare());
            var createDate = DateTime.Now;

            vnpay.Add("vnp_Version", _config.Version);
            vnpay.Add("vnp_Command", _config.Command);
            vnpay.Add("vnp_TmnCode", _config.TmnCode);
            vnpay.Add("vnp_Amount", (amount * 100).ToString()); // Số tiền * 100
            vnpay.Add("vnp_CreateDate", createDate.ToString("yyyyMMddHHmmss"));
            vnpay.Add("vnp_CurrCode", _config.CurrCode);
            vnpay.Add("vnp_IpAddr", "127.0.0.1"); // IP của khách hàng
            vnpay.Add("vnp_Locale", _config.Locale);
            vnpay.Add("vnp_OrderInfo", orderInfo);
            // Mã danh mục hàng hóa. Các giá trị có thể:
            // billpayment: Thanh toán hóa đơn
            // topup: Nạp tiền
            // fashion: Thời trang
            // other: Khác
            vnpay.Add("vnp_OrderType", "billpayment"); 
            vnpay.Add("vnp_ReturnUrl", _config.ReturnUrl);
            vnpay.Add("vnp_TxnRef", orderId); // Mã đơn hàng
            vnpay.Add("vnp_ExpireDate", createDate.AddMinutes(_config.ExpiredTime).ToString("yyyyMMddHHmmss")); // Thời gian hết hạn

            // Build chuỗi ký đúng chuẩn VNPay
            var signData = string.Join("&", vnpay
                .Where(x => !string.IsNullOrEmpty(x.Value) && x.Key.StartsWith("vnp_") && x.Key != "vnp_SecureHash" && x.Key != "vnp_SecureHashType")
                .OrderBy(x => x.Key)
                .Select(x => x.Key == "vnp_ReturnUrl"
                    ? $"{x.Key}={Uri.EscapeDataString(x.Value)}"
                    : $"{x.Key}={HttpUtility.UrlEncode(x.Value, Encoding.UTF8)}"
                ));
            var hash = HmacSHA512(_config.HashSecret, signData);
            vnpay.Add("vnp_SecureHash", hash);

            // Khi tạo URL mới encode value
            var queryString = string.Join("&", vnpay.Select(kvp =>
                kvp.Key == "vnp_ReturnUrl"
                    ? $"{kvp.Key}={Uri.EscapeDataString(kvp.Value)}"
                    : $"{kvp.Key}={HttpUtility.UrlEncode(kvp.Value, Encoding.UTF8)}"
            ));
            var paymentUrl = $"{_config.BaseUrl}?{queryString}";

            // Thêm log để debug
            Console.WriteLine("=== VNPAY DEBUG INFO ===");
            Console.WriteLine("SIGN DATA: " + signData);
            Console.WriteLine("HASH SECRET: " + _config.HashSecret);
            Console.WriteLine("HASH: " + hash);
            Console.WriteLine("PAYMENT URL: " + paymentUrl);
            Console.WriteLine("======================");

            return paymentUrl;
        }

        public bool ValidateSignature(IDictionary<string, string> vnpayData, string vnpSecureHash)
        {
            // Build lại chuỗi ký từ dữ liệu callback
            var signData = string.Join("&", vnpayData
                .Where(x => !string.IsNullOrEmpty(x.Value) && x.Key.StartsWith("vnp_") && x.Key != "vnp_SecureHash" && x.Key != "vnp_SecureHashType")
                .OrderBy(x => x.Key)
                .Select(x => $"{x.Key}={x.Value}"));
            var hash = HmacSHA512(_config.HashSecret, signData);
            Console.WriteLine("SIGN DATA CALLBACK: " + signData);
            Console.WriteLine("HASH CALLBACK: " + hash);
            Console.WriteLine("VNPAY HASH: " + vnpSecureHash);
            return hash.Equals(vnpSecureHash, StringComparison.InvariantCultureIgnoreCase);
        }

        private string HmacSHA512(string key, string inputData)
        {
            var hash = new StringBuilder();
            var keyBytes = Encoding.UTF8.GetBytes(key);
            var inputBytes = Encoding.UTF8.GetBytes(inputData);
            using (var hmac = new HMACSHA512(keyBytes))
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

    public class VNPayCompare : IComparer<string>
    {
        private readonly CompareInfo _compareInfo;

        public VNPayCompare()
        {
            _compareInfo = CompareInfo.GetCompareInfo("en-US");
        }

        public int Compare(string? x, string? y)
        {
            if (x == y) return 0;
            if (x == null) return -1;
            if (y == null) return 1;
            return _compareInfo.Compare(x, y, CompareOptions.Ordinal);
        }
    }
} 