using MovieTheater.Models;
namespace MovieTheater.ViewModels
{
    public class MovieDetailViewModel
    {
        public string? MovieNameEnglish { get; set; }
        public string? MovieNameVn { get; set; }
        public DateOnly? FromDate { get; set; }
        public DateOnly? ToDate { get; set; }
        public string? Actor { get; set; }
        public string? MovieProductionCompany { get; set; }
        public string? Director { get; set; }
        public int? Duration { get; set; }
        public string? Version { get; set; }
        public string? Content { get; set; }
        public int? CinemaRoomId { get; set; }
        public IFormFile? LargeImageFile { get; set; } 
        public IFormFile? SmallImageFile { get; set; } 

        public string? LargeImage { get; set; }
        public string? SmallImage { get; set; }

        public List<int> SelectedScheduleIds { get; set; } = new();
        public List<int> SelectedShowDateIds { get; set; } = new();
        public List<int> SelectedTypeIds { get; set; } = new();

        public string? CinemaRoomName { get; set; }

        public List<Schedule>? AvailableSchedules { get; set; }
        public List<ShowDate>? AvailableShowDates { get; set; }
        public List<Models.Type>? AvailableTypes { get; set; }
        public List<CinemaRoom> AvailableCinemaRooms { get; set; } = new();

    }
}

