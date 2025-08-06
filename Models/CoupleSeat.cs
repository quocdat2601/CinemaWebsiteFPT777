namespace MovieTheater.Models;

public partial class CoupleSeat
{
    public int CoupleSeatId { get; set; }

    public int FirstSeatId { get; set; }

    public int SecondSeatId { get; set; }

    public virtual Seat FirstSeat { get; set; } = null!;

    public virtual Seat SecondSeat { get; set; } = null!;
}
