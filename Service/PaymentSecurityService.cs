using Microsoft.EntityFrameworkCore;
using MovieTheater.Models;
using MovieTheater.ViewModels;
using Newtonsoft.Json;

namespace MovieTheater.Service
{
    public class PaymentSecurityService : IPaymentSecurityService
    {
        private readonly MovieTheaterContext _context;
        private readonly ILogger<PaymentSecurityService> _logger;
        private readonly IVNPayService _vnPayService;

        public PaymentSecurityService(
            MovieTheaterContext context,
            ILogger<PaymentSecurityService> logger,
            IVNPayService vnPayService)
        {
            _context = context;
            _logger = logger;
            _vnPayService = vnPayService;
        }

        public PaymentValidationResult ValidatePaymentData(PaymentViewModel model, string userId)
        {
            try
            {
                // 1. Validate required fields
                if (string.IsNullOrEmpty(model.InvoiceId))
                {
                    return new PaymentValidationResult
                    {
                        IsValid = false,
                        ErrorMessage = "Invoice ID không được để trống",
                        ErrorCode = "INVALID_INVOICE_ID"
                    };
                }

                if (model.TotalAmount <= 0)
                {
                    return new PaymentValidationResult
                    {
                        IsValid = false,
                        ErrorMessage = "Số tiền thanh toán không hợp lệ",
                        ErrorCode = "INVALID_AMOUNT"
                    };
                }

                // 2. Validate invoice exists and belongs to user
                var invoice = _context.Invoices
                    .Include(i => i.Account)
                    .FirstOrDefault(i => i.InvoiceId == model.InvoiceId);

                if (invoice == null)
                {
                    return new PaymentValidationResult
                    {
                        IsValid = false,
                        ErrorMessage = "Hóa đơn không tồn tại",
                        ErrorCode = "INVOICE_NOT_FOUND"
                    };
                }

                if (invoice.AccountId != userId)
                {
                    _logger.LogWarning("User {UserId} attempted to access invoice {InvoiceId} belonging to {AccountId}", userId, model.InvoiceId, invoice.AccountId);
                    return new PaymentValidationResult
                    {
                        IsValid = false,
                        ErrorMessage = "Bạn không có quyền truy cập hóa đơn này",
                        ErrorCode = "UNAUTHORIZED_ACCESS"
                    };
                }

                // 3. Validate invoice status
                if (invoice.Status != InvoiceStatus.Incomplete)
                {
                    return new PaymentValidationResult
                    {
                        IsValid = false,
                        ErrorMessage = "Hóa đơn đã được xử lý",
                        ErrorCode = "INVOICE_ALREADY_PROCESSED"
                    };
                }

                // 4. Validate amount calculation
                var amountValidation = ValidateAmount(model.InvoiceId, model.TotalAmount);
                if (!amountValidation.IsValid)
                {
                    return amountValidation;
                }

                return new PaymentValidationResult { IsValid = true };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating payment data for invoice {InvoiceId}", model.InvoiceId);
                return new PaymentValidationResult
                {
                    IsValid = false,
                    ErrorMessage = "Có lỗi xảy ra khi xác thực dữ liệu thanh toán",
                    ErrorCode = "VALIDATION_ERROR"
                };
            }
        }

        public PaymentValidationResult ValidateAmount(string invoiceId, decimal amount)
        {
            try
            {
                var invoice = _context.Invoices
                    .Include(i => i.ScheduleSeats)
                    .ThenInclude(ss => ss.Seat)
                    .ThenInclude(s => s.SeatType)
                    .Include(i => i.MovieShow)
                    .FirstOrDefault(i => i.InvoiceId == invoiceId);

                if (invoice == null)
                {
                    return new PaymentValidationResult
                    {
                        IsValid = false,
                        ErrorMessage = "Hóa đơn không tồn tại",
                        ErrorCode = "INVOICE_NOT_FOUND"
                    };
                }

                // Calculate expected amount
                decimal expectedAmount = CalculateExpectedAmount(invoice);

                // Allow small tolerance for rounding differences (0.01 VND)
                if (Math.Abs(amount - expectedAmount) > 0.01m)
                {
                    _logger.LogWarning("Amount mismatch for invoice {InvoiceId}: Expected {ExpectedAmount}, Received {Amount}", invoiceId, expectedAmount, amount);
                    return new PaymentValidationResult
                    {
                        IsValid = false,
                        ErrorMessage = "Số tiền thanh toán không khớp với giá trị thực tế",
                        ErrorCode = "AMOUNT_MISMATCH"
                    };
                }

                return new PaymentValidationResult { IsValid = true };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating amount for invoice {InvoiceId}", invoiceId);
                return new PaymentValidationResult
                {
                    IsValid = false,
                    ErrorMessage = "Có lỗi xảy ra khi xác thực số tiền",
                    ErrorCode = "AMOUNT_VALIDATION_ERROR"
                };
            }
        }

