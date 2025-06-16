namespace MovieTheater.Models;

public partial class Rank
{
    public int RankId { get; set; }

    public string? RankName { get; set; }

    public decimal? DiscountPercentage { get; set; }

    public int? RequiredPoints { get; set; }

    public virtual ICollection<Account> Accounts { get; set; } = new List<Account>();
}
