using System;
using System.Collections.Generic;

namespace MovieTheater.Models;

public partial class ScheduleSeat
{
    public string ScheduleSeatId { get; set; } = null!;

    public string? MovieId { get; set; }

    public int? ScheduleId { get; set; }

    public int? SeatId { get; set; }

    public string? SeatColumn { get; set; }

    public int? SeatRow { get; set; }

    public int? SeatStatus { get; set; }

    public int? SeatType { get; set; }
}
