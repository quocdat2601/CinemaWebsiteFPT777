using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using MovieTheater.Service;
using System.Text.Json;
using System.Threading.Tasks;
using MovieTheater.ViewModels;
using System.Security.Claims;

namespace MovieTheater.Middleware
{
    public class PaymentSecurityMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<PaymentSecurityMiddleware> _logger;

        public PaymentSecurityMiddleware(RequestDelegate next, ILogger<PaymentSecurityMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            if (IsPaymentRequest(context) && context.Request.Method == "POST")
            {
                var requestInfo = new
                {
                    Path = context.Request.Path,
                    Method = context.Request.Method,
                    UserAgent = context.Request.Headers["User-Agent"].ToString(),
                    IPAddress = GetClientIPAddress(context),
                    Timestamp = DateTime.UtcNow
                };

                _logger.LogInformation("Payment request detected: {@RequestInfo}", requestInfo);

                // Đọc body request (giả định là JSON PaymentRequest)
                context.Request.EnableBuffering();
                string body = string.Empty;
                using (var reader = new StreamReader(context.Request.Body, leaveOpen: true))
                {
                    body = await reader.ReadToEndAsync();
                    context.Request.Body.Position = 0;
                }

                // Resolve service kiểm tra bảo mật
                var paymentSecurityService = context.RequestServices.GetService(typeof(IPaymentSecurityService)) as IPaymentSecurityService;
                if (paymentSecurityService == null)
                {
                    _logger.LogWarning("Không resolve được IPaymentSecurityService. Bỏ qua kiểm tra bảo mật payment!");
                    await _next(context);
                    return;
                }

                // Deserialize body thành PaymentRequest (giả định có class này)
                PaymentRequest? paymentRequest = null;
                try
                {
                    paymentRequest = JsonSerializer.Deserialize<PaymentRequest>(body, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Không parse được PaymentRequest từ body");
                }

                if (paymentRequest != null)
                {
                    // Lấy userId từ ClaimsPrincipal
                    var userId = context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                    if (string.IsNullOrEmpty(userId))
                    {
                        _logger.LogWarning("User not authenticated for payment request");
                        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                        await context.Response.WriteAsync("User not authenticated");
                        return;
                    }

                    // Map PaymentRequest sang PaymentViewModel
                    var paymentViewModel = new PaymentViewModel
                    {
                        TotalAmount = paymentRequest.Amount,
                        OrderInfo = paymentRequest.OrderInfo,
                        InvoiceId = paymentRequest.OrderId
                    };

                    var result = paymentSecurityService.ValidatePaymentData(paymentViewModel, userId);
                    if (!result.IsValid)
                    {
                        _logger.LogWarning("Payment validation failed: {Reason}", result.ErrorMessage);
                        context.Response.StatusCode = StatusCodes.Status400BadRequest;
                        await context.Response.WriteAsync($"Payment validation failed: {result.ErrorMessage}");
                        return;
                    }
                }
                else
                {
                    _logger.LogWarning("Không thể kiểm tra payment vì không parse được PaymentRequest");
                }
            }

            await _next(context);
        }

        private bool IsPaymentRequest(HttpContext context)
        {
            var path = context.Request.Path.Value?.ToLower();
            return path != null && (
                path.Contains("/payment") ||
                path.Contains("/booking/processpayment") ||
                path.Contains("/api/payment") ||
                path.Contains("/vnpay-return") ||
                path.Contains("/vnpay-ipn")
            );
        }

        private string GetClientIPAddress(HttpContext context)
        {
            var forwardedHeader = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
            if (!string.IsNullOrEmpty(forwardedHeader))
            {
                return forwardedHeader.Split(',')[0].Trim();
            }
            var realIPHeader = context.Request.Headers["X-Real-IP"].FirstOrDefault();
            if (!string.IsNullOrEmpty(realIPHeader))
            {
                return realIPHeader;
            }
            return context.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
        }
    }
} 