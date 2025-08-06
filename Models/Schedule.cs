namespace MovieTheater.Models;

public partial class Schedule
{
    public int ScheduleId { get; set; }

    public TimeOnly? ScheduleTime { get; set; }

    public virtual ICollection<MovieShow> MovieShows { get; set; } = new List<MovieShow>();
}
