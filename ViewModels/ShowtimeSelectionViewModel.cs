namespace MovieTheater.ViewModels
{
    // ViewModel for MT-20: Select movie and showtime
    public class ShowtimeSelectionViewModel
    {
        // List of available screening dates
        public List<DateOnly> AvailableDates { get; set; }

        // Currently selected date
        public DateOnly SelectedDate { get; set; }

        // List of movies with showtimes for the selected date
        public List<MovieShowtimeInfo> Movies { get; set; }

        // URL to return to after completing ticket selling
        public string? ReturnUrl { get; set; }
    }

    // Movie and its showtimes for a specific date
    public class MovieShowtimeInfo
    {
        public string MovieId { get; set; } // Movie ID (string, e.g., 'M001')
        public string MovieName { get; set; } // Movie name
        public string PosterUrl { get; set; } // Poster image URL
        public List<VersionShowtimeInfo> VersionShowtimes { get; set; } = new List<VersionShowtimeInfo>();
    }

    // Version and its showtimes
    public class VersionShowtimeInfo
    {
        public int VersionId { get; set; }
        public string VersionName { get; set; } // e.g., "2D", "3D", "IMAX"
        public List<string> Showtimes { get; set; } = new List<string>(); // List of showtime strings (e.g., '08:00')
    }
}