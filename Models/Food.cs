using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MovieTheater.Models
{
    public class Food
    {
        [Key]
        [Column("FoodId")]
        public int FoodId { get; set; }
        
        [Required]
        [StringLength(50)]
        [Column("Category")]
        public string Category { get; set; } // food, drink, combo
        
        [Required]
        [StringLength(255)]
        [Column("Name")]
        public string Name { get; set; }
        
        [Required]
        [Range(0, double.MaxValue)]
        [Column("Price")]
        public decimal Price { get; set; }
        
        [StringLength(500)]
        [Column("Description")]
        public string? Description { get; set; }
        
        [StringLength(255)]
        [Column("Image")]
        public string? Image { get; set; }
        
        [Required]
        [Column("Status")]
        public bool Status { get; set; } = true; // true = active, false = inactive
        
        [Column("CreatedDate")]
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        
        [Column("UpdatedDate")]
        public DateTime? UpdatedDate { get; set; }
    }
} 