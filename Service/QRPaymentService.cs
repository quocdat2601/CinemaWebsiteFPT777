using MovieTheater.Models;
using QRCoder;
using System.Security.Cryptography;
using System.Text;
using System.Globalization;

namespace MovieTheater.Service
{
    public class QRPaymentService : IQRPaymentService
    {
        private readonly QRPaymentConfig _config;
        private readonly ILogger<QRPaymentService> _logger;
        private readonly VNPayConfig _vnPayConfig;

        public QRPaymentService(IConfiguration configuration, ILogger<QRPaymentService> logger)
        {
            _config = configuration.GetSection("QRPayment").Get<QRPaymentConfig>()
                      ?? throw new InvalidOperationException("QR Payment configuration is missing or invalid.");
            _vnPayConfig = configuration.GetSection("VNPay").Get<VNPayConfig>()
                          ?? throw new InvalidOperationException("VNPay configuration is missing or invalid.");
            _logger = logger;
        }

        public string GenerateQRCodeData(decimal amount, string orderInfo, string orderId)
        {
            try
            {
                // Tạo nội dung QR code theo chuẩn VietQR
                var qrContent = $"https://api.vietqr.io/image/{_config.BankCode}/{_config.AccountNumber}?amount={amount}&addInfo={orderId}&accountName={_config.AccountName}";
                
                _logger.LogInformation($"QR code data generated for order {orderId}: {qrContent}");
                return qrContent;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating QR code data");
                throw;
            }
        }

        /// <summary>
        /// Tạo QR code theo chuẩn VietQR thực tế
        /// </summary>
        public string GenerateVietQRCode(decimal amount, string orderInfo, string orderId)
        {
            try
            {
                // VietQR format: https://api.vietqr.io/image/{bankId}/{accountNo}/{amount}/{description}
                // Sử dụng VietQR API để tạo QR code thực tế
                var bankId = "VCB"; // Vietcombank
                var accountNo = "1234567890"; // Demo account
                var description = Uri.EscapeDataString(orderInfo);
                
                var vietQRUrl = $"https://api.vietqr.io/image/{bankId}/{accountNo}/{amount}/{description}";
                _logger.LogInformation("VietQR URL generated: {URL}", vietQRUrl);
                
                return vietQRUrl;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating VietQR code");
                return GenerateSimpleQRCode($"PAYMENT_{orderId}_{amount}");
            }
        }

        /// <summary>
        /// Tạo QR code VNPAY theo chuẩn thực tế
        /// </summary>
        public string GenerateVNPayQRCode(decimal amount, string orderInfo, string orderId)
        {
            try
            {
                // VNPAY QR code format
                var vnpUrl = "https://sandbox.vnpayment.vn/paymentv2/vpcpay.html";
                var vnpParams = new Dictionary<string, string>
                {
                    ["vnp_Version"] = "2.1.0",
                    ["vnp_Command"] = "pay",
                    ["vnp_TmnCode"] = "VVHLKKC6", // Demo TMN Code
                    ["vnp_Amount"] = (amount * 100).ToString(), // Convert to smallest currency unit
                    ["vnp_CurrCode"] = "VND",
                    ["vnp_TxnRef"] = orderId,
                    ["vnp_OrderInfo"] = orderInfo,
                    ["vnp_OrderType"] = "billpayment",
                    ["vnp_ReturnUrl"] = "https://localhost:7201/api/Payment/vnpay-return",
                    ["vnp_IpAddr"] = "127.0.0.1",
                    ["vnp_CreateDate"] = DateTime.Now.ToString("yyyyMMddHHmmss")
                };

                // Tạo signature
                var signature = GenerateVNPaySignature(vnpParams);
                vnpParams["vnp_SecureHash"] = signature;

                // Tạo URL hoàn chỉnh
                var queryString = string.Join("&", vnpParams.Select(kvp => $"{kvp.Key}={Uri.EscapeDataString(kvp.Value)}"));
                var fullUrl = $"{vnpUrl}?{queryString}";

                _logger.LogInformation("VNPAY QR URL generated: {URL}", fullUrl);
                return fullUrl;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating VNPAY QR code");
                return GenerateSimpleQRCode($"PAYMENT_{orderId}_{amount}");
            }
        }

        /// <summary>
        /// Tạo signature cho VNPAY
        /// </summary>
        private string GenerateVNPaySignature(Dictionary<string, string> parameters)
        {
            try
            {
                // Sắp xếp parameters theo key
                var sortedParams = parameters
                    .Where(p => !string.IsNullOrEmpty(p.Value) && p.Key != "vnp_SecureHash")
                    .OrderBy(p => p.Key)
                    .ToList();

                // Tạo query string
                var queryString = string.Join("&", sortedParams.Select(p => $"{p.Key}={p.Value}"));

                // Tạo HMAC-SHA512 signature
                var secretKey = "VVHLKKC6"; // Demo secret key
                var hmac = new System.Security.Cryptography.HMACSHA512(Encoding.UTF8.GetBytes(secretKey));
                var data = Encoding.UTF8.GetBytes(queryString);
                var hash = hmac.ComputeHash(data);
                var signature = BitConverter.ToString(hash).Replace("-", "").ToLower();

                _logger.LogInformation("VNPAY signature generated: {Signature}", signature);
                return signature;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating VNPAY signature");
                return "demo_signature";
            }
        }

        private string HmacSHA512(string key, string inputData)
        {
            var hash = new StringBuilder();
            var keyBytes = Encoding.UTF8.GetBytes(key);
            var inputBytes = Encoding.UTF8.GetBytes(inputData);
            using (var hmac = new HMACSHA512(keyBytes))
            {
                var hashBytes = hmac.ComputeHash(inputBytes);
                foreach (var b in hashBytes)
                {
                    hash.Append(b.ToString("x2"));
                }
            }
            return hash.ToString();
        }

        public bool ValidatePayment(string orderId, string transactionId)
        {
            try
            {
                // Trong thực tế, bạn sẽ kiểm tra với ngân hàng
                // Ở đây chỉ là demo
                _logger.LogInformation($"Validating payment for order {orderId}, transaction {transactionId}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating payment");
                return false;
            }
        }

        public string GetQRCodeImage(string qrData)
        {
            try
            {
                // Tạo QR code đơn giản bằng cách sử dụng Google Charts API
                var encodedData = Uri.EscapeDataString(qrData);
                var qrUrl = $"https://chart.googleapis.com/chart?cht=qr&chs=300x300&chl={encodedData}&chld=L|0";
                
                _logger.LogInformation($"QR code image URL generated: {qrUrl}");
                return qrUrl;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating QR code image");
                // Fallback: trả về một QR code demo đơn giản
                return "https://chart.googleapis.com/chart?cht=qr&chs=300x300&chl=DEMO_QR_CODE&chld=L|0";
            }
        }

        public string GenerateSimpleQRCode(string text)
        {
            try
            {
                // Tạo QR code đơn giản cho demo sử dụng QR Server API
                var encodedText = Uri.EscapeDataString(text);
                var qrUrl = $"https://api.qrserver.com/v1/create-qr-code/?size=300x300&data={encodedText}";
                
                _logger.LogInformation($"Simple QR code generated for text: {text}");
                return qrUrl;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating simple QR code");
                return "https://api.qrserver.com/v1/create-qr-code/?size=300x300&data=DEMO_QR_CODE";
            }
        }
    }


} 