namespace MovieTheater.ViewModels
{
    public class BookingResult
    {
        public bool Success { get; set; }
        public string ErrorMessage { get; set; }
        public string InvoiceId { get; set; }
        public decimal TotalPrice { get; set; }
    }
}