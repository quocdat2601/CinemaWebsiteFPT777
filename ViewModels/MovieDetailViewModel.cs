using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using MovieTheater.Models;

namespace MovieTheater.ViewModels
{
    public class MovieDetailViewModel
    {
        public string? MovieId { get; set; }

        [Required(ErrorMessage = "Movie name (English) is required.")]
        public string? MovieNameEnglish { get; set; }

        [Required(ErrorMessage = "Movie name (Vietnamese) is required.")]
        public string? MovieNameVn { get; set; }

        [Required(ErrorMessage = "Start date is required.")]
        public DateOnly? FromDate { get; set; }

        [Required(ErrorMessage = "End date is required.")]
        public DateOnly? ToDate { get; set; }

        [Required(ErrorMessage = "Actor is required.")]
        public string? Actor { get; set; }

        [Required(ErrorMessage = "Production company is required.")]
        public string? MovieProductionCompany { get; set; }

        [Required(ErrorMessage = "Director is required.")]
        public string? Director { get; set; }

        [Required(ErrorMessage = "Duration is required.")]
        [Range(1, 500, ErrorMessage = "Duration must be between 1 and 500 minutes.")]
        public int? Duration { get; set; }

        [Required(ErrorMessage = "Version is required.")]
        public string? Version { get; set; }

        [Required(ErrorMessage = "Description is required.")]
        public string? Content { get; set; }

        public int? CinemaRoomId { get; set; }
        public string? TrailerUrl { get; set; }
        public IFormFile? LargeImageFile { get; set; }

        public IFormFile? SmallImageFile { get; set; }

        public string? LargeImage { get; set; }
        public string? SmallImage { get; set; }

        public List<int> SelectedScheduleIds { get; set; } = new();

        [Required(ErrorMessage = "At least one type must be selected.")]
        public List<int> SelectedTypeIds { get; set; } = new();

        public List<int> SelectedShowDateIds { get; set; } = new();

        public string? CinemaRoomName { get; set; }

        public List<Schedule>? AvailableSchedules { get; set; }
        public List<ShowDate>? AvailableShowDates { get; set; }
        public List<Models.Type>? AvailableTypes { get; set; }
        public List<CinemaRoom> AvailableCinemaRooms { get; set; } = new();
    }
}
