using System;
using System.Collections.Generic;

namespace MovieTheater.Models;

public partial class ScheduleSeat
{
    public int ScheduleSeatId { get; set; }

    public int? MovieShowId { get; set; }

    public string? InvoiceId { get; set; }

    public int? SeatId { get; set; }

    public int? SeatStatusId { get; set; }

    public DateTime? HoldUntil { get; set; }

    public string? HoldBy { get; set; }

    public virtual Invoice? Invoice { get; set; }

    public virtual MovieShow? MovieShow { get; set; }

    public virtual Seat? Seat { get; set; }

    public virtual SeatStatus? SeatStatus { get; set; }
}
