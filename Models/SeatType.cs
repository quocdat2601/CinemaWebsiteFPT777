namespace MovieTheater.Models;

public partial class SeatType
{
    public int SeatTypeId { get; set; }

    public string? TypeName { get; set; }

    public decimal? PricePercent { get; set; }

    public string ColorHex { get; set; } = null!;

    public virtual ICollection<Seat> Seats { get; set; } = new List<Seat>();
}
