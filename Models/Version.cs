namespace MovieTheater.Models;

public partial class Version
{
    public int VersionId { get; set; }

    public string? VersionName { get; set; }

    public decimal? Multi { get; set; }

    public virtual ICollection<CinemaRoom> CinemaRooms { get; set; } = new List<CinemaRoom>();

    public virtual ICollection<MovieShow> MovieShows { get; set; } = new List<MovieShow>();

    public virtual ICollection<Movie> Movies { get; set; } = new List<Movie>();
}
