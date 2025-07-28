using MovieTheater.Models;

namespace MovieTheater.ViewModels
{
    public class SeatSelectionViewModel
    {
        public string MovieId { get; set; }
        public string MovieName { get; set; }
        public int MovieShowId { get; set; }
        public DateTime ShowDate { get; set; }
        public string ShowTime { get; set; }
        public int CinemaRoomId { get; set; }
        public string CinemaRoomName { get; set; }
        public string VersionName { get; set; }
        public int SeatWidth { get; set; }
        public int SeatLength { get; set; }
        public List<Seat> Seats { get; set; }
        public List<SeatType> SeatTypes { get; set; }
        public string ReturnUrl { get; set; }
        public string MoviePoster { get; set; } // Link ảnh poster
        public string MovieContent { get; set; } // Mô tả phim
        public string MovieDirector { get; set; } // Đạo diễn
        public string MovieActor { get; set; } // Diễn viên
        public string MovieGenre { get; set; } // Thể loại (chuỗi, phân cách bởi dấu phẩy)
        public DateOnly? MovieFromDate { get; set; } // Ngày khởi chiếu
        public int? MovieDuration { get; set; } // Thời lượng (phút)
    }
}