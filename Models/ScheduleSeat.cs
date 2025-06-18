using System;
using System.Collections.Generic;

namespace MovieTheater.Models;

public partial class ScheduleSeat
{
    public int MovieShowId { get; set; }

    public string? InvoiceId { get; set; }

    public int SeatId { get; set; }

    public int? SeatStatusId { get; set; }

    public virtual Invoice? Invoice { get; set; }

    public virtual MovieShow MovieShow { get; set; } = null!;

    public virtual Seat Seat { get; set; } = null!;

    public virtual SeatStatus? SeatStatus { get; set; }
}
