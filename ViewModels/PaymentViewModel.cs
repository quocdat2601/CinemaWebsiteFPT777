namespace MovieTheater.ViewModels
{
    public class PaymentViewModel
    {
        public string InvoiceId { get; set; }
        public string MovieName { get; set; }
        public DateTime ShowDate { get; set; }
        public string ShowTime { get; set; }
        public string Seats { get; set; }
        public decimal TotalAmount { get; set; }
        public string OrderInfo { get; set; }
    }
}