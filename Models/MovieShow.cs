namespace MovieTheater.Models;

public partial class MovieShow
{
    public int MovieShowId { get; set; }

    public string? MovieId { get; set; }

    public int? ShowDateId { get; set; }

    public int? ScheduleId { get; set; }

    public int? CinemaRoomId { get; set; }

    public virtual CinemaRoom? CinemaRoom { get; set; }

    public virtual Movie? Movie { get; set; }

    public virtual Schedule? Schedule { get; set; }

    public virtual ICollection<ScheduleSeat> ScheduleSeats { get; set; } = new List<ScheduleSeat>();

    public virtual ShowDate? ShowDate { get; set; }
}
