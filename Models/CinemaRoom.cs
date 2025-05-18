using System;
using System.Collections.Generic;

namespace MovieTheater.Models;

public partial class CinemaRoom
{
    public int CinemaRoomId { get; set; }

    public string? CinemaRoomName { get; set; }

    public int? SeatQuantity { get; set; }

    public virtual ICollection<Seat> Seats { get; set; } = new List<Seat>();
}
