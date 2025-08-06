namespace MovieTheater.Models;

public partial class MovieShow
{
    public int MovieShowId { get; set; }

    public string MovieId { get; set; } = null!;

    public int CinemaRoomId { get; set; }

    public DateOnly ShowDate { get; set; }

    public int ScheduleId { get; set; }

    public int VersionId { get; set; }

    public virtual CinemaRoom CinemaRoom { get; set; } = null!;

    public virtual ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();

    public virtual Movie Movie { get; set; } = null!;

    public virtual Schedule Schedule { get; set; } = null!;

    public virtual ICollection<ScheduleSeat> ScheduleSeats { get; set; } = new List<ScheduleSeat>();

    public virtual Version Version { get; set; } = null!;
}
