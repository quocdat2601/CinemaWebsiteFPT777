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
    }
} 