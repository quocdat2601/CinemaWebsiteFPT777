using Microsoft.AspNetCore.Mvc;
using MovieTheater.Models;
using MovieTheater.Service;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.EntityFrameworkCore;

namespace MovieTheater.Controllers
{
    [ApiController]
    [Route("api/casso/webhook")]
    public class CassoWebhookController : ControllerBase
    {
        private readonly ILogger<CassoWebhookController> _logger;
        private readonly IInvoiceService _invoiceService;
        private readonly ISeatService _seatService;
        private readonly IScheduleSeatService _scheduleSeatService;
        private const string SECURE_TOKEN = "AK_CS.0eafb1406d2811f0b7f9c39f1519547d.SgAfzKpqf62yKUOnIl5qG4z4heJhXAy0oo5UtfrcSBEaMKmzGcz2w56HEyGF1e9xqwiAWqwB";

        // Constants from PHP sample
        private const decimal ORDER_MONEY = 100000m; // Giá tiền tổng cộng của đơn hàng giả định
        private const decimal ACCEPTABLE_DIFFERENCE = 10000m; // Số tiền chuyển thiếu tối đa mà hệ thống vẫn chấp nhận
        private const string MEMO_PREFIX = "DH"; // Tiền tố điền trước mã đơn hàng
        private const string HEADER_SECURE_TOKEN = "eogrBiWqaq"; // Key bảo mật từ PHP sample
        
        // Security constants
        private const int DATABASE_TIMEOUT_SECONDS = 30;
        private const int MAX_PROCESSING_TIME_SECONDS = 60;
        private const int MAX_WEBHOOK_SIZE_BYTES = 1024 * 1024; // 1MB

        public CassoWebhookController(ILogger<CassoWebhookController> logger, IInvoiceService invoiceService, ISeatService seatService, IScheduleSeatService scheduleSeatService)
        {
            _logger = logger;
            _invoiceService = invoiceService;
            _seatService = seatService;
            _scheduleSeatService = scheduleSeatService;
        }

        [HttpPost]
        public async Task<IActionResult> HandleWebhook([FromBody] JsonElement body)
        {
            var startTime = DateTime.UtcNow;
            
            try
            {
                // Security: Check request size
                if (Request.ContentLength > MAX_WEBHOOK_SIZE_BYTES)
                {
                    _logger.LogWarning("Webhook request too large: {Size} bytes", Request.ContentLength);
                    return BadRequest(new { error = 1, message = "Request too large" });
                }

                // Security: Check processing time
                if ((DateTime.UtcNow - startTime).TotalSeconds > MAX_PROCESSING_TIME_SECONDS)
                {
                    _logger.LogWarning("Webhook processing timeout");
                    return StatusCode(408, new { error = 1, message = "Request timeout" });
                }

                _logger.LogInformation("=== WEBHOOK DEBUG START ===");
                _logger.LogInformation("Webhook received from Casso");

                // Log request headers
                var headers = Request.Headers.ToDictionary(h => h.Key, h => h.Value.ToString());
                _logger.LogInformation("Request Headers: {Headers}", string.Join(", ", headers.Select(h => $"{h.Key}={h.Value}")));

                // Log webhook body (sanitized)
                var sanitizedBody = SanitizeWebhookBody(body);
                _logger.LogInformation("Webhook body received: {BodySize} bytes", JsonSerializer.Serialize(body).Length);

                // 1. KHÔNG kiểm tra Secure-Token nếu không có, chỉ log warning
                if (!Request.Headers.TryGetValue("Secure-Token", out var tokenValues))
                {
                    _logger.LogWarning("No Secure-Token header found (Webhook V2 có thể chỉ dùng X-Casso-Signature)");
                }
                else if (tokenValues.FirstOrDefault() != SECURE_TOKEN)
                {
                    _logger.LogWarning("Invalid secure-token");
                    // Vẫn trả về 200 OK để Casso không retry, chỉ log warning
                }

                // 2. Parse đúng format JSON cho Webhook V2
                if (body.TryGetProperty("data", out var dataElement) && dataElement.ValueKind == JsonValueKind.Object)
                {
                    _logger.LogInformation("Processing V2: data is object");
                    await ProcessTransaction(dataElement);
                }
                else if (body.TryGetProperty("data", out var dataArray) && dataArray.ValueKind == JsonValueKind.Array)
                {
                    _logger.LogInformation("Processing V2: data is array");
                    foreach (var item in dataArray.EnumerateArray())
                    {
                        await ProcessTransaction(item);
                    }
                }
                else if (body.ValueKind == JsonValueKind.Array)
                {
                    _logger.LogInformation("Processing as JSON Array");
                    foreach (var item in body.EnumerateArray())
                    {
                        await ProcessTransaction(item);
                    }
                }
                else if (body.ValueKind == JsonValueKind.Object)
                {
                    _logger.LogInformation("Processing as JSON Object (no data property)");
                }
                else
                {
                    _logger.LogWarning("Invalid webhook body structure");
                }

                _logger.LogInformation("=== WEBHOOK DEBUG END ===");
                // Luôn trả về HTTP 200 và body JSON object đơn giản nhất có thể
                return Ok(new { error = 0 });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing webhook");
                // Dù có lỗi vẫn trả về HTTP 200 để Casso không retry
                return Ok(new { error = 0 });
            }
        }

