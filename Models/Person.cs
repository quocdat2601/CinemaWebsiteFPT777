using System;
using System.Collections.Generic;

namespace MovieTheater.Models;

public partial class Person
{
    public int PersonId { get; set; }

    public string Name { get; set; } = null!;

    public DateOnly? DateOfBirth { get; set; }

    public string? Nationality { get; set; }

    public bool? Gender { get; set; }

    public string? Image { get; set; }

    public bool? IsDirector { get; set; }

    public string? Description { get; set; }

    public virtual ICollection<Movie> Movies { get; set; } = new List<Movie>();
}
