using System;
using System.Collections.Generic;

namespace MovieTheater.Models;

public partial class Promotion
{
    public int PromotionId { get; set; }

    public string? Detail { get; set; }

    public int? DiscountLevel { get; set; }

    public DateOnly? EndTime { get; set; }

    public string? Image { get; set; }

    public DateOnly? StartTime { get; set; }

    public string? Title { get; set; }

    public bool IsActive { get; set; }

    public virtual ICollection<PromotionCondition> PromotionConditions { get; set; } = new List<PromotionCondition>();
}