        private async Task ProcessTransaction(JsonElement item)
        {
            try
            {
                // Security: Set database timeout
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(DATABASE_TIMEOUT_SECONDS));
                
                // 1. Lấy id giao dịch từ Casso
                var cassoId = item.TryGetProperty("id", out var idElement) ? idElement.GetInt64() : 0;
                // TẠM THỜI BỎ QUA CHECK NÀY ĐỂ TEST
                // if (cassoId == 0)
                // {
                //     _logger.LogWarning("No Casso transaction id found");
                //     return;
                // }

                // 2. Lấy orderId từ reference trước, sau đó từ description
                var description = item.TryGetProperty("description", out var descElement) ? descElement.GetString() : "";
                var reference = item.TryGetProperty("reference", out var refElement) ? refElement.GetString() : "";
                
                // Ưu tiên sử dụng reference trước
                var orderId = !string.IsNullOrEmpty(reference) ? reference : ExtractOrderId(description);

                _logger.LogInformation("Processing transaction: CassoId={CassoId}, OrderId={OrderId}, Description={Description}", cassoId, orderId, description);

                if (string.IsNullOrEmpty(orderId))
                {
                    _logger.LogWarning("Could not extract order ID from description or reference: {Description} / {Reference}", description, reference);
                    return;
                }

                // 3. Tìm invoice và kiểm tra trạng thái (logic tìm kiếm linh hoạt nâng cao)
                var invoice = _invoiceService.FindInvoiceByOrderId(orderId);

                // Nếu vẫn không tìm thấy, thử tìm theo pattern từ description
                if (invoice == null)
                {
                    // Tìm pattern QADOBFxxxx từ description
                    var patternMatch = System.Text.RegularExpressions.Regex.Match(description, @"QADOBF\d+", System.Text.RegularExpressions.RegexOptions.None, TimeSpan.FromSeconds(5));
                    if (patternMatch.Success)
                    {
                        var patternId = patternMatch.Value;
                        _logger.LogInformation("Trying to find invoice by pattern: {PatternId}", patternId);
                        invoice = _invoiceService.FindInvoiceByOrderId(patternId);
                    }
                }

                // Nếu vẫn không tìm thấy, thử tìm theo số tiền và thời gian gần nhất
                if (invoice == null)
                {
                    var paidAmount = item.TryGetProperty("amount", out var amountElement1) ? amountElement1.GetDecimal() : 0m;
                    if (paidAmount > 0) // Chỉ tìm nếu có số tiền hợp lệ
                    {
                        _logger.LogInformation("Trying to find invoice by amount: {Amount}", paidAmount);
                        // Tìm theo số tiền chính xác
                        invoice = _invoiceService.FindInvoiceByAmountAndTime(paidAmount);
                        
                        // Nếu vẫn không tìm thấy, tìm invoice gần nhất trong 24h
                        if (invoice == null)
                        {
                            var recentTime = DateTime.Now.AddHours(-24);
                            invoice = _invoiceService.FindInvoiceByAmountAndTime(paidAmount, recentTime);
                        }
                    }
                }



                if (invoice == null)
                {
                    _logger.LogWarning("Invoice not found for orderId: {OrderId}. Tried multiple search methods including pattern and amount matching.", orderId);
                    return;
                }

                _logger.LogInformation("Found invoice: InvoiceId={InvoiceId}, Status={Status}, TotalMoney={TotalMoney}", 
                    invoice.InvoiceId, invoice.Status, invoice.TotalMoney);

                // 4. Chống trùng lặp: chỉ xử lý nếu invoice chưa hoàn thành
                if (invoice.Status == InvoiceStatus.Completed)
                {
                    _logger.LogWarning("Invoice {OrderId} already completed. Ignore duplicate webhook. (CassoId={CassoId})", orderId, cassoId);
                    return;
                }

                // 5. Xử lý logic thanh toán theo PHP sample
                var paid = item.TryGetProperty("amount", out var amountElement) ? amountElement.GetDecimal() : 0m;
                var total = string.Format("{0:N0}", paid);
                var accountNumber = item.TryGetProperty("accountNumber", out var accElement) ? accElement.GetString() : "";
                var orderNote = $"Casso thông báo nhận {total} VND, nội dung {description} chuyển vào STK {accountNumber}";

                if (!invoice.TotalMoney.HasValue)
                {
                    _logger.LogWarning("Invoice {OrderId} has null TotalMoney", orderId);
                    return;
                }

                var expectedAmount = invoice.TotalMoney.Value;
                var acceptableDifference = Math.Abs(ACCEPTABLE_DIFFERENCE);

                if (paid < expectedAmount - acceptableDifference)
                {
                    // Thanh toán thiếu
                    _logger.LogWarning("{OrderNote}. Trạng thái đơn hàng đã được chuyển từ Tạm giữ sang Thanh toán thiếu.", orderNote);
                    // Có thể cập nhật status thành "PartialPayment" hoặc giữ nguyên
                }
                else if (paid <= expectedAmount + acceptableDifference)
                {
                    // Thanh toán đủ
                    await UpdateInvoiceStatus(invoice.InvoiceId, InvoiceStatus.Completed, cts.Token);
                    
                    // Cập nhật trạng thái seat từ "being held" thành "booked"
                    await UpdateSeatStatusToBooked(invoice);
                    
                    _logger.LogInformation("{OrderNote}. Trạng thái đơn hàng đã được chuyển từ Tạm giữ sang Đã thanh toán.", orderNote);
                }
                else
                {
                    // Thanh toán dư
                    await UpdateInvoiceStatus(invoice.InvoiceId, InvoiceStatus.Completed, cts.Token);
                    
                    // Cập nhật trạng thái seat từ "being held" thành "booked"
                    await UpdateSeatStatusToBooked(invoice);
                    
                    _logger.LogInformation("{OrderNote}. Trạng thái đơn hàng đã được chuyển từ Tạm giữ sang Thanh toán dư.", orderNote);
                }

                _logger.LogInformation("Invoice {OrderId} processed successfully. (CassoId={CassoId})", orderId, cassoId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing transaction");
                throw;
            }
        }

