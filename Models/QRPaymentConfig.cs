namespace MovieTheater.Models
{
    public class QRPaymentConfig
    {
        public string BankCode { get; set; }
        public string AccountNumber { get; set; }
        public string AccountName { get; set; }
        public string BankName { get; set; }
        public string QRContent { get; set; }
        public int ExpiredTime { get; set; } // Expiration time in minutes
        public string PayOSClientId { get; set; }
        public string PayOSApiKey { get; set; }
        public string PayOSChecksumKey { get; set; }
        public string PayOSReturnUrl { get; set; }
        public string PayOSCancelUrl { get; set; }
    }
} 