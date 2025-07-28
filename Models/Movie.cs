using System;
using System.Collections.Generic;

namespace MovieTheater.Models;

public partial class Movie
{
    public string MovieId { get; set; } = null!;

    public string? Content { get; set; }

    public int? Duration { get; set; }

    public DateOnly? FromDate { get; set; }

    public string? MovieProductionCompany { get; set; }

    public DateOnly? ToDate { get; set; }

    public string? MovieNameEnglish { get; set; }

    public string? LargeImage { get; set; }

    public string? SmallImage { get; set; }

    public string? TrailerUrl { get; set; }

    public string? LogoImage { get; set; }

    public virtual ICollection<MovieShow> MovieShows { get; set; } = new List<MovieShow>();

    public virtual ICollection<Account> Accounts { get; set; } = new List<Account>();

    public virtual ICollection<Person> People { get; set; } = new List<Person>();

    public virtual ICollection<Type> Types { get; set; } = new List<Type>();

    public virtual ICollection<Version> Versions { get; set; } = new List<Version>();
}
