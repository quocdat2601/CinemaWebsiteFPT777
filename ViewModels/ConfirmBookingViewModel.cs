namespace MovieTheater.ViewModels
{
    public class ConfirmBookingViewModel
    {
        // Phim & suất chiếu
        public string MovieId { get; set; }
        public string MovieName { get; set; }
        public string CinemaRoomName { get; set; }
        public DateTime ShowDate { get; set; }
        public string ShowTime { get; set; }

        // Ghế đã chọn
        public List<SeatDetailViewModel> SelectedSeats { get; set; }
        public decimal TotalPrice { get; set; }
        public decimal PricePerTicket { get; set; } // optional

        // Người dùng hiện tại
        public string FullName { get; set; }
        public string Email { get; set; }
        public string IdentityCard { get; set; }
        public string PhoneNumber { get; set; }
        public int CurrentScore { get; set; }
        public int UseScore { get; set; } // dùng để submit ngược lại nếu cần

    }
}