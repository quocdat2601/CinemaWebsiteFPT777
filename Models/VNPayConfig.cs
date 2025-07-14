namespace MovieTheater.Models
{
    public class VNPayConfig
    {
        public string TmnCode { get; set; }
        public string HashSecret { get; set; }
        public string BaseUrl { get; set; }
        public string Command { get; set; }
        public string CurrCode { get; set; }
        public string Version { get; set; }
        public string Locale { get; set; }
        public string ReturnUrl { get; set; }
        public string IpnUrl { get; set; }
        public int ExpiredTime { get; set; } // Thời gian hết hạn thanh toán (tính bằng phút)
    }
}