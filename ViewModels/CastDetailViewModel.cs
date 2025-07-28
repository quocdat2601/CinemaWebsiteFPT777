using MovieTheater.Models;

namespace MovieTheater.ViewModels
{
    public class CastDetailViewModel
    {
        public Person Person { get; set; }
        public IEnumerable<Movie> Movies { get; set; }

        public CastDetailViewModel()
        {
            Movies = new List<Movie>();
        }
    }
}
