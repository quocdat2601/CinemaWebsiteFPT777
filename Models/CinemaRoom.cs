namespace MovieTheater.Models;

public partial class CinemaRoom
{
    public int CinemaRoomId { get; set; }

    public string? CinemaRoomName { get; set; }

    public int? SeatWidth { get; set; }

    public int? SeatLength { get; set; }

    public int? VersionId { get; set; }

    public int? SeatQuantity { get; set; }

    public virtual ICollection<MovieShow> MovieShows { get; set; } = new List<MovieShow>();

    public virtual ICollection<Seat> Seats { get; set; } = new List<Seat>();

    public virtual Version? Version { get; set; }
}
