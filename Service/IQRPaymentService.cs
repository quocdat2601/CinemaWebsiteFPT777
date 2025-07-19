namespace MovieTheater.Service
{
    public interface IQRPaymentService
    {
        string GenerateQRCodeData(decimal amount, string orderInfo, string orderId);
        bool ValidatePayment(string orderId, string transactionId);
        string GetQRCodeImage(string qrData);
    }
} 