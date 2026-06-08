using FlightKS.Enums;

namespace FlightKS.Models.Entities;

public class Ticket
{
    public Guid Id { get; set; }

    public Guid BookingId { get; set; }
    public Booking Booking { get; set; } = null!;

    public Guid PassengerId { get; set; }
    public Passenger Passenger { get; set; } = null!;

    public Guid FlightScheduleId { get; set; }
    public FlightSchedule FlightSchedule { get; set; } = null!;

    public Guid? FlightSeatId { get; set; }
    public FlightSeat? FlightSeat { get; set; }

    public required string TicketNumber { get; set; }
    public TicketStatus TicketStatus { get; set; } = TicketStatus.Issued;
    public decimal Price { get; set; }
    public DateTime IssuedAt { get; set; } = DateTime.UtcNow;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
