using System.ComponentModel.DataAnnotations;

namespace MovieTheater.ViewModels
{
    public class FoodViewModel
    {
        public int FoodId { get; set; }
        
        [Required(ErrorMessage = "Category is required")]
        [Display(Name = "Category")]
        public string Category { get; set; }
        
        [Required(ErrorMessage = "Name is required")]
        [Display(Name = "Name")]
        [StringLength(255, ErrorMessage = "Name cannot exceed 255 characters")]
        public string Name { get; set; }
        
        [Required(ErrorMessage = "Price is required")]
        [Display(Name = "Price")]
        [Range(0, double.MaxValue, ErrorMessage = "Price must be greater than 0")]
        public decimal Price { get; set; }
        
        [Display(Name = "Description")]
        [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
        public string? Description { get; set; }
        
        [Display(Name = "Image")]
        public IFormFile? ImageFile { get; set; }
        
        public string? Image { get; set; }
        
        [Display(Name = "Status")]
        public bool Status { get; set; } = true;
        
        public DateTime CreatedDate { get; set; }
        
        public DateTime? UpdatedDate { get; set; }
    }
    
    public class FoodListViewModel
    {
        public List<FoodViewModel> Foods { get; set; } = new List<FoodViewModel>();
        public string? SearchKeyword { get; set; }
        public string? CategoryFilter { get; set; }
        public bool? StatusFilter { get; set; }
    }
} 