using System;
using System.Collections.Generic;

namespace MovieTheater.Models;

public partial class Invoice
{
    public string InvoiceId { get; set; } = null!;

    public int? AddScore { get; set; }

    public DateTime? BookingDate { get; set; }

    public string? MovieName { get; set; }

    public DateTime? ScheduleShow { get; set; }

    public string? ScheduleShowTime { get; set; }

    public int? Status { get; set; }

    public int? TotalMoney { get; set; }

    public int? UseScore { get; set; }

    public string? Seat { get; set; }

    public string? AccountId { get; set; }

    public virtual Account? Account { get; set; }
}
