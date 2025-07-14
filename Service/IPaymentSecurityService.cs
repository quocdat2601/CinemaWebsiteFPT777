using MovieTheater.ViewModels;

namespace MovieTheater.Service
{
    public interface IPaymentSecurityService
    {
        /// <summary>
        /// Validate payment data từ client
        /// </summary>
        /// <param name="model">Payment data từ client</param>
        /// <param name="userId">ID của user hiện tại</param>
        /// <returns>Validation result</returns>
        PaymentValidationResult ValidatePaymentData(PaymentViewModel model, string userId);

        /// <summary>
        /// Validate amount calculation
        /// </summary>
        /// <param name="invoiceId">Invoice ID</param>
        /// <param name="amount">Amount từ client</param>
        /// <returns>Validation result</returns>
        PaymentValidationResult ValidateAmount(string invoiceId, decimal amount);

        /// <summary>
        /// Validate payment response từ VNPay
        /// </summary>
        /// <param name="vnpayData">Data từ VNPay</param>
        /// <returns>Validation result</returns>
        PaymentValidationResult ValidatePaymentResponse(IDictionary<string, string> vnpayData);
    }

    public class PaymentValidationResult
    {
        public bool IsValid { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
        public string ErrorCode { get; set; } = string.Empty;
    }
} 