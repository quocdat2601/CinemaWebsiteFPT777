using System;
using System.Collections.Generic;

namespace MovieTheater.Models;

public partial class Rank
{
    public int RankId { get; set; }

    public string? RankName { get; set; }

    public decimal? DiscountPercentage { get; set; }

    public int? RequiredPoints { get; set; }

    public decimal? PointEarningPercentage { get; set; }

    public string? ColorGradient { get; set; }

    public string? IconClass { get; set; }

    public virtual ICollection<Account> Accounts { get; set; } = new List<Account>();
}
