namespace MovieTheater.Models;

public partial class Seat
{
    public int SeatId { get; set; }

    public int? CinemaRoomId { get; set; }

    public string? SeatColumn { get; set; }

    public int? SeatRow { get; set; }

    public int? SeatStatusId { get; set; }

    public int? SeatTypeId { get; set; }

    public string? SeatName { get; set; }

    public virtual CinemaRoom? CinemaRoom { get; set; }

    public virtual CoupleSeat? CoupleSeatFirstSeat { get; set; }

    public virtual CoupleSeat? CoupleSeatSecondSeat { get; set; }

    public virtual ICollection<ScheduleSeat> ScheduleSeats { get; set; } = new List<ScheduleSeat>();

    public virtual SeatStatus? SeatStatus { get; set; }

    public virtual SeatType? SeatType { get; set; }
}
