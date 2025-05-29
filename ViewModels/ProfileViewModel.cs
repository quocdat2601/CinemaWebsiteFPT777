using System.ComponentModel.DataAnnotations;
namespace MovieTheater.ViewModels
{
    public class ProfileViewModel
    {
        [Required(ErrorMessage = "Account ID is required")]
        public string AccountId { get; set; }

        [Required(ErrorMessage = "Username is required")]
        [StringLength(50, ErrorMessage = "Username cannot be longer than 50 characters")]
        public string Username { get; set; }

        [Required(ErrorMessage = "Password is required")]
        [DataType(DataType.Password)]
        [StringLength(100, ErrorMessage = "Password cannot be longer than 100 characters")]
        public string Password { get; set; }

        [Required(ErrorMessage = "Full name is required")]
        [StringLength(100, ErrorMessage = "Full name cannot be longer than 100 characters")]
        public string FullName { get; set; }

        [Required(ErrorMessage = "Date of birth is required")]
        [DataType(DataType.Date)]
        public DateOnly DateOfBirth { get; set; }

        [Required(ErrorMessage = "Gender is required")]
        public string Gender { get; set; }

        [Required(ErrorMessage = "Identity card is required")]
        [StringLength(20, ErrorMessage = "Identity card cannot be longer than 20 characters")]
        public string IdentityCard { get; set; }

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email address")]
        [StringLength(100, ErrorMessage = "Email cannot be longer than 100 characters")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Address is required")]
        [StringLength(200, ErrorMessage = "Address cannot be longer than 200 characters")]
        public string Address { get; set; }

        [Required(ErrorMessage = "Phone number is required")]
        [Phone(ErrorMessage = "Invalid phone number")]
        [StringLength(20, ErrorMessage = "Phone number cannot be longer than 20 characters")]
        public string PhoneNumber { get; set; }

        public IFormFile? ImageFile { get; set; } // for file upload
        public string? Image { get; set; } // for saving image path

        public int? RoleId { get; set; }
        public int? Status { get; set; }
    }
}