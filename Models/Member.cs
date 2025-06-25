using System;
using System.Collections.Generic;

namespace MovieTheater.Models;

public partial class Member
{
    public string MemberId { get; set; } = null!;

    public int? Score { get; set; }

    public int TotalPoints { get; set; } // Lifetime points, never decreases

    public string? AccountId { get; set; }

    public virtual Account? Account { get; set; }
}
