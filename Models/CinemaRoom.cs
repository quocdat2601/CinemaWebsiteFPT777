namespace MovieTheater.Models;

public partial class CinemaRoom
{
    public int CinemaRoomId { get; set; }

    public string? CinemaRoomName { get; set; }

    public int? SeatWidth { get; set; }

    public int? SeatLength { get; set; }

    public int? VersionId { get; set; }

    public int? StatusId { get; set; }

    public int? SeatQuantity { get; set; }

    public string? DisableReason { get; set; }

    public DateTime? UnavailableStartDate { get; set; }

    public DateTime? UnavailableEndDate { get; set; }

    public virtual ICollection<MovieShow> MovieShows { get; set; } = new List<MovieShow>();

    public virtual ICollection<Seat> Seats { get; set; } = new List<Seat>();

    public virtual Status? Status { get; set; }

    public virtual Version? Version { get; set; }
}
