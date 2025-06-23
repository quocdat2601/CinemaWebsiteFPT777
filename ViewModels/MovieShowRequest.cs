namespace MovieTheater.ViewModels
{
    public class MovieShowRequest
    {
        public string MovieId { get; set; }
        public int ShowDateId { get; set; }
        public int ScheduleId { get; set; }
        public int CinemaRoomId { get; set; }
        public DateOnly ShowDate { get; set; }
    }
} 