namespace MovieTheater.Models;

public partial class Schedule
{
    public int ScheduleId { get; set; }

    public string? ScheduleTime { get; set; }

    public virtual ICollection<MovieShow> MovieShows { get; set; } = new List<MovieShow>();

    public virtual ICollection<ScheduleSeat> ScheduleSeats { get; set; } = new List<ScheduleSeat>();
}
