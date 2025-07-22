namespace MovieTheater.ViewModels
{
    public class QRPaymentViewModel
    {
        public string OrderId { get; set; }
        public decimal Amount { get; set; }
        public string OrderInfo { get; set; }
        public string QRCodeData { get; set; }
        public string QRCodeImage { get; set; }
        public DateTime ExpiredTime { get; set; }
        public string CustomerName { get; set; }
        public string CustomerPhone { get; set; }
        public string MovieName { get; set; }
        public string ShowTime { get; set; }
        public string SeatInfo { get; set; }
        public string MoMoQRCodeBase64 { get; set; }
        public string PayOSQRCodeUrl { get; set; }
    }
} 