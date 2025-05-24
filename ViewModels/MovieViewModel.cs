using MovieTheater.Models;

namespace MovieTheater.ViewModels
{
    public class MovieViewModel
    {
        public string MovieId { get; set; } = null!;

        public string? MovieNameEnglish { get; set; }
        public int? Duration { get; set; }
        public string? SmallImage { get; set; }
        public List<MovieTheater.Models.Type> Types { get; set; }
    }
}