        public PaymentValidationResult ValidatePaymentResponse(IDictionary<string, string> vnpayData)
        {
            try
            {
                // 1. Validate required fields
                if (!vnpayData.ContainsKey("vnp_TxnRef") || !vnpayData.ContainsKey("vnp_ResponseCode"))
                {
                    return new PaymentValidationResult
                    {
                        IsValid = false,
                        ErrorMessage = "Dữ liệu phản hồi từ VNPay không hợp lệ",
                        ErrorCode = "INVALID_VNPAY_RESPONSE"
                    };
                }

                // 2. Validate signature
                if (!vnpayData.ContainsKey("vnp_SecureHash"))
                {
                    return new PaymentValidationResult
                    {
                        IsValid = false,
                        ErrorMessage = "Chữ ký bảo mật không hợp lệ",
                        ErrorCode = "INVALID_SIGNATURE"
                    };
                }

                var vnpSecureHash = vnpayData["vnp_SecureHash"];
                if (!_vnPayService.ValidateSignature(vnpayData, vnpSecureHash))
                {
                    _logger.LogWarning("Invalid VNPay signature for transaction {TransactionRef}", vnpayData["vnp_TxnRef"]);
                    return new PaymentValidationResult
                    {
                        IsValid = false,
                        ErrorMessage = "Chữ ký bảo mật không hợp lệ",
                        ErrorCode = "INVALID_SIGNATURE"
                    };
                }

                // 3. Validate response code
                var responseCode = vnpayData["vnp_ResponseCode"];
                if (responseCode != "00")
                {
                    return new PaymentValidationResult
                    {
                        IsValid = false,
                        ErrorMessage = $"Giao dịch thất bại với mã lỗi: {responseCode}",
                        ErrorCode = $"VNPAY_ERROR_{responseCode}"
                    };
                }

                // 4. Validate transaction reference exists
                var txnRef = vnpayData["vnp_TxnRef"];
                var invoice = _context.Invoices.FirstOrDefault(i => i.InvoiceId == txnRef);
                if (invoice == null)
                {
                    return new PaymentValidationResult
                    {
                        IsValid = false,
                        ErrorMessage = "Không tìm thấy hóa đơn tương ứng",
                        ErrorCode = "INVOICE_NOT_FOUND"
                    };
                }

                return new PaymentValidationResult { IsValid = true };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating VNPay response");
                return new PaymentValidationResult
                {
                    IsValid = false,
                    ErrorMessage = "Có lỗi xảy ra khi xác thực phản hồi từ VNPay",
                    ErrorCode = "VNPAY_VALIDATION_ERROR"
                };
            }
        }

        private decimal CalculateExpectedAmount(Invoice invoice)
        {
            decimal totalAmount = 0;

            // Parse promotion discount JSON
            int seatPromotionDiscount = 0;
            if (!string.IsNullOrEmpty(invoice.PromotionDiscount) && invoice.PromotionDiscount != "0")
            {
                try
                {
                    var promoObj = JsonConvert.DeserializeObject<dynamic>(invoice.PromotionDiscount);
                    seatPromotionDiscount = (int)(promoObj.seat ?? 0);
                }
                catch { seatPromotionDiscount = 0; }
            }

            // Calculate seat prices
            foreach (var scheduleSeat in invoice.ScheduleSeats)
            {
                if (scheduleSeat.Seat?.SeatType != null)
                {
                    decimal seatPrice = scheduleSeat.Seat.SeatType.PricePercent;
                    // Apply promotion discount if available
                    if (seatPromotionDiscount > 0)
                    {
                        decimal discount = Math.Round(seatPrice * (seatPromotionDiscount / 100m));
                        seatPrice -= discount;
                    }
                    totalAmount += seatPrice;
                }
            }

            // Subtract used score value
            if (invoice.UseScore.HasValue && invoice.UseScore.Value > 0)
            {
                totalAmount -= (invoice.UseScore.Value * 1000); // 1 point = 1000 VND
            }

            // Apply voucher discount if available
            if (!string.IsNullOrEmpty(invoice.VoucherId))
            {
                var voucher = _context.Vouchers.FirstOrDefault(v => v.VoucherId == invoice.VoucherId);
                if (voucher != null)
                {
                    totalAmount -= voucher.Value;
                }
            }

            return Math.Max(0, totalAmount);
        }
    }
}