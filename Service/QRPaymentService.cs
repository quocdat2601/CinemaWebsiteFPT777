using MovieTheater.Models;
using QRCoder;

namespace MovieTheater.Service
{
    public class QRPaymentService : IQRPaymentService
    {
        private readonly QRPaymentConfig _config;
        private readonly ILogger<QRPaymentService> _logger;

        public QRPaymentService(IConfiguration configuration, ILogger<QRPaymentService> logger)
        {
            _config = configuration.GetSection("QRPayment").Get<QRPaymentConfig>()
                      ?? throw new InvalidOperationException("QR Payment configuration is missing or invalid.");
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
                return qrUrl;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating QR code image");
                throw;
            }
        }
    }
} 