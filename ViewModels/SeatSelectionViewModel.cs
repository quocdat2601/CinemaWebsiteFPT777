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
    }
}