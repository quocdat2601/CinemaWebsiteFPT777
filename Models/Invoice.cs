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

    public string? SeatIds { get; set; }

    public string? AccountId { get; set; }

    public int? MovieShowId { get; set; }

    public string? PromotionDiscount { get; set; }

    public string? VoucherId { get; set; }

    public bool Cancel { get; set; }

    public DateTime? CancelDate { get; set; }

    public string? CancelBy { get; set; }

    public decimal? RankDiscountPercentage { get; set; }

    public virtual Account? Account { get; set; }

    public virtual ICollection<FoodInvoice> FoodInvoices { get; set; } = new List<FoodInvoice>();

    public virtual MovieShow? MovieShow { get; set; }

    public virtual ICollection<ScheduleSeat> ScheduleSeats { get; set; } = new List<ScheduleSeat>();

    public virtual Voucher? Voucher { get; set; }
}
