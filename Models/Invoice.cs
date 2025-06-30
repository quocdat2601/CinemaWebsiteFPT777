using System;
using System.Collections.Generic;

namespace MovieTheater.Models;

public partial class Invoice
{
    public string InvoiceId { get; set; } = null!;

    public int? AddScore { get; set; }

    public DateTime? BookingDate { get; set; }

    public int? Status { get; set; }

    public int? RoleId { get; set; }

    public decimal? TotalMoney { get; set; }

    public int? UseScore { get; set; }

    public string? Seat { get; set; }

    public string? AccountId { get; set; }

    public int? MovieShowId { get; set; }

    public virtual Account? Account { get; set; }

    public virtual MovieShow? MovieShow { get; set; }

    public virtual ICollection<ScheduleSeat> ScheduleSeats { get; set; } = new List<ScheduleSeat>();
}
