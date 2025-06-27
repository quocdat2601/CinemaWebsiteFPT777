using System;
using System.Collections.Generic;

namespace MovieTheater.Models;

public partial class SeatStatus
{
    public int SeatStatusId { get; set; }

    public string? StatusName { get; set; }

    public virtual ICollection<ScheduleSeat> ScheduleSeats { get; set; } = new List<ScheduleSeat>();

    public virtual ICollection<Seat> Seats { get; set; } = new List<Seat>();
}
