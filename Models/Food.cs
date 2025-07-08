using System;
using System.Collections.Generic;

namespace MovieTheater.Models;

public partial class Food
{
    public int FoodId { get; set; }

    public string Category { get; set; } = null!;

    public string Name { get; set; } = null!;

    public decimal Price { get; set; }

    public string? Description { get; set; }

    public string? Image { get; set; }

    public bool Status { get; set; }

    public DateTime CreatedDate { get; set; }

    public DateTime? UpdatedDate { get; set; }
}
