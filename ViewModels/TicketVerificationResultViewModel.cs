namespace MovieTheater.ViewModels
{
    public class TicketVerificationResultViewModel
    {
        public string InvoiceId { get; set; }
        public string MovieName { get; set; }
        public string ShowDate { get; set; }
        public string ShowTime { get; set; }
        public string CustomerName { get; set; }
        public string CustomerPhone { get; set; }
        public string Seats { get; set; }
        public string TotalAmount { get; set; }
        public bool IsSuccess { get; set; }
        public string VerificationTime { get; set; }
        public string Message { get; set; }
    }
}