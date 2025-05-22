namespace MovieTheater.ViewModels
{
    public class MovieViewModel
    {
        public string? MovieNameEnglish { get; set; }
        public int? Duration { get; set; }
        public string? SmallImage { get; set; }
        public List<TypeViewModel> Types { get; set; } = new List<TypeViewModel>();
    }
}
