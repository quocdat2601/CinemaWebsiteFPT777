using System;
using System.Collections.Generic;

namespace MovieTheater.Models;

public partial class Seat
{
    public int SeatId { get; set; }

    public int? CinemaRoomId { get; set; }

    public string? SeatColumn { get; set; }

    public int? SeatRow { get; set; }

    public int? SeatStatus { get; set; }

    public int? SeatType { get; set; }

    public virtual CinemaRoom? CinemaRoom { get; set; }
}
