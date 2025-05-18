using System;
using System.Collections.Generic;

namespace MovieTheater.Models;

public partial class Movie
{
    public string MovieId { get; set; } = null!;

    public string? Actor { get; set; }

    public int? CinemaRoomId { get; set; }

    public string? Content { get; set; }

    public string? Director { get; set; }

    public int? Duration { get; set; }

    public DateOnly? FromDate { get; set; }

    public string? MovieProductionCompany { get; set; }

    public DateOnly? ToDate { get; set; }

    public string? Version { get; set; }

    public string? MovieNameEnglish { get; set; }

    public string? MovieNameVn { get; set; }

    public string? LargeImage { get; set; }

    public string? SmallImage { get; set; }

    public virtual ICollection<Schedule> Schedules { get; set; } = new List<Schedule>();

    public virtual ICollection<ShowDate> ShowDates { get; set; } = new List<ShowDate>();

    public virtual ICollection<Type> Types { get; set; } = new List<Type>();
}
