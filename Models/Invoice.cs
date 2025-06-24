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

    public string? MovieName { get; set; }

    public DateTime? ScheduleShow { get; set; }

    public string? ScheduleShowTime { get; set; }

    public InvoiceStatus? Status { get; set; }

    public int? RoleId { get; set; }

    public decimal? TotalMoney { get; set; }

    public int? UseScore { get; set; }

    public string? Seat { get; set; }

    public string? AccountId { get; set; }

    public virtual Account? Account { get; set; }

    public virtual ICollection<ScheduleSeat> ScheduleSeats { get; set; } = new List<ScheduleSeat>();
}
