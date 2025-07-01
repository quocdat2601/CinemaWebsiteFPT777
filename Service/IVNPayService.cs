namespace MovieTheater.Service
{
    public interface IVNPayService
    {
        string CreatePaymentUrl(decimal amount, string orderInfo, string orderId);
        bool ValidateSignature(IDictionary<string, string> vnpayData, string vnpSecureHash);
    }
}