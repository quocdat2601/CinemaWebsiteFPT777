namespace MovieTheater.Service
{
    public interface IQRPaymentService
    {
        string GenerateQRCodeData(decimal amount, string orderInfo, string orderId);
        string GenerateVNPayQRCode(decimal amount, string orderInfo, string orderId);
        string GenerateVietQRCode(decimal amount, string orderInfo, string orderId);
        bool ValidatePayment(string orderId, string transactionId);
        string GetQRCodeImage(string qrData);
        string GenerateSimpleQRCode(string text);
        string GenerateMoMoQRCodeBase64(string phoneNumber);
        string GeneratePayOSQRCode(decimal amount, string orderInfo, string orderId);
    }
}