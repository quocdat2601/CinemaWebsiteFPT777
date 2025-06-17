namespace MovieTheater.Models;

public partial class ScheduleSeat
{
    public int ScheduleId { get; set; }

    public int SeatId { get; set; }

    public int? SeatStatusId { get; set; }

    public virtual Schedule Schedule { get; set; } = null!;

    public virtual Seat Seat { get; set; } = null!;

    public virtual SeatStatus? SeatStatus { get; set; }
}
