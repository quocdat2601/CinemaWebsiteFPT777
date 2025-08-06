namespace MovieTheater.Models;

public partial class Promotion
{
    public int PromotionId { get; set; }

    public string? Detail { get; set; }

    public int? DiscountLevel { get; set; }

    public DateTime? EndTime { get; set; }

    public string? Image { get; set; }

    public DateTime? StartTime { get; set; }

    public string? Title { get; set; }

    public bool IsActive { get; set; }

    public virtual ICollection<PromotionCondition> PromotionConditions { get; set; } = new List<PromotionCondition>();
}
