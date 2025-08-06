namespace MovieTheater.Models;

public partial class Ticket
{
    public int TicketId { get; set; }

    public decimal? Price { get; set; }

    public int? TicketType { get; set; }
}
