using MovieTheater.Models;

namespace MovieTheater.ViewModels
{
    public class ShowroomEditViewModel
    {
        public int CinemaRoomId { get; set; }
        public string CinemaRoomName { get; set; }
        public string? MovieName { get; set; }
        public int SeatLength { get; set; }
        public int SeatWidth { get; set; }
        public List<Seat> Seats { get; set; }
    }
}
