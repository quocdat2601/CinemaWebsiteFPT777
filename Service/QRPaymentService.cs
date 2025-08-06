using MovieTheater.Models;
using QRCoder;
using System.Security.Cryptography;
using System.Text;

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
                // Kiểm tra null values và throw exception
                if (string.IsNullOrEmpty(_config.BankCode) || string.IsNullOrEmpty(_config.AccountNumber) || string.IsNullOrEmpty(_config.AccountName))
                {
                    throw new InvalidOperationException("QR Payment configuration is missing required values (BankCode, AccountNumber, or AccountName).");
                }

                // Tạo nội dung QR code theo chuẩn VietQR
                var qrContent = $"https://api.vietqr.io/image/{_config.BankCode}/{_config.AccountNumber}?amount={amount}&addInfo={orderId}&accountName={_config.AccountName}";

                _logger.LogInformation("QR code data generated for order {OrderId}", orderId);
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
                // Kiểm tra null values và fallback về simple QR code
                if (string.IsNullOrEmpty(orderInfo) || string.IsNullOrEmpty(orderId))
                {
                    _logger.LogWarning("OrderInfo or OrderId is null/empty, falling back to simple QR code");
                    return GenerateSimpleQRCode($"PAYMENT_{orderId ?? "NULL"}_{amount}");
                }

                // VNPAY QR code format
                var vnpUrl = _vnPayConfig.BaseUrl;
                var vnpParams = new Dictionary<string, string>
                {
                    ["vnp_Version"] = "2.1.0",
                    ["vnp_Command"] = "pay",
                    ["vnp_TmnCode"] = _vnPayConfig.TmnCode,
                    ["vnp_Amount"] = (amount * 100).ToString(), // Convert to smallest currency unit
                    ["vnp_CurrCode"] = "VND",
                    ["vnp_TxnRef"] = orderId,
                    ["vnp_OrderInfo"] = orderInfo,
                    ["vnp_OrderType"] = "billpayment",
                    ["vnp_ReturnUrl"] = _vnPayConfig.ReturnUrl,
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
                var secretKey = _vnPayConfig.HashSecret;
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
                _logger.LogInformation("Validating payment for order {OrderId}, transaction {TransactionId}", orderId, transactionId);
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

                _logger.LogInformation("QR code image URL generated successfully");
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

                _logger.LogInformation("Simple QR code generated successfully");
                return qrUrl;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating simple QR code");
                return "https://api.qrserver.com/v1/create-qr-code/?size=300x300&data=DEMO_QR_CODE";
            }
        }

        public string GenerateMoMoQRCodeBase64(string phoneNumber)
        {
            if (phoneNumber.StartsWith("0"))
            {
                phoneNumber = "84" + phoneNumber.Substring(1);
            }

            string momoData = $"2|99|{phoneNumber}";

            // 1. Tạo dữ liệu QR
            QRCodeGenerator qrGenerator = new QRCodeGenerator();
            QRCodeData qrCodeData = qrGenerator.CreateQrCode(momoData, QRCodeGenerator.ECCLevel.Q);

            // 2. Dùng PngByteQRCode để render ra ảnh PNG dưới dạng mảng byte
            PngByteQRCode pngQrCode = new PngByteQRCode(qrCodeData);
            byte[] qrCodeImageBytes = pngQrCode.GetGraphic(20);

            // 3. Chuyển mảng byte sang Base64
            string base64 = Convert.ToBase64String(qrCodeImageBytes);

            return $"data:image/png;base64,{base64}";
        }

        public string GeneratePayOSQRCode(decimal amount, string orderInfo, string orderId)
        {
            try
            {
                var clientId = _config.PayOSClientId;
                var apiKey = _config.PayOSApiKey;
                var checksumKey = _config.PayOSChecksumKey;
                var returnUrl = _config.PayOSReturnUrl;
                var cancelUrl = _config.PayOSCancelUrl;

                //// Lấy orderCode là số nguyên dương từ orderId (ví dụ: INV142 -> 142)
                //int orderCode = 0;
                //if (!string.IsNullOrEmpty(orderId) && orderId.StartsWith("INV"))
                //{
                //    int.TryParse(orderId.Substring(3), out orderCode);
                //}
                //// Nếu không lấy được thì sinh số ngẫu nhiên nhỏ hơn 9007199254740991
                //if (orderCode <= 0 || orderCode > 9007199254740991)
                //{
                //    orderCode = new Random().Next(1, 999999999); // hoặc dùng timestamp
                //}

                // Tạo orderCode unique bằng timestamp + random để tránh trùng lặp
                var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                var random = new Random();
                var randomPart = random.Next(1000, 9999);
                var orderCode = timestamp * 10000 + randomPart; // Đảm bảo unique

                // Truncate description to max 25 characters for PayOS
                string shortDescription = orderInfo;
                if (!string.IsNullOrEmpty(shortDescription) && shortDescription.Length > 25)
                {
                    shortDescription = shortDescription.Substring(0, 25);
                }

                // Tạo signature đúng chuẩn PayOS
                var dataString = $"amount={(int)amount}&cancelUrl={cancelUrl}&description={shortDescription}&orderCode={orderCode}&returnUrl={returnUrl}";
                var signature = CreatePayOSSignature(dataString, checksumKey);

                using (var client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Add("x-client-id", clientId);
                    client.DefaultRequestHeaders.Add("x-api-key", apiKey);
                    client.Timeout = TimeSpan.FromSeconds(10);

                    var order = new
                    {
                        amount = (int)amount,
                        description = shortDescription,
                        orderCode = orderCode,
                        returnUrl = returnUrl,
                        cancelUrl = cancelUrl,
                        signature = signature
                    };
                    var content = new StringContent(System.Text.Json.JsonSerializer.Serialize(order), Encoding.UTF8, "application/json");
                    var response = client.PostAsync("https://api-merchant.payos.vn/v2/payment-requests", content).Result;
                    var responseBody = response.Content.ReadAsStringAsync().Result;
                    using var doc = System.Text.Json.JsonDocument.Parse(responseBody);
                    var root = doc.RootElement;
                    if (root.TryGetProperty("data", out var data))
                    {
                        if (data.TryGetProperty("qrCode", out var qrCode))
                        {
                            var qrString = qrCode.GetString();
                            // Render chuỗi qrCode thành ảnh QR (base64 PNG) bằng QRCoder
                            return RenderQRCodeBase64(qrString);
                        }
                    }
                    _logger.LogError("PayOS response error (không có qrCode)");
                    //return GenerateVietQRCode(amount, orderInfo, orderId);
                    return null; // Hoặc fallback sang QR khác
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating PayOS QR code");
                //return GenerateVietQRCode(amount, orderInfo, orderId);
                return null;
            }
        }

        // Hàm tạo signature HMAC_SHA256
        private string CreatePayOSSignature(string data, string key)
        {
            using (var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(key)))
            {
                var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
                return BitConverter.ToString(hash).Replace("-", "").ToLower();
            }
        }

        // Hàm render chuỗi QR thành ảnh base64 PNG
        private string RenderQRCodeBase64(string qrString)
        {
            if (string.IsNullOrEmpty(qrString)) return null;
            var qrGenerator = new QRCoder.QRCodeGenerator();
            var qrCodeData = qrGenerator.CreateQrCode(qrString, QRCoder.QRCodeGenerator.ECCLevel.Q);
            var qrCode = new QRCoder.PngByteQRCode(qrCodeData);
            var qrBytes = qrCode.GetGraphic(20);
            return $"data:image/png;base64,{Convert.ToBase64String(qrBytes)}";
        }
    }


}