using MovieTheater.Models;

namespace MovieTheater.ViewModels
{
    public class ConfirmBookingViewModel
    {
        // Phim & suất chiếu
        public string MovieId { get; set; }
        public string MovieName { get; set; }
        public string CinemaRoomName { get; set; }
        public string VersionName { get; set; }
        public DateOnly ShowDate { get; set; }
        public string ShowTime { get; set; }
        public InvoiceStatus Status { get; set; }

        // Ghế đã chọn
        public List<SeatDetailViewModel> SelectedSeats { get; set; }
        public decimal Subtotal { get; set; }
        public decimal RankDiscount { get; set; }
        public decimal TotalPrice { get; set; }
        public decimal PricePerTicket { get; set; } // optional

        // Người dùng hiện tại
        public string FullName { get; set; }
        public string Email { get; set; }
        public string IdentityCard { get; set; }
        public string PhoneNumber { get; set; }
        public int CurrentScore { get; set; }
        public int UseScore { get; set; } // dùng để submit ngược lại nếu cần

        public string InvoiceId { get; set; }
        public int ScoreUsed { get; set; }

        public int MovieShowId { get; set; }

        public int AddScore { get; set; }

        public decimal EarningRate { get; set; }
        public decimal RankDiscountPercent { get; set; }

        // Voucher properties
        public string SelectedVoucherId { get; set; }
        public decimal VoucherAmount { get; set; }
        public string SelectedPromotionId { get; set; }
        public decimal PromotionDiscountPercent { get; set; }
    }
}