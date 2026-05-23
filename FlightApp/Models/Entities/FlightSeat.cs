using FlightKS.Enums;

namespace FlightKS.Models.Entities;

public class FlightSeat
{
    public Guid Id { get; set; }

    public Guid FlightScheduleId { get; set; }
    public FlightSchedule FlightSchedule { get; set; } = null!;

    public Guid SeatId { get; set; }
    public Seat Seat { get; set; } = null!;

    public FlightSeatStatus Status { get; set; } = FlightSeatStatus.Available;
    public decimal Price { get; set; }
    public DateTime? ReservedUntil { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public Ticket? Ticket { get; set; }
}
