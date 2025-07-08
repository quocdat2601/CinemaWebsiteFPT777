using System;
using System.Collections.Generic;

namespace MovieTheater.Models;
public enum InvoiceStatus
{
    Incomplete = 0,
    Completed = 1
}
public partial class Invoice
{
    public string InvoiceId { get; set; } = null!;

    public int? AddScore { get; set; }

    public DateTime? BookingDate { get; set; }

    public InvoiceStatus? Status { get; set; }

    public decimal? TotalMoney { get; set; }

    public int? UseScore { get; set; }

    public string? Seat { get; set; }

    public string? AccountId { get; set; }

    public int? MovieShowId { get; set; }

    public int? PromotionDiscount { get; set; }

    public string? VoucherId { get; set; }

    public decimal? RankDiscountPercentage { get; set; }

    public string? Seat_IDs { get; set; } // Comma-separated seat IDs for this invoice

    public virtual Account? Account { get; set; }

    public virtual MovieShow? MovieShow { get; set; }

    public virtual ICollection<ScheduleSeat> ScheduleSeats { get; set; } = new List<ScheduleSeat>();

    public virtual Voucher? Voucher { get; set; }
}
