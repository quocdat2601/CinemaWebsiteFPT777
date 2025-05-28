using System;
using System.Collections.Generic;

namespace MovieTheater.Models;

public partial class Schedule
{
    public int ScheduleId { get; set; }

    public string? ScheduleTime { get; set; }

    public virtual ICollection<ScheduleSeat> ScheduleSeats { get; set; } = new List<ScheduleSeat>();

    public virtual ICollection<Movie> Movies { get; set; } = new List<Movie>();
}
