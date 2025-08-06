using System.ComponentModel.DataAnnotations;

namespace MovieTheater.ViewModels
{
    public class PersonFormModel
    {
        public IFormFile? ImageFile { get; set; }
        public string? Image { get; set; }
        public int PersonId { get; set; }
        [Required(ErrorMessage = "Full name is required")]
        public string Name { get; set; }

        [Required(ErrorMessage = "Date of Birth is required")]
        public DateOnly? DateOfBirth { get; set; }

        [Required(ErrorMessage = "Nationality is required")]
        public string Nationality { get; set; }

        [Required(ErrorMessage = "Gender is required")]
        public bool? Gender { get; set; } // Nullable to enforce selection

        [Required(ErrorMessage = "Please specify if the person is a director")]
        public bool? IsDirector { get; set; }

        [Required(ErrorMessage = "Description is required")]
        public string Description { get; set; }
    }
}
