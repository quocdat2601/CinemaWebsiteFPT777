using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

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
            // Log payment-related requests
            if (IsPaymentRequest(context))
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

                // Check for suspicious patterns
                if (IsSuspiciousRequest(context))
                {
                    _logger.LogWarning("Suspicious payment request detected: {@RequestInfo}", requestInfo);
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

        private bool IsSuspiciousRequest(HttpContext context)
        {
            // Check for suspicious patterns
            var userAgent = context.Request.Headers["User-Agent"].ToString().ToLower();
            var referer = context.Request.Headers["Referer"].ToString();

            // Check for missing or suspicious User-Agent
            if (string.IsNullOrEmpty(userAgent) || userAgent.Contains("bot") || userAgent.Contains("crawler"))
            {
                return true;
            }

            // Check for suspicious referer patterns
            if (!string.IsNullOrEmpty(referer) && (
                referer.Contains("suspicious-domain") ||
                referer.Contains("malicious-site")
            ))
            {
                return true;
            }

            // Check for rapid requests (basic rate limiting check)
            // This is a simplified check - in production, use proper rate limiting
            return false;
        }

        private string GetClientIPAddress(HttpContext context)
        {
            // Get the real IP address considering proxies
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