using System;
using System.Collections.Generic;

namespace MovieTheater.Models;

public partial class ShowDate
{
    public int ShowDateId { get; set; }

    public DateOnly? ShowDate1 { get; set; }

    public string? DateName { get; set; }

    public virtual ICollection<Movie> Movies { get; set; } = new List<Movie>();
}
