using System.ComponentModel.DataAnnotations;
using MovieTheater.Models;

namespace MovieTheater.ViewModels
{
    public class SeatEditViewModel
    {
        public int CinemaRoomId { get; set; }

        [Required(ErrorMessage = "Showroom name is required.")]
        [StringLength(100, ErrorMessage = "Showroom name cannot exceed 100 characters.")]
        public string CinemaRoomName { get; set; }

        [Required(ErrorMessage = "Seat width is required.")]
        [Range(1, 100, ErrorMessage = "Seat width must be between 1 and 100.")]
        public int? SeatWidth { get; set; }

        [Required(ErrorMessage = "Seat length is required.")]
        [Range(1, 100, ErrorMessage = "Seat length must be between 1 and 100.")]
        public int? SeatLength { get; set; }

        public List<Seat> Seats { get; set; }
    }
}