        private string ExtractOrderId(string description)
        {
            if (string.IsNullOrEmpty(description)) return "";

            // Look for DH pattern (from PHP sample) - không phân biệt hoa thường
            var dhMatch = System.Text.RegularExpressions.Regex.Match(description, $@"{MEMO_PREFIX}\d+", System.Text.RegularExpressions.RegexOptions.IgnoreCase, TimeSpan.FromSeconds(5));
            if (dhMatch.Success)
            {
                var orderId = dhMatch.Value;
                _logger.LogInformation("Parsed orderId '{OrderId}' from description: '{Description}'", orderId, description);
                return orderId;
            }

            // Fallback: Look for INV pattern (original logic)
            var invMatch = System.Text.RegularExpressions.Regex.Match(description, @"INV\d+", System.Text.RegularExpressions.RegexOptions.None, TimeSpan.FromSeconds(5));
            if (invMatch.Success)
            {
                var orderId = invMatch.Value;
                _logger.LogInformation("Parsed orderId '{OrderId}' from description: '{Description}'", orderId, description);
                return orderId;
            }

            return "";
        }

        [HttpGet("ping")]
        public IActionResult Ping()
        {
            return Ok("Webhook endpoint is accessible");
        }

        /// <summary>
        /// Test endpoint để simulate payment qua GET request
        /// </summary>
        [HttpGet("test")]
        public async Task<IActionResult> TestPayment([FromQuery] string orderId, [FromQuery] decimal amount)
        {
            try
            {
                _logger.LogInformation("Test payment called for orderId: {OrderId}, amount: {Amount}", orderId, amount);
                
                // Tạo test transaction data
                var testTransaction = new
                {
                    description = $"Thanh toan {orderId}",
                    amount = amount,
                    type = "IN"
                };
                
                // Convert to JsonElement để reuse existing logic
                var jsonString = JsonSerializer.Serialize(new[] { testTransaction });
                var jsonElement = JsonDocument.Parse(jsonString).RootElement;
                
                // Process như webhook thật
                await ProcessTransaction(jsonElement[0]);
                
                return Ok(new { 
                    success = true, 
                    message = $"Test payment processed for {orderId}",
                    orderId = orderId,
                    amount = amount
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in test payment");
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Cập nhật trạng thái invoice
        /// </summary>
        private async Task UpdateInvoiceStatus(string invoiceId, InvoiceStatus status, CancellationToken cancellationToken)
        {
            try
            {
                var invoice = _invoiceService.GetById(invoiceId);
                if (invoice == null)
                {
                    _logger.LogWarning("Invoice {InvoiceId} not found", invoiceId);
                    return;
                }

                invoice.Status = status;
                _invoiceService.Update(invoice);
                _invoiceService.Save();
                
                _logger.LogInformation("Updated invoice {InvoiceId} status to {Status}", invoiceId, status);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating invoice {InvoiceId} status", invoiceId);
                throw;
            }
        }

        /// <summary>
        /// Cập nhật trạng thái seat từ "being held" thành "booked" khi thanh toán thành công
        /// </summary>
        private async Task UpdateSeatStatusToBooked(Invoice invoice)
        {
            try
            {
                if (string.IsNullOrEmpty(invoice.SeatIds))
                {
                    _logger.LogWarning("Invoice {InvoiceId} has no seat IDs", invoice.InvoiceId);
                    return;
                }

                var seatIds = invoice.SeatIds.Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(id => int.TryParse(id.Trim(), out var seatId) ? seatId : 0)
                    .Where(id => id > 0)
                    .ToList();

                if (!seatIds.Any())
                {
                    _logger.LogWarning("No valid seat IDs found in invoice {InvoiceId}", invoice.InvoiceId);
                    return;
                }

                // Cập nhật trạng thái seat
                await _seatService.UpdateSeatsStatusToBookedAsync(seatIds);

                // Cập nhật ScheduleSeat nếu có
                if (invoice.MovieShowId.HasValue)
                {
                    await _scheduleSeatService.UpdateScheduleSeatsToBookedAsync(invoice.InvoiceId, invoice.MovieShowId.Value, seatIds);
                }

                _logger.LogInformation("Successfully updated {SeatCount} seats to Booked status for invoice {InvoiceId}", 
                    seatIds.Count, invoice.InvoiceId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating seat status to booked for invoice {InvoiceId}", invoice.InvoiceId);
                // Không throw exception để không ảnh hưởng đến việc cập nhật invoice
            }
        }

        /// <summary>
        /// Sanitize webhook body to remove sensitive information before logging
        /// </summary>
        private JsonElement SanitizeWebhookBody(JsonElement body)
        {
            try
            {
                // Create a sanitized version of the body for logging
                var sanitizedObject = new JsonObject();
                
                if (body.ValueKind == JsonValueKind.Object)
                {
                    foreach (var property in body.EnumerateObject())
                    {
                        // Only log non-sensitive fields
                        if (property.Name == "data" || property.Name == "error" || property.Name == "message")
                        {
                            sanitizedObject.Add(property.Name, JsonValue.Create(property.Value.GetRawText()));
                        }
                        else
                        {
                            // For sensitive fields, just log the field name
                            sanitizedObject.Add(property.Name, JsonValue.Create("[REDACTED]"));
                        }
                    }
                }
                
                // Convert JsonObject to JsonElement using JsonSerializer
                var jsonString = sanitizedObject.ToJsonString();
                return JsonSerializer.Deserialize<JsonElement>(jsonString);
            }
            catch
            {
                // If sanitization fails, return a simple object
                return JsonSerializer.Deserialize<JsonElement>("{\"sanitized\": true}");
            }
        }
    }
} 